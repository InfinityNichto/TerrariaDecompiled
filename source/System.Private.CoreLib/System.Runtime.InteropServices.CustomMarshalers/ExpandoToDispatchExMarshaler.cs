namespace System.Runtime.InteropServices.CustomMarshalers;

internal sealed class ExpandoToDispatchExMarshaler : ICustomMarshaler
{
	private static readonly ExpandoToDispatchExMarshaler s_ExpandoToDispatchExMarshaler = new ExpandoToDispatchExMarshaler();

	public static ICustomMarshaler GetInstance(string cookie)
	{
		return s_ExpandoToDispatchExMarshaler;
	}

	private ExpandoToDispatchExMarshaler()
	{
	}

	public void CleanUpManagedData(object ManagedObj)
	{
	}

	public void CleanUpNativeData(IntPtr pNativeData)
	{
	}

	public int GetNativeDataSize()
	{
		return -1;
	}

	public IntPtr MarshalManagedToNative(object ManagedObj)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_IExpando);
	}

	public object MarshalNativeToManaged(IntPtr pNativeData)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_IExpando);
	}
}
