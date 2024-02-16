using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics.Tracing;

internal static class Statics
{
	public static readonly TraceLoggingDataType IntPtrType;

	public static readonly TraceLoggingDataType UIntPtrType;

	public static readonly TraceLoggingDataType HexIntPtrType;

	public static byte[] MetadataForString(string name, int prefixSize, int suffixSize, int additionalSize)
	{
		CheckName(name);
		int num = Encoding.UTF8.GetByteCount(name) + 3 + prefixSize + suffixSize;
		byte[] array = new byte[num];
		ushort num2 = checked((ushort)(num + additionalSize));
		array[0] = (byte)num2;
		array[1] = (byte)(num2 >> 8);
		Encoding.UTF8.GetBytes(name, 0, name.Length, array, 2 + prefixSize);
		return array;
	}

	public static void EncodeTags(int tags, ref int pos, byte[] metadata)
	{
		int num = tags & 0xFFFFFFF;
		bool flag;
		do
		{
			byte b = (byte)((uint)(num >> 21) & 0x7Fu);
			flag = (num & 0x1FFFFF) != 0;
			b |= (byte)(flag ? 128u : 0u);
			num <<= 7;
			if (metadata != null)
			{
				metadata[pos] = b;
			}
			pos++;
		}
		while (flag);
	}

	public static byte Combine(int settingValue, byte defaultValue)
	{
		if ((byte)settingValue != settingValue)
		{
			return defaultValue;
		}
		return (byte)settingValue;
	}

	public static int Combine(int settingValue1, int settingValue2)
	{
		if ((byte)settingValue1 != settingValue1)
		{
			return settingValue2;
		}
		return settingValue1;
	}

	public static void CheckName(string name)
	{
		if (name != null && 0 <= name.IndexOf('\0'))
		{
			throw new ArgumentOutOfRangeException("name");
		}
	}

	public static bool ShouldOverrideFieldName(string fieldName)
	{
		if (fieldName.Length <= 2)
		{
			return fieldName[0] == '_';
		}
		return false;
	}

	public static TraceLoggingDataType MakeDataType(TraceLoggingDataType baseType, EventFieldFormat format)
	{
		return (TraceLoggingDataType)((int)(baseType & (TraceLoggingDataType)31) | ((int)format << 8));
	}

	public static TraceLoggingDataType Format8(EventFieldFormat format, TraceLoggingDataType native)
	{
		return format switch
		{
			EventFieldFormat.Default => native, 
			EventFieldFormat.String => TraceLoggingDataType.Char8, 
			EventFieldFormat.Boolean => TraceLoggingDataType.Boolean8, 
			EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt8, 
			_ => MakeDataType(native, format), 
		};
	}

	public static TraceLoggingDataType Format16(EventFieldFormat format, TraceLoggingDataType native)
	{
		return format switch
		{
			EventFieldFormat.Default => native, 
			EventFieldFormat.String => TraceLoggingDataType.Char16, 
			EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt16, 
			_ => MakeDataType(native, format), 
		};
	}

	public static TraceLoggingDataType Format32(EventFieldFormat format, TraceLoggingDataType native)
	{
		return format switch
		{
			EventFieldFormat.Default => native, 
			EventFieldFormat.Boolean => TraceLoggingDataType.Boolean32, 
			EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt32, 
			EventFieldFormat.HResult => TraceLoggingDataType.HResult, 
			_ => MakeDataType(native, format), 
		};
	}

	public static TraceLoggingDataType Format64(EventFieldFormat format, TraceLoggingDataType native)
	{
		return format switch
		{
			EventFieldFormat.Default => native, 
			EventFieldFormat.Hexadecimal => TraceLoggingDataType.HexInt64, 
			_ => MakeDataType(native, format), 
		};
	}

