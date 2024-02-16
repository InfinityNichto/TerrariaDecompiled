using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.CustomMarshalers;

[SupportedOSPlatform("windows")]
internal sealed class EnumerableToDispatchMarshaler : ICustomMarshaler
{
	private static readonly EnumerableToDispatchMarshaler s_enumerableToDispatchMarshaler = new EnumerableToDispatchMarshaler();

	public static ICustomMarshaler GetInstance(string cookie)
	{
		return s_enumerableToDispatchMarshaler;
	}

	private EnumerableToDispatchMarshaler()
	{
	}

	public void CleanUpManagedData(object ManagedObj)
	{
	}

	public void CleanUpNativeData(IntPtr pNativeData)
	{
		Marshal.Release(pNativeData);
	}

	public int GetNativeDataSize()
	{
		return -1;
	}

	public IntPtr MarshalManagedToNative(object ManagedObj)
	{
		if (ManagedObj == null)
		{
			throw new ArgumentNullException("ManagedObj");
		}
		return Marshal.GetComInterfaceForObject<object, IEnumerable>(ManagedObj);
	}

	public object MarshalNativeToManaged(IntPtr pNativeData)
	{
		if (pNativeData == IntPtr.Zero)
		{
			throw new ArgumentNullException("pNativeData");
		}
		object objectForIUnknown = Marshal.GetObjectForIUnknown(pNativeData);
		return ComDataHelpers.GetOrCreateManagedViewFromComData(objectForIUnknown, (object obj) => new EnumerableViewOfDispatch(obj));
	}
}
