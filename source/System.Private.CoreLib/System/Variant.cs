using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

internal struct Variant
{
	private object _objref;

	private long _data;

	private int _flags;

	internal const int CV_EMPTY = 0;

	internal const int CV_VOID = 1;

	internal const int CV_BOOLEAN = 2;

	internal const int CV_CHAR = 3;

	internal const int CV_I1 = 4;

	internal const int CV_U1 = 5;

	internal const int CV_I2 = 6;

	internal const int CV_U2 = 7;

	internal const int CV_I4 = 8;

	internal const int CV_U4 = 9;

	internal const int CV_I8 = 10;

	internal const int CV_U8 = 11;

	internal const int CV_R4 = 12;

	internal const int CV_R8 = 13;

	internal const int CV_STRING = 14;

	internal const int CV_PTR = 15;

	internal const int CV_DATETIME = 16;

	internal const int CV_TIMESPAN = 17;

	internal const int CV_OBJECT = 18;

	internal const int CV_DECIMAL = 19;

	internal const int CV_ENUM = 21;

	internal const int CV_MISSING = 22;

	internal const int CV_NULL = 23;

	internal const int CV_LAST = 24;

	internal const int TypeCodeBitMask = 65535;

	internal const int VTBitMask = -16777216;

	internal const int VTBitShift = 24;

	internal const int ArrayBitMask = 65536;

	internal const int EnumI1 = 1048576;

	internal const int EnumU1 = 2097152;

	internal const int EnumI2 = 3145728;

	internal const int EnumU2 = 4194304;

	internal const int EnumI4 = 5242880;

	internal const int EnumU4 = 6291456;

	internal const int EnumI8 = 7340032;

	internal const int EnumU8 = 8388608;

	internal const int EnumMask = 15728640;

	internal static readonly Type[] ClassTypes = new Type[23]
	{
		typeof(Empty),
		typeof(void),
		typeof(bool),
		typeof(char),
		typeof(sbyte),
		typeof(byte),
		typeof(short),
		typeof(ushort),
		typeof(int),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(float),
		typeof(double),
		typeof(string),
		typeof(void),
		typeof(DateTime),
		typeof(TimeSpan),
		typeof(object),
		typeof(decimal),
		typeof(object),
		typeof(Missing),
		typeof(DBNull)
	};

	internal static readonly Variant Empty;

	internal static readonly Variant Missing = new Variant(22, Type.Missing, 0L);

	internal static readonly Variant DBNull = new Variant(23, System.DBNull.Value, 0L);

	internal int CVType => _flags & 0xFFFF;

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal extern void SetFieldsObject(object val);

	internal Variant(int flags, object or, long data)
	{
		_flags = flags;
		_objref = or;
		_data = data;
	}

	public Variant(bool val)
	{
		_objref = null;
		_flags = 2;
		_data = (val ? 1 : 0);
	}

	public Variant(sbyte val)
	{
		_objref = null;
		_flags = 4;
		_data = val;
	}

	public Variant(byte val)
	{
		_objref = null;
		_flags = 5;
		_data = val;
	}

	public Variant(short val)
	{
		_objref = null;
		_flags = 6;
		_data = val;
	}

	public Variant(ushort val)
	{
		_objref = null;
		_flags = 7;
		_data = val;
	}

	public Variant(char val)
	{
		_objref = null;
		_flags = 3;
		_data = val;
	}

	public Variant(int val)
	{
		_objref = null;
		_flags = 8;
		_data = val;
	}

	public Variant(uint val)
	{
		_objref = null;
		_flags = 9;
		_data = val;
	}

	public Variant(long val)
	{
		_objref = null;
		_flags = 10;
		_data = val;
	}

	public Variant(ulong val)
	{
		_objref = null;
		_flags = 11;
		_data = (long)val;
	}

	public Variant(float val)
	{
		_objref = null;
		_flags = 12;
		_data = BitConverter.SingleToUInt32Bits(val);
	}

	public Variant(double val)
	{
		_objref = null;
		_flags = 13;
		_data = BitConverter.DoubleToInt64Bits(val);
	}

	public Variant(DateTime val)
	{
		_objref = null;
		_flags = 16;
		_data = val.Ticks;
	}

	public Variant(decimal val)
	{
		_objref = val;
		_flags = 19;
		_data = 0L;
	}

	public Variant(object obj)
	{
		_data = 0L;
		VarEnum varEnum = VarEnum.VT_EMPTY;
		if (obj is DateTime)
		{
			_objref = null;
			_flags = 16;
			_data = ((DateTime)obj).Ticks;
			return;
		}
		if (obj is string)
		{
			_flags = 14;
			_objref = obj;
			return;
		}
		if (obj == null)
		{
			this = Empty;
			return;
		}
		if (obj == System.DBNull.Value)
		{
			this = DBNull;
			return;
		}
		if (obj == Type.Missing)
		{
			this = Missing;
			return;
		}
		if (obj is Array)
		{
			_flags = 65554;
			_objref = obj;
			return;
		}
		_flags = 0;
		_objref = null;
		if (obj is UnknownWrapper)
		{
			varEnum = VarEnum.VT_UNKNOWN;
			obj = ((UnknownWrapper)obj).WrappedObject;
		}
		else if (obj is DispatchWrapper)
		{
			varEnum = VarEnum.VT_DISPATCH;
			obj = ((DispatchWrapper)obj).WrappedObject;
		}
		else if (obj is ErrorWrapper)
		{
			varEnum = VarEnum.VT_ERROR;
			obj = ((ErrorWrapper)obj).ErrorCode;
		}
		else if (obj is CurrencyWrapper)
		{
			varEnum = VarEnum.VT_CY;
			obj = ((CurrencyWrapper)obj).WrappedObject;
		}
		else if (obj is BStrWrapper)
		{
			varEnum = VarEnum.VT_BSTR;
			obj = ((BStrWrapper)obj).WrappedObject;
		}
		if (obj != null)
		{
			SetFieldsObject(obj);
		}
		if (varEnum != 0)
		{
			_flags |= (int)varEnum << 24;
		}
	}

