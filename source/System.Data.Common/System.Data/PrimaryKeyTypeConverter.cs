using System.ComponentModel;
using System.Globalization;

namespace System.Data;

internal sealed class PrimaryKeyTypeConverter : ReferenceConverter
{
	public PrimaryKeyTypeConverter()
		: base(typeof(DataColumn[]))
	{
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext context)
	{
		return false;
	}

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		if (!(destinationType == typeof(string)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (!(destinationType == typeof(string)))
		{
			return base.ConvertTo(context, culture, value, destinationType);
		}
		return Array.Empty<DataColumn>().GetType().Name;
	}
}
