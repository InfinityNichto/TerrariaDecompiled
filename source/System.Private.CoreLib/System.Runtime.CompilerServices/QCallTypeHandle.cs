using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

internal ref struct QCallTypeHandle
{
	private unsafe void* _ptr;

	private IntPtr _handle;

	internal unsafe QCallTypeHandle(ref RuntimeType type)
	{
		_ptr = Unsafe.AsPointer(ref type);
		if (type != null)
		{
			_handle = type.GetUnderlyingNativeHandle();
		}
		else
		{
			_handle = IntPtr.Zero;
		}
	}

	internal unsafe QCallTypeHandle(ref RuntimeTypeHandle rth)
	{
		_ptr = Unsafe.AsPointer(ref rth);
		_handle = rth.Value;
	}
}
