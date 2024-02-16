using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[XmlSchemaProvider("GetXsdType")]
public struct SqlMoney : INullable, IComparable, IXmlSerializable
{
	private bool _fNotNull;

	private long _value;

	public static readonly SqlMoney Null = new SqlMoney(fNull: true);

	public static readonly SqlMoney Zero = new SqlMoney(0);

	public static readonly SqlMoney MinValue = new SqlMoney(long.MinValue, 0);

	public static readonly SqlMoney MaxValue = new SqlMoney(long.MaxValue, 0);

	public bool IsNull => !_fNotNull;

	public decimal Value
	{
		get
		{
			if (_fNotNull)
			{
				return ToDecimal();
			}
			throw new SqlNullValueException();
		}
	}

	private SqlMoney(bool fNull)
	{
		_fNotNull = false;
		_value = 0L;
	}

	internal SqlMoney(long value, int ignored)
	{
		_value = value;
		_fNotNull = true;
	}

	public SqlMoney(int value)
	{
		_value = (long)value * 10000L;
		_fNotNull = true;
	}

	public SqlMoney(long value)
	{
		if (value < -922337203685477L || value > 922337203685477L)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		_value = value * 10000;
		_fNotNull = true;
	}

	public SqlMoney(decimal value)
	{
		SqlDecimal sqlDecimal = new SqlDecimal(value);
		sqlDecimal.AdjustScale(4 - sqlDecimal.Scale, fRound: true);
		if (sqlDecimal._data3 != 0 || sqlDecimal._data4 != 0)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		bool isPositive = sqlDecimal.IsPositive;
		ulong num = sqlDecimal._data1 + ((ulong)sqlDecimal._data2 << 32);
		if ((isPositive && num > long.MaxValue) || (!isPositive && num > 9223372036854775808uL))
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		_value = (long)(isPositive ? num : (0L - num));
		_fNotNull = true;
	}

	public SqlMoney(double value)
		: this(new decimal(value))
	{
	}

	public decimal ToDecimal()
	{
		if (IsNull)
		{
			throw new SqlNullValueException();
		}
		bool isNegative = false;
		long num = _value;
		if (_value < 0)
		{
			isNegative = true;
			num = -_value;
		}
		return new decimal((int)num, (int)(num >> 32), 0, isNegative, 4);
	}

	public long ToInt64()
	{
		if (IsNull)
		{
			throw new SqlNullValueException();
		}
		long num = _value / 1000;
		bool flag = num >= 0;
		long num2 = num % 10;
		num /= 10;
		if (num2 >= 5)
		{
			num = ((!flag) ? (num - 1) : (num + 1));
		}
		return num;
	}

	public int ToInt32()
	{
		return checked((int)ToInt64());
	}

	public double ToDouble()
	{
		return decimal.ToDouble(ToDecimal());
	}

	public static implicit operator SqlMoney(decimal x)
	{
		return new SqlMoney(x);
	}

	public static explicit operator SqlMoney(double x)
	{
		return new SqlMoney(x);
	}

	public static implicit operator SqlMoney(long x)
	{
		return new SqlMoney(new decimal(x));
	}

	public static explicit operator decimal(SqlMoney x)
	{
		return x.Value;
	}

	public override string ToString()
	{
		if (IsNull)
		{
			return SQLResource.NullString;
		}
		return ToDecimal().ToString("#0.00##", null);
	}

	public static SqlMoney Parse(string s)
	{
		if (s == SQLResource.NullString)
		{
			return Null;
		}
		decimal result;
		return (!decimal.TryParse(s, NumberStyles.Integer | NumberStyles.AllowTrailingSign | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint | NumberStyles.AllowCurrencySymbol, NumberFormatInfo.InvariantInfo, out result)) ? new SqlMoney(decimal.Parse(s, NumberStyles.Currency, NumberFormatInfo.CurrentInfo)) : new SqlMoney(result);
	}

