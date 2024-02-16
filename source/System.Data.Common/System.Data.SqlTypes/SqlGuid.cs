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
public struct SqlGuid : INullable, IComparable, IXmlSerializable
{
	private static readonly int[] s_rgiGuidOrder = new int[16]
	{
		10, 11, 12, 13, 14, 15, 8, 9, 6, 7,
		4, 5, 0, 1, 2, 3
	};

	private byte[] m_value;

	public static readonly SqlGuid Null = new SqlGuid(fNull: true);

	public bool IsNull => m_value == null;

	public Guid Value
	{
		get
		{
			if (m_value == null)
			{
				throw new SqlNullValueException();
			}
			return new Guid(m_value);
		}
	}

	private SqlGuid(bool fNull)
	{
		m_value = null;
	}

	public SqlGuid(byte[] value)
	{
		if (value == null || value.Length != 16)
		{
			throw new ArgumentException(SQLResource.InvalidArraySizeMessage);
		}
		m_value = new byte[16];
		value.CopyTo(m_value, 0);
	}

	public SqlGuid(string s)
	{
		m_value = new Guid(s).ToByteArray();
	}

	public SqlGuid(Guid g)
	{
		m_value = g.ToByteArray();
	}

	public SqlGuid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k)
		: this(new Guid(a, b, c, d, e, f, g, h, i, j, k))
	{
	}

	public static implicit operator SqlGuid(Guid x)
	{
		return new SqlGuid(x);
	}

	public static explicit operator Guid(SqlGuid x)
	{
		return x.Value;
	}

	public byte[]? ToByteArray()
	{
		byte[] array = new byte[16];
		m_value.CopyTo(array, 0);
		return array;
	}

	public override string ToString()
	{
		if (m_value == null)
		{
			return SQLResource.NullString;
		}
		return new Guid(m_value).ToString();
	}

	public static SqlGuid Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		return new SqlGuid(s);
	}

	private static EComparison Compare(SqlGuid x, SqlGuid y)
	{
		for (int i = 0; i < 16; i++)
		{
			byte b = x.m_value[s_rgiGuidOrder[i]];
			byte b2 = y.m_value[s_rgiGuidOrder[i]];
			if (b != b2)
			{
				if (b >= b2)
				{
					return EComparison.GT;
				}
				return EComparison.LT;
			}
		}
		return EComparison.EQ;
	}

	public static explicit operator SqlGuid(SqlString x)
	{
		if (!x.IsNull)
		{
			return new SqlGuid(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlGuid(SqlBinary x)
	{
		if (!x.IsNull)
		{
			return new SqlGuid(x.Value);
		}
		return Null;
	}

	public static SqlBoolean operator ==(SqlGuid x, SqlGuid y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(Compare(x, y) == EComparison.EQ);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlGuid x, SqlGuid y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlGuid x, SqlGuid y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(Compare(x, y) == EComparison.LT);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlGuid x, SqlGuid y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(Compare(x, y) == EComparison.GT);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlGuid x, SqlGuid y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		EComparison eComparison = Compare(x, y);
		return new SqlBoolean(eComparison == EComparison.LT || eComparison == EComparison.EQ);
	}

	public static SqlBoolean operator >=(SqlGuid x, SqlGuid y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		EComparison eComparison = Compare(x, y);
		return new SqlBoolean(eComparison == EComparison.GT || eComparison == EComparison.EQ);
	}

	public static SqlBoolean Equals(SqlGuid x, SqlGuid y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlGuid x, SqlGuid y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlGuid x, SqlGuid y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlGuid x, SqlGuid y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlGuid x, SqlGuid y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlGuid x, SqlGuid y)
	{
		return x >= y;
	}

	public SqlString ToSqlString()
	{
		return (SqlString)this;
	}

	public SqlBinary ToSqlBinary()
	{
		return (SqlBinary)this;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlGuid value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlGuid));
	}

	public int CompareTo(SqlGuid value)
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
		if (!(value is SqlGuid sqlGuid))
		{
			return false;
		}
		if (sqlGuid.IsNull || IsNull)
		{
			if (sqlGuid.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlGuid).Value;
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
			m_value = null;
		}
		else
		{
			m_value = new Guid(reader.ReadElementString()).ToByteArray();
		}
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		if (m_value == null)
		{
			writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
		}
		else
		{
			writer.WriteString(XmlConvert.ToString(new Guid(m_value)));
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");
	}
}
