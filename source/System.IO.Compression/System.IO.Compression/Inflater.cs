using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.IO.Compression;

internal sealed class Inflater : IDisposable
{
	private bool _finished;

	private bool _isDisposed;

	private readonly int _windowBits;

	private ZLibNative.ZLibStreamHandle _zlibStream;

	private MemoryHandle _inputBufferHandle;

	private readonly long _uncompressedSize;

	private long _currentInflatedCount;

	private object SyncLock => this;

	private unsafe bool IsInputBufferHandleAllocated => _inputBufferHandle.Pointer != null;

	internal Inflater(int windowBits, long uncompressedSize = -1L)
	{
		_finished = false;
		_isDisposed = false;
		_windowBits = windowBits;
		InflateInit(windowBits);
		_uncompressedSize = uncompressedSize;
	}

	public bool Finished()
	{
		return _finished;
	}

	public unsafe bool Inflate(out byte b)
	{
		fixed (byte* bufPtr = &b)
		{
			int num = InflateVerified(bufPtr, 1);
			return num != 0;
		}
	}

	public unsafe int Inflate(byte[] bytes, int offset, int length)
	{
		if (length == 0)
		{
			return 0;
		}
		fixed (byte* ptr = bytes)
		{
			return InflateVerified(ptr + offset, length);
		}
	}

	public unsafe int Inflate(Span<byte> destination)
	{
		if (destination.Length == 0)
		{
			return 0;
		}
		fixed (byte* bufPtr = &MemoryMarshal.GetReference(destination))
		{
			return InflateVerified(bufPtr, destination.Length);
		}
	}

	public unsafe int InflateVerified(byte* bufPtr, int length)
	{
		try
		{
			int bytesRead = 0;
			if (_uncompressedSize == -1)
			{
				ReadOutput(bufPtr, length, out bytesRead);
			}
			else if (_uncompressedSize > _currentInflatedCount)
			{
				length = (int)Math.Min(length, _uncompressedSize - _currentInflatedCount);
				ReadOutput(bufPtr, length, out bytesRead);
				_currentInflatedCount += bytesRead;
			}
			else
			{
				_finished = true;
				_zlibStream.AvailIn = 0u;
			}
			return bytesRead;
		}
		finally
		{
			if (_zlibStream.AvailIn == 0 && IsInputBufferHandleAllocated)
			{
				DeallocateInputBufferHandle();
			}
		}
	}

	private unsafe void ReadOutput(byte* bufPtr, int length, out int bytesRead)
	{
		if (ReadInflateOutput(bufPtr, length, ZLibNative.FlushCode.NoFlush, out bytesRead) == ZLibNative.ErrorCode.StreamEnd)
		{
			if (!NeedsInput() && IsGzipStream() && IsInputBufferHandleAllocated)
			{
				_finished = ResetStreamForLeftoverInput();
			}
			else
			{
				_finished = true;
			}
		}
	}

	private unsafe bool ResetStreamForLeftoverInput()
	{
		lock (SyncLock)
		{
			IntPtr nextIn = _zlibStream.NextIn;
			byte* ptr = (byte*)nextIn.ToPointer();
			uint availIn = _zlibStream.AvailIn;
			if (*ptr != 31 || (availIn > 1 && ptr[1] != 139))
			{
				return true;
			}
			_zlibStream.Dispose();
			InflateInit(_windowBits);
			_zlibStream.NextIn = nextIn;
			_zlibStream.AvailIn = availIn;
			_finished = false;
		}
		return false;
	}

	internal bool IsGzipStream()
	{
		if (_windowBits >= 24)
		{
			return _windowBits <= 31;
		}
		return false;
	}

	public bool NeedsInput()
	{
		return _zlibStream.AvailIn == 0;
	}

	public void SetInput(byte[] inputBuffer, int startIndex, int count)
	{
		SetInput(inputBuffer.AsMemory(startIndex, count));
	}

	public unsafe void SetInput(ReadOnlyMemory<byte> inputBuffer)
	{
		if (inputBuffer.IsEmpty)
		{
			return;
		}
		lock (SyncLock)
		{
			_inputBufferHandle = inputBuffer.Pin();
			_zlibStream.NextIn = (IntPtr)_inputBufferHandle.Pointer;
			_zlibStream.AvailIn = (uint)inputBuffer.Length;
			_finished = false;
		}
	}

	private void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			if (disposing)
			{
				_zlibStream.Dispose();
			}
			if (IsInputBufferHandleAllocated)
			{
				DeallocateInputBufferHandle();
			}
			_isDisposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~Inflater()
	{
		Dispose(disposing: false);
	}

	[MemberNotNull("_zlibStream")]
	private void InflateInit(int windowBits)
	{
		ZLibNative.ErrorCode errorCode;
		try
		{
			errorCode = ZLibNative.CreateZLibStreamForInflate(out _zlibStream, windowBits);
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
			throw new ZLibException(System.SR.ZLibErrorNotEnoughMemory, "inflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		case ZLibNative.ErrorCode.VersionError:
			throw new ZLibException(System.SR.ZLibErrorVersionMismatch, "inflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		case ZLibNative.ErrorCode.StreamError:
			throw new ZLibException(System.SR.ZLibErrorIncorrectInitParameters, "inflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		default:
			throw new ZLibException(System.SR.ZLibErrorUnexpected, "inflateInit2_", (int)errorCode, _zlibStream.GetErrorMessage());
		}
	}

	private unsafe ZLibNative.ErrorCode ReadInflateOutput(byte* bufPtr, int length, ZLibNative.FlushCode flushCode, out int bytesRead)
	{
		lock (SyncLock)
		{
			_zlibStream.NextOut = (IntPtr)bufPtr;
			_zlibStream.AvailOut = (uint)length;
			ZLibNative.ErrorCode result = Inflate(flushCode);
			bytesRead = length - (int)_zlibStream.AvailOut;
			return result;
		}
	}

	private ZLibNative.ErrorCode Inflate(ZLibNative.FlushCode flushCode)
	{
		ZLibNative.ErrorCode errorCode;
		try
		{
			errorCode = _zlibStream.Inflate(flushCode);
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
		case ZLibNative.ErrorCode.MemError:
			throw new ZLibException(System.SR.ZLibErrorNotEnoughMemory, "inflate_", (int)errorCode, _zlibStream.GetErrorMessage());
		case ZLibNative.ErrorCode.DataError:
			throw new InvalidDataException(System.SR.UnsupportedCompression);
		case ZLibNative.ErrorCode.StreamError:
			throw new ZLibException(System.SR.ZLibErrorInconsistentStream, "inflate_", (int)errorCode, _zlibStream.GetErrorMessage());
		default:
			throw new ZLibException(System.SR.ZLibErrorUnexpected, "inflate_", (int)errorCode, _zlibStream.GetErrorMessage());
		}
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
}
