using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel;

public class TimeSpanConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string text)
		{
			string input = text.Trim();
			try
			{
				return TimeSpan.Parse(input, culture);
			}
			catch (FormatException innerException)
			{
				throw new FormatException(System.SR.Format(System.SR.ConvertInvalidPrimitive, (string)value, "TimeSpan"), innerException);
			}
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(InstanceDescriptor) && value is TimeSpan)
		{
			return new InstanceDescriptor(typeof(TimeSpan).GetMethod("Parse", new Type[1] { typeof(string) }), new object[1] { value.ToString() });
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
