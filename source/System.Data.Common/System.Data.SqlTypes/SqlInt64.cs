using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[XmlSchemaProvider("GetXsdType")]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct SqlInt64 : INullable, IComparable, IXmlSerializable
{
	private bool m_fNotNull;

	private long m_value;

	public static readonly SqlInt64 Null = new SqlInt64(fNull: true);

	public static readonly SqlInt64 Zero = new SqlInt64(0L);

	public static readonly SqlInt64 MinValue = new SqlInt64(long.MinValue);

	public static readonly SqlInt64 MaxValue = new SqlInt64(long.MaxValue);

	public bool IsNull => !m_fNotNull;

	public long Value
	{
		get
		{
			if (m_fNotNull)
			{
				return m_value;
			}
			throw new SqlNullValueException();
		}
	}

	private SqlInt64(bool fNull)
	{
		m_fNotNull = false;
		m_value = 0L;
	}

	public SqlInt64(long value)
	{
		m_value = value;
		m_fNotNull = true;
	}

	public static implicit operator SqlInt64(long x)
	{
		return new SqlInt64(x);
	}

	public static explicit operator long(SqlInt64 x)
	{
		return x.Value;
	}

	public override string ToString()
	{
		if (!IsNull)
		{
			return m_value.ToString((IFormatProvider?)null);
		}
		return SQLResource.NullString;
	}

	public static SqlInt64 Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		return new SqlInt64(long.Parse(s, null));
	}

	public static SqlInt64 operator -(SqlInt64 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(-x.m_value);
		}
		return Null;
	}

	public static SqlInt64 operator ~(SqlInt64 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(~x.m_value);
		}
		return Null;
	}

	public static SqlInt64 operator +(SqlInt64 x, SqlInt64 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		long num = x.m_value + y.m_value;
		if (SameSignLong(x.m_value, y.m_value) && !SameSignLong(x.m_value, num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt64(num);
	}

	public static SqlInt64 operator -(SqlInt64 x, SqlInt64 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		long num = x.m_value - y.m_value;
		if (!SameSignLong(x.m_value, y.m_value) && SameSignLong(y.m_value, num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt64(num);
	}

	public static SqlInt64 operator *(SqlInt64 x, SqlInt64 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		bool flag = false;
		long num = x.m_value;
		long num2 = y.m_value;
		long num3 = 0L;
		if (num < 0)
		{
			flag = true;
			num = -num;
		}
		if (num2 < 0)
		{
			flag = !flag;
			num2 = -num2;
		}
		long num4 = num & 0xFFFFFFFFu;
		long num5 = (num >> 32) & 0xFFFFFFFFu;
		long num6 = num2 & 0xFFFFFFFFu;
		long num7 = (num2 >> 32) & 0xFFFFFFFFu;
		if (num5 != 0L && num7 != 0L)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		long num8 = num4 * num6;
		if (num8 < 0)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		if (num5 != 0L)
		{
			num3 = num5 * num6;
			if (num3 < 0 || num3 > long.MaxValue)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
		}
		else if (num7 != 0L)
		{
			num3 = num4 * num7;
			if (num3 < 0 || num3 > long.MaxValue)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
		}
		num8 += num3 << 32;
		if (num8 < 0)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		if (flag)
		{
			num8 = -num8;
		}
		return new SqlInt64(num8);
	}

	public static SqlInt64 operator /(SqlInt64 x, SqlInt64 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y.m_value != 0L)
		{
			if (x.m_value == long.MinValue && y.m_value == -1)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
			return new SqlInt64(x.m_value / y.m_value);
		}
		throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
	}

	public static SqlInt64 operator %(SqlInt64 x, SqlInt64 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y.m_value != 0L)
		{
			if (x.m_value == long.MinValue && y.m_value == -1)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
			return new SqlInt64(x.m_value % y.m_value);
		}
		throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
	}

	public static SqlInt64 operator &(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt64(x.m_value & y.m_value);
		}
		return Null;
	}

	public static SqlInt64 operator |(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt64(x.m_value | y.m_value);
		}
		return Null;
	}

	public static SqlInt64 operator ^(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt64(x.m_value ^ y.m_value);
		}
		return Null;
	}

	public static explicit operator SqlInt64(SqlBoolean x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(x.ByteValue);
		}
		return Null;
	}

	public static implicit operator SqlInt64(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlInt64(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlInt64(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlInt64(SqlSingle x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		float value = x.Value;
		if (value > 9.223372E+18f || value < -9.223372E+18f)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt64((long)value);
	}

	public static explicit operator SqlInt64(SqlDouble x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		double value = x.Value;
		if (value > 9.223372036854776E+18 || value < -9.223372036854776E+18)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt64((long)value);
	}

	public static explicit operator SqlInt64(SqlMoney x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(x.ToInt64());
		}
		return Null;
	}

	public static explicit operator SqlInt64(SqlDecimal x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		SqlDecimal sqlDecimal = x;
		sqlDecimal.AdjustScale(-sqlDecimal._bScale, fRound: false);
		if (sqlDecimal._bLen > 2)
		{
			throw new OverflowException(SQLResource.ConversionOverflowMessage);
		}
		long num2;
		if (sqlDecimal._bLen == 2)
		{
			ulong num = SqlDecimal.DWL(sqlDecimal._data1, sqlDecimal._data2);
			if (num > long.MaxValue && (sqlDecimal.IsPositive || num != 9223372036854775808uL))
			{
				throw new OverflowException(SQLResource.ConversionOverflowMessage);
			}
			num2 = (long)num;
		}
		else
		{
			num2 = sqlDecimal._data1;
		}
		if (!sqlDecimal.IsPositive)
		{
			num2 = -num2;
		}
		return new SqlInt64(num2);
	}

	public static explicit operator SqlInt64(SqlString x)
	{
		if (!x.IsNull)
		{
			return new SqlInt64(long.Parse(x.Value, null));
		}
		return Null;
	}

	private static bool SameSignLong(long x, long y)
	{
		return ((x ^ y) & long.MinValue) == 0;
	}

	public static SqlBoolean operator ==(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value == y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlInt64 x, SqlInt64 y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value < y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value > y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value <= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >=(SqlInt64 x, SqlInt64 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value >= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlInt64 OnesComplement(SqlInt64 x)
	{
		return ~x;
	}

	public static SqlInt64 Add(SqlInt64 x, SqlInt64 y)
	{
		return x + y;
	}

	public static SqlInt64 Subtract(SqlInt64 x, SqlInt64 y)
	{
		return x - y;
	}

	public static SqlInt64 Multiply(SqlInt64 x, SqlInt64 y)
	{
		return x * y;
	}

	public static SqlInt64 Divide(SqlInt64 x, SqlInt64 y)
	{
		return x / y;
	}

	public static SqlInt64 Mod(SqlInt64 x, SqlInt64 y)
	{
		return x % y;
	}

	public static SqlInt64 Modulus(SqlInt64 x, SqlInt64 y)
	{
		return x % y;
	}

	public static SqlInt64 BitwiseAnd(SqlInt64 x, SqlInt64 y)
	{
		return x & y;
	}

	public static SqlInt64 BitwiseOr(SqlInt64 x, SqlInt64 y)
	{
		return x | y;
	}

	public static SqlInt64 Xor(SqlInt64 x, SqlInt64 y)
	{
		return x ^ y;
	}

	public static SqlBoolean Equals(SqlInt64 x, SqlInt64 y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlInt64 x, SqlInt64 y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlInt64 x, SqlInt64 y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlInt64 x, SqlInt64 y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlInt64 x, SqlInt64 y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlInt64 x, SqlInt64 y)
	{
		return x >= y;
	}

	public SqlBoolean ToSqlBoolean()
	{
		return (SqlBoolean)this;
	}

	public SqlByte ToSqlByte()
	{
		return (SqlByte)this;
	}

	public SqlDouble ToSqlDouble()
	{
		return this;
	}

	public SqlInt16 ToSqlInt16()
	{
		return (SqlInt16)this;
	}

	public SqlInt32 ToSqlInt32()
	{
		return (SqlInt32)this;
	}

	public SqlMoney ToSqlMoney()
	{
		return this;
	}

	public SqlDecimal ToSqlDecimal()
	{
		return this;
	}

	public SqlSingle ToSqlSingle()
	{
		return this;
	}

	public SqlString ToSqlString()
	{
		return (SqlString)this;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlInt64 value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlInt64));
	}

	public int CompareTo(SqlInt64 value)
	{
		if (IsNull)
		{
			if (!value.IsNull)
			{
				return -1;
			}
			return 0;
		}
		if (value.IsNull)
		{
			return 1;
		}
		if (this < value)
		{
			return -1;
		}
		if (this > value)
		{
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (!(value is SqlInt64 sqlInt))
		{
			return false;
		}
		if (sqlInt.IsNull || IsNull)
		{
			if (sqlInt.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlInt).Value;
	}

	public override int GetHashCode()
	{
		if (!IsNull)
		{
			return Value.GetHashCode();
		}
		return 0;
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		return null;
	}

	void IXmlSerializable.ReadXml(XmlReader reader)
	{
		string attribute = reader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance");
		if (attribute != null && XmlConvert.ToBoolean(attribute))
		{
			reader.ReadElementString();
			m_fNotNull = false;
		}
		else
		{
			m_value = XmlConvert.ToInt64(reader.ReadElementString());
			m_fNotNull = true;
		}
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		if (IsNull)
		{
			writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
		}
		else
		{
			writer.WriteString(XmlConvert.ToString(m_value));
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("long", "http://www.w3.org/2001/XMLSchema");
	}
}
