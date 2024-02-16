using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel;

public class EnumConverter : TypeConverter
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)]
	protected Type EnumType { get; }

	protected StandardValuesCollection? Values { get; set; }

	protected virtual IComparer Comparer => InvariantComparer.Default;

	public EnumConverter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
	{
		EnumType = type;
	}

	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (sourceType == typeof(string) || sourceType == typeof(Enum[]))
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (destinationType == typeof(Enum[]) || destinationType == typeof(InstanceDescriptor))
		{
			return true;
		}
		return base.CanConvertTo(context, destinationType);
	}

	private static long GetEnumValue(bool isUnderlyingTypeUInt64, Enum enumVal, CultureInfo culture)
	{
		if (!isUnderlyingTypeUInt64)
		{
			return Convert.ToInt64(enumVal, culture);
		}
		return (long)Convert.ToUInt64(enumVal, culture);
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string text)
		{
			try
			{
				if (text.Contains(','))
				{
					bool isUnderlyingTypeUInt = Enum.GetUnderlyingType(EnumType) == typeof(ulong);
					long num = 0L;
					string[] array = text.Split(',');
					string[] array2 = array;
					foreach (string value2 in array2)
					{
						num |= GetEnumValue(isUnderlyingTypeUInt, (Enum)Enum.Parse(EnumType, value2, ignoreCase: true), culture);
					}
					return Enum.ToObject(EnumType, num);
				}
				return Enum.Parse(EnumType, text, ignoreCase: true);
			}
			catch (Exception innerException)
			{
				throw new FormatException(System.SR.Format(System.SR.ConvertInvalidPrimitive, (string)value, EnumType.Name), innerException);
			}
		}
		if (value is Enum[])
		{
			bool isUnderlyingTypeUInt2 = Enum.GetUnderlyingType(EnumType) == typeof(ulong);
			long num2 = 0L;
			Enum[] array3 = (Enum[])value;
			foreach (Enum enumVal in array3)
			{
				num2 |= GetEnumValue(isUnderlyingTypeUInt2, enumVal, culture);
			}
			return Enum.ToObject(EnumType, num2);
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == typeof(string) && value != null)
		{
			if (!EnumType.IsDefined(typeof(FlagsAttribute), inherit: false) && !Enum.IsDefined(EnumType, value))
			{
				throw new ArgumentException(System.SR.Format(System.SR.EnumConverterInvalidValue, value, EnumType.Name));
			}
			return Enum.Format(EnumType, value, "G");
		}
		if (destinationType == typeof(InstanceDescriptor) && value != null)
		{
			string text = ConvertToInvariantString(context, value);
			if (EnumType.IsDefined(typeof(FlagsAttribute), inherit: false) && text.Contains(','))
			{
				Type underlyingType = Enum.GetUnderlyingType(EnumType);
				if (value is IConvertible)
				{
					object obj = ((IConvertible)value).ToType(underlyingType, culture);
					MethodInfo method = typeof(Enum).GetMethod("ToObject", new Type[2]
					{
						typeof(Type),
						underlyingType
					});
					if (method != null)
					{
						return new InstanceDescriptor(method, new object[2] { EnumType, obj });
					}
				}
			}
			else
			{
				FieldInfo field = EnumType.GetField(text);
				if (field != null)
				{
					return new InstanceDescriptor(field, null);
				}
			}
		}
		if (destinationType == typeof(Enum[]) && value != null)
		{
			if (EnumType.IsDefined(typeof(FlagsAttribute), inherit: false))
			{
				bool isUnderlyingTypeUInt = Enum.GetUnderlyingType(EnumType) == typeof(ulong);
				List<Enum> list = new List<Enum>();
				Array values = Enum.GetValues(EnumType);
				long[] array = new long[values.Length];
				for (int i = 0; i < values.Length; i++)
				{
					array[i] = GetEnumValue(isUnderlyingTypeUInt, (Enum)values.GetValue(i), culture);
				}
				long num = GetEnumValue(isUnderlyingTypeUInt, (Enum)value, culture);
				bool flag = true;
				while (flag)
				{
					flag = false;
					long[] array2 = array;
					foreach (long num2 in array2)
					{
						if ((num2 != 0L && (num2 & num) == num2) || num2 == num)
						{
							list.Add((Enum)Enum.ToObject(EnumType, num2));
							flag = true;
							num &= ~num2;
							break;
						}
					}
					if (num == 0L)
					{
						break;
					}
				}
				if (!flag && num != 0L)
				{
					list.Add((Enum)Enum.ToObject(EnumType, num));
				}
				return list.ToArray();
			}
			return new Enum[1] { (Enum)Enum.ToObject(EnumType, value) };
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
	{
		if (Values == null)
		{
			Type type = TypeDescriptor.GetReflectionType(EnumType) ?? EnumType;
			FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
			ArrayList arrayList = null;
			if (fields != null && fields.Length != 0)
			{
				arrayList = new ArrayList(fields.Length);
			}
			if (arrayList != null)
			{
				FieldInfo[] array = fields;
				foreach (FieldInfo fieldInfo in array)
				{
					BrowsableAttribute browsableAttribute = null;
					object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(BrowsableAttribute), inherit: false);
					for (int j = 0; j < customAttributes.Length; j++)
					{
						Attribute attribute = (Attribute)customAttributes[j];
						browsableAttribute = attribute as BrowsableAttribute;
					}
					if (browsableAttribute != null && !browsableAttribute.Browsable)
					{
						continue;
					}
					object obj = null;
					try
					{
						if (fieldInfo.Name != null)
						{
							obj = Enum.Parse(EnumType, fieldInfo.Name);
						}
					}
					catch (ArgumentException)
					{
					}
					if (obj != null)
					{
						arrayList.Add(obj);
					}
				}
				IComparer comparer = Comparer;
				if (comparer != null)
				{
					arrayList.Sort(comparer);
				}
			}
			Array values = arrayList?.ToArray();
			Values = new StandardValuesCollection(values);
		}
		return Values;
	}

	public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
	{
		return !EnumType.IsDefined(typeof(FlagsAttribute), inherit: false);
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
	{
		return true;
	}

	public override bool IsValid(ITypeDescriptorContext? context, object? value)
	{
		return Enum.IsDefined(EnumType, value);
	}
}