	public static TraceLoggingDataType FormatScalar(EventFieldFormat format, TraceLoggingDataType nativeFormat)
	{
		switch (nativeFormat)
		{
		case TraceLoggingDataType.Int8:
		case TraceLoggingDataType.UInt8:
		case TraceLoggingDataType.Boolean8:
			return Format8(format, nativeFormat);
		case TraceLoggingDataType.Int16:
		case TraceLoggingDataType.UInt16:
		case TraceLoggingDataType.Char16:
			return Format16(format, nativeFormat);
		case TraceLoggingDataType.Int32:
		case TraceLoggingDataType.UInt32:
		case TraceLoggingDataType.Float:
			return Format32(format, nativeFormat);
		case TraceLoggingDataType.Int64:
		case TraceLoggingDataType.UInt64:
		case TraceLoggingDataType.Double:
			return Format64(format, nativeFormat);
		default:
			return MakeDataType(nativeFormat, format);
		}
	}

	public static bool HasCustomAttribute(PropertyInfo propInfo, Type attributeType)
	{
		return propInfo.IsDefined(attributeType, inherit: false);
	}

	public static AttributeType GetCustomAttribute<AttributeType>(PropertyInfo propInfo) where AttributeType : Attribute
	{
		AttributeType result = null;
		object[] customAttributes = propInfo.GetCustomAttributes(typeof(AttributeType), inherit: false);
		if (customAttributes.Length != 0)
		{
			return (AttributeType)customAttributes[0];
		}
		return result;
	}

	public static AttributeType GetCustomAttribute<AttributeType>(Type type) where AttributeType : Attribute
	{
		AttributeType result = null;
		object[] customAttributes = type.GetCustomAttributes(typeof(AttributeType), inherit: false);
		if (customAttributes.Length != 0)
		{
			return (AttributeType)customAttributes[0];
		}
		return result;
	}

	public static Type FindEnumerableElementType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
	{
		Type type2 = null;
		if (IsGenericMatch(type, typeof(IEnumerable<>)))
		{
			type2 = type.GetGenericArguments()[0];
		}
		else
		{
			Type[] array = type.FindInterfaces(IsGenericMatch, typeof(IEnumerable<>));
			Type[] array2 = array;
			foreach (Type type3 in array2)
			{
				if (type2 != null)
				{
					type2 = null;
					break;
				}
				type2 = type3.GetGenericArguments()[0];
			}
		}
		return type2;
	}

	public static bool IsGenericMatch(Type type, object openType)
	{
		if (type.IsGenericType)
		{
			return type.GetGenericTypeDefinition() == (Type)openType;
		}
		return false;
	}

