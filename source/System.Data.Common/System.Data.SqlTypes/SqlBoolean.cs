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
public struct SqlBoolean : INullable, IComparable, IXmlSerializable
{
	private byte m_value;

	public static readonly SqlBoolean True = new SqlBoolean(value: true);

	public static readonly SqlBoolean False = new SqlBoolean(value: false);

	public static readonly SqlBoolean Null = new SqlBoolean(0, fNull: true);

	public static readonly SqlBoolean Zero = new SqlBoolean(0);

	public static readonly SqlBoolean One = new SqlBoolean(1);

	public bool IsNull => m_value == 0;

	public bool Value => m_value switch
	{
		2 => true, 
		1 => false, 
		_ => throw new SqlNullValueException(), 
	};

	public bool IsTrue => m_value == 2;

	public bool IsFalse => m_value == 1;

	public byte ByteValue
	{
		get
		{
			if (!IsNull)
			{
				if (m_value != 2)
				{
					return 0;
				}
				return 1;
			}
			throw new SqlNullValueException();
		}
	}

	public SqlBoolean(bool value)
	{
		m_value = (byte)((!value) ? 1 : 2);
	}

	public SqlBoolean(int value)
		: this(value, fNull: false)
	{
	}

	private SqlBoolean(int value, bool fNull)
	{
		if (fNull)
		{
			m_value = 0;
		}
		else
		{
			m_value = (byte)((value == 0) ? 1 : 2);
		}
	}

	public static implicit operator SqlBoolean(bool x)
	{
		return new SqlBoolean(x);
	}

	public static explicit operator bool(SqlBoolean x)
	{
		return x.Value;
	}

	public static SqlBoolean operator !(SqlBoolean x)
	{
		return x.m_value switch
		{
			2 => False, 
			1 => True, 
			_ => Null, 
		};
	}

	public static bool operator true(SqlBoolean x)
	{
		return x.IsTrue;
	}

	public static bool operator false(SqlBoolean x)
	{
		return x.IsFalse;
	}

	public static SqlBoolean operator &(SqlBoolean x, SqlBoolean y)
	{
		if (x.m_value == 1 || y.m_value == 1)
		{
			return False;
		}
		if (x.m_value == 2 && y.m_value == 2)
		{
			return True;
		}
		return Null;
	}

	public static SqlBoolean operator |(SqlBoolean x, SqlBoolean y)
	{
		if (x.m_value == 2 || y.m_value == 2)
		{
			return True;
		}
		if (x.m_value == 1 && y.m_value == 1)
		{
			return False;
		}
		return Null;
	}

	public override string ToString()
	{
		if (!IsNull)
		{
			return Value.ToString();
		}
		return SQLResource.NullString;
	}

	public static SqlBoolean Parse(string s)
	{
		if (s == null)
		{
			return new SqlBoolean(bool.Parse(s));
		}
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		s = s.TrimStart();
		char c = s[0];
		if (char.IsNumber(c) || '-' == c || '+' == c)
		{
			return new SqlBoolean(int.Parse(s, null));
		}
		return new SqlBoolean(bool.Parse(s));
	}

	public static SqlBoolean operator ~(SqlBoolean x)
	{
		return !x;
	}

	public static SqlBoolean operator ^(SqlBoolean x, SqlBoolean y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value != y.m_value);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlBoolean(x.Value != 0);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlBoolean(x.Value != 0);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlBoolean(x.Value != 0);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlInt64 x)
	{
		if (!x.IsNull)
		{
			return new SqlBoolean(x.Value != 0);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlDouble x)
	{
		if (!x.IsNull)
		{
			return new SqlBoolean(x.Value != 0.0);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlSingle x)
	{
		if (!x.IsNull)
		{
			return new SqlBoolean((double)x.Value != 0.0);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlMoney x)
	{
		if (!x.IsNull)
		{
			return x != SqlMoney.Zero;
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlDecimal x)
	{
		if (!x.IsNull)
		{
			return new SqlBoolean(x._data1 != 0 || x._data2 != 0 || x._data3 != 0 || x._data4 != 0);
		}
		return Null;
	}

	public static explicit operator SqlBoolean(SqlString x)
	{
		if (!x.IsNull)
		{
			return Parse(x.Value);
		}
		return Null;
	}

	public static SqlBoolean operator ==(SqlBoolean x, SqlBoolean y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value == y.m_value);
		}
		return Null;
	}

	public static SqlBoolean operator !=(SqlBoolean x, SqlBoolean y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlBoolean x, SqlBoolean y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value < y.m_value);
		}
		return Null;
	}

	public static SqlBoolean operator >(SqlBoolean x, SqlBoolean y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value > y.m_value);
		}
		return Null;
	}

	public static SqlBoolean operator <=(SqlBoolean x, SqlBoolean y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value <= y.m_value);
		}
		return Null;
	}

	public static SqlBoolean operator >=(SqlBoolean x, SqlBoolean y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x.m_value >= y.m_value);
		}
		return Null;
	}

	public static SqlBoolean OnesComplement(SqlBoolean x)
	{
		return ~x;
	}

	public static SqlBoolean And(SqlBoolean x, SqlBoolean y)
	{
		return x & y;
	}

	public static SqlBoolean Or(SqlBoolean x, SqlBoolean y)
	{
		return x | y;
	}

	public static SqlBoolean Xor(SqlBoolean x, SqlBoolean y)
	{
		return x ^ y;
	}

	public static SqlBoolean Equals(SqlBoolean x, SqlBoolean y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlBoolean x, SqlBoolean y)
	{
		return x != y;
	}

	public static SqlBoolean GreaterThan(SqlBoolean x, SqlBoolean y)
	{
		return x > y;
	}

	public static SqlBoolean LessThan(SqlBoolean x, SqlBoolean y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThanOrEquals(SqlBoolean x, SqlBoolean y)
	{
		return x >= y;
	}

	public static SqlBoolean LessThanOrEquals(SqlBoolean x, SqlBoolean y)
	{
		return x <= y;
	}

	public SqlByte ToSqlByte()
	{
		return (SqlByte)this;
	}

	public SqlDouble ToSqlDouble()
	{
		return (SqlDouble)this;
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
		if (value is SqlBoolean value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlBoolean));
	}

	public int CompareTo(SqlBoolean value)
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
		if (ByteValue < value.ByteValue)
		{
			return -1;
		}
		if (ByteValue > value.ByteValue)
		{
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (!(value is SqlBoolean sqlBoolean))
		{
			return false;
		}
		if (sqlBoolean.IsNull || IsNull)
		{
			if (sqlBoolean.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlBoolean).Value;
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
			m_value = 0;
		}
		else
		{
			m_value = (byte)((!XmlConvert.ToBoolean(reader.ReadElementString())) ? 1 : 2);
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
			writer.WriteString((m_value == 2) ? "true" : "false");
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("boolean", "http://www.w3.org/2001/XMLSchema");
	}
}
