using System.Runtime.InteropServices;

namespace System.Reflection;

internal sealed class LoaderAllocatorScout
{
	internal IntPtr m_nativeLoaderAllocator;

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern bool Destroy(IntPtr nativeLoaderAllocator);

	~LoaderAllocatorScout()
	{
		if (!(m_nativeLoaderAllocator == IntPtr.Zero) && !Destroy(m_nativeLoaderAllocator))
		{
			GC.ReRegisterForFinalize(this);
		}
	}
}
