using System.Collections.Generic;
using System.Text;

namespace System.Reflection;

public readonly struct CustomAttributeTypedArgument
{
	private readonly object _value;

	private readonly Type _argumentType;

	public Type ArgumentType => _argumentType;

	public object? Value => _value;

	private static Type CustomAttributeEncodingToType(CustomAttributeEncoding encodedType)
	{
		return encodedType switch
		{
			CustomAttributeEncoding.Enum => typeof(Enum), 
			CustomAttributeEncoding.Int32 => typeof(int), 
			CustomAttributeEncoding.String => typeof(string), 
			CustomAttributeEncoding.Type => typeof(Type), 
			CustomAttributeEncoding.Array => typeof(Array), 
			CustomAttributeEncoding.Char => typeof(char), 
			CustomAttributeEncoding.Boolean => typeof(bool), 
			CustomAttributeEncoding.SByte => typeof(sbyte), 
			CustomAttributeEncoding.Byte => typeof(byte), 
			CustomAttributeEncoding.Int16 => typeof(short), 
			CustomAttributeEncoding.UInt16 => typeof(ushort), 
			CustomAttributeEncoding.UInt32 => typeof(uint), 
			CustomAttributeEncoding.Int64 => typeof(long), 
			CustomAttributeEncoding.UInt64 => typeof(ulong), 
			CustomAttributeEncoding.Float => typeof(float), 
			CustomAttributeEncoding.Double => typeof(double), 
			CustomAttributeEncoding.Object => typeof(object), 
			_ => throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)encodedType), "encodedType"), 
		};
	}

	private unsafe static object EncodedValueToRawValue(long val, CustomAttributeEncoding encodedType)
	{
		return encodedType switch
		{
			CustomAttributeEncoding.Boolean => (byte)val != 0, 
			CustomAttributeEncoding.Char => (char)val, 
			CustomAttributeEncoding.Byte => (byte)val, 
			CustomAttributeEncoding.SByte => (sbyte)val, 
			CustomAttributeEncoding.Int16 => (short)val, 
			CustomAttributeEncoding.UInt16 => (ushort)val, 
			CustomAttributeEncoding.Int32 => (int)val, 
			CustomAttributeEncoding.UInt32 => (uint)val, 
			CustomAttributeEncoding.Int64 => val, 
			CustomAttributeEncoding.UInt64 => (ulong)val, 
			CustomAttributeEncoding.Float => *(float*)(&val), 
			CustomAttributeEncoding.Double => *(double*)(&val), 
			_ => throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)val), "val"), 
		};
	}

	private static RuntimeType ResolveType(RuntimeModule scope, string typeName)
	{
		RuntimeType typeByNameUsingCARules = RuntimeTypeHandle.GetTypeByNameUsingCARules(typeName, scope);
		if ((object)typeByNameUsingCARules == null)
		{
			throw new InvalidOperationException(SR.Format(SR.Arg_CATypeResolutionFailed, typeName));
		}
		return typeByNameUsingCARules;
	}

	internal CustomAttributeTypedArgument(RuntimeModule scope, CustomAttributeEncodedArgument encodedArg)
	{
		CustomAttributeEncoding encodedType = encodedArg.CustomAttributeType.EncodedType;
		switch (encodedType)
		{
		case CustomAttributeEncoding.Undefined:
			throw new ArgumentException(null, "encodedArg");
		case CustomAttributeEncoding.Enum:
			_argumentType = ResolveType(scope, encodedArg.CustomAttributeType.EnumName);
			_value = EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedArg.CustomAttributeType.EncodedEnumType);
			break;
		case CustomAttributeEncoding.String:
			_argumentType = typeof(string);
			_value = encodedArg.StringValue;
			break;
		case CustomAttributeEncoding.Type:
			_argumentType = typeof(Type);
			_value = null;
			if (encodedArg.StringValue != null)
			{
				_value = ResolveType(scope, encodedArg.StringValue);
			}
			break;
		case CustomAttributeEncoding.Array:
		{
			encodedType = encodedArg.CustomAttributeType.EncodedArrayType;
			Type type = ((encodedType != CustomAttributeEncoding.Enum) ? CustomAttributeEncodingToType(encodedType) : ResolveType(scope, encodedArg.CustomAttributeType.EnumName));
			_argumentType = type.MakeArrayType();
			if (encodedArg.ArrayValue == null)
			{
				_value = null;
				break;
			}
			CustomAttributeTypedArgument[] array = new CustomAttributeTypedArgument[encodedArg.ArrayValue.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new CustomAttributeTypedArgument(scope, encodedArg.ArrayValue[i]);
			}
			_value = Array.AsReadOnly(array);
			break;
		}
		default:
			_argumentType = CustomAttributeEncodingToType(encodedType);
			_value = EncodedValueToRawValue(encodedArg.PrimitiveValue, encodedType);
			break;
		}
	}

	public static bool operator ==(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CustomAttributeTypedArgument left, CustomAttributeTypedArgument right)
	{
		return !left.Equals(right);
	}

	public CustomAttributeTypedArgument(Type argumentType, object? value)
	{
		if ((object)argumentType == null)
		{
			throw new ArgumentNullException("argumentType");
		}
		_value = CanonicalizeValue(value);
		_argumentType = argumentType;
	}

	public CustomAttributeTypedArgument(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		_value = CanonicalizeValue(value);
		_argumentType = value.GetType();
	}

	public override string ToString()
	{
		return ToString(typed: false);
	}

	internal string ToString(bool typed)
	{
		if ((object)_argumentType == null)
		{
			return base.ToString();
		}
		if (ArgumentType.IsEnum)
		{
			if (typed)
			{
				return $"{Value}";
			}
			return $"({ArgumentType.FullName}){Value}";
		}
		if (Value == null)
		{
			if (!typed)
			{
				return "(" + ArgumentType.Name + ")null";
			}
			return "null";
		}
		if (ArgumentType == typeof(string))
		{
			return $"\"{Value}\"";
		}
		if (ArgumentType == typeof(char))
		{
			return $"'{Value}'";
		}
		if (ArgumentType == typeof(Type))
		{
			return "typeof(" + ((Type)Value).FullName + ")";
		}
		if (ArgumentType.IsArray)
		{
			IList<CustomAttributeTypedArgument> list = (IList<CustomAttributeTypedArgument>)Value;
			Type elementType = ArgumentType.GetElementType();
			Span<char> initialBuffer = stackalloc char[256];
			ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
			valueStringBuilder.Append("new ");
			valueStringBuilder.Append(elementType.IsEnum ? elementType.FullName : elementType.Name);
			valueStringBuilder.Append('[');
			int count = list.Count;
			valueStringBuilder.Append(count.ToString());
			valueStringBuilder.Append(']');
			for (int i = 0; i < count; i++)
			{
				if (i != 0)
				{
					valueStringBuilder.Append(", ");
				}
				valueStringBuilder.Append(list[i].ToString(elementType != typeof(object)));
			}
			valueStringBuilder.Append(" }");
			return valueStringBuilder.ToString();
		}
		if (typed)
		{
			return $"{Value}";
		}
		return $"({ArgumentType.Name}){Value}";
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object? obj)
	{
		return obj == (object)this;
	}

	private static object CanonicalizeValue(object value)
	{
		if (!(value is Enum @enum))
		{
			return value;
		}
		return @enum.GetValue();
	}
}
