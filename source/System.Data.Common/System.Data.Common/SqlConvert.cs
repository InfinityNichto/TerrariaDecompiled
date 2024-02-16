using System.Data.SqlTypes;
using System.Globalization;
using System.Numerics;
using System.Xml;

namespace System.Data.Common;

internal static class SqlConvert
{
	public static SqlByte ConvertToSqlByte(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlByte.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.Byte => (byte)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlByte)), 
		};
	}

	public static SqlInt16 ConvertToSqlInt16(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlInt16.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.Byte => (byte)value, 
			StorageType.Int16 => (short)value, 
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.SqlInt16 => (SqlInt16)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlInt16)), 
		};
	}

	public static SqlInt32 ConvertToSqlInt32(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlInt32.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlInt32 => (SqlInt32)value, 
			StorageType.Int32 => (int)value, 
			StorageType.SqlInt16 => (SqlInt16)value, 
			StorageType.Int16 => (short)value, 
			StorageType.UInt16 => (ushort)value, 
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.Byte => (byte)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlInt32)), 
		};
	}

	public static SqlInt64 ConvertToSqlInt64(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlInt32.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlInt64 => (SqlInt64)value, 
			StorageType.Int64 => (long)value, 
			StorageType.SqlInt16 => (SqlInt16)value, 
			StorageType.Int16 => (short)value, 
			StorageType.UInt16 => (ushort)value, 
			StorageType.SqlInt32 => (SqlInt32)value, 
			StorageType.Int32 => (int)value, 
			StorageType.UInt32 => (uint)value, 
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.Byte => (byte)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlInt64)), 
		};
	}

	public static SqlDouble ConvertToSqlDouble(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlDouble.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlDouble => (SqlDouble)value, 
			StorageType.Double => (double)value, 
			StorageType.SqlInt64 => (SqlInt64)value, 
			StorageType.Int64 => (long)value, 
			StorageType.UInt64 => (ulong)value, 
			StorageType.SqlInt16 => (SqlInt16)value, 
			StorageType.Int16 => (short)value, 
			StorageType.UInt16 => (int)(ushort)value, 
			StorageType.SqlInt32 => (SqlInt32)value, 
			StorageType.Int32 => (int)value, 
			StorageType.UInt32 => (uint)value, 
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.Byte => (int)(byte)value, 
			StorageType.SqlSingle => (SqlSingle)value, 
			StorageType.Single => (float)value, 
			StorageType.SqlMoney => (SqlMoney)value, 
			StorageType.SqlDecimal => (SqlDecimal)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlDouble)), 
		};
	}

	public static SqlDecimal ConvertToSqlDecimal(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlDecimal.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlDecimal => (SqlDecimal)value, 
			StorageType.Decimal => (decimal)value, 
			StorageType.SqlInt64 => (SqlInt64)value, 
			StorageType.Int64 => (long)value, 
			StorageType.UInt64 => (ulong)value, 
			StorageType.SqlInt16 => (SqlInt16)value, 
			StorageType.Int16 => (short)value, 
			StorageType.UInt16 => (ushort)value, 
			StorageType.SqlInt32 => (SqlInt32)value, 
			StorageType.Int32 => (int)value, 
			StorageType.UInt32 => (uint)value, 
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.Byte => (byte)value, 
			StorageType.SqlMoney => (SqlMoney)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlDecimal)), 
		};
	}

	public static SqlSingle ConvertToSqlSingle(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlSingle.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlSingle => (SqlSingle)value, 
			StorageType.Single => (float)value, 
			StorageType.SqlInt64 => (SqlInt64)value, 
			StorageType.Int64 => (long)value, 
			StorageType.UInt64 => (ulong)value, 
			StorageType.SqlInt16 => (SqlInt16)value, 
			StorageType.Int16 => (short)value, 
			StorageType.UInt16 => (int)(ushort)value, 
			StorageType.SqlInt32 => (SqlInt32)value, 
			StorageType.Int32 => (int)value, 
			StorageType.UInt32 => (uint)value, 
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.Byte => (int)(byte)value, 
			StorageType.SqlMoney => (SqlMoney)value, 
			StorageType.SqlDecimal => (SqlDecimal)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlSingle)), 
		};
	}

	public static SqlMoney ConvertToSqlMoney(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlMoney.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlMoney => (SqlMoney)value, 
			StorageType.Decimal => (decimal)value, 
			StorageType.SqlInt64 => (SqlInt64)value, 
			StorageType.Int64 => (long)value, 
			StorageType.UInt64 => (ulong)value, 
			StorageType.SqlInt16 => (SqlInt16)value, 
			StorageType.Int16 => (short)value, 
			StorageType.UInt16 => (ushort)value, 
			StorageType.SqlInt32 => (SqlInt32)value, 
			StorageType.Int32 => (int)value, 
			StorageType.UInt32 => (uint)value, 
			StorageType.SqlByte => (SqlByte)value, 
			StorageType.Byte => (byte)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlMoney)), 
		};
	}

	public static SqlDateTime ConvertToSqlDateTime(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlDateTime.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlDateTime => (SqlDateTime)value, 
			StorageType.DateTime => (DateTime)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlDateTime)), 
		};
	}

	public static SqlBoolean ConvertToSqlBoolean(object value)
	{
		if (value == DBNull.Value || value == null)
		{
			return SqlBoolean.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlBoolean => (SqlBoolean)value, 
			StorageType.Boolean => (bool)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlBoolean)), 
		};
	}

	public static SqlGuid ConvertToSqlGuid(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlGuid.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlGuid => (SqlGuid)value, 
			StorageType.Guid => (Guid)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlGuid)), 
		};
	}

	public static SqlBinary ConvertToSqlBinary(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlBinary.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlBinary => (SqlBinary)value, 
			StorageType.ByteArray => (byte[])value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlBinary)), 
		};
	}

	public static SqlString ConvertToSqlString(object value)
	{
		if (value == DBNull.Value || value == null)
		{
			return SqlString.Null;
		}
		Type type = value.GetType();
		return DataStorage.GetStorageType(type) switch
		{
			StorageType.SqlString => (SqlString)value, 
			StorageType.String => (string)value, 
			_ => throw ExceptionBuilder.ConvertFailed(type, typeof(SqlString)), 
		};
	}

	public static SqlChars ConvertToSqlChars(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlChars.Null;
		}
		Type type = value.GetType();
		StorageType storageType = DataStorage.GetStorageType(type);
		if (storageType == StorageType.SqlChars)
		{
			return (SqlChars)value;
		}
		throw ExceptionBuilder.ConvertFailed(type, typeof(SqlChars));
	}

	public static SqlBytes ConvertToSqlBytes(object value)
	{
		if (value == DBNull.Value)
		{
			return SqlBytes.Null;
		}
		Type type = value.GetType();
		StorageType storageType = DataStorage.GetStorageType(type);
		if (storageType == StorageType.SqlBytes)
		{
			return (SqlBytes)value;
		}
		throw ExceptionBuilder.ConvertFailed(type, typeof(SqlBytes));
	}

	public static DateTimeOffset ConvertStringToDateTimeOffset(string value, IFormatProvider formatProvider)
	{
		return DateTimeOffset.Parse(value, formatProvider);
	}

	public static object ChangeTypeForDefaultValue(object value, Type type, IFormatProvider formatProvider)
	{
		if (type == typeof(BigInteger))
		{
			if (DBNull.Value == value || value == null)
			{
				return DBNull.Value;
			}
			return BigIntegerStorage.ConvertToBigInteger(value, formatProvider);
		}
		if (value is BigInteger)
		{
			return BigIntegerStorage.ConvertFromBigInteger((BigInteger)value, type, formatProvider);
		}
		return ChangeType2(value, DataStorage.GetStorageType(type), type, formatProvider);
	}

	public static object ChangeType2(object value, StorageType stype, Type type, IFormatProvider formatProvider)
	{
		switch (stype)
		{
		case StorageType.SqlBinary:
			return ConvertToSqlBinary(value);
		case StorageType.SqlBoolean:
			return ConvertToSqlBoolean(value);
		case StorageType.SqlByte:
			return ConvertToSqlByte(value);
		case StorageType.SqlBytes:
			return ConvertToSqlBytes(value);
		case StorageType.SqlChars:
			return ConvertToSqlChars(value);
		case StorageType.SqlDateTime:
			return ConvertToSqlDateTime(value);
		case StorageType.SqlDecimal:
			return ConvertToSqlDecimal(value);
		case StorageType.SqlDouble:
			return ConvertToSqlDouble(value);
		case StorageType.SqlGuid:
			return ConvertToSqlGuid(value);
		case StorageType.SqlInt16:
			return ConvertToSqlInt16(value);
		case StorageType.SqlInt32:
			return ConvertToSqlInt32(value);
		case StorageType.SqlInt64:
			return ConvertToSqlInt64(value);
		case StorageType.SqlMoney:
			return ConvertToSqlMoney(value);
		case StorageType.SqlSingle:
			return ConvertToSqlSingle(value);
		case StorageType.SqlString:
			return ConvertToSqlString(value);
		default:
		{
			if (DBNull.Value == value || value == null)
			{
				return DBNull.Value;
			}
			Type type2 = value.GetType();
			StorageType storageType = DataStorage.GetStorageType(type2);
			if ((uint)(storageType - 26) <= 14u)
			{
				throw ExceptionBuilder.ConvertFailed(type2, type);
			}
			if (StorageType.String == stype)
			{
				switch (storageType)
				{
				case StorageType.Boolean:
					return ((IConvertible)(bool)value).ToString(formatProvider);
				case StorageType.Char:
					return ((IConvertible)(char)value).ToString(formatProvider);
				case StorageType.SByte:
					return ((sbyte)value).ToString(formatProvider);
				case StorageType.Byte:
					return ((byte)value).ToString(formatProvider);
				case StorageType.Int16:
					return ((short)value).ToString(formatProvider);
				case StorageType.UInt16:
					return ((ushort)value).ToString(formatProvider);
				case StorageType.Int32:
					return ((int)value).ToString(formatProvider);
				case StorageType.UInt32:
					return ((uint)value).ToString(formatProvider);
				case StorageType.Int64:
					return ((long)value).ToString(formatProvider);
				case StorageType.UInt64:
					return ((ulong)value).ToString(formatProvider);
				case StorageType.Single:
					return ((float)value).ToString(formatProvider);
				case StorageType.Double:
					return ((double)value).ToString(formatProvider);
				case StorageType.Decimal:
					return ((decimal)value).ToString(formatProvider);
				case StorageType.DateTime:
					return ((DateTime)value).ToString(formatProvider);
				case StorageType.TimeSpan:
					return XmlConvert.ToString((TimeSpan)value);
				case StorageType.Guid:
					return XmlConvert.ToString((Guid)value);
				case StorageType.String:
					return (string)value;
				case StorageType.CharArray:
					return new string((char[])value);
				case StorageType.DateTimeOffset:
					return ((DateTimeOffset)value).ToString(formatProvider);
				default:
					if (value is IConvertible convertible)
					{
						return convertible.ToString(formatProvider);
					}
					if (value is IFormattable formattable)
					{
						return formattable.ToString(null, formatProvider);
					}
					return value.ToString();
				case StorageType.BigInteger:
					break;
				}
			}
			else
			{
				if (StorageType.TimeSpan == stype)
				{
					return storageType switch
					{
						StorageType.String => XmlConvert.ToTimeSpan((string)value), 
						StorageType.Int32 => new TimeSpan((int)value), 
						StorageType.Int64 => new TimeSpan((long)value), 
						_ => (TimeSpan)value, 
					};
				}
				if (StorageType.DateTimeOffset == stype)
				{
					return (DateTimeOffset)value;
				}
				if (StorageType.String == storageType)
				{
					switch (stype)
					{
					case StorageType.String:
						return (string)value;
					case StorageType.Boolean:
						if ("1" == (string)value)
						{
							return true;
						}
						if ("0" == (string)value)
						{
							return false;
						}
						break;
					case StorageType.Char:
						return ((IConvertible)(string)value).ToChar(formatProvider);
					case StorageType.SByte:
						return ((IConvertible)(string)value).ToSByte(formatProvider);
					case StorageType.Byte:
						return ((IConvertible)(string)value).ToByte(formatProvider);
					case StorageType.Int16:
						return ((IConvertible)(string)value).ToInt16(formatProvider);
					case StorageType.UInt16:
						return ((IConvertible)(string)value).ToUInt16(formatProvider);
					case StorageType.Int32:
						return ((IConvertible)(string)value).ToInt32(formatProvider);
					case StorageType.UInt32:
						return ((IConvertible)(string)value).ToUInt32(formatProvider);
					case StorageType.Int64:
						return ((IConvertible)(string)value).ToInt64(formatProvider);
					case StorageType.UInt64:
						return ((IConvertible)(string)value).ToUInt64(formatProvider);
					case StorageType.Single:
						return ((IConvertible)(string)value).ToSingle(formatProvider);
					case StorageType.Double:
						return ((IConvertible)(string)value).ToDouble(formatProvider);
					case StorageType.Decimal:
						return ((IConvertible)(string)value).ToDecimal(formatProvider);
					case StorageType.DateTime:
						return ((IConvertible)(string)value).ToDateTime(formatProvider);
					case StorageType.TimeSpan:
						return XmlConvert.ToTimeSpan((string)value);
					case StorageType.Guid:
						return XmlConvert.ToGuid((string)value);
					case StorageType.Uri:
						return new Uri((string)value);
					}
				}
			}
			return Convert.ChangeType(value, type, formatProvider);
		}
		}
	}

	public static object ChangeTypeForXML(object value, Type type)
	{
		StorageType storageType = DataStorage.GetStorageType(type);
		Type type2 = value.GetType();
		StorageType storageType2 = DataStorage.GetStorageType(type2);
		switch (storageType)
		{
		case StorageType.SqlBinary:
			return new SqlBinary(Convert.FromBase64String((string)value));
		case StorageType.SqlBoolean:
			return new SqlBoolean(XmlConvert.ToBoolean((string)value));
		case StorageType.SqlByte:
			return new SqlByte(XmlConvert.ToByte((string)value));
		case StorageType.SqlBytes:
			return new SqlBytes(Convert.FromBase64String((string)value));
		case StorageType.SqlChars:
			return new SqlChars(((string)value).ToCharArray());
		case StorageType.SqlDateTime:
			return new SqlDateTime(XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.RoundtripKind));
		case StorageType.SqlDecimal:
			return SqlDecimal.Parse((string)value);
		case StorageType.SqlDouble:
			return new SqlDouble(XmlConvert.ToDouble((string)value));
		case StorageType.SqlGuid:
			return new SqlGuid(XmlConvert.ToGuid((string)value));
		case StorageType.SqlInt16:
			return new SqlInt16(XmlConvert.ToInt16((string)value));
		case StorageType.SqlInt32:
			return new SqlInt32(XmlConvert.ToInt32((string)value));
		case StorageType.SqlInt64:
			return new SqlInt64(XmlConvert.ToInt64((string)value));
		case StorageType.SqlMoney:
			return new SqlMoney(XmlConvert.ToDecimal((string)value));
		case StorageType.SqlSingle:
			return new SqlSingle(XmlConvert.ToSingle((string)value));
		case StorageType.SqlString:
			return new SqlString((string)value);
		case StorageType.Boolean:
			if ("1" == (string)value)
			{
				return true;
			}
			if ("0" == (string)value)
			{
				return false;
			}
			return XmlConvert.ToBoolean((string)value);
		case StorageType.Char:
			return XmlConvert.ToChar((string)value);
		case StorageType.SByte:
			return XmlConvert.ToSByte((string)value);
		case StorageType.Byte:
			return XmlConvert.ToByte((string)value);
		case StorageType.Int16:
			return XmlConvert.ToInt16((string)value);
		case StorageType.UInt16:
			return XmlConvert.ToUInt16((string)value);
		case StorageType.Int32:
			return XmlConvert.ToInt32((string)value);
		case StorageType.UInt32:
			return XmlConvert.ToUInt32((string)value);
		case StorageType.Int64:
			return XmlConvert.ToInt64((string)value);
		case StorageType.UInt64:
			return XmlConvert.ToUInt64((string)value);
		case StorageType.Single:
			return XmlConvert.ToSingle((string)value);
		case StorageType.Double:
			return XmlConvert.ToDouble((string)value);
		case StorageType.Decimal:
			return XmlConvert.ToDecimal((string)value);
		case StorageType.DateTime:
			return XmlConvert.ToDateTime((string)value, XmlDateTimeSerializationMode.RoundtripKind);
		case StorageType.Guid:
			return XmlConvert.ToGuid((string)value);
		case StorageType.Uri:
			return new Uri((string)value);
		case StorageType.DateTimeOffset:
			return XmlConvert.ToDateTimeOffset((string)value);
		case StorageType.TimeSpan:
			return storageType2 switch
			{
				StorageType.String => XmlConvert.ToTimeSpan((string)value), 
				StorageType.Int32 => new TimeSpan((int)value), 
				StorageType.Int64 => new TimeSpan((long)value), 
				_ => (TimeSpan)value, 
			};
		default:
			if (DBNull.Value == value || value == null)
			{
				return DBNull.Value;
			}
			switch (storageType2)
			{
			case StorageType.SqlBinary:
				return Convert.ToBase64String(((SqlBinary)value).Value);
			case StorageType.SqlBoolean:
				return XmlConvert.ToString(((SqlBoolean)value).Value);
			case StorageType.SqlByte:
				return XmlConvert.ToString(((SqlByte)value).Value);
			case StorageType.SqlBytes:
				return Convert.ToBase64String(((SqlBytes)value).Value);
			case StorageType.SqlChars:
				return new string(((SqlChars)value).Value);
			case StorageType.SqlDateTime:
				return XmlConvert.ToString(((SqlDateTime)value).Value, XmlDateTimeSerializationMode.RoundtripKind);
			case StorageType.SqlDecimal:
				return ((SqlDecimal)value).ToString();
			case StorageType.SqlDouble:
				return XmlConvert.ToString(((SqlDouble)value).Value);
			case StorageType.SqlGuid:
				return XmlConvert.ToString(((SqlGuid)value).Value);
			case StorageType.SqlInt16:
				return XmlConvert.ToString(((SqlInt16)value).Value);
			case StorageType.SqlInt32:
				return XmlConvert.ToString(((SqlInt32)value).Value);
			case StorageType.SqlInt64:
				return XmlConvert.ToString(((SqlInt64)value).Value);
			case StorageType.SqlMoney:
				return XmlConvert.ToString(((SqlMoney)value).Value);
			case StorageType.SqlSingle:
				return XmlConvert.ToString(((SqlSingle)value).Value);
			case StorageType.SqlString:
				return ((SqlString)value).Value;
			case StorageType.Boolean:
				return XmlConvert.ToString((bool)value);
			case StorageType.Char:
				return XmlConvert.ToString((char)value);
			case StorageType.SByte:
				return XmlConvert.ToString((sbyte)value);
			case StorageType.Byte:
				return XmlConvert.ToString((byte)value);
			case StorageType.Int16:
				return XmlConvert.ToString((short)value);
			case StorageType.UInt16:
				return XmlConvert.ToString((ushort)value);
			case StorageType.Int32:
				return XmlConvert.ToString((int)value);
			case StorageType.UInt32:
				return XmlConvert.ToString((uint)value);
			case StorageType.Int64:
				return XmlConvert.ToString((long)value);
			case StorageType.UInt64:
				return XmlConvert.ToString((ulong)value);
			case StorageType.Single:
				return XmlConvert.ToString((float)value);
			case StorageType.Double:
				return XmlConvert.ToString((double)value);
			case StorageType.Decimal:
				return XmlConvert.ToString((decimal)value);
			case StorageType.DateTime:
				return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
			case StorageType.TimeSpan:
				return XmlConvert.ToString((TimeSpan)value);
			case StorageType.Guid:
				return XmlConvert.ToString((Guid)value);
			case StorageType.String:
				return (string)value;
			case StorageType.CharArray:
				return new string((char[])value);
			case StorageType.DateTimeOffset:
				return XmlConvert.ToString((DateTimeOffset)value);
			default:
				if (value is IConvertible convertible)
				{
					return convertible.ToString(CultureInfo.InvariantCulture);
				}
				if (value is IFormattable formattable)
				{
					return formattable.ToString(null, CultureInfo.InvariantCulture);
				}
				return value.ToString();
			}
		}
	}
}