	public static SqlMoney operator -(SqlMoney x)
	{
		if (x.IsNull)
		{
			return Null;
		}
		if (x._value == -922337203685477L)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
		return new SqlMoney(-x._value, 0);
	}

	public static SqlMoney operator +(SqlMoney x, SqlMoney y)
	{
		try
		{
			return (x.IsNull || y.IsNull) ? Null : new SqlMoney(checked(x._value + y._value), 0);
		}
		catch (OverflowException)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
	}

	public static SqlMoney operator -(SqlMoney x, SqlMoney y)
	{
		try
		{
			return (x.IsNull || y.IsNull) ? Null : new SqlMoney(checked(x._value - y._value), 0);
		}
		catch (OverflowException)
		{
			throw new OverflowException(SQLResource.ArithOverflowMessage);
		}
	}

	public static SqlMoney operator *(SqlMoney x, SqlMoney y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlMoney(decimal.Multiply(x.ToDecimal(), y.ToDecimal()));
		}
		return Null;
	}

	public static SqlMoney operator /(SqlMoney x, SqlMoney y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlMoney(decimal.Divide(x.ToDecimal(), y.ToDecimal()));
		}
		return Null;
	}

	public static explicit operator SqlMoney(SqlBoolean x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.ByteValue);
		}
		return Null;
	}

	public static implicit operator SqlMoney(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlMoney(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlMoney(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.Value);
		}
		return Null;
	}

	public static implicit operator SqlMoney(SqlInt64 x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlMoney(SqlSingle x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlMoney(SqlDouble x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlMoney(SqlDecimal x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(x.Value);
		}
		return Null;
	}

	public static explicit operator SqlMoney(SqlString x)
	{
		if (!x.IsNull)
		{
			return new SqlMoney(decimal.Parse(x.Value, NumberStyles.Currency, null));
		}
		return Null;
	}

	public static SqlBoolean operator ==(SqlMoney x, SqlMoney y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value == y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator !=(SqlMoney x, SqlMoney y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlMoney x, SqlMoney y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value < y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >(SqlMoney x, SqlMoney y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value > y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator <=(SqlMoney x, SqlMoney y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value <= y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlBoolean operator >=(SqlMoney x, SqlMoney y)
	{
		if (!x.IsNull && !y.IsNull)
		{
			return new SqlBoolean(x._value >= y._value);
		}
		return SqlBoolean.Null;
	}

	public static SqlMoney Add(SqlMoney x, SqlMoney y)
	{
		return x + y;
	}

	public static SqlMoney Subtract(SqlMoney x, SqlMoney y)
	{
		return x - y;
	}

	public static SqlMoney Multiply(SqlMoney x, SqlMoney y)
	{
		return x * y;
	}

	public static SqlMoney Divide(SqlMoney x, SqlMoney y)
	{
		return x / y;
	}

	public static SqlBoolean Equals(SqlMoney x, SqlMoney y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlMoney x, SqlMoney y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlMoney x, SqlMoney y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlMoney x, SqlMoney y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlMoney x, SqlMoney y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlMoney x, SqlMoney y)
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
		if (value is SqlMoney value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlMoney));
	}

	public int CompareTo(SqlMoney value)
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
		if (!(value is SqlMoney sqlMoney))
		{
			return false;
		}
		if (sqlMoney.IsNull || IsNull)
		{
			if (sqlMoney.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlMoney).Value;
	}

	public override int GetHashCode()
	{
		if (!IsNull)
		{
			return _value.GetHashCode();
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
			SqlMoney sqlMoney = new SqlMoney(XmlConvert.ToDecimal(reader.ReadElementString()));
			_fNotNull = sqlMoney._fNotNull;
			_value = sqlMoney._value;
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
			writer.WriteString(XmlConvert.ToString(ToDecimal()));
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("decimal", "http://www.w3.org/2001/XMLSchema");
	}
}
