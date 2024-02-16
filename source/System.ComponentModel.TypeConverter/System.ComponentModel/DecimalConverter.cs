using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel;

public class DecimalConverter : BaseNumberConverter
{
	internal override bool AllowHex => false;

	internal override Type TargetType => typeof(decimal);

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(InstanceDescriptor) && value is decimal d)
		{
			ConstructorInfo constructor = typeof(decimal).GetConstructor(new Type[1] { typeof(int[]) });
			return new InstanceDescriptor(constructor, new object[1] { decimal.GetBits(d) });
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	internal override object FromString(string value, int radix)
	{
		return Convert.ToDecimal(value, CultureInfo.CurrentCulture);
	}

	internal override object FromString(string value, NumberFormatInfo formatInfo)
	{
		return decimal.Parse(value, NumberStyles.Float, formatInfo);
	}

	internal override string ToString(object value, NumberFormatInfo formatInfo)
	{
		return ((decimal)value).ToString("G", formatInfo);
	}
}
