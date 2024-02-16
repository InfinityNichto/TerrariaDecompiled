using System.ComponentModel;
using System.Globalization;

namespace System.Data;

internal sealed class DefaultValueTypeConverter : StringConverter
{
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == typeof(string))
		{
			if (value == null)
			{
				return "<null>";
			}
			if (value == DBNull.Value)
			{
				return "<DBNull>";
			}
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value != null && value.GetType() == typeof(string))
		{
			string a = (string)value;
			if (string.Equals(a, "<null>", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			if (string.Equals(a, "<DBNull>", StringComparison.OrdinalIgnoreCase))
			{
				return DBNull.Value;
			}
		}
		return base.ConvertFrom(context, culture, value);
	}
}
