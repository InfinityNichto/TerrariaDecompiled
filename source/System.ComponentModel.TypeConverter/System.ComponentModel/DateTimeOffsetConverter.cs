using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel;

public class DateTimeOffsetConverter : TypeConverter
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
			string text2 = text.Trim();
			if (text2.Length == 0)
			{
				return DateTimeOffset.MinValue;
			}
			try
			{
				DateTimeFormatInfo dateTimeFormatInfo = null;
				if (culture != null)
				{
					dateTimeFormatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
				}
				if (dateTimeFormatInfo != null)
				{
					return DateTimeOffset.Parse(text2, dateTimeFormatInfo);
				}
				return DateTimeOffset.Parse(text2, culture);
			}
			catch (FormatException innerException)
			{
				throw new FormatException(System.SR.Format(System.SR.ConvertInvalidPrimitive, (string)value, "DateTimeOffset"), innerException);
			}
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string) && value is DateTimeOffset dateTimeOffset)
		{
			if (dateTimeOffset == DateTimeOffset.MinValue)
			{
				return string.Empty;
			}
			if (culture == null)
			{
				culture = CultureInfo.CurrentCulture;
			}
			DateTimeFormatInfo dateTimeFormatInfo = null;
			dateTimeFormatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
			if (culture == CultureInfo.InvariantCulture)
			{
				if (dateTimeOffset.TimeOfDay.TotalSeconds == 0.0)
				{
					return dateTimeOffset.ToString("yyyy-MM-dd zzz", culture);
				}
				return dateTimeOffset.ToString(culture);
			}
			string text = ((dateTimeOffset.TimeOfDay.TotalSeconds != 0.0) ? (dateTimeFormatInfo.ShortDatePattern + " " + dateTimeFormatInfo.ShortTimePattern + " zzz") : (dateTimeFormatInfo.ShortDatePattern + " zzz"));
			return dateTimeOffset.ToString(text, CultureInfo.CurrentCulture);
		}
		if (destinationType == typeof(InstanceDescriptor) && value is DateTimeOffset dateTimeOffset2)
		{
			if (dateTimeOffset2.Ticks == 0L)
			{
				return new InstanceDescriptor(typeof(DateTimeOffset).GetConstructor(new Type[1] { typeof(long) }), new object[1] { dateTimeOffset2.Ticks });
			}
			return new InstanceDescriptor(typeof(DateTimeOffset).GetConstructor(new Type[8]
			{
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(TimeSpan)
			}), new object[8] { dateTimeOffset2.Year, dateTimeOffset2.Month, dateTimeOffset2.Day, dateTimeOffset2.Hour, dateTimeOffset2.Minute, dateTimeOffset2.Second, dateTimeOffset2.Millisecond, dateTimeOffset2.Offset });
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
