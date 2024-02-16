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
public struct SqlInt32 : INullable, IComparable, IXmlSerializable
{
	private bool m_fNotNull;

	private int m_value;

	public static readonly SqlInt32 Null = new SqlInt32(fNull: true);

	public static readonly SqlInt32 Zero = new SqlInt32(0);

	public static readonly SqlInt32 MinValue = new SqlInt32(int.MinValue);

	public static readonly SqlInt32 MaxValue = new SqlInt32(int.MaxValue);

	public bool IsNull => !m_fNotNull;

	public int Value
	{
		get
		{
			if (IsNull)
			{
				throw new SqlNullValueException();
			}
			return m_value;
		}
	}

	private SqlInt32(bool fNull)
	{
		m_fNotNull = false;
		m_value = 0;
	}

	public SqlInt32(int value)
	{
		m_value = value;
		m_fNotNull = true;
	}

	public static implicit operator SqlInt32(int x)
	{
		return new SqlInt32(x);
	}

	public static explicit operator int(SqlInt32 x)
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

	public static SqlInt32 Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		return new SqlInt32(int.Parse(s, null));
	}

	public static SqlInt32 operator -(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt32(-x.m_value);
		}
		return Null;
	}

	public static SqlInt32 operator ~(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt32(~x.m_value);
		}
		return Null;
	}

	public static SqlInt32 operator +(SqlInt32 x, SqlInt32 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		int num = x.m_value + y.m_value;
		if (SameSignInt(x.m_value, y.m_value) && !SameSignInt(x.m_value, num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt32(num);
	}

	public static SqlInt32 operator -(SqlInt32 x, SqlInt32 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		int num = x.m_value - y.m_value;
		if (!SameSignInt(x.m_value, y.m_value) && SameSignInt(y.m_value, num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt32(num);
	}

	public static SqlInt32 operator *(SqlInt32 x, SqlInt32 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		long num = (long)x.m_value * (long)y.m_value;
		long num2 = num & int.MinValue;
		if (num2 != 0L && num2 != int.MinValue)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt32((int)num);
	}

	public static SqlInt32 operator /(SqlInt32 x, SqlInt32 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y.m_value != 0)
		{
			if ((long)x.m_value == int.MinValue && y.m_value == -1)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
			return new SqlInt32(x.m_value / y.m_value);
		}
		throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
	}

	public static SqlInt32 operator %(SqlInt32 x, SqlInt32 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y.m_value != 0)
		{
			if ((long)x.m_value == int.MinValue && y.m_value == -1)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
			return new SqlInt32(x.m_value % y.m_value);
		}
		throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
	}

	public static SqlInt32 operator &(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt32(x.m_value & y.m_value);
		}
		return Null;
	}

	public static SqlInt32 operator |(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt32(x.m_value | y.m_value);
		}
		return Null;
	}

	public static SqlInt32 operator ^(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt32(x.m_value ^ y.m_value);
		}
		return Null;
	}

	public static explicit operator SqlInt32(SqlBoolean x)
	{
		if (!x.IsNull)
		{
			return new SqlInt32(x.ByteValue);
		}
		return Null;
	}

	public static implicit operator SqlInt32(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlInt32(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlInt32(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt32(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlInt32(SqlInt64 x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		long value = x.Value;
		if (value > int.MaxValue || value < int.MinValue)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt32((int)value);
	}

	public static explicit operator SqlInt32(SqlSingle x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		float value = x.Value;
		if (value > 2.1474836E+09f || value < -2.1474836E+09f)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt32((int)value);
	}

	public static explicit operator SqlInt32(SqlDouble x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		double value = x.Value;
		if (value > 2147483647.0 || value < -2147483648.0)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt32((int)value);
	}

	public static explicit operator SqlInt32(SqlMoney x)
	{
		if (!x.IsNull)
		{
			return new SqlInt32(x.ToInt32());
		}
		return Null;
	}

	public static explicit operator SqlInt32(SqlDecimal x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		x.AdjustScale(-x.Scale, fRound: true);
		long num = x._data1;
		if (!x.IsPositive)
		{
			num = -num;
		}
		if (x._bLen > 1 || num > int.MaxValue || num < int.MinValue)
		{
			throw new OverflowException(SQLResource.ConversionOverflowMessage);
		}
		return new SqlInt32((int)num);
	}

	public static explicit operator SqlInt32(SqlString x)
	{
		if (!x.IsNull)
		{
			return new SqlInt32(int.Parse(x.Value, null));
		}
		return Null;
	}

	private static bool SameSignInt(int x, int y)
	{
		return ((x ^ y) & 0x80000000u) == 0;
	}

	public static SqlBoolean operator ==(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value == y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlInt32 x, SqlInt32 y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value < y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value > y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value <= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >=(SqlInt32 x, SqlInt32 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value >= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlInt32 OnesComplement(SqlInt32 x)
	{
		return ~x;
	}

	public static SqlInt32 Add(SqlInt32 x, SqlInt32 y)
	{
		return x + y;
	}

	public static SqlInt32 Subtract(SqlInt32 x, SqlInt32 y)
	{
		return x - y;
	}

	public static SqlInt32 Multiply(SqlInt32 x, SqlInt32 y)
	{
		return x * y;
	}

	public static SqlInt32 Divide(SqlInt32 x, SqlInt32 y)
	{
		return x / y;
	}

	public static SqlInt32 Mod(SqlInt32 x, SqlInt32 y)
	{
		return x % y;
	}

	public static SqlInt32 Modulus(SqlInt32 x, SqlInt32 y)
	{
		return x % y;
	}

	public static SqlInt32 BitwiseAnd(SqlInt32 x, SqlInt32 y)
	{
		return x & y;
	}

	public static SqlInt32 BitwiseOr(SqlInt32 x, SqlInt32 y)
	{
		return x | y;
	}

	public static SqlInt32 Xor(SqlInt32 x, SqlInt32 y)
	{
		return x ^ y;
	}

	public static SqlBoolean Equals(SqlInt32 x, SqlInt32 y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlInt32 x, SqlInt32 y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlInt32 x, SqlInt32 y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlInt32 x, SqlInt32 y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlInt32 x, SqlInt32 y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlInt32 x, SqlInt32 y)
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

	public SqlInt64 ToSqlInt64()
	{
		return this;
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
		if (value is SqlInt32 value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlInt32));
	}

	public int CompareTo(SqlInt32 value)
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
		if (!(value is SqlInt32 sqlInt))
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
			m_value = XmlConvert.ToInt32(reader.ReadElementString());
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
		return new XmlQualifiedName("int", "http://www.w3.org/2001/XMLSchema");
	}
}
