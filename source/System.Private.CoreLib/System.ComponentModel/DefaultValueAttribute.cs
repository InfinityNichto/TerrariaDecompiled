using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public class DefaultValueAttribute : Attribute
{
	private object _value;

	private static object s_convertFromInvariantString;

	public virtual object? Value => _value;

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	public DefaultValueAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string? value)
	{
		if (type == null)
		{
			return;
		}
		try
		{
			if (TryConvertFromInvariantString(type, value, out var conversionResult2))
			{
				_value = conversionResult2;
			}
			else if (type.IsSubclassOf(typeof(Enum)) && value != null)
			{
				_value = Enum.Parse(type, value, ignoreCase: true);
			}
			else if (type == typeof(TimeSpan) && value != null)
			{
				_value = TimeSpan.Parse(value);
			}
			else
			{
				_value = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
			}
			[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
			static bool TryConvertFromInvariantString([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type typeToConvert, string stringValue, out object conversionResult)
			{
				conversionResult = null;
				if (s_convertFromInvariantString == null)
				{
					MethodInfo methodInfo = Type.GetType("System.ComponentModel.TypeDescriptor, System.ComponentModel.TypeConverter", throwOnError: false)?.GetMethod("ConvertFromInvariantString", BindingFlags.Static | BindingFlags.NonPublic);
					Volatile.Write(ref s_convertFromInvariantString, (methodInfo == null) ? new object() : methodInfo.CreateDelegate(typeof(Func<Type, string, object>)));
				}
				if (!(s_convertFromInvariantString is Func<Type, string, object> func))
				{
					return false;
				}
				try
				{
					conversionResult = func(typeToConvert, stringValue);
				}
				catch
				{
					return false;
				}
				return true;
			}
		}
		catch
		{
		}
	}

	public DefaultValueAttribute(char value)
	{
		_value = value;
	}

	public DefaultValueAttribute(byte value)
	{
		_value = value;
	}

	public DefaultValueAttribute(short value)
	{
		_value = value;
	}

	public DefaultValueAttribute(int value)
	{
		_value = value;
	}

	public DefaultValueAttribute(long value)
	{
		_value = value;
	}

	public DefaultValueAttribute(float value)
	{
		_value = value;
	}

	public DefaultValueAttribute(double value)
	{
		_value = value;
	}

	public DefaultValueAttribute(bool value)
	{
		_value = value;
	}

	public DefaultValueAttribute(string? value)
	{
		_value = value;
	}

	public DefaultValueAttribute(object? value)
	{
		_value = value;
	}

	[CLSCompliant(false)]
	public DefaultValueAttribute(sbyte value)
	{
		_value = value;
	}

	[CLSCompliant(false)]
	public DefaultValueAttribute(ushort value)
	{
		_value = value;
	}

	[CLSCompliant(false)]
	public DefaultValueAttribute(uint value)
	{
		_value = value;
	}

	[CLSCompliant(false)]
	public DefaultValueAttribute(ulong value)
	{
		_value = value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (!(obj is DefaultValueAttribute defaultValueAttribute))
		{
			return false;
		}
		if (Value == null)
		{
			return defaultValueAttribute.Value == null;
		}
		return Value.Equals(defaultValueAttribute.Value);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	protected void SetValue(object? value)
	{
		_value = value;
	}
}
