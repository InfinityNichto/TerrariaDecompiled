using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[XmlSchemaProvider("GetXsdType")]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct SqlString : INullable, IComparable, IXmlSerializable
{
	private string m_value;

	private CompareInfo m_cmpInfo;

	private readonly int m_lcid;

	private readonly SqlCompareOptions m_flag;

	private bool m_fNotNull;

	public static readonly SqlString Null = new SqlString(fNull: true);

	internal static readonly UnicodeEncoding s_unicodeEncoding = new UnicodeEncoding();

	public static readonly int IgnoreCase = 1;

	public static readonly int IgnoreWidth = 16;

	public static readonly int IgnoreNonSpace = 2;

	public static readonly int IgnoreKanaType = 8;

	public static readonly int BinarySort = 32768;

	public static readonly int BinarySort2 = 16384;

	public bool IsNull => !m_fNotNull;

	public string Value
	{
		get
		{
			if (!IsNull)
			{
				return m_value;
			}
			throw new SqlNullValueException();
		}
	}

	public int LCID
	{
		get
		{
			if (!IsNull)
			{
				return m_lcid;
			}
			throw new SqlNullValueException();
		}
	}

	public CultureInfo CultureInfo
	{
		get
		{
			if (!IsNull)
			{
				return System.Globalization.CultureInfo.GetCultureInfo(m_lcid);
			}
			throw new SqlNullValueException();
		}
	}

	public CompareInfo CompareInfo
	{
		get
		{
			if (!IsNull)
			{
				SetCompareInfo();
				return m_cmpInfo;
			}
			throw new SqlNullValueException();
		}
	}

	public SqlCompareOptions SqlCompareOptions
	{
		get
		{
			if (!IsNull)
			{
				return m_flag;
			}
			throw new SqlNullValueException();
		}
	}

	private SqlString(bool fNull)
	{
		m_value = null;
		m_cmpInfo = null;
		m_lcid = 0;
		m_flag = SqlCompareOptions.None;
		m_fNotNull = false;
	}

	public SqlString(int lcid, SqlCompareOptions compareOptions, byte[]? data, int index, int count, bool fUnicode)
	{
		m_lcid = lcid;
		ValidateSqlCompareOptions(compareOptions);
		m_flag = compareOptions;
		if (data == null)
		{
			m_fNotNull = false;
			m_value = null;
			m_cmpInfo = null;
			return;
		}
		m_fNotNull = true;
		m_cmpInfo = null;
		if (fUnicode)
		{
			m_value = s_unicodeEncoding.GetString(data, index, count);
			return;
		}
		CultureInfo cultureInfo = new CultureInfo(m_lcid);
		Encoding encoding = Encoding.GetEncoding(cultureInfo.TextInfo.ANSICodePage);
		m_value = encoding.GetString(data, index, count);
	}

	public SqlString(int lcid, SqlCompareOptions compareOptions, byte[] data, bool fUnicode)
		: this(lcid, compareOptions, data, 0, data.Length, fUnicode)
	{
	}

	public SqlString(int lcid, SqlCompareOptions compareOptions, byte[]? data, int index, int count)
		: this(lcid, compareOptions, data, index, count, fUnicode: true)
	{
	}

	public SqlString(int lcid, SqlCompareOptions compareOptions, byte[] data)
		: this(lcid, compareOptions, data, 0, data.Length, fUnicode: true)
	{
	}

	public SqlString(string? data, int lcid, SqlCompareOptions compareOptions)
	{
		m_lcid = lcid;
		ValidateSqlCompareOptions(compareOptions);
		m_flag = compareOptions;
		m_cmpInfo = null;
		if (data == null)
		{
			m_fNotNull = false;
			m_value = null;
		}
		else
		{
			m_fNotNull = true;
			m_value = data;
		}
	}

	public SqlString(string? data, int lcid)
		: this(data, lcid, SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth)
	{
	}

	public SqlString(string? data)
		: this(data, System.Globalization.CultureInfo.CurrentCulture.LCID, SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth)
	{
	}

	private SqlString(int lcid, SqlCompareOptions compareOptions, string data, CompareInfo cmpInfo)
	{
		m_lcid = lcid;
		ValidateSqlCompareOptions(compareOptions);
		m_flag = compareOptions;
		if (data == null)
		{
			m_fNotNull = false;
			m_value = null;
			m_cmpInfo = null;
		}
		else
		{
			m_value = data;
			m_cmpInfo = cmpInfo;
			m_fNotNull = true;
		}
	}

	private void SetCompareInfo()
	{
		if (m_cmpInfo == null)
		{
			m_cmpInfo = System.Globalization.CultureInfo.GetCultureInfo(m_lcid).CompareInfo;
		}
	}

	public static implicit operator SqlString(string x)
	{
		return new SqlString(x);
	}

	public static explicit operator string(SqlString x)
	{
		return x.Value;
	}

	public override string ToString()
	{
		if (!IsNull)
		{
			return m_value;
		}
		return SQLResource.NullString;
	}

	public byte[]? GetUnicodeBytes()
	{
		if (IsNull)
		{
			return null;
		}
		return s_unicodeEncoding.GetBytes(m_value);
	}

	public byte[]? GetNonUnicodeBytes()
	{
		if (IsNull)
		{
			return null;
		}
		CultureInfo cultureInfo = new CultureInfo(m_lcid);
		Encoding encoding = Encoding.GetEncoding(cultureInfo.TextInfo.ANSICodePage);
		return encoding.GetBytes(m_value);
	}

	public static SqlString operator +(SqlString x, SqlString y)
	{
		if (x.IsNull || y.IsNull)
		{
			return Null;
		}
		if (x.m_lcid != y.m_lcid || x.m_flag != y.m_flag)
		{
			throw new SqlTypeException(SQLResource.ConcatDiffCollationMessage);
		}
		return new SqlString(x.m_lcid, x.m_flag, x.m_value + y.m_value, (x.m_cmpInfo == null) ? y.m_cmpInfo : x.m_cmpInfo);
	}

	private static int StringCompare(SqlString x, SqlString y)
	{
		if (x.m_lcid != y.m_lcid || x.m_flag != y.m_flag)
		{
			throw new SqlTypeException(SQLResource.CompareDiffCollationMessage);
		}
		x.SetCompareInfo();
		y.SetCompareInfo();
		if ((x.m_flag & SqlCompareOptions.BinarySort) != 0)
		{
			return CompareBinary(x, y);
		}
		if ((x.m_flag & SqlCompareOptions.BinarySort2) != 0)
		{
			return CompareBinary2(x, y);
		}
		string value = x.m_value;
		string value2 = y.m_value;
		int num = value.Length;
		int num2 = value2.Length;
		while (num > 0 && value[num - 1] == ' ')
		{
			num--;
		}
		while (num2 > 0 && value2[num2 - 1] == ' ')
		{
			num2--;
		}
		CompareOptions options = CompareOptionsFromSqlCompareOptions(x.m_flag);
		return x.m_cmpInfo.Compare(x.m_value, 0, num, y.m_value, 0, num2, options);
	}

	private static SqlBoolean Compare(SqlString x, SqlString y, EComparison ecExpectedResult)
	{
		if (x.IsNull || y.IsNull)
		{
			return SqlBoolean.Null;
		}
		int num = StringCompare(x, y);
		bool flag = false;
		switch (ecExpectedResult)
		{
		case EComparison.EQ:
			flag = num == 0;
			break;
		case EComparison.LT:
			flag = num < 0;
			break;
		case EComparison.LE:
			flag = num <= 0;
			break;
		case EComparison.GT:
			flag = num > 0;
			break;
		case EComparison.GE:
			flag = num >= 0;
			break;
		default:
			return SqlBoolean.Null;
		}
		return new SqlBoolean(flag);
	}

	public static explicit operator SqlString(SqlBoolean x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.Value.ToString());
		}
		return Null;
	}

	public static explicit operator SqlString(SqlByte x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.Value.ToString((IFormatProvider?)null));
		}
		return Null;
	}

	public static explicit operator SqlString(SqlInt16 x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.Value.ToString((IFormatProvider?)null));
		}
		return Null;
	}

	public static explicit operator SqlString(SqlInt32 x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.Value.ToString((IFormatProvider?)null));
		}
		return Null;
	}

	public static explicit operator SqlString(SqlInt64 x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.Value.ToString((IFormatProvider?)null));
		}
		return Null;
	}

	public static explicit operator SqlString(SqlSingle x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.Value.ToString((IFormatProvider?)null));
		}
		return Null;
	}

	public static explicit operator SqlString(SqlDouble x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.Value.ToString((IFormatProvider?)null));
		}
		return Null;
	}

	public static explicit operator SqlString(SqlDecimal x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.ToString());
		}
		return Null;
	}

	public static explicit operator SqlString(SqlMoney x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.ToString());
		}
		return Null;
	}

	public static explicit operator SqlString(SqlDateTime x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.ToString());
		}
		return Null;
	}

	public static explicit operator SqlString(SqlGuid x)
	{
		if (!x.IsNull)
		{
			return new SqlString(x.ToString());
		}
		return Null;
	}

	public SqlString Clone()
	{
		if (IsNull)
		{
			return new SqlString(fNull: true);
		}
		return new SqlString(m_value, m_lcid, m_flag);
	}

	public static SqlBoolean operator ==(SqlString x, SqlString y)
	{
		return Compare(x, y, EComparison.EQ);
	}

	public static SqlBoolean operator !=(SqlString x, SqlString y)
	{
		return !(x == y);
	}

	public static SqlBoolean operator <(SqlString x, SqlString y)
	{
		return Compare(x, y, EComparison.LT);
	}

	public static SqlBoolean operator >(SqlString x, SqlString y)
	{
		return Compare(x, y, EComparison.GT);
	}

	public static SqlBoolean operator <=(SqlString x, SqlString y)
	{
		return Compare(x, y, EComparison.LE);
	}

	public static SqlBoolean operator >=(SqlString x, SqlString y)
	{
		return Compare(x, y, EComparison.GE);
	}

	public static SqlString Concat(SqlString x, SqlString y)
	{
		return x + y;
	}

	public static SqlString Add(SqlString x, SqlString y)
	{
		return x + y;
	}

	public static SqlBoolean Equals(SqlString x, SqlString y)
	{
		return x == y;
	}

	public static SqlBoolean NotEquals(SqlString x, SqlString y)
	{
		return x != y;
	}

	public static SqlBoolean LessThan(SqlString x, SqlString y)
	{
		return x < y;
	}

	public static SqlBoolean GreaterThan(SqlString x, SqlString y)
	{
		return x > y;
	}

	public static SqlBoolean LessThanOrEqual(SqlString x, SqlString y)
	{
		return x <= y;
	}

	public static SqlBoolean GreaterThanOrEqual(SqlString x, SqlString y)
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

	public SqlDateTime ToSqlDateTime()
	{
		return (SqlDateTime)this;
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

	public SqlGuid ToSqlGuid()
	{
		return (SqlGuid)this;
	}

	private static void ValidateSqlCompareOptions(SqlCompareOptions compareOptions)
	{
		if ((compareOptions & (SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreNonSpace | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth | SqlCompareOptions.BinarySort | SqlCompareOptions.BinarySort2)) != compareOptions)
		{
			throw new ArgumentOutOfRangeException("compareOptions");
		}
	}

	public static CompareOptions CompareOptionsFromSqlCompareOptions(SqlCompareOptions compareOptions)
	{
		CompareOptions compareOptions2 = CompareOptions.None;
		ValidateSqlCompareOptions(compareOptions);
		if ((compareOptions & (SqlCompareOptions.BinarySort | SqlCompareOptions.BinarySort2)) != 0)
		{
			throw ADP.ArgumentOutOfRange("compareOptions");
		}
		if ((compareOptions & SqlCompareOptions.IgnoreCase) != 0)
		{
			compareOptions2 |= CompareOptions.IgnoreCase;
		}
		if ((compareOptions & SqlCompareOptions.IgnoreNonSpace) != 0)
		{
			compareOptions2 |= CompareOptions.IgnoreNonSpace;
		}
		if ((compareOptions & SqlCompareOptions.IgnoreKanaType) != 0)
		{
			compareOptions2 |= CompareOptions.IgnoreKanaType;
		}
		if ((compareOptions & SqlCompareOptions.IgnoreWidth) != 0)
		{
			compareOptions2 |= CompareOptions.IgnoreWidth;
		}
		return compareOptions2;
	}

	private bool FBinarySort()
	{
		if (!IsNull)
		{
			return (m_flag & (SqlCompareOptions.BinarySort | SqlCompareOptions.BinarySort2)) != 0;
		}
		return false;
	}

	private static int CompareBinary(SqlString x, SqlString y)
	{
		byte[] bytes = s_unicodeEncoding.GetBytes(x.m_value);
		byte[] bytes2 = s_unicodeEncoding.GetBytes(y.m_value);
		int num = bytes.Length;
		int num2 = bytes2.Length;
		int num3 = ((num < num2) ? num : num2);
		int i;
		for (i = 0; i < num3; i++)
		{
			if (bytes[i] < bytes2[i])
			{
				return -1;
			}
			if (bytes[i] > bytes2[i])
			{
				return 1;
			}
		}
		i = num3;
		int num4 = 32;
		if (num < num2)
		{
			for (; i < num2; i += 2)
			{
				int num5 = bytes2[i + 1] << 8 + bytes2[i];
				if (num5 != num4)
				{
					if (num4 <= num5)
					{
						return -1;
					}
					return 1;
				}
			}
		}
		else
		{
			for (; i < num; i += 2)
			{
				int num5 = bytes[i + 1] << 8 + bytes[i];
				if (num5 != num4)
				{
					if (num5 <= num4)
					{
						return -1;
					}
					return 1;
				}
			}
		}
		return 0;
	}

	private static int CompareBinary2(SqlString x, SqlString y)
	{
		string value = x.m_value;
		string value2 = y.m_value;
		int length = value.Length;
		int length2 = value2.Length;
		int num = ((length < length2) ? length : length2);
		for (int i = 0; i < num; i++)
		{
			if (value[i] < value2[i])
			{
				return -1;
			}
			if (value[i] > value2[i])
			{
				return 1;
			}
		}
		char c = ' ';
		if (length < length2)
		{
			for (int i = num; i < length2; i++)
			{
				if (value2[i] != c)
				{
					if (c <= value2[i])
					{
						return -1;
					}
					return 1;
				}
			}
		}
		else
		{
			for (int i = num; i < length; i++)
			{
				if (value[i] != c)
				{
					if (value[i] <= c)
					{
						return -1;
					}
					return 1;
				}
			}
		}
		return 0;
	}

	public int CompareTo(object? value)
	{
		if (value is SqlString value2)
		{
			return CompareTo(value2);
		}
		throw ADP.WrongType(value.GetType(), typeof(SqlString));
	}

	public int CompareTo(SqlString value)
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
		int num = StringCompare(this, value);
		if (num < 0)
		{
			return -1;
		}
		if (num > 0)
		{
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (!(value is SqlString sqlString))
		{
			return false;
		}
		if (sqlString.IsNull || IsNull)
		{
			if (sqlString.IsNull)
			{
				return IsNull;
			}
			return false;
		}
		return (this == sqlString).Value;
	}

	public override int GetHashCode()
	{
		if (IsNull)
		{
			return 0;
		}
		byte[] array;
		if (FBinarySort())
		{
			array = s_unicodeEncoding.GetBytes(m_value.TrimEnd());
		}
		else
		{
			CompareInfo compareInfo;
			CompareOptions options;
			try
			{
				SetCompareInfo();
				compareInfo = m_cmpInfo;
				options = CompareOptionsFromSqlCompareOptions(m_flag);
			}
			catch (ArgumentException)
			{
				compareInfo = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
				options = CompareOptions.None;
			}
			array = compareInfo.GetSortKey(m_value.TrimEnd(), options).KeyData;
		}
		return SqlBinary.HashByteArray(array, array.Length);
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
			m_value = reader.ReadElementString();
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
			writer.WriteString(m_value);
		}
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");
	}
}
