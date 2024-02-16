using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary;

internal static class Converter
{
	internal static readonly Type s_typeofISerializable = typeof(ISerializable);

	internal static readonly Type s_typeofString = typeof(string);

	internal static readonly Type s_typeofConverter = typeof(Converter);

	internal static readonly Type s_typeofBoolean = typeof(bool);

	internal static readonly Type s_typeofByte = typeof(byte);

	internal static readonly Type s_typeofChar = typeof(char);

	internal static readonly Type s_typeofDecimal = typeof(decimal);

	internal static readonly Type s_typeofDouble = typeof(double);

	internal static readonly Type s_typeofInt16 = typeof(short);

	internal static readonly Type s_typeofInt32 = typeof(int);

	internal static readonly Type s_typeofInt64 = typeof(long);

	internal static readonly Type s_typeofSByte = typeof(sbyte);

	internal static readonly Type s_typeofSingle = typeof(float);

	internal static readonly Type s_typeofTimeSpan = typeof(TimeSpan);

	internal static readonly Type s_typeofDateTime = typeof(DateTime);

	internal static readonly Type s_typeofUInt16 = typeof(ushort);

	internal static readonly Type s_typeofUInt32 = typeof(uint);

	internal static readonly Type s_typeofUInt64 = typeof(ulong);

	internal static readonly Type s_typeofObject = typeof(object);

	internal static readonly Type s_typeofSystemVoid = typeof(void);

	internal static readonly Assembly s_urtAssembly = Assembly.Load("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");

	internal static readonly string s_urtAssemblyString = s_urtAssembly.FullName;

	internal static readonly Assembly s_urtAlternativeAssembly = s_typeofString.Assembly;

	internal static readonly string s_urtAlternativeAssemblyString = s_urtAlternativeAssembly.FullName;

	internal static readonly Type s_typeofTypeArray = typeof(Type[]);

	internal static readonly Type s_typeofObjectArray = typeof(object[]);

	internal static readonly Type s_typeofStringArray = typeof(string[]);

	internal static readonly Type s_typeofBooleanArray = typeof(bool[]);

	internal static readonly Type s_typeofByteArray = typeof(byte[]);

	internal static readonly Type s_typeofCharArray = typeof(char[]);

	internal static readonly Type s_typeofDecimalArray = typeof(decimal[]);

	internal static readonly Type s_typeofDoubleArray = typeof(double[]);

	internal static readonly Type s_typeofInt16Array = typeof(short[]);

	internal static readonly Type s_typeofInt32Array = typeof(int[]);

	internal static readonly Type s_typeofInt64Array = typeof(long[]);

	internal static readonly Type s_typeofSByteArray = typeof(sbyte[]);

	internal static readonly Type s_typeofSingleArray = typeof(float[]);

	internal static readonly Type s_typeofTimeSpanArray = typeof(TimeSpan[]);

	internal static readonly Type s_typeofDateTimeArray = typeof(DateTime[]);

	internal static readonly Type s_typeofUInt16Array = typeof(ushort[]);

	internal static readonly Type s_typeofUInt32Array = typeof(uint[]);

	internal static readonly Type s_typeofUInt64Array = typeof(ulong[]);

	internal static readonly Type s_typeofMarshalByRefObject = typeof(MarshalByRefObject);

	private static volatile Type[] s_typeA;

	private static volatile Type[] s_arrayTypeA;

	private static volatile string[] s_valueA;

	private static volatile TypeCode[] s_typeCodeA;

	private static volatile InternalPrimitiveTypeE[] s_codeA;

	internal static InternalPrimitiveTypeE ToCode(Type type)
	{
		if (!(type == null))
		{
			if (!type.IsPrimitive)
			{
				if ((object)type != s_typeofDateTime)
				{
					if ((object)type != s_typeofTimeSpan)
					{
						if ((object)type != s_typeofDecimal)
						{
							return InternalPrimitiveTypeE.Invalid;
						}
						return InternalPrimitiveTypeE.Decimal;
					}
					return InternalPrimitiveTypeE.TimeSpan;
				}
				return InternalPrimitiveTypeE.DateTime;
			}
			return ToPrimitiveTypeEnum(Type.GetTypeCode(type));
		}
		return ToPrimitiveTypeEnum(TypeCode.Empty);
	}

