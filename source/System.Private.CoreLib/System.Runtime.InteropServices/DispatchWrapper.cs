using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

[SupportedOSPlatform("windows")]
public sealed class DispatchWrapper
{
	public object? WrappedObject { get; }

	public DispatchWrapper(object? obj)
	{
		if (obj != null)
		{
			IntPtr iDispatchForObject = Marshal.GetIDispatchForObject(obj);
			Marshal.Release(iDispatchForObject);
			WrappedObject = obj;
		}
	}
}
