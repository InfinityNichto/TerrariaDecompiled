using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text;

internal sealed class TranscodingStream : Stream
{
	private readonly Encoding _innerEncoding;

	private readonly Encoding _thisEncoding;

	private Stream _innerStream;

	private readonly bool _leaveOpen;

	private Encoder _innerEncoder;

	private Decoder _thisDecoder;

	private Encoder _thisEncoder;

	private Decoder _innerDecoder;

	private int _readCharBufferMaxSize;

	private byte[] _readBuffer;

	private int _readBufferOffset;

	private int _readBufferCount;

	public override bool CanRead => _innerStream?.CanRead ?? false;

	public override bool CanSeek => false;

	public override bool CanWrite => _innerStream?.CanWrite ?? false;

	public override long Length
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_UnseekableStream);
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException(SR.NotSupported_UnseekableStream);
		}
		set
		{
			ThrowHelper.ThrowNotSupportedException_UnseekableStream();
		}
	}

	internal TranscodingStream(Stream innerStream, Encoding innerEncoding, Encoding thisEncoding, bool leaveOpen)
	{
		_innerStream = innerStream;
		_leaveOpen = leaveOpen;
		_innerEncoding = innerEncoding;
		_thisEncoding = thisEncoding;
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	protected override void Dispose(bool disposing)
	{
		if (_innerStream != null)
		{
			ArraySegment<byte> arraySegment = FinalFlushWriteBuffers();
			if (arraySegment.Count != 0)
			{
				_innerStream.Write(arraySegment);
			}
			Stream innerStream = _innerStream;
			_innerStream = null;
			if (!_leaveOpen)
			{
				innerStream.Dispose();
			}
		}
	}

	public override ValueTask DisposeAsync()
	{
		if (_innerStream == null)
		{
			return default(ValueTask);
		}
		ArraySegment<byte> pendingData2 = FinalFlushWriteBuffers();
		if (pendingData2.Count == 0)
		{
			Stream innerStream = _innerStream;
			_innerStream = null;
			if (!_leaveOpen)
			{
				return innerStream.DisposeAsync();
			}
			return default(ValueTask);
		}
		return DisposeAsyncCore(pendingData2);
		async ValueTask DisposeAsyncCore(ArraySegment<byte> pendingData)
		{
			Stream innerStream2 = _innerStream;
			_innerStream = null;
			await innerStream2.WriteAsync(pendingData.AsMemory()).ConfigureAwait(continueOnCapturedContext: false);
			if (!_leaveOpen)
			{
				await innerStream2.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return TaskToApm.End<int>(asyncResult);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		TaskToApm.End(asyncResult);
	}

	[MemberNotNull(new string[] { "_innerDecoder", "_thisEncoder", "_readBuffer" })]
	private void EnsurePreReadConditions()
	{
		ThrowIfDisposed();
		if (_innerDecoder == null)
		{
			InitializeReadDataStructures();
		}
		void InitializeReadDataStructures()
		{
			if (!CanRead)
			{
				ThrowHelper.ThrowNotSupportedException_UnreadableStream();
			}
			_innerDecoder = _innerEncoding.GetDecoder();
			_thisEncoder = _thisEncoding.GetEncoder();
			_readCharBufferMaxSize = _innerEncoding.GetMaxCharCount(4096);
			_readBuffer = GC.AllocateUninitializedArray<byte>(_thisEncoding.GetMaxByteCount(_readCharBufferMaxSize));
		}
	}

	[MemberNotNull(new string[] { "_thisDecoder", "_innerEncoder" })]
	private void EnsurePreWriteConditions()
	{
		ThrowIfDisposed();
		if (_innerEncoder == null)
		{
			InitializeReadDataStructures();
		}
		void InitializeReadDataStructures()
		{
			if (!CanWrite)
			{
				ThrowHelper.ThrowNotSupportedException_UnwritableStream();
			}
			_innerEncoder = _innerEncoding.GetEncoder();
			_thisDecoder = _thisEncoding.GetDecoder();
		}
	}

	private ArraySegment<byte> FinalFlushWriteBuffers()
	{
		if (_thisDecoder == null || _innerEncoder == null)
		{
			return default(ArraySegment<byte>);
		}
		char[] chars = Array.Empty<char>();
		int num = _thisDecoder.GetCharCount(Array.Empty<byte>(), 0, 0, flush: true);
		if (num > 0)
		{
			chars = new char[num];
			num = _thisDecoder.GetChars(Array.Empty<byte>(), 0, 0, chars, 0, flush: true);
		}
		byte[] array = Array.Empty<byte>();
		int num2 = _innerEncoder.GetByteCount(chars, 0, num, flush: true);
		if (num2 > 0)
		{
			array = new byte[num2];
			num2 = _innerEncoder.GetBytes(chars, 0, num, array, 0, flush: true);
		}
		return new ArraySegment<byte>(array, 0, num2);
	}

	public override void Flush()
	{
		ThrowIfDisposed();
		_innerStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		return _innerStream.FlushAsync(cancellationToken);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return Read(new Span<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> buffer)
	{
		EnsurePreReadConditions();
		if (_readBufferCount == 0)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(4096);
			char[] array2 = ArrayPool<char>.Shared.Rent(_readCharBufferMaxSize);
			try
			{
				bool flag;
				int bytes;
				do
				{
					int num = _innerStream.Read(array, 0, 4096);
					flag = num == 0;
					int chars = _innerDecoder.GetChars(array, 0, num, array2, 0, flag);
					bytes = _thisEncoder.GetBytes(array2, 0, chars, _readBuffer, 0, flag);
				}
				while (!flag && bytes == 0);
				_readBufferOffset = 0;
				_readBufferCount = bytes;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
				ArrayPool<char>.Shared.Return(array2);
			}
		}
		int num2 = Math.Min(_readBufferCount, buffer.Length);
		_readBuffer.AsSpan(_readBufferOffset, num2).CopyTo(buffer);
		_readBufferOffset += num2;
		_readBufferCount -= num2;
		return num2;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		EnsurePreReadConditions();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		return ReadAsyncCore(buffer, cancellationToken);
		async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			if (_readBufferCount == 0)
			{
				byte[] rentedBytes = ArrayPool<byte>.Shared.Rent(4096);
				char[] rentedChars = ArrayPool<char>.Shared.Rent(_readCharBufferMaxSize);
				try
				{
					bool flag;
					int bytes;
					do
					{
						int num = await _innerStream.ReadAsync(rentedBytes.AsMemory(0, 4096), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						flag = num == 0;
						int chars = _innerDecoder.GetChars(rentedBytes, 0, num, rentedChars, 0, flag);
						bytes = _thisEncoder.GetBytes(rentedChars, 0, chars, _readBuffer, 0, flag);
					}
					while (!flag && bytes == 0);
					_readBufferOffset = 0;
					_readBufferCount = bytes;
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(rentedBytes);
					ArrayPool<char>.Shared.Return(rentedChars);
				}
			}
			int num2 = Math.Min(_readBufferCount, buffer.Length);
			_readBuffer.AsSpan(_readBufferOffset, num2).CopyTo(buffer.Span);
			_readBufferOffset += num2;
			_readBufferCount -= num2;
			return num2;
		}
	}

	public unsafe override int ReadByte()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out byte result);
		if (Read(new Span<byte>(&result, 1)) == 0)
		{
			return -1;
		}
		return result;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(SR.NotSupported_UnseekableStream);
	}

	public override void SetLength(long value)
	{
		ThrowHelper.ThrowNotSupportedException_UnseekableStream();
	}

	[StackTraceHidden]
	private void ThrowIfDisposed()
	{
		if (_innerStream == null)
		{
			ThrowObjectDisposedException();
		}
	}

	[DoesNotReturn]
	[StackTraceHidden]
	private void ThrowObjectDisposedException()
	{
		ThrowHelper.ThrowObjectDisposedException_StreamClosed(GetType().Name);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		Write(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		EnsurePreWriteConditions();
		if (buffer.IsEmpty)
		{
			return;
		}
		int minimumLength = Math.Clamp(buffer.Length, 4096, 1048576);
		char[] array = ArrayPool<char>.Shared.Rent(minimumLength);
		byte[] array2 = ArrayPool<byte>.Shared.Rent(minimumLength);
		try
		{
			bool completed;
			do
			{
				_thisDecoder.Convert(buffer, array, flush: false, out var bytesUsed, out var charsUsed, out completed);
				buffer = buffer.Slice(bytesUsed);
				Span<char> span = array.AsSpan(0, charsUsed);
				bool completed2;
				do
				{
					_innerEncoder.Convert(span, array2, flush: false, out var charsUsed2, out var bytesUsed2, out completed2);
					span = span.Slice(charsUsed2);
					_innerStream.Write(array2, 0, bytesUsed2);
				}
				while (!completed2);
			}
			while (!completed);
		}
		finally
		{
			ArrayPool<char>.Shared.Return(array);
			ArrayPool<byte>.Shared.Return(array2);
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		EnsurePreWriteConditions();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		if (buffer.IsEmpty)
		{
			return ValueTask.CompletedTask;
		}
		return WriteAsyncCore(buffer, cancellationToken);
		async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> remainingOuterEncodedBytes, CancellationToken cancellationToken)
		{
			int minimumLength = Math.Clamp(remainingOuterEncodedBytes.Length, 4096, 1048576);
			char[] scratchChars = ArrayPool<char>.Shared.Rent(minimumLength);
			byte[] scratchBytes = ArrayPool<byte>.Shared.Rent(minimumLength);
			try
			{
				bool decoderFinished;
				do
				{
					_thisDecoder.Convert(remainingOuterEncodedBytes.Span, scratchChars, flush: false, out var bytesUsed, out var charsUsed, out decoderFinished);
					remainingOuterEncodedBytes = remainingOuterEncodedBytes.Slice(bytesUsed);
					ArraySegment<char> decodedChars = new ArraySegment<char>(scratchChars, 0, charsUsed);
					bool encoderFinished;
					do
					{
						_innerEncoder.Convert(decodedChars, scratchBytes, flush: false, out var charsUsed2, out var bytesUsed2, out encoderFinished);
						decodedChars = decodedChars.Slice(charsUsed2);
						await _innerStream.WriteAsync(new ReadOnlyMemory<byte>(scratchBytes, 0, bytesUsed2), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					while (!encoderFinished);
				}
				while (!decoderFinished);
			}
			finally
			{
				ArrayPool<char>.Shared.Return(scratchChars);
				ArrayPool<byte>.Shared.Return(scratchBytes);
			}
		}
	}

	public unsafe override void WriteByte(byte value)
	{
		Write(new ReadOnlySpan<byte>(&value, 1));
	}
}
