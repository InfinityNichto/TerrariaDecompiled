using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Sequential)]
internal sealed class LAHashDependentHashTracker
{
	private GCHandle _dependentHandle;

	private IntPtr _loaderAllocator;

	~LAHashDependentHashTracker()
	{
		if (_dependentHandle.IsAllocated)
		{
			_dependentHandle.Free();
		}
	}
}
