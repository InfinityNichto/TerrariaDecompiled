using System.Collections;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.CustomMarshalers;

[SupportedOSPlatform("windows")]
internal sealed class EnumeratorToEnumVariantMarshaler : ICustomMarshaler
{
	private static readonly EnumeratorToEnumVariantMarshaler s_enumeratorToEnumVariantMarshaler = new EnumeratorToEnumVariantMarshaler();

	public static ICustomMarshaler GetInstance(string cookie)
	{
		return s_enumeratorToEnumVariantMarshaler;
	}

	private EnumeratorToEnumVariantMarshaler()
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
		if (ManagedObj is EnumeratorViewOfEnumVariant enumeratorViewOfEnumVariant)
		{
			return Marshal.GetComInterfaceForObject<object, IEnumVARIANT>(enumeratorViewOfEnumVariant.GetUnderlyingObject());
		}
		EnumVariantViewOfEnumerator o = new EnumVariantViewOfEnumerator((System.Collections.IEnumerator)ManagedObj);
		return Marshal.GetComInterfaceForObject<EnumVariantViewOfEnumerator, IEnumVARIANT>(o);
	}

	public object MarshalNativeToManaged(IntPtr pNativeData)
	{
		if (pNativeData == IntPtr.Zero)
		{
			throw new ArgumentNullException("pNativeData");
		}
		object objectForIUnknown = Marshal.GetObjectForIUnknown(pNativeData);
		if (!objectForIUnknown.GetType().IsCOMObject)
		{
			if (objectForIUnknown is EnumVariantViewOfEnumerator enumVariantViewOfEnumerator)
			{
				return enumVariantViewOfEnumerator.Enumerator;
			}
			return objectForIUnknown as System.Collections.IEnumerator;
		}
		return ComDataHelpers.GetOrCreateManagedViewFromComData(objectForIUnknown, (IEnumVARIANT var) => new EnumeratorViewOfEnumVariant(var));
	}
}
