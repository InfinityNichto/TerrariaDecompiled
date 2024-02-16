using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal sealed class SqlStringStorage : DataStorage
{
	private SqlString[] _values;

	public SqlStringStorage(DataColumn column)
		: base(column, typeof(SqlString), SqlString.Null, SqlString.Null, StorageType.SqlString)
	{
	}

	public override object Aggregate(int[] recordNos, AggregateType kind)
	{
		try
		{
			switch (kind)
			{
			case AggregateType.Min:
			{
				int num3 = -1;
				int i;
				for (i = 0; i < recordNos.Length; i++)
				{
					if (!IsNull(recordNos[i]))
					{
						num3 = recordNos[i];
						break;
					}
				}
				if (num3 >= 0)
				{
					for (i++; i < recordNos.Length; i++)
					{
						if (!IsNull(recordNos[i]) && Compare(num3, recordNos[i]) > 0)
						{
							num3 = recordNos[i];
						}
					}
					return Get(num3);
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				int num2 = -1;
				int i;
				for (i = 0; i < recordNos.Length; i++)
				{
					if (!IsNull(recordNos[i]))
					{
						num2 = recordNos[i];
						break;
					}
				}
				if (num2 >= 0)
				{
					for (i++; i < recordNos.Length; i++)
					{
						if (Compare(num2, recordNos[i]) < 0)
						{
							num2 = recordNos[i];
						}
					}
					return Get(num2);
				}
				return _nullValue;
			}
			case AggregateType.Count:
			{
				int num = 0;
				for (int i = 0; i < recordNos.Length; i++)
				{
					if (!IsNull(recordNos[i]))
					{
						num++;
					}
				}
				return num;
			}
			case AggregateType.First:
				break;
			}
		}
		catch (OverflowException)
		{
			throw ExprException.Overflow(typeof(SqlString));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		return Compare(_values[recordNo1], _values[recordNo2]);
	}

	public int Compare(SqlString valueNo1, SqlString valueNo2)
	{
		if (valueNo1.IsNull && valueNo2.IsNull)
		{
			return 0;
		}
		if (valueNo1.IsNull)
		{
			return -1;
		}
		if (valueNo2.IsNull)
		{
			return 1;
		}
		return _table.Compare(valueNo1.Value, valueNo2.Value);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		return Compare(_values[recordNo], (SqlString)value);
	}

	public override object ConvertValue(object value)
	{
		if (value != null)
		{
			return SqlConvert.ConvertToSqlString(value);
		}
		return _nullValue;
	}

	public override void Copy(int recordNo1, int recordNo2)
	{
		_values[recordNo2] = _values[recordNo1];
	}

	public override object Get(int record)
	{
		return _values[record];
	}

	public override int GetStringLength(int record)
	{
		SqlString sqlString = _values[record];
		if (!sqlString.IsNull)
		{
			return sqlString.Value.Length;
		}
		return 0;
	}

	public override bool IsNull(int record)
	{
		return _values[record].IsNull;
	}

	public override void Set(int record, object value)
	{
		_values[record] = SqlConvert.ConvertToSqlString(value);
	}

	public override void SetCapacity(int capacity)
	{
		SqlString[] array = new SqlString[capacity];
		if (_values != null)
		{
			Array.Copy(_values, array, Math.Min(capacity, _values.Length));
		}
		_values = array;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		SqlString sqlString = default(SqlString);
		string s2 = "<col>" + s + "</col>";
		StringReader input = new StringReader(s2);
		IXmlSerializable xmlSerializable = sqlString;
		using (XmlTextReader reader = new XmlTextReader(input))
		{
			xmlSerializable.ReadXml(reader);
		}
		return (SqlString)(object)xmlSerializable;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		StringWriter stringWriter = new StringWriter(base.FormatProvider);
		using (XmlTextWriter writer = new XmlTextWriter(stringWriter))
		{
			((IXmlSerializable)value).WriteXml(writer);
		}
		return stringWriter.ToString();
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new SqlString[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		SqlString[] array = (SqlString[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (SqlString[])store;
	}
}
