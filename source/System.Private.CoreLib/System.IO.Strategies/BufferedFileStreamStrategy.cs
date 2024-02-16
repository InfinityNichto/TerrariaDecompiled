using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal sealed class BufferedFileStreamStrategy : FileStreamStrategy
{
	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CReadAsyncSlowPath_003Ed__39 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public PoolingAsyncValueTaskMethodBuilder<int> _003C_003Et__builder;

		public Task semaphoreLockTask;

		public BufferedFileStreamStrategy _003C_003E4__this;

		public Memory<byte> buffer;

		public CancellationToken cancellationToken;

		private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _003C_003Eu__1;

		private int _003CbytesAlreadySatisfied_003E5__2;

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _003C_003Eu__2;

		private ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter _003C_003Eu__3;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			BufferedFileStreamStrategy bufferedFileStreamStrategy = _003C_003E4__this;
			int result;
			try
			{
				ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
				if (num != 0)
				{
					if ((uint)(num - 1) <= 2u)
					{
						goto IL_007d;
					}
					awaiter = semaphoreLockTask.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
				}
				else
				{
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
					num = (_003C_003E1__state = -1);
				}
				awaiter.GetResult();
				goto IL_007d;
				IL_007d:
				try
				{
					ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter4;
					ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter awaiter3;
					ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter awaiter2;
					int result2;
					int num2;
					switch (num)
					{
					default:
						num2 = 0;
						_003CbytesAlreadySatisfied_003E5__2 = 0;
						if (bufferedFileStreamStrategy._readLen - bufferedFileStreamStrategy._readPos <= 0)
						{
							goto IL_0146;
						}
						num2 = Math.Min(buffer.Length, bufferedFileStreamStrategy._readLen - bufferedFileStreamStrategy._readPos);
						if (num2 > 0)
						{
							bufferedFileStreamStrategy._buffer.AsSpan(bufferedFileStreamStrategy._readPos, num2).CopyTo(buffer.Span);
							bufferedFileStreamStrategy._readPos += num2;
						}
						if (num2 != buffer.Length)
						{
							if (num2 > 0)
							{
								buffer = buffer.Slice(num2);
								_003CbytesAlreadySatisfied_003E5__2 += num2;
							}
							goto IL_0146;
						}
						result = num2;
						goto end_IL_007d;
					case 1:
						awaiter4 = _003C_003Eu__2;
						_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
						num = (_003C_003E1__state = -1);
						goto IL_01e6;
					case 2:
						awaiter3 = _003C_003Eu__3;
						_003C_003Eu__3 = default(ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter);
						num = (_003C_003E1__state = -1);
						goto IL_0280;
					case 3:
						{
							awaiter2 = _003C_003Eu__3;
							_003C_003Eu__3 = default(ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							break;
						}
						IL_0280:
						result2 = awaiter3.GetResult();
						result = result2 + _003CbytesAlreadySatisfied_003E5__2;
						goto end_IL_007d;
						IL_0146:
						bufferedFileStreamStrategy._readPos = (bufferedFileStreamStrategy._readLen = 0);
						if (bufferedFileStreamStrategy._writePos > 0)
						{
							awaiter4 = bufferedFileStreamStrategy._strategy.WriteAsync(new ReadOnlyMemory<byte>(bufferedFileStreamStrategy._buffer, 0, bufferedFileStreamStrategy._writePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
							if (!awaiter4.IsCompleted)
							{
								num = (_003C_003E1__state = 1);
								_003C_003Eu__2 = awaiter4;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter4, ref this);
								return;
							}
							goto IL_01e6;
						}
						goto IL_01f4;
						IL_01e6:
						awaiter4.GetResult();
						bufferedFileStreamStrategy._writePos = 0;
						goto IL_01f4;
						IL_01f4:
						if (buffer.Length >= bufferedFileStreamStrategy._bufferSize)
						{
							awaiter3 = bufferedFileStreamStrategy._strategy.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
							if (!awaiter3.IsCompleted)
							{
								num = (_003C_003E1__state = 2);
								_003C_003Eu__3 = awaiter3;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter3, ref this);
								return;
							}
							goto IL_0280;
						}
						bufferedFileStreamStrategy.EnsureBufferAllocated();
						awaiter2 = bufferedFileStreamStrategy._strategy.ReadAsync(new Memory<byte>(bufferedFileStreamStrategy._buffer, 0, bufferedFileStreamStrategy._bufferSize), cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
						if (!awaiter2.IsCompleted)
						{
							num = (_003C_003E1__state = 3);
							_003C_003Eu__3 = awaiter2;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
							return;
						}
						break;
					}
					int result3 = awaiter2.GetResult();
					bufferedFileStreamStrategy._readLen = result3;
					num2 = Math.Min(bufferedFileStreamStrategy._readLen, buffer.Length);
					bufferedFileStreamStrategy._buffer.AsSpan(0, num2).CopyTo(buffer.Span);
					bufferedFileStreamStrategy._readPos += num2;
					result = _003CbytesAlreadySatisfied_003E5__2 + num2;
					end_IL_007d:;
				}
				finally
				{
					if (num < 0)
					{
						bufferedFileStreamStrategy._asyncActiveSemaphore.Release();
					}
				}
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003C_003Et__builder.SetResult(result);
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CWriteAsyncSlowPath_003Ed__50 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public PoolingAsyncValueTaskMethodBuilder _003C_003Et__builder;

		public Task semaphoreLockTask;

		public BufferedFileStreamStrategy _003C_003E4__this;

		public ReadOnlyMemory<byte> source;

		public CancellationToken cancellationToken;

		private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _003C_003Eu__1;

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _003C_003Eu__2;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			BufferedFileStreamStrategy bufferedFileStreamStrategy = _003C_003E4__this;
			try
			{
				ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
				if (num != 0)
				{
					if ((uint)(num - 1) <= 1u)
					{
						goto IL_007c;
					}
					awaiter = semaphoreLockTask.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
				}
				else
				{
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
					num = (_003C_003E1__state = -1);
				}
				awaiter.GetResult();
				goto IL_007c;
				IL_007c:
				try
				{
					ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter2;
					if (num == 1)
					{
						awaiter2 = _003C_003Eu__2;
						_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
						num = (_003C_003E1__state = -1);
						goto IL_01e1;
					}
					ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter3;
					if (num == 2)
					{
						awaiter3 = _003C_003Eu__2;
						_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
						num = (_003C_003E1__state = -1);
						goto IL_0278;
					}
					if (bufferedFileStreamStrategy._writePos == 0)
					{
						bufferedFileStreamStrategy.ClearReadBufferBeforeWrite();
					}
					if (bufferedFileStreamStrategy._writePos <= 0)
					{
						goto IL_01ef;
					}
					int num2 = bufferedFileStreamStrategy._bufferSize - bufferedFileStreamStrategy._writePos;
					if (num2 <= 0)
					{
						goto IL_015f;
					}
					ReadOnlySpan<byte> readOnlySpan;
					if (num2 < source.Length)
					{
						readOnlySpan = source.Span;
						readOnlySpan = readOnlySpan.Slice(0, num2);
						readOnlySpan.CopyTo(bufferedFileStreamStrategy._buffer.AsSpan(bufferedFileStreamStrategy._writePos));
						bufferedFileStreamStrategy._writePos += num2;
						source = source.Slice(num2);
						goto IL_015f;
					}
					readOnlySpan = source.Span;
					readOnlySpan.CopyTo(bufferedFileStreamStrategy._buffer.AsSpan(bufferedFileStreamStrategy._writePos));
					bufferedFileStreamStrategy._writePos += source.Length;
					goto end_IL_007c;
					IL_0278:
					awaiter3.GetResult();
					goto end_IL_007c;
					IL_015f:
					awaiter2 = bufferedFileStreamStrategy._strategy.WriteAsync(new ReadOnlyMemory<byte>(bufferedFileStreamStrategy._buffer, 0, bufferedFileStreamStrategy._writePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
					if (!awaiter2.IsCompleted)
					{
						num = (_003C_003E1__state = 1);
						_003C_003Eu__2 = awaiter2;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
						return;
					}
					goto IL_01e1;
					IL_01ef:
					if (source.Length >= bufferedFileStreamStrategy._bufferSize)
					{
						awaiter3 = bufferedFileStreamStrategy._strategy.WriteAsync(source, cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
						if (!awaiter3.IsCompleted)
						{
							num = (_003C_003E1__state = 2);
							_003C_003Eu__2 = awaiter3;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter3, ref this);
							return;
						}
						goto IL_0278;
					}
					if (source.Length != 0)
					{
						bufferedFileStreamStrategy.EnsureBufferAllocated();
						readOnlySpan = source.Span;
						readOnlySpan.CopyTo(bufferedFileStreamStrategy._buffer.AsSpan(bufferedFileStreamStrategy._writePos));
						bufferedFileStreamStrategy._writePos = source.Length;
					}
					goto end_IL_007c;
					IL_01e1:
					awaiter2.GetResult();
					bufferedFileStreamStrategy._writePos = 0;
					goto IL_01ef;
					end_IL_007c:;
				}
				finally
				{
					if (num < 0)
					{
						bufferedFileStreamStrategy._asyncActiveSemaphore.Release();
					}
				}
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003C_003Et__builder.SetResult();
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	private readonly FileStreamStrategy _strategy;

	private readonly int _bufferSize;

	private byte[] _buffer;

	private int _writePos;

	private int _readPos;

	private int _readLen;

	private Task<int> _lastSyncCompletedReadTask;

	public override bool CanRead => _strategy.CanRead;

	public override bool CanWrite => _strategy.CanWrite;

	public override bool CanSeek => _strategy.CanSeek;

	public override long Length
	{
		get
		{
			long num = _strategy.Length;
			if (_writePos > 0 && _strategy.Position + _writePos > num)
			{
				num = _writePos + _strategy.Position;
			}
			return num;
		}
	}

	public override long Position
	{
		get
		{
			return _strategy.Position + _readPos - _readLen + _writePos;
		}
		set
		{
			if (_writePos > 0)
			{
				FlushWrite();
			}
			_readPos = 0;
			_readLen = 0;
			_strategy.Position = value;
		}
	}

	internal override bool IsAsync => _strategy.IsAsync;

	internal override bool IsClosed => _strategy.IsClosed;

	internal override string Name => _strategy.Name;

	internal override SafeFileHandle SafeFileHandle
	{
		get
		{
			Flush();
			return _strategy.SafeFileHandle;
		}
	}

	internal BufferedFileStreamStrategy(FileStreamStrategy strategy, int bufferSize)
	{
		_strategy = strategy;
		_bufferSize = bufferSize;
	}

	~BufferedFileStreamStrategy()
	{
		try
		{
			Dispose(disposing: true);
		}
		catch (Exception e) when (FileStreamHelpers.IsIoRelatedException(e))
		{
		}
	}

	public override async ValueTask DisposeAsync()
	{
		_ = 1;
		try
		{
			if (!_strategy.IsClosed)
			{
				try
				{
					await FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					await _strategy.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		finally
		{
			_writePos = 0;
		}
	}

	internal override void DisposeInternal(bool disposing)
	{
		Dispose(disposing);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && !_strategy.IsClosed)
			{
				try
				{
					Flush();
					return;
				}
				finally
				{
					_strategy.Dispose();
				}
			}
		}
		finally
		{
			base.Dispose(disposing);
			_writePos = 0;
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return ReadSpan(new Span<byte>(buffer, offset, count), new ArraySegment<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> destination)
	{
		EnsureNotClosed();
		return ReadSpan(destination, default(ArraySegment<byte>));
	}

	private int ReadSpan(Span<byte> destination, ArraySegment<byte> arraySegment)
	{
		bool flag = false;
		int num = _readLen - _readPos;
		if (num == 0)
		{
			EnsureCanRead();
			if (_writePos > 0)
			{
				FlushWrite();
			}
			if (!_strategy.CanSeek || destination.Length >= _bufferSize)
			{
				num = ((arraySegment.Array != null) ? _strategy.Read(arraySegment.Array, arraySegment.Offset, arraySegment.Count) : _strategy.Read(destination));
				_readPos = 0;
				_readLen = 0;
				return num;
			}
			EnsureBufferAllocated();
			num = _strategy.Read(_buffer, 0, _bufferSize);
			if (num == 0)
			{
				return 0;
			}
			flag = num < _bufferSize;
			_readPos = 0;
			_readLen = num;
		}
		if (num > destination.Length)
		{
			num = destination.Length;
		}
		new ReadOnlySpan<byte>(_buffer, _readPos, num).CopyTo(destination);
		_readPos += num;
		if (_strategy.CanSeek && num < destination.Length && !flag)
		{
			int num2 = ((arraySegment.Array != null) ? _strategy.Read(arraySegment.Array, arraySegment.Offset + num, arraySegment.Count - num) : _strategy.Read(destination.Slice(num)));
			num += num2;
			_readPos = 0;
			_readLen = 0;
		}
		return num;
	}

	public override int ReadByte()
	{
		if (_readPos == _readLen)
		{
			return ReadByteSlow();
		}
		return _buffer[_readPos++];
	}

	private int ReadByteSlow()
	{
		EnsureNotClosed();
		EnsureCanRead();
		if (_writePos > 0)
		{
			FlushWrite();
		}
		EnsureBufferAllocated();
		_readLen = _strategy.Read(_buffer, 0, _bufferSize);
		_readPos = 0;
		if (_readLen == 0)
		{
			return -1;
		}
		return _buffer[_readPos++];
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ValueTask<int> valueTask = ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
		if (!valueTask.IsCompletedSuccessfully)
		{
			return valueTask.AsTask();
		}
		return LastSyncCompletedReadTask(valueTask.Result);
		Task<int> LastSyncCompletedReadTask(int val)
		{
			Task<int> lastSyncCompletedReadTask = _lastSyncCompletedReadTask;
			if (lastSyncCompletedReadTask != null && lastSyncCompletedReadTask.Result == val)
			{
				return lastSyncCompletedReadTask;
			}
			return _lastSyncCompletedReadTask = Task.FromResult(val);
		}
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		EnsureCanRead();
		if (!_strategy.CanSeek)
		{
			return ReadFromNonSeekableAsync(buffer, cancellationToken);
		}
		SemaphoreSlim semaphoreSlim = EnsureAsyncActiveSemaphoreInitialized();
		Task task = semaphoreSlim.WaitAsync(cancellationToken);
		if (task.IsCompletedSuccessfully && _writePos == 0)
		{
			bool flag = true;
			try
			{
				if (_readLen == _readPos && buffer.Length >= _bufferSize)
				{
					return _strategy.ReadAsync(buffer, cancellationToken);
				}
				if (_readLen - _readPos >= buffer.Length)
				{
					_buffer.AsSpan(_readPos, buffer.Length).CopyTo(buffer.Span);
					_readPos += buffer.Length;
					return new ValueTask<int>(buffer.Length);
				}
				flag = false;
			}
			finally
			{
				if (flag)
				{
					semaphoreSlim.Release();
				}
			}
		}
		return ReadAsyncSlowPath(task, buffer, cancellationToken);
	}

	private async ValueTask<int> ReadFromNonSeekableAsync(Memory<byte> destination, CancellationToken cancellationToken)
	{
		await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (_readPos < _readLen)
			{
				int num = Math.Min(_readLen - _readPos, destination.Length);
				new Span<byte>(_buffer, _readPos, num).CopyTo(destination.Span);
				_readPos += num;
				return num;
			}
			return await _strategy.ReadAsync(destination, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}

	[AsyncStateMachine(typeof(_003CReadAsyncSlowPath_003Ed__39))]
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
	private ValueTask<int> ReadAsyncSlowPath(Task semaphoreLockTask, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _003CReadAsyncSlowPath_003Ed__39 stateMachine);
		stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder<int>.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine.semaphoreLockTask = semaphoreLockTask;
		stateMachine.buffer = buffer;
		stateMachine.cancellationToken = cancellationToken;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return TaskToApm.End<int>(asyncResult);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		WriteSpan(new ReadOnlySpan<byte>(buffer, offset, count), new ArraySegment<byte>(buffer, offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		EnsureNotClosed();
		WriteSpan(buffer, default(ArraySegment<byte>));
	}

	private void WriteSpan(ReadOnlySpan<byte> source, ArraySegment<byte> arraySegment)
	{
		if (_writePos == 0)
		{
			EnsureCanWrite();
			ClearReadBufferBeforeWrite();
		}
		if (_writePos > 0)
		{
			int num = _bufferSize - _writePos;
			if (num > 0)
			{
				if (num >= source.Length)
				{
					source.CopyTo(_buffer.AsSpan(_writePos));
					_writePos += source.Length;
					return;
				}
				source.Slice(0, num).CopyTo(_buffer.AsSpan(_writePos));
				_writePos += num;
				source = source.Slice(num);
				if (arraySegment.Array != null)
				{
					arraySegment = arraySegment.Slice(num);
				}
			}
			FlushWrite();
		}
		if (source.Length >= _bufferSize)
		{
			if (arraySegment.Array != null)
			{
				_strategy.Write(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
			}
			else
			{
				_strategy.Write(source);
			}
		}
		else if (source.Length != 0)
		{
			EnsureBufferAllocated();
			source.CopyTo(_buffer.AsSpan(_writePos));
			_writePos = source.Length;
		}
	}

	public override void WriteByte(byte value)
	{
		if (_writePos > 0 && _writePos < _bufferSize - 1)
		{
			_buffer[_writePos++] = value;
		}
		else
		{
			WriteByteSlow(value);
		}
	}

	private void WriteByteSlow(byte value)
	{
		if (_writePos == 0)
		{
			EnsureNotClosed();
			EnsureCanWrite();
			ClearReadBufferBeforeWrite();
			EnsureBufferAllocated();
		}
		else
		{
			FlushWrite();
		}
		_buffer[_writePos++] = value;
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		EnsureCanWrite();
		if (!_strategy.CanSeek)
		{
			return WriteToNonSeekableAsync(buffer, cancellationToken);
		}
		SemaphoreSlim semaphoreSlim = EnsureAsyncActiveSemaphoreInitialized();
		Task task = semaphoreSlim.WaitAsync(cancellationToken);
		if (task.IsCompletedSuccessfully && _readPos == _readLen)
		{
			bool flag = true;
			try
			{
				if (_writePos == 0 && buffer.Length >= _bufferSize)
				{
					return _strategy.WriteAsync(buffer, cancellationToken);
				}
				if (_bufferSize - _writePos >= buffer.Length)
				{
					EnsureBufferAllocated();
					buffer.Span.CopyTo(_buffer.AsSpan(_writePos));
					_writePos += buffer.Length;
					return default(ValueTask);
				}
				flag = false;
			}
			finally
			{
				if (flag)
				{
					semaphoreSlim.Release();
				}
			}
		}
		return WriteAsyncSlowPath(task, buffer, cancellationToken);
	}

	private async ValueTask WriteToNonSeekableAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
	{
		await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await _strategy.WriteAsync(source, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}

	[AsyncStateMachine(typeof(_003CWriteAsyncSlowPath_003Ed__50))]
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
	private ValueTask WriteAsyncSlowPath(Task semaphoreLockTask, ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _003CWriteAsyncSlowPath_003Ed__50 stateMachine);
		stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine.semaphoreLockTask = semaphoreLockTask;
		stateMachine.source = source;
		stateMachine.cancellationToken = cancellationToken;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		TaskToApm.End(asyncResult);
	}

	public override void SetLength(long value)
	{
		Flush();
		_strategy.SetLength(value);
	}

	public override void Flush()
	{
		Flush(flushToDisk: false);
	}

	internal override void Flush(bool flushToDisk)
	{
		EnsureNotClosed();
		if (_writePos > 0 && _strategy.CanWrite)
		{
			FlushWrite();
		}
		else if (_readPos < _readLen)
		{
			if (_strategy.CanSeek)
			{
				FlushRead();
			}
		}
		else
		{
			_strategy.Flush(flushToDisk);
			_writePos = (_readPos = (_readLen = 0));
		}
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		EnsureNotClosed();
		return FlushAsyncInternal(cancellationToken);
	}

	private async Task FlushAsyncInternal(CancellationToken cancellationToken)
	{
		await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (_writePos > 0)
			{
				await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_writePos = 0;
			}
			else if (_readPos < _readLen && _strategy.CanSeek)
			{
				FlushRead();
			}
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		EnsureNotClosed();
		EnsureCanRead();
		if (!cancellationToken.IsCancellationRequested)
		{
			return CopyToAsyncCore(destination, bufferSize, cancellationToken);
		}
		return Task.FromCanceled<int>(cancellationToken);
	}

	private async Task CopyToAsyncCore(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			int num = _readLen - _readPos;
			if (num > 0)
			{
				await destination.WriteAsync(new ReadOnlyMemory<byte>(_buffer, _readPos, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_readPos = (_readLen = 0);
			}
			else if (_writePos > 0)
			{
				await _strategy.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_writePos = 0;
			}
			await _strategy.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		EnsureNotClosed();
		EnsureCanRead();
		int num = _readLen - _readPos;
		if (num > 0)
		{
			destination.Write(_buffer, _readPos, num);
			_readPos = (_readLen = 0);
		}
		else if (_writePos > 0)
		{
			FlushWrite();
		}
		_strategy.CopyTo(destination, bufferSize);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		EnsureNotClosed();
		EnsureCanSeek();
		if (_writePos > 0)
		{
			FlushWrite();
			return _strategy.Seek(offset, origin);
		}
		if (_readLen - _readPos > 0 && origin == SeekOrigin.Current)
		{
			offset -= _readLen - _readPos;
		}
		long position = Position;
		long num = _strategy.Seek(offset, origin);
		long num2 = num - (position - _readPos);
		if (0 <= num2 && num2 < _readLen)
		{
			_readPos = (int)num2;
			_strategy.Seek(_readLen - _readPos, SeekOrigin.Current);
		}
		else
		{
			_readPos = (_readLen = 0);
		}
		return num;
	}

	internal override void Lock(long position, long length)
	{
		_strategy.Lock(position, length);
	}

	internal override void Unlock(long position, long length)
	{
		_strategy.Unlock(position, length);
	}

	private void FlushRead()
	{
		if (_readPos - _readLen != 0)
		{
			_strategy.Seek(_readPos - _readLen, SeekOrigin.Current);
		}
		_readPos = 0;
		_readLen = 0;
	}

	private void FlushWrite()
	{
		_strategy.Write(_buffer, 0, _writePos);
		_writePos = 0;
	}

	private void ClearReadBufferBeforeWrite()
	{
		if (_readPos == _readLen)
		{
			_readPos = (_readLen = 0);
		}
		else
		{
			FlushRead();
		}
	}

	private void EnsureNotClosed()
	{
		if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
		}
	}

	private void EnsureCanSeek()
	{
		if (!_strategy.CanSeek)
		{
			ThrowHelper.ThrowNotSupportedException_UnseekableStream();
		}
	}

	private void EnsureCanRead()
	{
		if (!_strategy.CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
	}

	private void EnsureCanWrite()
	{
		if (!_strategy.CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
	}

	[MemberNotNull("_buffer")]
	private void EnsureBufferAllocated()
	{
		if (_buffer == null)
		{
			AllocateBuffer();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[MemberNotNull("_buffer")]
	private void AllocateBuffer()
	{
		Interlocked.CompareExchange(ref _buffer, GC.AllocateUninitializedArray<byte>(_bufferSize), null);
	}
}
