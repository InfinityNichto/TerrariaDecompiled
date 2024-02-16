using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[XmlSchemaProvider("GetXsdType")]
public struct SqlSingle : INullable, IComparable, IXmlSerializable
{
	private bool _fNotNull;

	private float _value;

	public static readonly SqlSingle Null = new SqlSingle(fNull: true);

	public static readonly SqlSingle Zero = new SqlSingle(0f);

	public static readonly SqlSingle MinValue = new SqlSingle(float.MinValue);

	public static readonly SqlSingle MaxValue = new SqlSingle(float.MaxValue);

	public bool IsNull => !_fNotNull;

	public float Value
	{
		get
		{
			if (_fNotNull)
			{
				return _value;
			}
			throw new SqlNullValueException();
		}
	}

	private SqlSingle(bool fNull)
	{
		_fNotNull = false;
		_value = 0f;
	}

	public SqlSingle(float value)
	{
		if (!float.IsFinite(value))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		_fNotNull = true;
		_value = value;
	}

	public SqlSingle(double value)
		: this((float)value)
	{
	}

	public static implicit operator SqlSingle(float x)
	{
		return new SqlSingle(x);
	}

	public static explicit operator float(SqlSingle x)
	{
		return x.Value;
	}

	public override string ToString()
	{
		if (!IsNull)
		{
			return _value.ToString((IFormatProvider?)null);
		}
		return SQLResource.NullString;
	}

	public static SqlSingle Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		return new SqlSingle(float.Parse(s, CultureInfo.InvariantCulture));
	}

	public static SqlSingle operator -(SqlSingle x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle(0f - x._value);
		}
		return Null;
	}

	public static SqlSingle operator +(SqlSingle x, SqlSingle y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		float num = x._value + y._value;
		if (float.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlSingle(num);
	}

	public static SqlSingle operator -(SqlSingle x, SqlSingle y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		float num = x._value - y._value;
		if (float.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlSingle(num);
	}

	public static SqlSingle operator *(SqlSingle x, SqlSingle y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		float num = x._value * y._value;
		if (float.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlSingle(num);
	}

	public static SqlSingle operator /(SqlSingle x, SqlSingle y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (y._value == 0f)
		{
			throw new DivideByZeroException(SQLResource.DivideByZeroMessage);
		}
		float num = x._value / y._value;
		if (float.IsInfinity(num))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlSingle(num);
	}

	public static explicit operator SqlSingle(SqlBoolean x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle((int)x.ByteValue);
		}
		return Null;
	}

	public static implicit operator SqlSingle(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle((int)x.Value);
		}
		return Null;
	}

	public static implicit operator SqlSingle(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlSingle(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlSingle(SqlInt64 x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlSingle(SqlMoney x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle(x.ToDouble());
		}
		return Null;
	}

	public static implicit operator SqlSingle(SqlDecimal x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle(x.ToDouble());
		}
		return Null;
	}

	public static explicit operator SqlSingle(SqlDouble x)
	{
		if (!x.IsNull)
		{
			return new SqlSingle(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlSingle(SqlString x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		return Parse(x.Value);
	}

	public static SqlBoolean operator ==(SqlSingle x, SqlSingle y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value == y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlSingle x, SqlSingle y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlSingle x, SqlSingle y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value < y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlSingle x, SqlSingle y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value > y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlSingle x, SqlSingle y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value <= y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >=(SqlSingle x, SqlSingle y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value >= y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlSingle Add(SqlSingle x, SqlSingle y)
	{
		return x + y;
	}

	public static SqlSingle Subtract(SqlSingle x, SqlSingle y)
	{
		return x - y;
	}

	public static SqlSingle Multiply(SqlSingle x, SqlSingle y)
	{
		return x * y;
	}

	public static SqlSingle Divide(SqlSingle x, SqlSingle y)
	{
		return x / y;
	}

	public static SqlBoolean Equals(SqlSingle x, SqlSingle y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlSingle x, SqlSingle y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlSingle x, SqlSingle y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlSingle x, SqlSingle y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlSingle x, SqlSingle y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlSingle x, SqlSingle y)
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

	public SqlString ToSqlString()
	{
		return (SqlString)this;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlSingle value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlSingle));
	}

	public int CompareTo(SqlSingle value)
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
		if (!(value is SqlSingle sqlSingle))
		{
			return false;
		}
		if (sqlSingle.IsNull || IsNull)
		{
			if (sqlSingle.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlSingle).Value;
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
			_fNotNull = false;
		}
		else
		{
			_value = XmlConvert.ToSingle(reader.ReadElementString());
			_fNotNull = true;
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
			writer.WriteString(XmlConvert.ToString(_value));
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("float", "http://www.w3.org/2001/XMLSchema");
	}
}
