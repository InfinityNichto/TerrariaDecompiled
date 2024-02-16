using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.CustomMarshalers;

internal static class ComDataHelpers
{
	[SupportedOSPlatform("windows")]
	public static TView GetOrCreateManagedViewFromComData<T, TView>(object comObject, Func<T, TView> createCallback)
	{
		object typeFromHandle = typeof(TView);
		object comObjectData = Marshal.GetComObjectData(comObject, typeFromHandle);
		if (comObjectData is TView)
		{
			return (TView)comObjectData;
		}
		TView val = createCallback((T)comObject);
		if (!Marshal.SetComObjectData(comObject, typeFromHandle, val))
		{
			val = (TView)Marshal.GetComObjectData(comObject, typeFromHandle);
		}
		return val;
	}
}
