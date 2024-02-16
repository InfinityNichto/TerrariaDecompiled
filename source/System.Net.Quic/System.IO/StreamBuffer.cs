using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.IO;

internal sealed class StreamBuffer : IDisposable
{
	private sealed class ResettableValueTaskSource : IValueTaskSource
	{
		private ManualResetValueTaskSourceCore<bool> _waitSource;

		private CancellationTokenRegistration _waitSourceCancellation;

		private int _hasWaiter;

		ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
		{
			return _waitSource.GetStatus(token);
		}

		void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_waitSource.OnCompleted(continuation, state, token, flags);
		}

		void IValueTaskSource.GetResult(short token)
		{
			_waitSourceCancellation.Dispose();
			_waitSourceCancellation = default(CancellationTokenRegistration);
			_waitSource.GetResult(token);
		}

		public void SignalWaiter()
		{
			if (Interlocked.Exchange(ref _hasWaiter, 0) == 1)
			{
				_waitSource.SetResult(result: true);
			}
		}

		private void CancelWaiter(CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref _hasWaiter, 0) == 1)
			{
				_waitSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(cancellationToken)));
			}
		}

		public void Reset()
		{
			if (_hasWaiter != 0)
			{
				throw new InvalidOperationException("Concurrent use is not supported");
			}
			_waitSource.Reset();
			Volatile.Write(ref _hasWaiter, 1);
		}

		public void Wait()
		{
			_waitSource.RunContinuationsAsynchronously = false;
			new ValueTask(this, _waitSource.Version).AsTask().GetAwaiter().GetResult();
		}

		public ValueTask WaitAsync(CancellationToken cancellationToken)
		{
			_waitSource.RunContinuationsAsynchronously = true;
			_waitSourceCancellation = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken token)
			{
				((ResettableValueTaskSource)s).CancelWaiter(token);
			}, this);
			return new ValueTask(this, _waitSource.Version);
		}
	}

	private MultiArrayBuffer _buffer;

	private readonly int _maxBufferSize;

	private bool _writeEnded;

	private bool _readAborted;

	private readonly ResettableValueTaskSource _readTaskSource;

	private readonly ResettableValueTaskSource _writeTaskSource;

	private object SyncObject => _readTaskSource;

	public bool IsComplete
	{
		get
		{
			lock (SyncObject)
			{
				return _writeEnded && _buffer.IsEmpty;
			}
		}
	}

	public StreamBuffer(int initialBufferSize = 4096, int maxBufferSize = 32768)
	{
		_buffer = new MultiArrayBuffer(initialBufferSize);
		_maxBufferSize = maxBufferSize;
		_readTaskSource = new ResettableValueTaskSource();
		_writeTaskSource = new ResettableValueTaskSource();
	}

	private (bool wait, int bytesWritten) TryWriteToBuffer(ReadOnlySpan<byte> buffer)
	{
		lock (SyncObject)
		{
			if (_writeEnded)
			{
				throw new InvalidOperationException();
			}
			if (_readAborted)
			{
				return (wait: false, bytesWritten: buffer.Length);
			}
			_buffer.EnsureAvailableSpaceUpToLimit(buffer.Length, _maxBufferSize);
			int num = Math.Min(buffer.Length, _buffer.AvailableMemory.Length);
			if (num > 0)
			{
				_buffer.AvailableMemory.CopyFrom(buffer.Slice(0, num));
				_buffer.Commit(num);
				_readTaskSource.SignalWaiter();
			}
			buffer = buffer.Slice(num);
			if (buffer.Length == 0)
			{
				return (wait: false, bytesWritten: num);
			}
			_writeTaskSource.Reset();
			return (wait: true, bytesWritten: num);
		}
	}

	public void Write(ReadOnlySpan<byte> buffer)
	{
		if (buffer.Length == 0)
		{
			return;
		}
		while (true)
		{
			var (flag, start) = TryWriteToBuffer(buffer);
			if (flag)
			{
				buffer = buffer.Slice(start);
				_writeTaskSource.Wait();
				continue;
			}
			break;
		}
	}

	public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (buffer.Length == 0)
		{
			return;
		}
		while (true)
		{
			var (flag, start) = TryWriteToBuffer(buffer.Span);
			if (flag)
			{
				buffer = buffer.Slice(start);
				await _writeTaskSource.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				continue;
			}
			break;
		}
	}

	public void EndWrite()
	{
		lock (SyncObject)
		{
			if (!_writeEnded)
			{
				_writeEnded = true;
				_readTaskSource.SignalWaiter();
			}
		}
	}

	private (bool wait, int bytesRead) TryReadFromBuffer(Span<byte> buffer)
	{
		lock (SyncObject)
		{
			if (_readAborted)
			{
				return (wait: false, bytesRead: 0);
			}
			if (!_buffer.IsEmpty)
			{
				int num = Math.Min(buffer.Length, _buffer.ActiveMemory.Length);
				_buffer.ActiveMemory.Slice(0, num).CopyTo(buffer);
				_buffer.Discard(num);
				_writeTaskSource.SignalWaiter();
				return (wait: false, bytesRead: num);
			}
			if (_writeEnded)
			{
				return (wait: false, bytesRead: 0);
			}
			_readTaskSource.Reset();
			return (wait: true, bytesRead: 0);
		}
	}

	public int Read(Span<byte> buffer)
	{
		if (buffer.Length == 0)
		{
			return 0;
		}
		int result;
		bool flag;
		(flag, result) = TryReadFromBuffer(buffer);
		if (flag)
		{
			_readTaskSource.Wait();
			(flag, result) = TryReadFromBuffer(buffer);
		}
		return result;
	}

	public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (buffer.Length == 0)
		{
			return 0;
		}
		var (flag, result) = TryReadFromBuffer(buffer.Span);
		if (flag)
		{
			await _readTaskSource.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			(bool wait, int bytesRead) tuple2 = TryReadFromBuffer(buffer.Span);
			_ = tuple2.wait;
			result = tuple2.bytesRead;
		}
		return result;
	}

	public void AbortRead()
	{
		lock (SyncObject)
		{
			if (!_readAborted)
			{
				_readAborted = true;
				_buffer.DiscardAll();
				_readTaskSource.SignalWaiter();
				_writeTaskSource.SignalWaiter();
			}
		}
	}

	public void Dispose()
	{
		AbortRead();
		EndWrite();
		lock (SyncObject)
		{
			_buffer.Dispose();
		}
	}
}
