using System.Reflection;
using System.Reflection.Emit;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

internal ref struct QCallModule
{
	private unsafe void* _ptr;

	private IntPtr _module;

	internal unsafe QCallModule(ref RuntimeModule module)
	{
		_ptr = Unsafe.AsPointer(ref module);
		_module = module.GetUnderlyingNativeHandle();
	}

	internal unsafe QCallModule(ref ModuleBuilder module)
	{
		_ptr = Unsafe.AsPointer(ref module);
		_module = module.InternalModule.GetUnderlyingNativeHandle();
	}
}
