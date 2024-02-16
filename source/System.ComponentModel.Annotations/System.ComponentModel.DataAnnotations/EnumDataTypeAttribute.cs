using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class EnumDataTypeAttribute : DataTypeAttribute
{
	public Type EnumType { get; }

	public EnumDataTypeAttribute(Type enumType)
		: base("Enumeration")
	{
		EnumType = enumType;
	}

	public override bool IsValid(object? value)
	{
		if (EnumType == null)
		{
			throw new InvalidOperationException(System.SR.EnumDataTypeAttribute_TypeCannotBeNull);
		}
		if (!EnumType.IsEnum)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.EnumDataTypeAttribute_TypeNeedsToBeAnEnum, EnumType.FullName));
		}
		if (value == null)
		{
			return true;
		}
		string text = value as string;
		if (text != null && text.Length == 0)
		{
			return true;
		}
		Type type = value.GetType();
		if (type.IsEnum && EnumType != type)
		{
			return false;
		}
		if (!type.IsValueType && type != typeof(string))
		{
			return false;
		}
		if (type == typeof(bool) || type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(char))
		{
			return false;
		}
		object obj;
		if (type.IsEnum)
		{
			obj = value;
		}
		else
		{
			try
			{
				obj = ((text != null) ? Enum.Parse(EnumType, text, ignoreCase: false) : Enum.ToObject(EnumType, value));
			}
			catch (ArgumentException)
			{
				return false;
			}
		}
		if (IsEnumTypeInFlagsMode(EnumType))
		{
			string underlyingTypeValueString = GetUnderlyingTypeValueString(EnumType, obj);
			string value2 = obj.ToString();
			return !underlyingTypeValueString.Equals(value2);
		}
		return Enum.IsDefined(EnumType, obj);
	}

	private static bool IsEnumTypeInFlagsMode(Type enumType)
	{
		return enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
	}

	private static string GetUnderlyingTypeValueString(Type enumType, object enumValue)
	{
		return Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumType), CultureInfo.InvariantCulture).ToString();
	}
}
