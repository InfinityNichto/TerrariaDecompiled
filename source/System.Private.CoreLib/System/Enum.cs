using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class Enum : ValueType, IComparable, IFormattable, IConvertible
{
	internal sealed class EnumInfo
	{
		public readonly bool HasFlagsAttribute;

		public readonly ulong[] Values;

		public readonly string[] Names;

		public EnumInfo(bool hasFlagsAttribute, ulong[] values, string[] names)
		{
			HasFlagsAttribute = hasFlagsAttribute;
			Values = values;
			Names = names;
		}
	}

	private const char EnumSeparatorChar = ',';

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public override extern bool Equals([NotNullWhen(true)] object? obj);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalBoxEnum(RuntimeType enumType, long value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern CorElementType InternalGetCorElementType();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool InternalHasFlag(Enum flags);

	private static EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
	{
		EnumInfo enumInfo = enumType.GenericCache as EnumInfo;
		if (enumInfo == null || (getNames && enumInfo.Names == null))
		{
			ulong[] o = null;
			string[] o2 = null;
			RuntimeTypeHandle rth = enumType.GetTypeHandleInternal();
			GetEnumValuesAndNames(new QCallTypeHandle(ref rth), ObjectHandleOnStack.Create(ref o), ObjectHandleOnStack.Create(ref o2), getNames ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
			bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
			enumInfo = (EnumInfo)(enumType.GenericCache = new EnumInfo(hasFlagsAttribute, o, o2));
		}
		return enumInfo;
	}

	private string ValueToString()
	{
		ref byte rawData = ref this.GetRawData();
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_U1 => rawData.ToString(), 
			CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref rawData).ToString(), 
			CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref rawData).ToString(), 
			_ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType), 
		};
	}

	private string ValueToHexString()
	{
		ref byte rawData = ref this.GetRawData();
		Span<byte> destination = stackalloc byte[8];
		int length;
		switch (InternalGetCorElementType())
		{
		case CorElementType.ELEMENT_TYPE_I1:
		case CorElementType.ELEMENT_TYPE_U1:
			destination[0] = rawData;
			length = 1;
			break;
		case CorElementType.ELEMENT_TYPE_BOOLEAN:
			if (rawData == 0)
			{
				return "00";
			}
			return "01";
		case CorElementType.ELEMENT_TYPE_CHAR:
		case CorElementType.ELEMENT_TYPE_I2:
		case CorElementType.ELEMENT_TYPE_U2:
			BinaryPrimitives.WriteUInt16BigEndian(destination, Unsafe.As<byte, ushort>(ref rawData));
			length = 2;
			break;
		case CorElementType.ELEMENT_TYPE_I4:
		case CorElementType.ELEMENT_TYPE_U4:
			BinaryPrimitives.WriteUInt32BigEndian(destination, Unsafe.As<byte, uint>(ref rawData));
			length = 4;
			break;
		case CorElementType.ELEMENT_TYPE_I8:
		case CorElementType.ELEMENT_TYPE_U8:
			BinaryPrimitives.WriteUInt64BigEndian(destination, Unsafe.As<byte, ulong>(ref rawData));
			length = 8;
			break;
		default:
			throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
		}
		return HexConverter.ToString(destination.Slice(0, length));
	}

	private static string ValueToHexString(object value)
	{
		return Convert.GetTypeCode(value) switch
		{
			TypeCode.SByte => ((byte)(sbyte)value).ToString("X2", null), 
			TypeCode.Byte => ((byte)value).ToString("X2", null), 
			TypeCode.Boolean => ((bool)value) ? "01" : "00", 
			TypeCode.Int16 => ((ushort)(short)value).ToString("X4", null), 
			TypeCode.UInt16 => ((ushort)value).ToString("X4", null), 
			TypeCode.Char => ((ushort)(char)value).ToString("X4", null), 
			TypeCode.UInt32 => ((uint)value).ToString("X8", null), 
			TypeCode.Int32 => ((uint)(int)value).ToString("X8", null), 
			TypeCode.UInt64 => ((ulong)value).ToString("X16", null), 
			TypeCode.Int64 => ((ulong)(long)value).ToString("X16", null), 
			_ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType), 
		};
	}

	internal static string GetEnumName(RuntimeType enumType, ulong ulValue)
	{
		return GetEnumName(GetEnumInfo(enumType), ulValue);
	}

	private static string GetEnumName(EnumInfo enumInfo, ulong ulValue)
	{
		int num = Array.BinarySearch(enumInfo.Values, ulValue);
		if (num >= 0)
		{
			return enumInfo.Names[num];
		}
		return null;
	}

	private static string InternalFormat(RuntimeType enumType, ulong value)
	{
		EnumInfo enumInfo = GetEnumInfo(enumType);
		if (!enumInfo.HasFlagsAttribute)
		{
			return GetEnumName(enumInfo, value);
		}
		return InternalFlagsFormat(enumInfo, value);
	}

	private static string InternalFlagsFormat(RuntimeType enumType, ulong result)
	{
		return InternalFlagsFormat(GetEnumInfo(enumType), result);
	}

	private static string InternalFlagsFormat(EnumInfo enumInfo, ulong resultValue)
	{
		string[] names = enumInfo.Names;
		ulong[] values = enumInfo.Values;
		if (resultValue == 0L)
		{
			if (values.Length == 0 || values[0] != 0L)
			{
				return "0";
			}
			return names[0];
		}
		Span<int> span = stackalloc int[64];
		int num;
		for (num = values.Length - 1; num >= 0; num--)
		{
			if (values[num] == resultValue)
			{
				return names[num];
			}
			if (values[num] < resultValue)
			{
				break;
			}
		}
		int num2 = 0;
		int num3 = 0;
		while (num >= 0)
		{
			ulong num4 = values[num];
			if (num == 0 && num4 == 0L)
			{
				break;
			}
			if ((resultValue & num4) == num4)
			{
				resultValue -= num4;
				span[num3++] = num;
				num2 = checked(num2 + names[num].Length);
			}
			num--;
		}
		if (resultValue != 0L)
		{
			return null;
		}
		string text = string.FastAllocateString(checked(num2 + 2 * (num3 - 1)));
		Span<char> destination = new Span<char>(ref text.GetRawStringData(), text.Length);
		string text2 = names[span[--num3]];
		text2.CopyTo(destination);
		destination = destination.Slice(text2.Length);
		while (--num3 >= 0)
		{
			destination[0] = ',';
			destination[1] = ' ';
			destination = destination.Slice(2);
			text2 = names[span[num3]];
			text2.CopyTo(destination);
			destination = destination.Slice(text2.Length);
		}
		return text;
	}

	internal static ulong ToUInt64(object value)
	{
		return Convert.GetTypeCode(value) switch
		{
			TypeCode.SByte => (ulong)(sbyte)value, 
			TypeCode.Byte => (byte)value, 
			TypeCode.Boolean => (ulong)(((bool)value) ? 1 : 0), 
			TypeCode.Int16 => (ulong)(short)value, 
			TypeCode.UInt16 => (ushort)value, 
			TypeCode.Char => (char)value, 
			TypeCode.UInt32 => (uint)value, 
			TypeCode.Int32 => (ulong)(int)value, 
			TypeCode.UInt64 => (ulong)value, 
			TypeCode.Int64 => (ulong)(long)value, 
			_ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType), 
		};
	}

	private static ulong ToUInt64<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		return Type.GetTypeCode(typeof(TEnum)) switch
		{
			TypeCode.SByte => (ulong)Unsafe.As<TEnum, sbyte>(ref value), 
			TypeCode.Byte => Unsafe.As<TEnum, byte>(ref value), 
			TypeCode.Boolean => (ulong)(Unsafe.As<TEnum, bool>(ref value) ? 1 : 0), 
			TypeCode.Int16 => (ulong)Unsafe.As<TEnum, short>(ref value), 
			TypeCode.UInt16 => Unsafe.As<TEnum, ushort>(ref value), 
			TypeCode.Char => Unsafe.As<TEnum, char>(ref value), 
			TypeCode.UInt32 => Unsafe.As<TEnum, uint>(ref value), 
			TypeCode.Int32 => (ulong)Unsafe.As<TEnum, int>(ref value), 
			TypeCode.UInt64 => Unsafe.As<TEnum, ulong>(ref value), 
			TypeCode.Int64 => (ulong)Unsafe.As<TEnum, long>(ref value), 
			_ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType), 
		};
	}

	public static string? GetName<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		return GetEnumName((RuntimeType)typeof(TEnum), ToUInt64(value));
	}

	public static string? GetName(Type enumType, object value)
	{
		if ((object)enumType == null)
		{
			throw new ArgumentNullException("enumType");
		}
		return enumType.GetEnumName(value);
	}

	public static string[] GetNames<TEnum>() where TEnum : struct, Enum
	{
		return new ReadOnlySpan<string>(InternalGetNames((RuntimeType)typeof(TEnum))).ToArray();
	}

	public static string[] GetNames(Type enumType)
	{
		if ((object)enumType == null)
		{
			throw new ArgumentNullException("enumType");
		}
		return enumType.GetEnumNames();
	}

	internal static string[] InternalGetNames(RuntimeType enumType)
	{
		return GetEnumInfo(enumType).Names;
	}

	public static Type GetUnderlyingType(Type enumType)
	{
		if (enumType == null)
		{
			throw new ArgumentNullException("enumType");
		}
		return enumType.GetEnumUnderlyingType();
	}

	public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
	{
		return (TEnum[])GetValues(typeof(TEnum));
	}

	public static Array GetValues(Type enumType)
	{
		if ((object)enumType == null)
		{
			throw new ArgumentNullException("enumType");
		}
		return enumType.GetEnumValues();
	}

	[Intrinsic]
	public bool HasFlag(Enum flag)
	{
		if (flag == null)
		{
			throw new ArgumentNullException("flag");
		}
		if (!GetType().IsEquivalentTo(flag.GetType()))
		{
			throw new ArgumentException(SR.Format(SR.Argument_EnumTypeDoesNotMatch, flag.GetType(), GetType()));
		}
		return InternalHasFlag(flag);
	}

	internal static ulong[] InternalGetValues(RuntimeType enumType)
	{
		return GetEnumInfo(enumType, getNames: false).Values;
	}

	public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		ulong[] array = InternalGetValues(enumType);
		ulong value2 = ToUInt64(value);
		return Array.BinarySearch(array, value2) >= 0;
	}

	public static bool IsDefined(Type enumType, object value)
	{
		if ((object)enumType == null)
		{
			throw new ArgumentNullException("enumType");
		}
		return enumType.IsEnumDefined(value);
	}

	public static object Parse(Type enumType, string value)
	{
		return Parse(enumType, value, ignoreCase: false);
	}

	public static object Parse(Type enumType, ReadOnlySpan<char> value)
	{
		return Parse(enumType, value, ignoreCase: false);
	}

	public static object Parse(Type enumType, string value, bool ignoreCase)
	{
		object result;
		bool flag = TryParse(enumType, value, ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static object Parse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase)
	{
		object result;
		bool flag = TryParse(enumType, value, ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static TEnum Parse<TEnum>(string value) where TEnum : struct
	{
		return Parse<TEnum>(value, ignoreCase: false);
	}

	public static TEnum Parse<TEnum>(ReadOnlySpan<char> value) where TEnum : struct
	{
		return Parse<TEnum>(value, ignoreCase: false);
	}

	public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct
	{
		TEnum result;
		bool flag = TryParse<TEnum>(value, ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static TEnum Parse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase) where TEnum : struct
	{
		TEnum result;
		bool flag = TryParse<TEnum>(value, ignoreCase, throwOnFailure: true, out result);
		return result;
	}

	public static bool TryParse(Type enumType, string? value, out object? result)
	{
		return TryParse(enumType, value, ignoreCase: false, out result);
	}

	public static bool TryParse(Type enumType, ReadOnlySpan<char> value, out object? result)
	{
		return TryParse(enumType, value, ignoreCase: false, out result);
	}

	public static bool TryParse(Type enumType, string? value, bool ignoreCase, out object? result)
	{
		return TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);
	}

	public static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, out object? result)
	{
		return TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);
	}

	private static bool TryParse(Type enumType, string value, bool ignoreCase, bool throwOnFailure, out object result)
	{
		if (value == null)
		{
			if (throwOnFailure)
			{
				throw new ArgumentNullException("value");
			}
			result = null;
			return false;
		}
		return TryParse(enumType, value.AsSpan(), ignoreCase, throwOnFailure, out result);
	}

	private static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out object result)
	{
		RuntimeType runtimeType = ValidateRuntimeType(enumType);
		value = value.TrimStart();
		if (value.Length == 0)
		{
			if (throwOnFailure)
			{
				throw new ArgumentException(SR.Arg_MustContainEnumInfo, "value");
			}
			result = null;
			return false;
		}
		int result3;
		uint result4;
		switch (Type.GetTypeCode(runtimeType))
		{
		case TypeCode.SByte:
		{
			bool flag = TryParseInt32Enum(runtimeType, value, -128, 127, ignoreCase, throwOnFailure, TypeCode.SByte, out result3);
			result = (flag ? InternalBoxEnum(runtimeType, result3) : null);
			return flag;
		}
		case TypeCode.Int16:
		{
			bool flag = TryParseInt32Enum(runtimeType, value, -32768, 32767, ignoreCase, throwOnFailure, TypeCode.Int16, out result3);
			result = (flag ? InternalBoxEnum(runtimeType, result3) : null);
			return flag;
		}
		case TypeCode.Int32:
		{
			bool flag = TryParseInt32Enum(runtimeType, value, int.MinValue, int.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int32, out result3);
			result = (flag ? InternalBoxEnum(runtimeType, result3) : null);
			return flag;
		}
		case TypeCode.Byte:
		{
			bool flag = TryParseUInt32Enum(runtimeType, value, 255u, ignoreCase, throwOnFailure, TypeCode.Byte, out result4);
			result = (flag ? InternalBoxEnum(runtimeType, result4) : null);
			return flag;
		}
		case TypeCode.UInt16:
		{
			bool flag = TryParseUInt32Enum(runtimeType, value, 65535u, ignoreCase, throwOnFailure, TypeCode.UInt16, out result4);
			result = (flag ? InternalBoxEnum(runtimeType, result4) : null);
			return flag;
		}
		case TypeCode.UInt32:
		{
			bool flag = TryParseUInt32Enum(runtimeType, value, uint.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt32, out result4);
			result = (flag ? InternalBoxEnum(runtimeType, result4) : null);
			return flag;
		}
		case TypeCode.Int64:
		{
			long result5;
			bool flag = TryParseInt64Enum(runtimeType, value, ignoreCase, throwOnFailure, out result5);
			result = (flag ? InternalBoxEnum(runtimeType, result5) : null);
			return flag;
		}
		case TypeCode.UInt64:
		{
			ulong result2;
			bool flag = TryParseUInt64Enum(runtimeType, value, ignoreCase, throwOnFailure, out result2);
			result = (flag ? InternalBoxEnum(runtimeType, (long)result2) : null);
			return flag;
		}
		default:
			return TryParseRareEnum(runtimeType, value, ignoreCase, throwOnFailure, out result);
		}
	}

	public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, out TEnum result) where TEnum : struct
	{
		return TryParse<TEnum>(value, ignoreCase: false, out result);
	}

	public static bool TryParse<TEnum>(ReadOnlySpan<char> value, out TEnum result) where TEnum : struct
	{
		return TryParse<TEnum>(value, ignoreCase: false, out result);
	}

	public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, bool ignoreCase, out TEnum result) where TEnum : struct
	{
		return TryParse<TEnum>(value, ignoreCase, throwOnFailure: false, out result);
	}

	public static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, out TEnum result) where TEnum : struct
	{
		return TryParse<TEnum>(value, ignoreCase, throwOnFailure: false, out result);
	}

	private static bool TryParse<TEnum>(string value, bool ignoreCase, bool throwOnFailure, out TEnum result) where TEnum : struct
	{
		if (value == null)
		{
			if (throwOnFailure)
			{
				throw new ArgumentNullException("value");
			}
			result = default(TEnum);
			return false;
		}
		return TryParse<TEnum>(value.AsSpan(), ignoreCase, throwOnFailure, out result);
	}

	private static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TEnum result) where TEnum : struct
	{
		if (!typeof(TEnum).IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "TEnum");
		}
		value = value.TrimStart();
		if (value.Length == 0)
		{
			if (throwOnFailure)
			{
				throw new ArgumentException(SR.Arg_MustContainEnumInfo, "value");
			}
			result = default(TEnum);
			return false;
		}
		RuntimeType enumType = (RuntimeType)typeof(TEnum);
		int result3;
		uint result6;
		switch (Type.GetTypeCode(typeof(TEnum)))
		{
		case TypeCode.SByte:
		{
			bool flag = TryParseInt32Enum(enumType, value, -128, 127, ignoreCase, throwOnFailure, TypeCode.SByte, out result3);
			sbyte source2 = (sbyte)result3;
			result = Unsafe.As<sbyte, TEnum>(ref source2);
			return flag;
		}
		case TypeCode.Int16:
		{
			bool flag = TryParseInt32Enum(enumType, value, -32768, 32767, ignoreCase, throwOnFailure, TypeCode.Int16, out result3);
			short source = (short)result3;
			result = Unsafe.As<short, TEnum>(ref source);
			return flag;
		}
		case TypeCode.Int32:
		{
			bool flag = TryParseInt32Enum(enumType, value, int.MinValue, int.MaxValue, ignoreCase, throwOnFailure, TypeCode.Int32, out result3);
			result = Unsafe.As<int, TEnum>(ref result3);
			return flag;
		}
		case TypeCode.Byte:
		{
			bool flag = TryParseUInt32Enum(enumType, value, 255u, ignoreCase, throwOnFailure, TypeCode.Byte, out result6);
			byte source4 = (byte)result6;
			result = Unsafe.As<byte, TEnum>(ref source4);
			return flag;
		}
		case TypeCode.UInt16:
		{
			bool flag = TryParseUInt32Enum(enumType, value, 65535u, ignoreCase, throwOnFailure, TypeCode.UInt16, out result6);
			ushort source3 = (ushort)result6;
			result = Unsafe.As<ushort, TEnum>(ref source3);
			return flag;
		}
		case TypeCode.UInt32:
		{
			bool flag = TryParseUInt32Enum(enumType, value, uint.MaxValue, ignoreCase, throwOnFailure, TypeCode.UInt32, out result6);
			result = Unsafe.As<uint, TEnum>(ref result6);
			return flag;
		}
		case TypeCode.Int64:
		{
			long result5;
			bool flag = TryParseInt64Enum(enumType, value, ignoreCase, throwOnFailure, out result5);
			result = Unsafe.As<long, TEnum>(ref result5);
			return flag;
		}
		case TypeCode.UInt64:
		{
			ulong result4;
			bool flag = TryParseUInt64Enum(enumType, value, ignoreCase, throwOnFailure, out result4);
			result = Unsafe.As<ulong, TEnum>(ref result4);
			return flag;
		}
		default:
		{
			object result2;
			bool flag = TryParseRareEnum(enumType, value, ignoreCase, throwOnFailure, out result2);
			result = (flag ? ((TEnum)result2) : default(TEnum));
			return flag;
		}
		}
	}

	private static bool TryParseInt32Enum(RuntimeType enumType, ReadOnlySpan<char> value, int minInclusive, int maxInclusive, bool ignoreCase, bool throwOnFailure, TypeCode type, out int result)
	{
		Number.ParsingStatus parsingStatus = Number.ParsingStatus.OK;
		if (StartsNumber(value[0]))
		{
			parsingStatus = Number.TryParseInt32IntegerStyle(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture.NumberFormat, out result);
			if (parsingStatus == Number.ParsingStatus.OK)
			{
				if ((uint)(result - minInclusive) <= (uint)(maxInclusive - minInclusive))
				{
					return true;
				}
				parsingStatus = Number.ParsingStatus.Overflow;
			}
		}
		ulong result2;
		if (parsingStatus == Number.ParsingStatus.Overflow)
		{
			if (throwOnFailure)
			{
				Number.ThrowOverflowException(type);
			}
		}
		else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out result2))
		{
			result = (int)result2;
			return true;
		}
		result = 0;
		return false;
	}

	private static bool TryParseUInt32Enum(RuntimeType enumType, ReadOnlySpan<char> value, uint maxInclusive, bool ignoreCase, bool throwOnFailure, TypeCode type, out uint result)
	{
		Number.ParsingStatus parsingStatus = Number.ParsingStatus.OK;
		if (StartsNumber(value[0]))
		{
			parsingStatus = Number.TryParseUInt32IntegerStyle(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture.NumberFormat, out result);
			if (parsingStatus == Number.ParsingStatus.OK)
			{
				if (result <= maxInclusive)
				{
					return true;
				}
				parsingStatus = Number.ParsingStatus.Overflow;
			}
		}
		ulong result2;
		if (parsingStatus == Number.ParsingStatus.Overflow)
		{
			if (throwOnFailure)
			{
				Number.ThrowOverflowException(type);
			}
		}
		else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out result2))
		{
			result = (uint)result2;
			return true;
		}
		result = 0u;
		return false;
	}

	private static bool TryParseInt64Enum(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out long result)
	{
		Number.ParsingStatus parsingStatus = Number.ParsingStatus.OK;
		if (StartsNumber(value[0]))
		{
			parsingStatus = Number.TryParseInt64IntegerStyle(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture.NumberFormat, out result);
			if (parsingStatus == Number.ParsingStatus.OK)
			{
				return true;
			}
		}
		ulong result2;
		if (parsingStatus == Number.ParsingStatus.Overflow)
		{
			if (throwOnFailure)
			{
				Number.ThrowOverflowException(TypeCode.Int64);
			}
		}
		else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out result2))
		{
			result = (long)result2;
			return true;
		}
		result = 0L;
		return false;
	}

	private static bool TryParseUInt64Enum(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out ulong result)
	{
		Number.ParsingStatus parsingStatus = Number.ParsingStatus.OK;
		if (StartsNumber(value[0]))
		{
			parsingStatus = Number.TryParseUInt64IntegerStyle(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture.NumberFormat, out result);
			if (parsingStatus == Number.ParsingStatus.OK)
			{
				return true;
			}
		}
		if (parsingStatus == Number.ParsingStatus.Overflow)
		{
			if (throwOnFailure)
			{
				Number.ThrowOverflowException(TypeCode.UInt64);
			}
		}
		else if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out result))
		{
			return true;
		}
		result = 0uL;
		return false;
	}

	private static bool TryParseRareEnum(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out object result)
	{
		if (StartsNumber(value[0]))
		{
			Type underlyingType = GetUnderlyingType(enumType);
			try
			{
				result = ToObject(enumType, Convert.ChangeType(value.ToString(), underlyingType, CultureInfo.InvariantCulture));
				return true;
			}
			catch (FormatException)
			{
			}
			catch when (!throwOnFailure)
			{
				result = null;
				return false;
			}
		}
		if (TryParseByName(enumType, value, ignoreCase, throwOnFailure, out var result2))
		{
			try
			{
				result = ToObject(enumType, result2);
				return true;
			}
			catch when (!throwOnFailure)
			{
			}
		}
		result = null;
		return false;
	}

	private static bool TryParseByName(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out ulong result)
	{
		ReadOnlySpan<char> readOnlySpan = value;
		EnumInfo enumInfo = GetEnumInfo(enumType);
		string[] names = enumInfo.Names;
		ulong[] values = enumInfo.Values;
		bool flag = true;
		ulong num = 0uL;
		while (value.Length > 0)
		{
			int num2 = value.IndexOf(',');
			ReadOnlySpan<char> span;
			if (num2 == -1)
			{
				span = value.Trim();
				value = default(ReadOnlySpan<char>);
			}
			else
			{
				if (num2 == value.Length - 1)
				{
					flag = false;
					break;
				}
				span = value.Slice(0, num2).Trim();
				value = value.Slice(num2 + 1);
			}
			bool flag2 = false;
			if (ignoreCase)
			{
				for (int i = 0; i < names.Length; i++)
				{
					if (span.EqualsOrdinalIgnoreCase(names[i]))
					{
						num |= values[i];
						flag2 = true;
						break;
					}
				}
			}
			else
			{
				for (int j = 0; j < names.Length; j++)
				{
					if (span.EqualsOrdinal(names[j]))
					{
						num |= values[j];
						flag2 = true;
						break;
					}
				}
			}
			if (!flag2)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			result = num;
			return true;
		}
		if (throwOnFailure)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumValueNotFound, readOnlySpan.ToString()));
		}
		result = 0uL;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool StartsNumber(char c)
	{
		if (!char.IsInRange(c, '0', '9') && c != '-')
		{
			return c == '+';
		}
		return true;
	}

	public static object ToObject(Type enumType, object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return Convert.GetTypeCode(value) switch
		{
			TypeCode.Int32 => ToObject(enumType, (int)value), 
			TypeCode.SByte => ToObject(enumType, (sbyte)value), 
			TypeCode.Int16 => ToObject(enumType, (short)value), 
			TypeCode.Int64 => ToObject(enumType, (long)value), 
			TypeCode.UInt32 => ToObject(enumType, (uint)value), 
			TypeCode.Byte => ToObject(enumType, (byte)value), 
			TypeCode.UInt16 => ToObject(enumType, (ushort)value), 
			TypeCode.UInt64 => ToObject(enumType, (ulong)value), 
			TypeCode.Char => ToObject(enumType, (char)value), 
			TypeCode.Boolean => ToObject(enumType, (bool)value), 
			_ => throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, "value"), 
		};
	}

	public static string Format(Type enumType, object value, string format)
	{
		RuntimeType enumType2 = ValidateRuntimeType(enumType);
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (format == null)
		{
			throw new ArgumentNullException("format");
		}
		Type type = value.GetType();
		if (type.IsEnum)
		{
			if (!type.IsEquivalentTo(enumType))
			{
				throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, type, enumType));
			}
			if (format.Length != 1)
			{
				throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
			}
			return ((Enum)value).ToString(format);
		}
		Type underlyingType = GetUnderlyingType(enumType);
		if (type != underlyingType)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType, type, underlyingType));
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'G':
			case 'g':
				return InternalFormat(enumType2, ToUInt64(value)) ?? value.ToString();
			case 'D':
			case 'd':
				return value.ToString();
			case 'X':
			case 'x':
				return ValueToHexString(value);
			case 'F':
			case 'f':
				return InternalFlagsFormat(enumType2, ToUInt64(value)) ?? value.ToString();
			}
		}
		throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
	}

	internal object GetValue()
	{
		ref byte rawData = ref this.GetRawData();
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U1 => rawData, 
			CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref rawData), 
			CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref rawData), 
			CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref rawData), 
			CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref rawData), 
			CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref rawData), 
			CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref rawData), 
			CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref rawData), 
			CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref rawData), 
			CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref rawData), 
			_ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType), 
		};
	}

	private ulong ToUInt64()
	{
		ref byte rawData = ref this.GetRawData();
		switch (InternalGetCorElementType())
		{
		case CorElementType.ELEMENT_TYPE_I1:
			return (ulong)Unsafe.As<byte, sbyte>(ref rawData);
		case CorElementType.ELEMENT_TYPE_U1:
			return rawData;
		case CorElementType.ELEMENT_TYPE_BOOLEAN:
			if (rawData == 0)
			{
				return 0uL;
			}
			return 1uL;
		case CorElementType.ELEMENT_TYPE_I2:
			return (ulong)Unsafe.As<byte, short>(ref rawData);
		case CorElementType.ELEMENT_TYPE_CHAR:
		case CorElementType.ELEMENT_TYPE_U2:
			return Unsafe.As<byte, ushort>(ref rawData);
		case CorElementType.ELEMENT_TYPE_I4:
			return (ulong)Unsafe.As<byte, int>(ref rawData);
		case CorElementType.ELEMENT_TYPE_U4:
		case CorElementType.ELEMENT_TYPE_R4:
			return Unsafe.As<byte, uint>(ref rawData);
		case CorElementType.ELEMENT_TYPE_I8:
			return (ulong)Unsafe.As<byte, long>(ref rawData);
		case CorElementType.ELEMENT_TYPE_U8:
		case CorElementType.ELEMENT_TYPE_R8:
			return Unsafe.As<byte, ulong>(ref rawData);
		case CorElementType.ELEMENT_TYPE_I:
			return (ulong)(long)Unsafe.As<byte, IntPtr>(ref rawData);
		case CorElementType.ELEMENT_TYPE_U:
			return (ulong)Unsafe.As<byte, UIntPtr>(ref rawData);
		default:
			throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
		}
	}

	public override int GetHashCode()
	{
		ref byte rawData = ref this.GetRawData();
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U1 => rawData.GetHashCode(), 
			CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref rawData).GetHashCode(), 
			CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref rawData).GetHashCode(), 
			_ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType), 
		};
	}

	public override string ToString()
	{
		return InternalFormat((RuntimeType)GetType(), ToUInt64()) ?? ValueToString();
	}

	public int CompareTo(object? target)
	{
		if (target == this)
		{
			return 0;
		}
		if (target == null)
		{
			return 1;
		}
		if (GetType() != target.GetType())
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, target.GetType(), GetType()));
		}
		ref byte rawData = ref this.GetRawData();
		ref byte rawData2 = ref target.GetRawData();
		switch (InternalGetCorElementType())
		{
		case CorElementType.ELEMENT_TYPE_I1:
			return Unsafe.As<byte, sbyte>(ref rawData).CompareTo(Unsafe.As<byte, sbyte>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_BOOLEAN:
		case CorElementType.ELEMENT_TYPE_U1:
			return rawData.CompareTo(rawData2);
		case CorElementType.ELEMENT_TYPE_I2:
			return Unsafe.As<byte, short>(ref rawData).CompareTo(Unsafe.As<byte, short>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_CHAR:
		case CorElementType.ELEMENT_TYPE_U2:
			return Unsafe.As<byte, ushort>(ref rawData).CompareTo(Unsafe.As<byte, ushort>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_I4:
			return Unsafe.As<byte, int>(ref rawData).CompareTo(Unsafe.As<byte, int>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_U4:
			return Unsafe.As<byte, uint>(ref rawData).CompareTo(Unsafe.As<byte, uint>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_I8:
		case CorElementType.ELEMENT_TYPE_I:
			return Unsafe.As<byte, long>(ref rawData).CompareTo(Unsafe.As<byte, long>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_U8:
		case CorElementType.ELEMENT_TYPE_U:
			return Unsafe.As<byte, ulong>(ref rawData).CompareTo(Unsafe.As<byte, ulong>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_R4:
			return Unsafe.As<byte, float>(ref rawData).CompareTo(Unsafe.As<byte, float>(ref rawData2));
		case CorElementType.ELEMENT_TYPE_R8:
			return Unsafe.As<byte, double>(ref rawData).CompareTo(Unsafe.As<byte, double>(ref rawData2));
		default:
			throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
		}
	}

	[Obsolete("The provider argument is not used. Use ToString(String) instead.")]
	public string ToString(string? format, IFormatProvider? provider)
	{
		return ToString(format);
	}

	public string ToString(string? format)
	{
		if (string.IsNullOrEmpty(format))
		{
			return ToString();
		}
		if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'G':
			case 'g':
				return ToString();
			case 'D':
			case 'd':
				return ValueToString();
			case 'X':
			case 'x':
				return ValueToHexString();
			case 'F':
			case 'f':
				return InternalFlagsFormat((RuntimeType)GetType(), ToUInt64()) ?? ValueToString();
			}
		}
		throw new FormatException(SR.Format_InvalidEnumFormatSpecification);
	}

	[Obsolete("The provider argument is not used. Use ToString() instead.")]
	public string ToString(IFormatProvider? provider)
	{
		return ToString();
	}

	public TypeCode GetTypeCode()
	{
		return InternalGetCorElementType() switch
		{
			CorElementType.ELEMENT_TYPE_I1 => TypeCode.SByte, 
			CorElementType.ELEMENT_TYPE_U1 => TypeCode.Byte, 
			CorElementType.ELEMENT_TYPE_BOOLEAN => TypeCode.Boolean, 
			CorElementType.ELEMENT_TYPE_I2 => TypeCode.Int16, 
			CorElementType.ELEMENT_TYPE_U2 => TypeCode.UInt16, 
			CorElementType.ELEMENT_TYPE_CHAR => TypeCode.Char, 
			CorElementType.ELEMENT_TYPE_I4 => TypeCode.Int32, 
			CorElementType.ELEMENT_TYPE_U4 => TypeCode.UInt32, 
			CorElementType.ELEMENT_TYPE_I8 => TypeCode.Int64, 
			CorElementType.ELEMENT_TYPE_U8 => TypeCode.UInt64, 
			_ => throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType), 
		};
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(GetValue());
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return Convert.ToChar(GetValue());
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return Convert.ToSByte(GetValue());
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return Convert.ToByte(GetValue());
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return Convert.ToInt16(GetValue());
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return Convert.ToUInt16(GetValue());
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return Convert.ToInt32(GetValue());
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return Convert.ToUInt32(GetValue());
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return Convert.ToInt64(GetValue());
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return Convert.ToUInt64(GetValue());
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		return Convert.ToSingle(GetValue());
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return Convert.ToDouble(GetValue());
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return Convert.ToDecimal(GetValue());
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Enum", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, sbyte value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, short value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, int value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, byte value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, ushort value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, uint value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	public static object ToObject(Type enumType, long value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	[CLSCompliant(false)]
	public static object ToObject(Type enumType, ulong value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), (long)value);
	}

	private static object ToObject(Type enumType, char value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value);
	}

	private static object ToObject(Type enumType, bool value)
	{
		return InternalBoxEnum(ValidateRuntimeType(enumType), value ? 1 : 0);
	}

	private static RuntimeType ValidateRuntimeType(Type enumType)
	{
		if (enumType == null)
		{
			throw new ArgumentNullException("enumType");
		}
		if (!enumType.IsEnum)
		{
			throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
		}
		if (!(enumType is RuntimeType result))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "enumType");
		}
		return result;
	}
}
