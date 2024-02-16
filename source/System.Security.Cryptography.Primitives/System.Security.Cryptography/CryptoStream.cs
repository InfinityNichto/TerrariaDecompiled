using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography;

public class CryptoStream : Stream, IDisposable
{
	private readonly Stream _stream;

	private readonly ICryptoTransform _transform;

	private byte[] _inputBuffer;

	private int _inputBufferIndex;

	private int _inputBlockSize;

	private byte[] _outputBuffer;

	private int _outputBufferIndex;

	private int _outputBlockSize;

	private bool _canRead;

	private bool _canWrite;

	private bool _finalBlockTransformed;

	private SemaphoreSlim _lazyAsyncActiveSemaphore;

	private readonly bool _leaveOpen;

	public override bool CanRead => _canRead;

	public override bool CanSeek => false;

	public override bool CanWrite => _canWrite;

	public override long Length
	{
		get
		{
			throw new NotSupportedException(System.SR.NotSupported_UnseekableStream);
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException(System.SR.NotSupported_UnseekableStream);
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_UnseekableStream);
		}
	}

	public bool HasFlushedFinalBlock => _finalBlockTransformed;

	[MemberNotNull("_lazyAsyncActiveSemaphore")]
	private SemaphoreSlim AsyncActiveSemaphore
	{
		[MemberNotNull("_lazyAsyncActiveSemaphore")]
		get
		{
			return LazyInitializer.EnsureInitialized(ref _lazyAsyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
		}
	}

	public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
		: this(stream, transform, mode, leaveOpen: false)
	{
	}

	public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode, bool leaveOpen)
	{
		if (transform == null)
		{
			throw new ArgumentNullException("transform");
		}
		_stream = stream;
		_transform = transform;
		_leaveOpen = leaveOpen;
		switch (mode)
		{
		case CryptoStreamMode.Read:
			if (!_stream.CanRead)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Argument_StreamNotReadable, "stream"));
			}
			_canRead = true;
			break;
		case CryptoStreamMode.Write:
			if (!_stream.CanWrite)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Argument_StreamNotWritable, "stream"));
			}
			_canWrite = true;
			break;
		default:
			throw new ArgumentException(System.SR.Argument_InvalidValue, "mode");
		}
		_inputBlockSize = _transform.InputBlockSize;
		_inputBuffer = new byte[_inputBlockSize];
		_outputBlockSize = _transform.OutputBlockSize;
		_outputBuffer = new byte[_outputBlockSize];
	}

	public void FlushFinalBlock()
	{
		FlushFinalBlockAsync(useAsync: false, default(CancellationToken)).AsTask().GetAwaiter().GetResult();
	}

	public ValueTask FlushFinalBlockAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		return FlushFinalBlockAsync(useAsync: true, cancellationToken);
	}

	private async ValueTask FlushFinalBlockAsync(bool useAsync, CancellationToken cancellationToken)
	{
		if (_finalBlockTransformed)
		{
			throw new NotSupportedException(System.SR.Cryptography_CryptoStream_FlushFinalBlockTwice);
		}
		_finalBlockTransformed = true;
		if (_canWrite)
		{
			byte[] array = _transform.TransformFinalBlock(_inputBuffer, 0, _inputBufferIndex);
			if (useAsync)
			{
				await _stream.WriteAsync(new ReadOnlyMemory<byte>(array), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				_stream.Write(array, 0, array.Length);
			}
		}
		if (_stream is CryptoStream cryptoStream)
		{
			if (!cryptoStream.HasFlushedFinalBlock)
			{
				await cryptoStream.FlushFinalBlockAsync(useAsync, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		else if (useAsync)
		{
			await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			_stream.Flush();
		}
		if (_inputBuffer != null)
		{
			Array.Clear(_inputBuffer);
		}
		if (_outputBuffer != null)
		{
			Array.Clear(_outputBuffer);
		}
	}

	public override void Flush()
	{
		if (_canWrite)
		{
			_stream.Flush();
		}
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (GetType() != typeof(CryptoStream))
		{
			return base.FlushAsync(cancellationToken);
		}
		if (!cancellationToken.IsCancellationRequested)
		{
			if (_canWrite)
			{
				return _stream.FlushAsync(cancellationToken);
			}
			return Task.CompletedTask;
		}
		return Task.FromCanceled(cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.NotSupported_UnseekableStream);
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException(System.SR.NotSupported_UnseekableStream);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		CheckReadArguments(buffer, offset, count);
		return ReadAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!CanRead)
		{
			return ValueTask.FromException<int>(new NotSupportedException(System.SR.NotSupported_UnreadableStream));
		}
		return ReadAsyncInternal(buffer, cancellationToken);
	}

	private async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		await AsyncActiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			return await ReadAsyncCore(buffer, cancellationToken, useAsync: true).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_lazyAsyncActiveSemaphore.Release();
		}
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public override int ReadByte()
	{
		if (_outputBufferIndex > 1)
		{
			byte result = _outputBuffer[0];
			Buffer.BlockCopy(_outputBuffer, 1, _outputBuffer, 0, _outputBufferIndex - 1);
			_outputBufferIndex--;
			return result;
		}
		return base.ReadByte();
	}

	public override void WriteByte(byte value)
	{
		if (_inputBufferIndex + 1 < _inputBlockSize)
		{
			_inputBuffer[_inputBufferIndex++] = value;
		}
		else
		{
			base.WriteByte(value);
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		CheckReadArguments(buffer, offset, count);
		return ReadAsyncCore(buffer.AsMemory(offset, count), default(CancellationToken), useAsync: false).GetAwaiter().GetResult();
	}

	private void CheckReadArguments(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.NotSupported_UnreadableStream);
		}
	}

	private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken, bool useAsync)
	{
		while (true)
		{
			if (_outputBufferIndex != 0)
			{
				int num = Math.Min(_outputBufferIndex, buffer.Length);
				if (num != 0)
				{
					new ReadOnlySpan<byte>(_outputBuffer, 0, num).CopyTo(buffer.Span);
					_outputBufferIndex -= num;
					_outputBuffer.AsSpan(num).CopyTo(_outputBuffer);
					CryptographicOperations.ZeroMemory(_outputBuffer.AsSpan(_outputBufferIndex, num));
				}
				return num;
			}
			if (_finalBlockTransformed)
			{
				break;
			}
			int num2 = 0;
			bool flag = false;
			int num3 = buffer.Length / _outputBlockSize;
			if (num3 > 1 && _transform.CanTransformMultipleBlocks)
			{
				int numWholeBlocksInBytes = checked(num3 * _inputBlockSize);
				byte[] tempInputBuffer = ArrayPool<byte>.Shared.Rent(numWholeBlocksInBytes);
				try
				{
					int num4 = ((!useAsync) ? _stream.Read(tempInputBuffer, _inputBufferIndex, numWholeBlocksInBytes - _inputBufferIndex) : (await _stream.ReadAsync(new Memory<byte>(tempInputBuffer, _inputBufferIndex, numWholeBlocksInBytes - _inputBufferIndex), cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
					num2 = num4;
					flag = num2 == 0;
					int num5 = _inputBufferIndex + num2;
					if (num5 >= _inputBlockSize)
					{
						Buffer.BlockCopy(_inputBuffer, 0, tempInputBuffer, 0, _inputBufferIndex);
						CryptographicOperations.ZeroMemory(new Span<byte>(_inputBuffer, 0, _inputBufferIndex));
						num2 += _inputBufferIndex;
						int num6 = num2 / _inputBlockSize;
						int num7 = num6 * _inputBlockSize;
						_inputBufferIndex = num2 - num7;
						if (_inputBufferIndex != 0)
						{
							Buffer.BlockCopy(tempInputBuffer, num7, _inputBuffer, 0, _inputBufferIndex);
						}
						int num8;
						if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment))
						{
							num8 = _transform.TransformBlock(tempInputBuffer, 0, num7, segment.Array, segment.Offset);
						}
						else
						{
							byte[] array = ArrayPool<byte>.Shared.Rent(num6 * _outputBlockSize);
							num8 = num6 * _outputBlockSize;
							try
							{
								num8 = _transform.TransformBlock(tempInputBuffer, 0, num7, array, 0);
								array.AsSpan(0, num8).CopyTo(buffer.Span);
							}
							finally
							{
								CryptographicOperations.ZeroMemory(new Span<byte>(array, 0, num8));
								ArrayPool<byte>.Shared.Return(array);
							}
						}
						if (num8 != 0)
						{
							return num8;
						}
					}
					else
					{
						Buffer.BlockCopy(tempInputBuffer, _inputBufferIndex, _inputBuffer, _inputBufferIndex, num2);
						_inputBufferIndex = num5;
					}
				}
				finally
				{
					CryptographicOperations.ZeroMemory(new Span<byte>(tempInputBuffer, 0, numWholeBlocksInBytes));
					ArrayPool<byte>.Shared.Return(tempInputBuffer);
				}
			}
			if (!flag)
			{
				while (_inputBufferIndex < _inputBlockSize)
				{
					int num9 = ((!useAsync) ? _stream.Read(_inputBuffer, _inputBufferIndex, _inputBlockSize - _inputBufferIndex) : (await _stream.ReadAsync(new Memory<byte>(_inputBuffer, _inputBufferIndex, _inputBlockSize - _inputBufferIndex), cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
					num2 = num9;
					if (num2 <= 0)
					{
						break;
					}
					_inputBufferIndex += num2;
				}
			}
			if (num2 <= 0)
			{
				_outputBuffer = _transform.TransformFinalBlock(_inputBuffer, 0, _inputBufferIndex);
				_outputBufferIndex = _outputBuffer.Length;
				_finalBlockTransformed = true;
			}
			else
			{
				_outputBufferIndex = _transform.TransformBlock(_inputBuffer, 0, _inputBufferIndex, _outputBuffer, 0);
			}
			_inputBufferIndex = 0;
		}
		return 0;
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		CheckWriteArguments(buffer, offset, count);
		return WriteAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!CanWrite)
		{
			return ValueTask.FromException(new NotSupportedException(System.SR.NotSupported_UnwritableStream));
		}
		return WriteAsyncInternal(buffer, cancellationToken);
	}

	private async ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		await AsyncActiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await WriteAsyncCore(buffer, cancellationToken, useAsync: true).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_lazyAsyncActiveSemaphore.Release();
		}
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		CheckWriteArguments(buffer, offset, count);
		WriteAsyncCore(buffer.AsMemory(offset, count), default(CancellationToken), useAsync: false).AsTask().GetAwaiter().GetResult();
	}

	private void CheckWriteArguments(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!CanWrite)
		{
			throw new NotSupportedException(System.SR.NotSupported_UnwritableStream);
		}
	}

	private async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, bool useAsync)
	{
		int bytesToWrite = buffer.Length;
		int currentInputIndex = 0;
		if (_inputBufferIndex > 0)
		{
			if (buffer.Length < _inputBlockSize - _inputBufferIndex)
			{
				buffer.CopyTo(_inputBuffer.AsMemory(_inputBufferIndex));
				_inputBufferIndex += buffer.Length;
				return;
			}
			buffer.Slice(0, _inputBlockSize - _inputBufferIndex).CopyTo(_inputBuffer.AsMemory(_inputBufferIndex));
			currentInputIndex += _inputBlockSize - _inputBufferIndex;
			bytesToWrite -= _inputBlockSize - _inputBufferIndex;
			_inputBufferIndex = _inputBlockSize;
		}
		if (_inputBufferIndex == _inputBlockSize)
		{
			int numOutputBytes2 = _transform.TransformBlock(_inputBuffer, 0, _inputBlockSize, _outputBuffer, 0);
			if (useAsync)
			{
				await _stream.WriteAsync(new ReadOnlyMemory<byte>(_outputBuffer, 0, numOutputBytes2), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				_stream.Write(_outputBuffer, 0, numOutputBytes2);
			}
			_inputBufferIndex = 0;
		}
		while (bytesToWrite > 0)
		{
			if (bytesToWrite >= _inputBlockSize)
			{
				int num = bytesToWrite / _inputBlockSize;
				if (_transform.CanTransformMultipleBlocks && num > 1)
				{
					int numWholeBlocksInBytes = num * _inputBlockSize;
					byte[] tempOutputBuffer = ArrayPool<byte>.Shared.Rent(checked(num * _outputBlockSize));
					int numOutputBytes2 = 0;
					try
					{
						numOutputBytes2 = TransformBlock(_transform, buffer.Slice(currentInputIndex, numWholeBlocksInBytes), tempOutputBuffer, 0);
						if (useAsync)
						{
							await _stream.WriteAsync(new ReadOnlyMemory<byte>(tempOutputBuffer, 0, numOutputBytes2), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						}
						else
						{
							_stream.Write(tempOutputBuffer, 0, numOutputBytes2);
						}
						currentInputIndex += numWholeBlocksInBytes;
						bytesToWrite -= numWholeBlocksInBytes;
						CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes2));
						ArrayPool<byte>.Shared.Return(tempOutputBuffer);
						tempOutputBuffer = null;
					}
					catch
					{
						CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes2));
						throw;
					}
				}
				else
				{
					int numOutputBytes2 = TransformBlock(_transform, buffer.Slice(currentInputIndex, _inputBlockSize), _outputBuffer, 0);
					if (useAsync)
					{
						await _stream.WriteAsync(new ReadOnlyMemory<byte>(_outputBuffer, 0, numOutputBytes2), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						_stream.Write(_outputBuffer, 0, numOutputBytes2);
					}
					currentInputIndex += _inputBlockSize;
					bytesToWrite -= _inputBlockSize;
				}
				continue;
			}
			buffer.Slice(currentInputIndex, bytesToWrite).CopyTo(_inputBuffer);
			_inputBufferIndex += bytesToWrite;
			break;
		}
		unsafe static int TransformBlock(ICryptoTransform transform, ReadOnlyMemory<byte> inputBuffer, byte[] outputBuffer, int outputOffset)
		{
			if (MemoryMarshal.TryGetArray(inputBuffer, out var segment))
			{
				return transform.TransformBlock(segment.Array, segment.Offset, inputBuffer.Length, outputBuffer, outputOffset);
			}
			byte[] array = ArrayPool<byte>.Shared.Rent(inputBuffer.Length);
			int result = 0;
			fixed (byte* ptr = &array[0])
			{
				try
				{
					inputBuffer.CopyTo(array);
					result = transform.TransformBlock(array, 0, inputBuffer.Length, outputBuffer, outputOffset);
				}
				finally
				{
					CryptographicOperations.ZeroMemory(array.AsSpan(0, inputBuffer.Length));
				}
			}
			ArrayPool<byte>.Shared.Return(array);
			array = null;
			return result;
		}
	}

	public unsafe override void CopyTo(Stream destination, int bufferSize)
	{
		CheckCopyToArguments(destination, bufferSize);
		byte[] array = ArrayPool<byte>.Shared.Rent(bufferSize);
		fixed (byte* ptr = &array[0])
		{
			try
			{
				int num;
				do
				{
					num = Read(array, 0, bufferSize);
					destination.Write(array, 0, num);
				}
				while (num > 0);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(array.AsSpan(0, bufferSize));
			}
		}
		ArrayPool<byte>.Shared.Return(array);
		array = null;
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		CheckCopyToArguments(destination, bufferSize);
		return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
	}

	private async Task CopyToAsyncInternal(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
		GCHandle pinHandle = GCHandle.Alloc(rentedBuffer, GCHandleType.Pinned);
		try
		{
			int bytesRead;
			do
			{
				bytesRead = await ReadAsync(rentedBuffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				await destination.WriteAsync(rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			while (bytesRead > 0);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, bufferSize));
			pinHandle.Free();
		}
		ArrayPool<byte>.Shared.Return(rentedBuffer);
	}

	private void CheckCopyToArguments(Stream destination, int bufferSize)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		EnsureNotDisposed(destination, "destination");
		if (!destination.CanWrite)
		{
			throw new NotSupportedException(System.SR.NotSupported_UnwritableStream);
		}
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.NotSupported_UnreadableStream);
		}
	}

	private static void EnsureNotDisposed(Stream stream, string objectName)
	{
		if (!stream.CanRead && !stream.CanWrite)
		{
			throw new ObjectDisposedException(objectName);
		}
	}

	public void Clear()
	{
		Close();
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				if (!_finalBlockTransformed)
				{
					FlushFinalBlock();
				}
				if (!_leaveOpen)
				{
					_stream.Dispose();
				}
			}
		}
		finally
		{
			try
			{
				_finalBlockTransformed = true;
				if (_inputBuffer != null)
				{
					Array.Clear(_inputBuffer);
				}
				if (_outputBuffer != null)
				{
					Array.Clear(_outputBuffer);
				}
				_inputBuffer = null;
				_outputBuffer = null;
				_canRead = false;
				_canWrite = false;
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
	}

	public override ValueTask DisposeAsync()
	{
		if (!(GetType() != typeof(CryptoStream)))
		{
			return DisposeAsyncCore();
		}
		return base.DisposeAsync();
	}

	private async ValueTask DisposeAsyncCore()
	{
		_ = 1;
		try
		{
			if (!_finalBlockTransformed)
			{
				await FlushFinalBlockAsync(useAsync: true, default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (!_leaveOpen)
			{
				await _stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			_finalBlockTransformed = true;
			if (_inputBuffer != null)
			{
				Array.Clear(_inputBuffer);
			}
			if (_outputBuffer != null)
			{
				Array.Clear(_outputBuffer);
			}
			_inputBuffer = null;
			_outputBuffer = null;
			_canRead = false;
			_canWrite = false;
		}
	}
}
