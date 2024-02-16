using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

internal ref struct StringHandleOnStack
{
	private unsafe void* _ptr;

	internal unsafe StringHandleOnStack(ref string s)
	{
		_ptr = Unsafe.AsPointer(ref s);
	}
}