	[RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	public static TraceLoggingTypeInfo CreateDefaultTypeInfo(Type dataType, List<Type> recursionCheck)
	{
		if (recursionCheck.Contains(dataType))
		{
			throw new NotSupportedException(SR.EventSource_RecursiveTypeDefinition);
		}
		recursionCheck.Add(dataType);
		EventDataAttribute customAttribute = GetCustomAttribute<EventDataAttribute>(dataType);
		if (customAttribute != null || GetCustomAttribute<CompilerGeneratedAttribute>(dataType) != null || IsGenericMatch(dataType, typeof(KeyValuePair<, >)))
		{
			TypeAnalysis typeAnalysis = new TypeAnalysis(dataType, customAttribute, recursionCheck);
			return new InvokeTypeInfo(dataType, typeAnalysis);
		}
		if (dataType.IsArray)
		{
			Type elementType = dataType.GetElementType();
			if (elementType == typeof(bool))
			{
				return ScalarArrayTypeInfo.Boolean();
			}
			if (elementType == typeof(byte))
			{
				return ScalarArrayTypeInfo.Byte();
			}
			if (elementType == typeof(sbyte))
			{
				return ScalarArrayTypeInfo.SByte();
			}
			if (elementType == typeof(short))
			{
				return ScalarArrayTypeInfo.Int16();
			}
			if (elementType == typeof(ushort))
			{
				return ScalarArrayTypeInfo.UInt16();
			}
			if (elementType == typeof(int))
			{
				return ScalarArrayTypeInfo.Int32();
			}
			if (elementType == typeof(uint))
			{
				return ScalarArrayTypeInfo.UInt32();
			}
			if (elementType == typeof(long))
			{
				return ScalarArrayTypeInfo.Int64();
			}
			if (elementType == typeof(ulong))
			{
				return ScalarArrayTypeInfo.UInt64();
			}
			if (elementType == typeof(char))
			{
				return ScalarArrayTypeInfo.Char();
			}
			if (elementType == typeof(double))
			{
				return ScalarArrayTypeInfo.Double();
			}
			if (elementType == typeof(float))
			{
				return ScalarArrayTypeInfo.Single();
			}
			if (elementType == typeof(IntPtr))
			{
				return ScalarArrayTypeInfo.IntPtr();
			}
			if (elementType == typeof(UIntPtr))
			{
				return ScalarArrayTypeInfo.UIntPtr();
			}
			if (elementType == typeof(Guid))
			{
				return ScalarArrayTypeInfo.Guid();
			}
			return new ArrayTypeInfo(dataType, TraceLoggingTypeInfo.GetInstance(elementType, recursionCheck));
		}
		if (dataType.IsEnum)
		{
			dataType = Enum.GetUnderlyingType(dataType);
		}
		if (dataType == typeof(string))
		{
			return StringTypeInfo.Instance();
		}
		if (dataType == typeof(bool))
		{
			return ScalarTypeInfo.Boolean();
		}
		if (dataType == typeof(byte))
		{
			return ScalarTypeInfo.Byte();
		}
		if (dataType == typeof(sbyte))
		{
			return ScalarTypeInfo.SByte();
		}
		if (dataType == typeof(short))
		{
			return ScalarTypeInfo.Int16();
		}
		if (dataType == typeof(ushort))
		{
			return ScalarTypeInfo.UInt16();
		}
		if (dataType == typeof(int))
		{
			return ScalarTypeInfo.Int32();
		}
		if (dataType == typeof(uint))
		{
			return ScalarTypeInfo.UInt32();
		}
		if (dataType == typeof(long))
		{
			return ScalarTypeInfo.Int64();
		}
		if (dataType == typeof(ulong))
		{
			return ScalarTypeInfo.UInt64();
		}
		if (dataType == typeof(char))
		{
			return ScalarTypeInfo.Char();
		}
		if (dataType == typeof(double))
		{
			return ScalarTypeInfo.Double();
		}
		if (dataType == typeof(float))
		{
			return ScalarTypeInfo.Single();
		}
		if (dataType == typeof(DateTime))
		{
			return DateTimeTypeInfo.Instance();
		}
		if (dataType == typeof(decimal))
		{
			return DecimalTypeInfo.Instance();
		}
		if (dataType == typeof(IntPtr))
		{
			return ScalarTypeInfo.IntPtr();
		}
		if (dataType == typeof(UIntPtr))
		{
			return ScalarTypeInfo.UIntPtr();
		}
		if (dataType == typeof(Guid))
		{
			return ScalarTypeInfo.Guid();
		}
		if (dataType == typeof(TimeSpan))
		{
			return TimeSpanTypeInfo.Instance();
		}
		if (dataType == typeof(DateTimeOffset))
		{
			return DateTimeOffsetTypeInfo.Instance();
		}
		if (dataType == typeof(EmptyStruct))
		{
			return NullTypeInfo.Instance();
		}
		if (IsGenericMatch(dataType, typeof(Nullable<>)))
		{
			return new NullableTypeInfo(dataType, recursionCheck);
		}
		Type type = FindEnumerableElementType(dataType);
		if (type != null)
		{
			return new EnumerableTypeInfo(dataType, TraceLoggingTypeInfo.GetInstance(type, recursionCheck));
		}
		throw new ArgumentException(SR.Format(SR.EventSource_NonCompliantTypeError, dataType.Name));
	}

	static Statics()
	{
		if (IntPtr.Size != 8)
		{
		}
		IntPtrType = TraceLoggingDataType.Int64;
		if (IntPtr.Size != 8)
		{
		}
		UIntPtrType = TraceLoggingDataType.UInt64;
		if (IntPtr.Size != 8)
		{
		}
		HexIntPtrType = TraceLoggingDataType.HexInt64;
	}
}
