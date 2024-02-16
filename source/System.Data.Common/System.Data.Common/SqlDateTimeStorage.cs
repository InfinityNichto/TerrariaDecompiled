using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal sealed class SqlDateTimeStorage : DataStorage
{
	private SqlDateTime[] _values;

	public SqlDateTimeStorage(DataColumn column)
		: base(column, typeof(SqlDateTime), SqlDateTime.Null, SqlDateTime.Null, StorageType.SqlDateTime)
	{
	}

	public override object Aggregate(int[] records, AggregateType kind)
	{
		bool flag = false;
		try
		{
			switch (kind)
			{
			case AggregateType.Min:
			{
				SqlDateTime sqlDateTime2 = SqlDateTime.MaxValue;
				foreach (int num3 in records)
				{
					if (!IsNull(num3))
					{
						if (SqlDateTime.LessThan(_values[num3], sqlDateTime2).IsTrue)
						{
							sqlDateTime2 = _values[num3];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlDateTime2;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				SqlDateTime sqlDateTime = SqlDateTime.MinValue;
				foreach (int num2 in records)
				{
					if (!IsNull(num2))
					{
						if (SqlDateTime.GreaterThan(_values[num2], sqlDateTime).IsTrue)
						{
							sqlDateTime = _values[num2];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlDateTime;
				}
				return _nullValue;
			}
			case AggregateType.First:
				if (records.Length != 0)
				{
					return _values[records[0]];
				}
				return null;
			case AggregateType.Count:
			{
				int num = 0;
				for (int i = 0; i < records.Length; i++)
				{
					if (!IsNull(records[i]))
					{
						num++;
					}
				}
				return num;
			}
			}
		}
		catch (OverflowException)
		{
			throw ExprException.Overflow(typeof(SqlDateTime));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		return _values[recordNo1].CompareTo(_values[recordNo2]);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		return _values[recordNo].CompareTo((SqlDateTime)value);
	}

	public override object ConvertValue(object value)
	{
		if (value != null)
		{
			return SqlConvert.ConvertToSqlDateTime(value);
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

	public override bool IsNull(int record)
	{
		return _values[record].IsNull;
	}

	public override void Set(int record, object value)
	{
		_values[record] = SqlConvert.ConvertToSqlDateTime(value);
	}

	public override void SetCapacity(int capacity)
	{
		SqlDateTime[] array = new SqlDateTime[capacity];
		if (_values != null)
		{
			Array.Copy(_values, array, Math.Min(capacity, _values.Length));
		}
		_values = array;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		SqlDateTime sqlDateTime = default(SqlDateTime);
		string s2 = "<col>" + s + "</col>";
		StringReader input = new StringReader(s2);
		IXmlSerializable xmlSerializable = sqlDateTime;
		using (XmlTextReader reader = new XmlTextReader(input))
		{
			xmlSerializable.ReadXml(reader);
		}
		return (SqlDateTime)(object)xmlSerializable;
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
		return new SqlDateTime[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		SqlDateTime[] array = (SqlDateTime[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(record, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (SqlDateTime[])store;
	}
}