	internal static bool IsWriteAsByteArray(InternalPrimitiveTypeE code)
	{
		switch (code)
		{
		case InternalPrimitiveTypeE.Boolean:
		case InternalPrimitiveTypeE.Byte:
		case InternalPrimitiveTypeE.Char:
		case InternalPrimitiveTypeE.Double:
		case InternalPrimitiveTypeE.Int16:
		case InternalPrimitiveTypeE.Int32:
		case InternalPrimitiveTypeE.Int64:
		case InternalPrimitiveTypeE.SByte:
		case InternalPrimitiveTypeE.Single:
		case InternalPrimitiveTypeE.UInt16:
		case InternalPrimitiveTypeE.UInt32:
		case InternalPrimitiveTypeE.UInt64:
			return true;
		default:
			return false;
		}
	}

	internal static int TypeLength(InternalPrimitiveTypeE code)
	{
		return code switch
		{
			InternalPrimitiveTypeE.Boolean => 1, 
			InternalPrimitiveTypeE.Char => 2, 
			InternalPrimitiveTypeE.Byte => 1, 
			InternalPrimitiveTypeE.Double => 8, 
			InternalPrimitiveTypeE.Int16 => 2, 
			InternalPrimitiveTypeE.Int32 => 4, 
			InternalPrimitiveTypeE.Int64 => 8, 
			InternalPrimitiveTypeE.SByte => 1, 
			InternalPrimitiveTypeE.Single => 4, 
			InternalPrimitiveTypeE.UInt16 => 2, 
			InternalPrimitiveTypeE.UInt32 => 4, 
			InternalPrimitiveTypeE.UInt64 => 8, 
			_ => 0, 
		};
	}

	internal static Type ToArrayType(InternalPrimitiveTypeE code)
	{
		if (s_arrayTypeA == null)
		{
			InitArrayTypeA();
		}
		return s_arrayTypeA[(int)code];
	}

	private static void InitTypeA()
	{
		s_typeA = new Type[17]
		{
			null, s_typeofBoolean, s_typeofByte, s_typeofChar, null, s_typeofDecimal, s_typeofDouble, s_typeofInt16, s_typeofInt32, s_typeofInt64,
			s_typeofSByte, s_typeofSingle, s_typeofTimeSpan, s_typeofDateTime, s_typeofUInt16, s_typeofUInt32, s_typeofUInt64
		};
	}

	private static void InitArrayTypeA()
	{
		s_arrayTypeA = new Type[17]
		{
			null, s_typeofBooleanArray, s_typeofByteArray, s_typeofCharArray, null, s_typeofDecimalArray, s_typeofDoubleArray, s_typeofInt16Array, s_typeofInt32Array, s_typeofInt64Array,
			s_typeofSByteArray, s_typeofSingleArray, s_typeofTimeSpanArray, s_typeofDateTimeArray, s_typeofUInt16Array, s_typeofUInt32Array, s_typeofUInt64Array
		};
	}

	internal static Type ToType(InternalPrimitiveTypeE code)
	{
		if (s_typeA == null)
		{
			InitTypeA();
		}
		return s_typeA[(int)code];
	}

	internal static Array CreatePrimitiveArray(InternalPrimitiveTypeE code, int length)
	{
		return code switch
		{
			InternalPrimitiveTypeE.Boolean => new bool[length], 
			InternalPrimitiveTypeE.Byte => new byte[length], 
			InternalPrimitiveTypeE.Char => new char[length], 
			InternalPrimitiveTypeE.Decimal => new decimal[length], 
			InternalPrimitiveTypeE.Double => new double[length], 
			InternalPrimitiveTypeE.Int16 => new short[length], 
			InternalPrimitiveTypeE.Int32 => new int[length], 
			InternalPrimitiveTypeE.Int64 => new long[length], 
			InternalPrimitiveTypeE.SByte => new sbyte[length], 
			InternalPrimitiveTypeE.Single => new float[length], 
			InternalPrimitiveTypeE.TimeSpan => new TimeSpan[length], 
			InternalPrimitiveTypeE.DateTime => new DateTime[length], 
			InternalPrimitiveTypeE.UInt16 => new ushort[length], 
			InternalPrimitiveTypeE.UInt32 => new uint[length], 
			InternalPrimitiveTypeE.UInt64 => new ulong[length], 
			_ => null, 
		};
	}

