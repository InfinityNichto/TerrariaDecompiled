using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

internal ref struct ObjectHandleOnStack
{
	private unsafe void* _ptr;

	private unsafe ObjectHandleOnStack(void* pObject)
	{
		_ptr = pObject;
	}

	internal unsafe static ObjectHandleOnStack Create<T>(ref T o) where T : class
	{
		return new ObjectHandleOnStack(Unsafe.AsPointer(ref o));
	}
}
