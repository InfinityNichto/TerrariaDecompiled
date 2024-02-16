using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[XmlSchemaProvider("GetXsdType")]
public struct SqlBinary : INullable, IComparable, IXmlSerializable
{
	private byte[] _value;

	public static readonly SqlBinary Null = new SqlBinary(fNull: true);

	public bool IsNull => _value == null;

	public byte[] Value
	{
		get
		{
			if (_value == null)
			{
				throw new SqlNullValueException();
			}
			byte[] array = new byte[_value.Length];
			_value.CopyTo(array, 0);
			return array;
		}
	}

	public byte this[int index]
	{
		get
		{
			if (_value == null)
			{
				throw new SqlNullValueException();
			}
			return _value[index];
		}
	}

	public int Length
	{
		get
		{
			if (_value != null)
			{
				return _value.Length;
			}
			throw new SqlNullValueException();
		}
	}

	private SqlBinary(bool fNull)
	{
		_value = null;
	}

	public SqlBinary(byte[]? value)
	{
		if (value == null)
		{
			_value = null;
			return;
		}
		_value = new byte[value.Length];
		value.CopyTo(_value, 0);
	}

	public static implicit operator SqlBinary(byte[] x)
	{
		return new SqlBinary(x);
	}

	public static explicit operator byte[]?(SqlBinary x)
	{
		return x.Value;
	}

	public override string ToString()
	{
		if (_value != null)
		{
			return $"SqlBinary({_value.Length})";
		}
		return SQLResource.NullString;
	}

	public static SqlBinary operator +(SqlBinary x, SqlBinary y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		byte[] array = new byte[x.Value.Length + y.Value.Length];
		x.Value.CopyTo(array, 0);
		y.Value.CopyTo(array, x.Value.Length);
		return new SqlBinary(array);
	}

	private static EComparison PerformCompareByte(byte[] x, byte[] y)
	{
		int num = ((x.Length < y.Length) ? x.Length : y.Length);
		for (int i = 0; i < num; i++)
		{
			if (x[i] != y[i])
			{
				if (x[i] < y[i])
				{
					return EComparison.LT;
				}
				return EComparison.GT;
			}
		}
		if (x.Length == y.Length)
		{
			return EComparison.EQ;
		}
		byte b = 0;
		if (x.Length < y.Length)
		{
			for (int i = num; i < y.Length; i++)
			{
				if (y[i] != b)
				{
					return EComparison.LT;
				}
			}
		}
		else
		{
			for (int i = num; i < x.Length; i++)
			{
				if (x[i] != b)
				{
					return EComparison.GT;
				}
			}
		}
		return EComparison.EQ;
	}

	public static explicit operator SqlBinary(SqlGuid x)
	{
		if (!x.IsNull)
		{
			return new SqlBinary(x.ToByteArray());
		}
		return Null;
	}

	public static SqlBoolean operator ==(SqlBinary x, SqlBinary y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.EQ);
	}

	public static SqlBoolean operator !=(SqlBinary x, SqlBinary y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlBinary x, SqlBinary y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.LT);
	}

	public static SqlBoolean operator >(SqlBinary x, SqlBinary y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		return new SqlBoolean(PerformCompareByte(x.Value, y.Value) == EComparison.GT);
	}

	public static SqlBoolean operator <=(SqlBinary x, SqlBinary y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		EComparison eComparison = PerformCompareByte(x.Value, y.Value);
		return new SqlBoolean(eComparison == EComparison.LT || eComparison == EComparison.EQ);
	}

	public static SqlBoolean operator >=(SqlBinary x, SqlBinary y)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		EComparison eComparison = PerformCompareByte(x.Value, y.Value);
		return new SqlBoolean(eComparison == EComparison.GT || eComparison == EComparison.EQ);
	}

	public static SqlBinary Add(SqlBinary x, SqlBinary y)
	{
		return x + y;
	}

	public static SqlBinary Concat(SqlBinary x, SqlBinary y)
	{
		return x + y;
	}

	public static SqlBoolean Equals(SqlBinary x, SqlBinary y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlBinary x, SqlBinary y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlBinary x, SqlBinary y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlBinary x, SqlBinary y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlBinary x, SqlBinary y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlBinary x, SqlBinary y)
	{
		return x >= y;
	}

	public SqlGuid ToSqlGuid()
	{
		return (SqlGuid)this;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlBinary value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlBinary));
	}

	public int CompareTo(SqlBinary value)
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
		if (!(value is SqlBinary sqlBinary))
		{
			return false;
		}
		if (sqlBinary.IsNull || IsNull)
		{
			if (sqlBinary.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlBinary).Value;
	}

	internal static int HashByteArray(byte[] rgbValue, int length)
	{
		if (length <= 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < length; i++)
		{
			int num2 = (num >> 28) & 0xFF;
			num <<= 4;
			num = num ^ rgbValue[i] ^ num2;
		}
		return num;
	}

	public override int GetHashCode()
	{
		if (_value == null)
		{
			return 0;
		}
		int num = _value.Length;
		while (num > 0 && _value[num - 1] == 0)
		{
			num--;
		}
		return HashByteArray(_value, num);
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
			_value = null;
			return;
		}
		string text = reader.ReadElementString();
		if (text == null)
		{
			_value = Array.Empty<byte>();
			return;
		}
		text = text.Trim();
		if (text.Length == 0)
		{
			_value = Array.Empty<byte>();
		}
		else
		{
			_value = Convert.FromBase64String(text);
		}
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		if (_value == null)
		{
			writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
		}
		else
		{
			writer.WriteString(Convert.ToBase64String(_value));
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("base64Binary", "http://www.w3.org/2001/XMLSchema");
	}
}