	internal static bool IsPrimitiveArray(Type type, [NotNullWhen(true)] out object typeInformation)
	{
		if ((object)type == s_typeofBooleanArray)
		{
			typeInformation = InternalPrimitiveTypeE.Boolean;
		}
		else if ((object)type == s_typeofByteArray)
		{
			typeInformation = InternalPrimitiveTypeE.Byte;
		}
		else if ((object)type == s_typeofCharArray)
		{
			typeInformation = InternalPrimitiveTypeE.Char;
		}
		else if ((object)type == s_typeofDoubleArray)
		{
			typeInformation = InternalPrimitiveTypeE.Double;
		}
		else if ((object)type == s_typeofInt16Array)
		{
			typeInformation = InternalPrimitiveTypeE.Int16;
		}
		else if ((object)type == s_typeofInt32Array)
		{
			typeInformation = InternalPrimitiveTypeE.Int32;
		}
		else if ((object)type == s_typeofInt64Array)
		{
			typeInformation = InternalPrimitiveTypeE.Int64;
		}
		else if ((object)type == s_typeofSByteArray)
		{
			typeInformation = InternalPrimitiveTypeE.SByte;
		}
		else if ((object)type == s_typeofSingleArray)
		{
			typeInformation = InternalPrimitiveTypeE.Single;
		}
		else if ((object)type == s_typeofUInt16Array)
		{
			typeInformation = InternalPrimitiveTypeE.UInt16;
		}
		else if ((object)type == s_typeofUInt32Array)
		{
			typeInformation = InternalPrimitiveTypeE.UInt32;
		}
		else
		{
			if ((object)type != s_typeofUInt64Array)
			{
				typeInformation = null;
				return false;
			}
			typeInformation = InternalPrimitiveTypeE.UInt64;
		}
		return true;
	}

	private static void InitValueA()
	{
		s_valueA = new string[17]
		{
			null, "Boolean", "Byte", "Char", null, "Decimal", "Double", "Int16", "Int32", "Int64",
			"SByte", "Single", "TimeSpan", "DateTime", "UInt16", "UInt32", "UInt64"
		};
	}

	internal static string ToComType(InternalPrimitiveTypeE code)
	{
		if (s_valueA == null)
		{
			InitValueA();
		}
		return s_valueA[(int)code];
	}

	private static void InitTypeCodeA()
	{
		s_typeCodeA = new TypeCode[17]
		{
			TypeCode.Object,
			TypeCode.Boolean,
			TypeCode.Byte,
			TypeCode.Char,
			TypeCode.Empty,
			TypeCode.Decimal,
			TypeCode.Double,
			TypeCode.Int16,
			TypeCode.Int32,
			TypeCode.Int64,
			TypeCode.SByte,
			TypeCode.Single,
			TypeCode.Object,
			TypeCode.DateTime,
			TypeCode.UInt16,
			TypeCode.UInt32,
			TypeCode.UInt64
		};
	}

	internal static TypeCode ToTypeCode(InternalPrimitiveTypeE code)
	{
		if (s_typeCodeA == null)
		{
			InitTypeCodeA();
		}
		return s_typeCodeA[(int)code];
	}

	private static void InitCodeA()
	{
		s_codeA = new InternalPrimitiveTypeE[19]
		{
			InternalPrimitiveTypeE.Invalid,
			InternalPrimitiveTypeE.Invalid,
			InternalPrimitiveTypeE.Invalid,
			InternalPrimitiveTypeE.Boolean,
			InternalPrimitiveTypeE.Char,
			InternalPrimitiveTypeE.SByte,
			InternalPrimitiveTypeE.Byte,
			InternalPrimitiveTypeE.Int16,
			InternalPrimitiveTypeE.UInt16,
			InternalPrimitiveTypeE.Int32,
			InternalPrimitiveTypeE.UInt32,
			InternalPrimitiveTypeE.Int64,
			InternalPrimitiveTypeE.UInt64,
			InternalPrimitiveTypeE.Single,
			InternalPrimitiveTypeE.Double,
			InternalPrimitiveTypeE.Decimal,
			InternalPrimitiveTypeE.DateTime,
			InternalPrimitiveTypeE.Invalid,
			InternalPrimitiveTypeE.Invalid
		};
	}

	internal static InternalPrimitiveTypeE ToPrimitiveTypeEnum(TypeCode typeCode)
	{
		if (s_codeA == null)
		{
			InitCodeA();
		}
		return s_codeA[(int)typeCode];
	}

	internal static object FromString(string value, InternalPrimitiveTypeE code)
	{
		if (code == InternalPrimitiveTypeE.Invalid)
		{
			return value;
		}
		return Convert.ChangeType(value, ToTypeCode(code), CultureInfo.InvariantCulture);
	}
}
