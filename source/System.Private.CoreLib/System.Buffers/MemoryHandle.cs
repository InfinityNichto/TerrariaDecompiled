using System.Runtime.InteropServices;

namespace System.Buffers;

public struct MemoryHandle : IDisposable
{
	private unsafe void* _pointer;

	private GCHandle _handle;

	private IPinnable _pinnable;

	[CLSCompliant(false)]
	public unsafe void* Pointer => _pointer;

	[CLSCompliant(false)]
	public unsafe MemoryHandle(void* pointer, GCHandle handle = default(GCHandle), IPinnable? pinnable = null)
	{
		_pointer = pointer;
		_handle = handle;
		_pinnable = pinnable;
	}

	public unsafe void Dispose()
	{
		if (_handle.IsAllocated)
		{
			_handle.Free();
		}
		if (_pinnable != null)
		{
			_pinnable.Unpin();
			_pinnable = null;
		}
		_pointer = null;
	}
}
