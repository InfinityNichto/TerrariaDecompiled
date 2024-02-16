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
public struct SqlInt16 : INullable, IComparable, IXmlSerializable
{
	private bool m_fNotNull;

	private short m_value;

	public static readonly SqlInt16 Null = new SqlInt16(fNull: true);

	public static readonly SqlInt16 Zero = new SqlInt16(0);

	public static readonly SqlInt16 MinValue = new SqlInt16(short.MinValue);

	public static readonly SqlInt16 MaxValue = new SqlInt16(short.MaxValue);

	public bool IsNull => !m_fNotNull;

	public short Value
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

	private SqlInt16(bool fNull)
	{
		m_fNotNull = false;
		m_value = 0;
	}

	public SqlInt16(short value)
	{
		m_value = value;
		m_fNotNull = true;
	}

	public static implicit operator SqlInt16(short x)
	{
		return new SqlInt16(x);
	}

	public static explicit operator short(SqlInt16 x)
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

	public static SqlInt16 Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		return new SqlInt16(short.Parse(s, null));
	}

	public static SqlInt16 operator -(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt16((short)(-x.m_value));
		}
		return Null;
	}

	public static SqlInt16 operator ~(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlInt16((short)(~x.m_value));
		}
		return Null;
	}

	public static SqlInt16 operator +(SqlInt16 x, SqlInt16 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		int num = x.m_value + y.m_value;
		if (((uint)((num >> 15) ^ (num >> 16)) & (true ? 1u : 0u)) != 0)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt16((short)num);
	}

	public static SqlInt16 operator -(SqlInt16 x, SqlInt16 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		int num = x.m_value - y.m_value;
		if (((uint)((num >> 15) ^ (num >> 16)) & (true ? 1u : 0u)) != 0)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt16((short)num);
	}

	public static SqlInt16 operator *(SqlInt16 x, SqlInt16 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		int num = x.m_value * y.m_value;
		int num2 = num & -32768;
		if (num2 != 0 && num2 != -32768)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt16((short)num);
	}

	public static SqlInt16 operator /(SqlInt16 x, SqlInt16 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y.m_value != 0)
		{
			if (x.m_value == short.MinValue && y.m_value == -1)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
			return new SqlInt16((short)(x.m_value / y.m_value));
		}
		throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
	}

	public static SqlInt16 operator %(SqlInt16 x, SqlInt16 y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y.m_value != 0)
		{
			if (x.m_value == short.MinValue && y.m_value == -1)
			{
				throw new OverflowException(SQLResource.ArithOverflowMessage);
			}
			return new SqlInt16((short)(x.m_value % y.m_value));
		}
		throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
	}

	public static SqlInt16 operator &(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt16((short)(x.m_value & y.m_value));
		}
		return Null;
	}

	public static SqlInt16 operator |(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt16((short)((ushort)x.m_value | (ushort)y.m_value));
		}
		return Null;
	}

	public static SqlInt16 operator ^(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlInt16((short)(x.m_value ^ y.m_value));
		}
		return Null;
	}

	public static explicit operator SqlInt16(SqlBoolean x)
	{
		if (!x.IsNull)
		{
			return new SqlInt16(x.ByteValue);
		}
		return Null;
	}

	public static implicit operator SqlInt16(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlInt16(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlInt16(SqlInt32 x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		int value = x.Value;
		if (value > 32767 || value < -32768)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt16((short)value);
	}

	public static explicit operator SqlInt16(SqlInt64 x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		long value = x.Value;
		if (value > 32767 || value < -32768)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt16((short)value);
	}

	public static explicit operator SqlInt16(SqlSingle x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		float value = x.Value;
		if (value < -32768f || value > 32767f)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt16((short)value);
	}

	public static explicit operator SqlInt16(SqlDouble x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		double value = x.Value;
		if (value < -32768.0 || value > 32767.0)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlInt16((short)value);
	}

	public static explicit operator SqlInt16(SqlMoney x)
	{
		if (!x.IsNull)
		{
			return new SqlInt16(checked((short)x.ToInt32()));
		}
		return Null;
	}

	public static explicit operator SqlInt16(SqlDecimal x)
	{
		return (SqlInt16)(SqlInt32)x;
	}

	public static explicit operator SqlInt16(SqlString x)
	{
		if (!x.IsNull)
		{
			return new SqlInt16(short.Parse(x.Value, null));
		}
		return Null;
	}

	public static SqlBoolean operator ==(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value == y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlInt16 x, SqlInt16 y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value < y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value > y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value <= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >=(SqlInt16 x, SqlInt16 y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value >= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlInt16 OnesComplement(SqlInt16 x)
	{
		return ~x;
	}

	public static SqlInt16 Add(SqlInt16 x, SqlInt16 y)
	{
		return x + y;
	}

	public static SqlInt16 Subtract(SqlInt16 x, SqlInt16 y)
	{
		return x - y;
	}

	public static SqlInt16 Multiply(SqlInt16 x, SqlInt16 y)
	{
		return x * y;
	}

	public static SqlInt16 Divide(SqlInt16 x, SqlInt16 y)
	{
		return x / y;
	}

	public static SqlInt16 Mod(SqlInt16 x, SqlInt16 y)
	{
		return x % y;
	}

	public static SqlInt16 Modulus(SqlInt16 x, SqlInt16 y)
	{
		return x % y;
	}

	public static SqlInt16 BitwiseAnd(SqlInt16 x, SqlInt16 y)
	{
		return x & y;
	}

	public static SqlInt16 BitwiseOr(SqlInt16 x, SqlInt16 y)
	{
		return x | y;
	}

	public static SqlInt16 Xor(SqlInt16 x, SqlInt16 y)
	{
		return x ^ y;
	}

	public static SqlBoolean Equals(SqlInt16 x, SqlInt16 y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlInt16 x, SqlInt16 y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlInt16 x, SqlInt16 y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlInt16 x, SqlInt16 y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlInt16 x, SqlInt16 y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlInt16 x, SqlInt16 y)
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

	public SqlInt32 ToSqlInt32()
	{
		return this;
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
		if (value is SqlInt16 value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlInt16));
	}

	public int CompareTo(SqlInt16 value)
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
		if (!(value is SqlInt16 sqlInt))
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
			m_value = XmlConvert.ToInt16(reader.ReadElementString());
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
		return new XmlQualifiedName("short", "http://www.w3.org/2001/XMLSchema");
	}
}
