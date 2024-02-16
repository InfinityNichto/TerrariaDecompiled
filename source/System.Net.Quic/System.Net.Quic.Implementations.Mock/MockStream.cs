using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock;

internal sealed class MockStream : QuicStreamProvider
{
	internal sealed class StreamState
	{
		public readonly long _streamId;

		public StreamBuffer _outboundStreamBuffer;

		public StreamBuffer _inboundStreamBuffer;

		public long _outboundReadErrorCode;

		public long _inboundReadErrorCode;

		public long _outboundWriteErrorCode;

		public long _inboundWriteErrorCode;

		public TaskCompletionSource _outboundWritesCompletedTcs;

		public TaskCompletionSource _inboundWritesCompletedTcs;

		public StreamState(long streamId, bool bidirectional)
		{
			_streamId = streamId;
			_outboundStreamBuffer = new StreamBuffer();
			_inboundStreamBuffer = (bidirectional ? new StreamBuffer() : null);
			_outboundWritesCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_inboundWritesCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		}
	}

	private bool _disposed;

	private readonly bool _isInitiator;

	private readonly MockConnection _connection;

	private readonly StreamState _streamState;

	private bool _writesCanceled;

	internal override long StreamId
	{
		get
		{
			CheckDisposed();
			return _streamState._streamId;
		}
	}

	private StreamBuffer ReadStreamBuffer
	{
		get
		{
			if (!_isInitiator)
			{
				return _streamState._outboundStreamBuffer;
			}
			return _streamState._inboundStreamBuffer;
		}
	}

	internal override bool CanTimeout => false;

	internal override int ReadTimeout
	{
		get
		{
			throw new InvalidOperationException();
		}
		set
		{
			throw new InvalidOperationException();
		}
	}

	internal override int WriteTimeout
	{
		get
		{
			throw new InvalidOperationException();
		}
		set
		{
			throw new InvalidOperationException();
		}
	}

	internal override bool CanRead
	{
		get
		{
			if (!_disposed)
			{
				return ReadStreamBuffer != null;
			}
			return false;
		}
	}

	internal override bool ReadsCompleted => ReadStreamBuffer?.IsComplete ?? false;

	private StreamBuffer WriteStreamBuffer
	{
		get
		{
			if (!_isInitiator)
			{
				return _streamState._inboundStreamBuffer;
			}
			return _streamState._outboundStreamBuffer;
		}
	}

	internal override bool CanWrite
	{
		get
		{
			if (!_disposed)
			{
				return WriteStreamBuffer != null;
			}
			return false;
		}
	}

	private TaskCompletionSource WritesCompletedTcs
	{
		get
		{
			if (!_isInitiator)
			{
				return _streamState._inboundWritesCompletedTcs;
			}
			return _streamState._outboundWritesCompletedTcs;
		}
	}

	internal MockStream(MockConnection connection, StreamState streamState, bool isInitiator)
	{
		_connection = connection;
		_streamState = streamState;
		_isInitiator = isInitiator;
	}

	internal override int Read(Span<byte> buffer)
	{
		CheckDisposed();
		StreamBuffer readStreamBuffer = ReadStreamBuffer;
		if (readStreamBuffer == null)
		{
			throw new NotSupportedException();
		}
		return readStreamBuffer.Read(buffer);
	}