	public object ToObject()
	{
		return CVType switch
		{
			0 => null, 
			2 => (int)_data != 0, 
			4 => (sbyte)_data, 
			5 => (byte)_data, 
			3 => (char)_data, 
			6 => (short)_data, 
			7 => (ushort)_data, 
			8 => (int)_data, 
			9 => (uint)_data, 
			10 => _data, 
			11 => (ulong)_data, 
			12 => BitConverter.UInt32BitsToSingle((uint)_data), 
			13 => BitConverter.Int64BitsToDouble(_data), 
			16 => new DateTime(_data), 
			17 => new TimeSpan(_data), 
			21 => BoxEnum(), 
			22 => Type.Missing, 
			23 => System.DBNull.Value, 
			_ => _objref, 
		};
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern object BoxEnum();

	internal static void MarshalHelperConvertObjectToVariant(object o, ref Variant v)
	{
		if (o == null)
		{
			v = Empty;
		}
		else if (o is IConvertible convertible)
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			v = convertible.GetTypeCode() switch
			{
				TypeCode.Empty => Empty, 
				TypeCode.Object => new Variant(o), 
				TypeCode.DBNull => DBNull, 
				TypeCode.Boolean => new Variant(convertible.ToBoolean(invariantCulture)), 
				TypeCode.Char => new Variant(convertible.ToChar(invariantCulture)), 
				TypeCode.SByte => new Variant(convertible.ToSByte(invariantCulture)), 
				TypeCode.Byte => new Variant(convertible.ToByte(invariantCulture)), 
				TypeCode.Int16 => new Variant(convertible.ToInt16(invariantCulture)), 
				TypeCode.UInt16 => new Variant(convertible.ToUInt16(invariantCulture)), 
				TypeCode.Int32 => new Variant(convertible.ToInt32(invariantCulture)), 
				TypeCode.UInt32 => new Variant(convertible.ToUInt32(invariantCulture)), 
				TypeCode.Int64 => new Variant(convertible.ToInt64(invariantCulture)), 
				TypeCode.UInt64 => new Variant(convertible.ToUInt64(invariantCulture)), 
				TypeCode.Single => new Variant(convertible.ToSingle(invariantCulture)), 
				TypeCode.Double => new Variant(convertible.ToDouble(invariantCulture)), 
				TypeCode.Decimal => new Variant(convertible.ToDecimal(invariantCulture)), 
				TypeCode.DateTime => new Variant(convertible.ToDateTime(invariantCulture)), 
				TypeCode.String => new Variant(convertible.ToString(invariantCulture)), 
				_ => throw new NotSupportedException(SR.Format(SR.NotSupported_UnknownTypeCode, convertible.GetTypeCode())), 
			};
		}
		else
		{
			v = new Variant(o);
		}
	}

	internal static object MarshalHelperConvertVariantToObject(ref Variant v)
	{
		return v.ToObject();
	}

	internal static void MarshalHelperCastVariant(object pValue, int vt, ref Variant v)
	{
		if (!(pValue is IConvertible convertible))
		{
			switch (vt)
			{
			case 9:
				v = new Variant(new DispatchWrapper(pValue));
				return;
			case 12:
				v = new Variant(pValue);
				return;
			case 13:
				v = new Variant(new UnknownWrapper(pValue));
				return;
			case 36:
				v = new Variant(pValue);
				return;
			case 8:
				if (pValue == null)
				{
					v = new Variant(null)
					{
						_flags = 14
					};
					return;
				}
				break;
			}
			throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant);
		}
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		v = vt switch
		{
			0 => Empty, 
			1 => DBNull, 
			2 => new Variant(convertible.ToInt16(invariantCulture)), 
			3 => new Variant(convertible.ToInt32(invariantCulture)), 
			4 => new Variant(convertible.ToSingle(invariantCulture)), 
			5 => new Variant(convertible.ToDouble(invariantCulture)), 
			6 => new Variant(new CurrencyWrapper(convertible.ToDecimal(invariantCulture))), 
			7 => new Variant(convertible.ToDateTime(invariantCulture)), 
			8 => new Variant(convertible.ToString(invariantCulture)), 
			9 => new Variant(new DispatchWrapper(convertible)), 
			10 => new Variant(new ErrorWrapper(convertible.ToInt32(invariantCulture))), 
			11 => new Variant(convertible.ToBoolean(invariantCulture)), 
			12 => new Variant(convertible), 
			13 => new Variant(new UnknownWrapper(convertible)), 
			14 => new Variant(convertible.ToDecimal(invariantCulture)), 
			16 => new Variant(convertible.ToSByte(invariantCulture)), 
			17 => new Variant(convertible.ToByte(invariantCulture)), 
			18 => new Variant(convertible.ToUInt16(invariantCulture)), 
			19 => new Variant(convertible.ToUInt32(invariantCulture)), 
			20 => new Variant(convertible.ToInt64(invariantCulture)), 
			21 => new Variant(convertible.ToUInt64(invariantCulture)), 
			22 => new Variant(convertible.ToInt32(invariantCulture)), 
			23 => new Variant(convertible.ToUInt32(invariantCulture)), 
			_ => throw new InvalidCastException(SR.InvalidCast_CannotCoerceByRefVariant), 
		};
	}
}
