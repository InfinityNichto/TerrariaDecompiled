using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace System;

internal sealed class OleAutBinder : DefaultBinder
{
	public override object ChangeType(object value, Type type, CultureInfo cultureInfo)
	{
		Variant source = new Variant(value);
		if (cultureInfo == null)
		{
			cultureInfo = CultureInfo.CurrentCulture;
		}
		if (type.IsByRef)
		{
			type = type.GetElementType();
		}
		if (!type.IsPrimitive && type.IsInstanceOfType(value))
		{
			return value;
		}
		Type type2 = value.GetType();
		if (type.IsEnum && type2.IsPrimitive)
		{
			return Enum.Parse(type, value.ToString());
		}
		try
		{
			return OAVariantLib.ChangeType(source, type, 16, cultureInfo).ToObject();
		}
		catch (NotSupportedException)
		{
			throw new COMException(SR.Interop_COM_TypeMismatch, -2147352571);
		}
	}
}
