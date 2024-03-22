using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Drawing;

public class PointConverter : TypeConverter
{
	private static readonly string[] s_propertySort = new string[2] { "X", "Y" };

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
				return null;
			}
			if (culture == null)
			{
				culture = CultureInfo.CurrentCulture;
			}
			char separator = culture.TextInfo.ListSeparator[0];
			string[] array = text2.Split(separator);
			int[] array2 = new int[array.Length];
			TypeConverter converterTrimUnsafe = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = (int)converterTrimUnsafe.ConvertFromString(context, culture, array[i]);
			}
			if (array2.Length == 2)
			{
				return new Point(array2[0], array2[1]);
			}
			throw new ArgumentException(System.SR.Format(System.SR.TextParseFailedFormat, text2, "x, y"));
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (value is Point point)
		{
			if (destinationType == typeof(string))
			{
				if (culture == null)
				{
					culture = CultureInfo.CurrentCulture;
				}
				string separator = culture.TextInfo.ListSeparator + " ";
				TypeConverter converterTrimUnsafe = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));
				string[] value2 = new string[2]
				{
					converterTrimUnsafe.ConvertToString(context, culture, point.X),
					converterTrimUnsafe.ConvertToString(context, culture, point.Y)
				};
				return string.Join(separator, value2);
			}
			if (destinationType == typeof(InstanceDescriptor))
			{
				ConstructorInfo constructor = typeof(Point).GetConstructor(new Type[2]
				{
					typeof(int),
					typeof(int)
				});
				if (constructor != null)
				{
					return new InstanceDescriptor(constructor, new object[2] { point.X, point.Y });
				}
			}
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues)
	{
		if (propertyValues == null)
		{
			throw new ArgumentNullException("propertyValues");
		}
		object obj = propertyValues["X"];
		object obj2 = propertyValues["Y"];
		if (obj == null || obj2 == null || !(obj is int) || !(obj2 is int))
		{
			throw new ArgumentException(System.SR.PropertyValueInvalidEntry);
		}
		return new Point((int)obj, (int)obj2);
	}

	public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context)
	{
		return true;
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object? value, Attribute[]? attributes)
	{
		PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(Point), attributes);
		return properties.Sort(s_propertySort);
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext? context)
	{
		return true;
	}
}