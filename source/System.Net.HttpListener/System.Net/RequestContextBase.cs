using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net;

internal abstract class RequestContextBase : IDisposable
{
	private unsafe global::Interop.HttpApi.HTTP_REQUEST* _memoryBlob;

	private unsafe global::Interop.HttpApi.HTTP_REQUEST* _originalBlobAddress;

	private IntPtr _backingBuffer = IntPtr.Zero;

	private int _backingBufferLength;

	internal unsafe global::Interop.HttpApi.HTTP_REQUEST* RequestBlob => _memoryBlob;

	internal IntPtr RequestBuffer => _backingBuffer;

	internal uint Size => (uint)_backingBufferLength;

	internal unsafe IntPtr OriginalBlobAddress
	{
		get
		{
			global::Interop.HttpApi.HTTP_REQUEST* memoryBlob = _memoryBlob;
			return (IntPtr)((memoryBlob == null) ? _originalBlobAddress : memoryBlob);
		}
	}

	protected unsafe void BaseConstruction(global::Interop.HttpApi.HTTP_REQUEST* requestBlob)
	{
		if (requestBlob != null)
		{
			_memoryBlob = requestBlob;
		}
	}

	internal unsafe void ReleasePins()
	{
		_originalBlobAddress = _memoryBlob;
		UnsetBlob();
		OnReleasePins();
	}

	protected abstract void OnReleasePins();

	public void Close()
	{
		Dispose();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		IntPtr intPtr = Interlocked.Exchange(ref _backingBuffer, IntPtr.Zero);
		if (intPtr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}

	~RequestContextBase()
	{
		Dispose(disposing: false);
	}

	protected unsafe void SetBlob(global::Interop.HttpApi.HTTP_REQUEST* requestBlob)
	{
		if (requestBlob == null)
		{
			UnsetBlob();
		}
		else
		{
			_memoryBlob = requestBlob;
		}
	}

	protected unsafe void UnsetBlob()
	{
		_memoryBlob = null;
	}

	protected unsafe void SetBuffer(int size)
	{
		if (_backingBuffer != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_backingBuffer);
		}
		_backingBuffer = ((size == 0) ? IntPtr.Zero : Marshal.AllocHGlobal(size));
		_backingBufferLength = size;
		new Span<byte>(_backingBuffer.ToPointer(), size).Clear();
	}
}
