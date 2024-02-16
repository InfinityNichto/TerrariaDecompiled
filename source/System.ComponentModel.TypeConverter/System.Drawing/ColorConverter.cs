using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.Drawing;

public class ColorConverter : TypeConverter
{
	private sealed class ColorComparer : IComparer<Color>
	{
		public int Compare(Color left, Color right)
		{
			return string.CompareOrdinal(left.Name, right.Name);
		}
	}

	private static readonly Lazy<StandardValuesCollection> s_valuesLazy = new Lazy<StandardValuesCollection>(delegate
	{
		HashSet<Color> source = new HashSet<Color>(System.Drawing.ColorTable.Colors.Values);
		return new StandardValuesCollection(source.OrderBy((Color c) => c, new ColorComparer()).ToList());
	});

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
		if (value is string strValue)
		{
			return System.Drawing.ColorConverterCommon.ConvertFromString(strValue, culture ?? CultureInfo.CurrentCulture);
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (value is Color color)
		{
			if (destinationType == typeof(string))
			{
				if (color == Color.Empty)
				{
					return string.Empty;
				}
				if (System.Drawing.ColorTable.IsKnownNamedColor(color.Name))
				{
					return color.Name;
				}
				if (color.IsNamedColor)
				{
					return "'" + color.Name + "'";
				}
				if (culture == null)
				{
					culture = CultureInfo.CurrentCulture;
				}
				string separator = culture.TextInfo.ListSeparator + " ";
				TypeConverter converterTrimUnsafe = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));
				int num = 0;
				string[] array;
				if (color.A < byte.MaxValue)
				{
					array = new string[4];
					array[num++] = converterTrimUnsafe.ConvertToString(context, culture, color.A);
				}
				else
				{
					array = new string[3];
				}
				array[num++] = converterTrimUnsafe.ConvertToString(context, culture, color.R);
				array[num++] = converterTrimUnsafe.ConvertToString(context, culture, color.G);
				array[num++] = converterTrimUnsafe.ConvertToString(context, culture, color.B);
				return string.Join(separator, array);
			}
			if (destinationType == typeof(InstanceDescriptor))
			{
				MemberInfo memberInfo = null;
				object[] arguments = null;
				if (color.IsEmpty)
				{
					memberInfo = typeof(Color).GetField("Empty");
				}
				else if (System.Drawing.ColorTable.IsKnownNamedColor(color.Name))
				{
					memberInfo = typeof(Color).GetProperty(color.Name) ?? typeof(SystemColors).GetProperty(color.Name);
				}
				else if (color.A != byte.MaxValue)
				{
					memberInfo = typeof(Color).GetMethod("FromArgb", new Type[4]
					{
						typeof(int),
						typeof(int),
						typeof(int),
						typeof(int)
					});
					arguments = new object[4] { color.A, color.R, color.G, color.B };
				}
				else if (color.IsNamedColor)
				{
					memberInfo = typeof(Color).GetMethod("FromName", new Type[1] { typeof(string) });
					arguments = new object[1] { color.Name };
				}
				else
				{
					memberInfo = typeof(Color).GetMethod("FromArgb", new Type[3]
					{
						typeof(int),
						typeof(int),
						typeof(int)
					});
					arguments = new object[3] { color.R, color.G, color.B };
				}
				if (memberInfo != null)
				{
					return new InstanceDescriptor(memberInfo, arguments);
				}
				return null;
			}
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
	{
		return s_valuesLazy.Value;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
	{
		return true;
	}
}
