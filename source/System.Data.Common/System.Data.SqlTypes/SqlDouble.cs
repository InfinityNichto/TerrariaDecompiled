using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[XmlSchemaProvider("GetXsdType")]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct SqlDouble : INullable, IComparable, IXmlSerializable
{
	private bool m_fNotNull;

	private double m_value;

	public static readonly SqlDouble Null = new SqlDouble(fNull: true);

	public static readonly SqlDouble Zero = new SqlDouble(0.0);

	public static readonly SqlDouble MinValue = new SqlDouble(double.MinValue);

	public static readonly SqlDouble MaxValue = new SqlDouble(double.MaxValue);

	public bool IsNull => !m_fNotNull;

	public double Value
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

	private SqlDouble(bool fNull)
	{
		m_fNotNull = false;
		m_value = 0.0;
	}

	public SqlDouble(double value)
	{
		if (!double.IsFinite(value))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		m_value = value;
		m_fNotNull = true;
	}

	public static implicit operator SqlDouble(double x)
	{
		return new SqlDouble(x);
	}

	public static explicit operator double(SqlDouble x)
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

	public static SqlDouble Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		return new SqlDouble(double.Parse(s, CultureInfo.InvariantCulture));
	}

	public static SqlDouble operator -(SqlDouble x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble(0.0 - x.m_value);
		}
		return Null;
	}

	public static SqlDouble operator +(SqlDouble x, SqlDouble y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		double num = x.m_value + y.m_value;
		if (double.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlDouble(num);
	}

	public static SqlDouble operator -(SqlDouble x, SqlDouble y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		double num = x.m_value - y.m_value;
		if (double.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlDouble(num);
	}

	public static SqlDouble operator *(SqlDouble x, SqlDouble y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		double num = x.m_value * y.m_value;
		if (double.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlDouble(num);
	}

	public static SqlDouble operator /(SqlDouble x, SqlDouble y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y.m_value == 0.0)
		{
			throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
		}
		double num = x.m_value / y.m_value;
		if (double.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlDouble(num);
	}

	public static explicit operator SqlDouble(SqlBoolean x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble((int)x.ByteValue);
		}
		return Null;
	}

	public static implicit operator SqlDouble(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble((int)x.Value);
		}
		return Null;
	}

	public static implicit operator SqlDouble(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlDouble(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlDouble(SqlInt64 x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlDouble(SqlSingle x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlDouble(SqlMoney x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble(x.ToDouble());
		}
		return Null;
	}

	public static implicit operator SqlDouble(SqlDecimal x)
	{
		if (!x.IsNull)
		{
			return new SqlDouble(x.ToDouble());
		}
		return Null;
	}

	public static explicit operator SqlDouble(SqlString x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		return Parse(x.Value);
	}

	public static SqlBoolean operator ==(SqlDouble x, SqlDouble y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value == y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlDouble x, SqlDouble y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlDouble x, SqlDouble y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value < y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlDouble x, SqlDouble y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value > y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlDouble x, SqlDouble y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value <= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >=(SqlDouble x, SqlDouble y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value >= y.m_value);
		}
		return SqlBoolean.Null;
	}

	public static SqlDouble Add(SqlDouble x, SqlDouble y)
	{
		return x + y;
	}

	public static SqlDouble Subtract(SqlDouble x, SqlDouble y)
	{
		return x - y;
	}

	public static SqlDouble Multiply(SqlDouble x, SqlDouble y)
	{
		return x * y;
	}

	public static SqlDouble Divide(SqlDouble x, SqlDouble y)
	{
		return x / y;
	}

	public static SqlBoolean Equals(SqlDouble x, SqlDouble y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlDouble x, SqlDouble y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlDouble x, SqlDouble y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlDouble x, SqlDouble y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlDouble x, SqlDouble y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlDouble x, SqlDouble y)
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

	public SqlInt16 ToSqlInt16()
	{
		return (SqlInt16)this;
	}

	public SqlInt32 ToSqlInt32()
	{
		return (SqlInt32)this;
	}

	public SqlInt64 ToSqlInt64()
	{
		return (SqlInt64)this;
	}

	public SqlMoney ToSqlMoney()
	{
		return (SqlMoney)this;
	}

	public SqlDecimal ToSqlDecimal()
	{
		return (SqlDecimal)this;
	}

	public SqlSingle ToSqlSingle()
	{
		return (SqlSingle)this;
	}

	public SqlString ToSqlString()
	{
		return (SqlString)this;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlDouble value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlDouble));
	}

	public int CompareTo(SqlDouble value)
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
		if (!(value is SqlDouble sqlDouble))
		{
			return false;
		}
		if (sqlDouble.IsNull || IsNull)
		{
			if (sqlDouble.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlDouble).Value;
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
			m_value = XmlConvert.ToDouble(reader.ReadElementString());
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
		return new XmlQualifiedName("double", "http://www.w3.org/2001/XMLSchema");
	}
}
