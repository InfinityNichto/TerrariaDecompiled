using System.Buffers;

namespace System.IO.Compression;

internal sealed class Deflater : IDisposable
{
	private readonly ZLibNative.ZLibStreamHandle _zlibStream;

	private MemoryHandle _inputBufferHandle;

	private bool _isDisposed;

	private object SyncLock => this;

	internal Deflater(CompressionLevel compressionLevel, int windowBits)
	{
		ZLibNative.CompressionLevel level;
		int memLevel;
		switch (compressionLevel)
		{
		case CompressionLevel.Optimal:
			level = ZLibNative.CompressionLevel.DefaultCompression;
			memLevel = 8;
			break;
		case CompressionLevel.Fastest:
			level = ZLibNative.CompressionLevel.BestSpeed;
			memLevel = 8;
			break;
		case CompressionLevel.NoCompression:
			level = ZLibNative.CompressionLevel.NoCompression;
			memLevel = 7;
			break;
		case CompressionLevel.SmallestSize:
			level = ZLibNative.CompressionLevel.BestCompression;
			memLevel = 8;
			break;
		default:
			throw new ArgumentOutOfRangeException("compressionLevel");
		}
		ZLibNative.CompressionStrategy strategy = ZLibNative.CompressionStrategy.DefaultStrategy;
		ZLibNative.ErrorCode errorCode;
		try
		{
			errorCode = ZLibNative.CreateZLibStreamForDeflate(out _zlibStream, level, windowBits, memLevel, strategy);
		}
		catch (Exception innerException)
		{
			throw new ZLibException(System.SR.ZLibErrorDLLLoadError, innerException);
		}
		switch (errorCode)
		{
		case ZLibNative.ErrorCode.Ok:
			break;
		case ZLibNative.ErrorCode.MemError:
			throw new ZLibException(System.SR.ZLibErrorNotEnoughMemory, "deflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		case ZLibNative.ErrorCode.VersionError:
			throw new ZLibException(System.SR.ZLibErrorVersionMismatch, "deflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		case ZLibNative.ErrorCode.StreamError:
			throw new ZLibException(System.SR.ZLibErrorIncorrectInitParameters, "deflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		default:
			throw new ZLibException(System.SR.ZLibErrorUnexpected, "deflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		}
	}

	~Deflater()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			if (disposing)
			{
				_zlibStream.Dispose();
			}
			DeallocateInputBufferHandle();
			_isDisposed = true;
		}
	}

	public bool NeedsInput()
	{
		return _zlibStream.AvailIn == 0;
	}

	internal unsafe void SetInput(ReadOnlyMemory<byte> inputBuffer)
	{
		if (inputBuffer.Length == 0)
		{
			return;
		}
		lock (SyncLock)
		{
			_inputBufferHandle = inputBuffer.Pin();
			_zlibStream.NextIn = (IntPtr)_inputBufferHandle.Pointer;
			_zlibStream.AvailIn = (uint)inputBuffer.Length;
		}
	}

	internal unsafe void SetInput(byte* inputBufferPtr, int count)
	{
		if (count == 0)
		{
			return;
		}
		lock (SyncLock)
		{
			_zlibStream.NextIn = (IntPtr)inputBufferPtr;
			_zlibStream.AvailIn = (uint)count;
		}
	}

	internal int GetDeflateOutput(byte[] outputBuffer)
	{
		try
		{
			ReadDeflateOutput(outputBuffer, ZLibNative.FlushCode.NoFlush, out var bytesRead);
			return bytesRead;
		}
		finally
		{
			if (_zlibStream.AvailIn == 0)
			{
				DeallocateInputBufferHandle();
			}
		}
	}

	private unsafe ZLibNative.ErrorCode ReadDeflateOutput(byte[] outputBuffer, ZLibNative.FlushCode flushCode, out int bytesRead)
	{
		lock (SyncLock)
		{
			fixed (byte* ptr = &outputBuffer[0])
			{
				_zlibStream.NextOut = (IntPtr)ptr;
				_zlibStream.AvailOut = (uint)outputBuffer.Length;
				ZLibNative.ErrorCode result = Deflate(flushCode);
				bytesRead = outputBuffer.Length - (int)_zlibStream.AvailOut;
				return result;
			}
		}
	}

	internal bool Finish(byte[] outputBuffer, out int bytesRead)
	{
		ZLibNative.ErrorCode errorCode = ReadDeflateOutput(outputBuffer, ZLibNative.FlushCode.Finish, out bytesRead);
		return errorCode == ZLibNative.ErrorCode.StreamEnd;
	}

	internal bool Flush(byte[] outputBuffer, out int bytesRead)
	{
		return ReadDeflateOutput(outputBuffer, ZLibNative.FlushCode.SyncFlush, out bytesRead) == ZLibNative.ErrorCode.Ok;
	}

	private void DeallocateInputBufferHandle()
	{
		lock (SyncLock)
		{
			_zlibStream.AvailIn = 0u;
			_zlibStream.NextIn = ZLibNative.ZNullPtr;
			_inputBufferHandle.Dispose();
		}
	}

	private ZLibNative.ErrorCode Deflate(ZLibNative.FlushCode flushCode)
	{
		ZLibNative.ErrorCode errorCode;
		try
		{
			errorCode = _zlibStream.Deflate(flushCode);
		}
		catch (Exception innerException)
		{
			throw new ZLibException(System.SR.ZLibErrorDLLLoadError, innerException);
		}
		switch (errorCode)
		{
		case ZLibNative.ErrorCode.Ok:
		case ZLibNative.ErrorCode.StreamEnd:
			return errorCode;
		case ZLibNative.ErrorCode.BufError:
			return errorCode;
		case ZLibNative.ErrorCode.StreamError:
			throw new ZLibException(System.SR.ZLibErrorInconsistentStream, "deflate", (int)errorCode, _zlibStream.GetErrorMessage());
		default:
			throw new ZLibException(System.SR.ZLibErrorUnexpected, "deflate", (int)errorCode, _zlibStream.GetErrorMessage());
		}
	}
}