	internal override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckDisposed();
		StreamBuffer readStreamBuffer = ReadStreamBuffer;
		if (readStreamBuffer == null)
		{
			throw new NotSupportedException();
		}
		int num = await readStreamBuffer.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (num == 0)
		{
			long? connectionError = _connection.ConnectionError;
			if (connectionError.HasValue)
			{
				long valueOrDefault = connectionError.GetValueOrDefault();
				throw new QuicConnectionAbortedException(valueOrDefault);
			}
			long num2 = (_isInitiator ? _streamState._inboundReadErrorCode : _streamState._outboundReadErrorCode);
			if (num2 != 0L)
			{
				throw (num2 == -1) ? ((QuicException)new QuicOperationAbortedException()) : ((QuicException)new QuicStreamAbortedException(num2));
			}
		}
		return num;
	}

	internal override void Write(ReadOnlySpan<byte> buffer)
	{
		CheckDisposed();
		if (Volatile.Read(ref _writesCanceled))
		{
			throw new OperationCanceledException();
		}
		StreamBuffer writeStreamBuffer = WriteStreamBuffer;
		if (writeStreamBuffer == null)
		{
			throw new NotSupportedException();
		}
		writeStreamBuffer.Write(buffer);
	}

	internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAsync(buffer, endStream: false, cancellationToken);
	}

	internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckDisposed();
		if (Volatile.Read(ref _writesCanceled))
		{
			cancellationToken.ThrowIfCancellationRequested();
			throw new OperationCanceledException();
		}
		StreamBuffer streamBuffer = WriteStreamBuffer;
		if (streamBuffer == null)
		{
			throw new NotSupportedException();
		}
		long? connectionError = _connection.ConnectionError;
		if (connectionError.HasValue)
		{
			long valueOrDefault = connectionError.GetValueOrDefault();
			throw new QuicConnectionAbortedException(valueOrDefault);
		}
		long num = (_isInitiator ? _streamState._inboundWriteErrorCode : _streamState._outboundWriteErrorCode);
		if (num != 0L)
		{
			throw new QuicStreamAbortedException(num);
		}
		using (cancellationToken.UnsafeRegister(delegate(object s)
		{
			MockStream mockStream = (MockStream)s;
			Volatile.Write(ref mockStream._writesCanceled, value: true);
		}, this))
		{
			await streamBuffer.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (endStream)
			{
				streamBuffer.EndWrite();
				WritesCompletedTcs.TrySetResult();
			}
		}
	}

	internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default(CancellationToken))
	{
		throw new NotImplementedException();
	}

	internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		throw new NotImplementedException();
	}

	internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default(CancellationToken))
	{
		for (int i = 0; i < buffers.Length; i++)
		{
			await WriteAsync(buffers.Span[i], cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		throw new NotImplementedException();
	}

	internal override void Flush()
	{
		CheckDisposed();
	}

	internal override Task FlushAsync(CancellationToken cancellationToken)
	{
		CheckDisposed();
		return Task.CompletedTask;
	}

	internal override void AbortRead(long errorCode)
	{
		if (_isInitiator)
		{
			_streamState._outboundWriteErrorCode = errorCode;
			_streamState._inboundWritesCompletedTcs.TrySetException(new QuicStreamAbortedException(errorCode));
		}
		else
		{
			_streamState._inboundWriteErrorCode = errorCode;
			_streamState._outboundWritesCompletedTcs.TrySetException(new QuicOperationAbortedException());
		}
		ReadStreamBuffer?.AbortRead();
	}

	internal override void AbortWrite(long errorCode)
	{
		if (_isInitiator)
		{
			_streamState._outboundReadErrorCode = errorCode;
			_streamState._outboundWritesCompletedTcs.TrySetException(new QuicStreamAbortedException(errorCode));
		}
		else
		{
			_streamState._inboundReadErrorCode = errorCode;
			_streamState._inboundWritesCompletedTcs.TrySetException(new QuicOperationAbortedException());
		}
		WriteStreamBuffer?.EndWrite();
	}

	internal override ValueTask ShutdownCompleted(CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckDisposed();
		return default(ValueTask);
	}

	internal override void Shutdown()
	{
		CheckDisposed();
		WriteStreamBuffer?.EndWrite();
		if (_streamState._inboundStreamBuffer == null)
		{
			_connection.LocalStreamLimit.Unidirectional.Decrement();
		}
		else
		{
			_connection.LocalStreamLimit.Bidirectional.Decrement();
		}
		WritesCompletedTcs.TrySetResult();
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("QuicStream");
		}
	}

	public override void Dispose()
	{
		if (!_disposed)
		{
			Shutdown();
			_disposed = true;
		}
	}

	public override ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			Shutdown();
			_disposed = true;
		}
		return default(ValueTask);
	}

	internal override ValueTask WaitForWriteCompletionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckDisposed();
		return new ValueTask(WritesCompletedTcs.Task);
	}
}
