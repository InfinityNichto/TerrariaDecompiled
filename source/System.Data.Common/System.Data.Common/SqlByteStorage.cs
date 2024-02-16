using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data.Common;

internal sealed class SqlByteStorage : DataStorage
{
	private SqlByte[] _values;

	public SqlByteStorage(DataColumn column)
		: base(column, typeof(SqlByte), SqlByte.Null, SqlByte.Null, StorageType.SqlByte)
	{
	}

	public override object Aggregate(int[] records, AggregateType kind)
	{
		bool flag = false;
		try
		{
			switch (kind)
			{
			case AggregateType.Sum:
			{
				SqlInt64 sqlInt = 0L;
				foreach (int num3 in records)
				{
					if (!IsNull(num3))
					{
						sqlInt += (SqlInt64)_values[num3];
						flag = true;
					}
				}
				if (flag)
				{
					return sqlInt;
				}
				return _nullValue;
			}
			case AggregateType.Mean:
			{
				SqlInt64 sqlInt2 = 0L;
				int num5 = 0;
				foreach (int num6 in records)
				{
					if (!IsNull(num6))
					{
						sqlInt2 += _values[num6].ToSqlInt64();
						num5++;
						flag = true;
					}
				}
				if (flag)
				{
					SqlByte sqlByte2 = (byte)0;
					sqlByte2 = (sqlInt2 / num5).ToSqlByte();
					return sqlByte2;
				}
				return _nullValue;
			}
			case AggregateType.Var:
			case AggregateType.StDev:
			{
				int num = 0;
				SqlDouble sqlDouble = 0.0;
				SqlDouble sqlDouble2 = 0.0;
				SqlDouble sqlDouble3 = 0.0;
				SqlDouble sqlDouble4 = 0.0;
				foreach (int num4 in records)
				{
					if (!IsNull(num4))
					{
						sqlDouble3 += _values[num4].ToSqlDouble();
						sqlDouble4 += _values[num4].ToSqlDouble() * _values[num4].ToSqlDouble();
						num++;
					}
				}
				if (num > 1)
				{
					sqlDouble = num * sqlDouble4 - sqlDouble3 * sqlDouble3;
					sqlDouble2 = sqlDouble / (sqlDouble3 * sqlDouble3);
					if (sqlDouble2 < 1E-15 || sqlDouble < 0.0)
					{
						sqlDouble = 0.0;
					}
					else
					{
						sqlDouble /= (SqlDouble)(num * (num - 1));
					}
					if (kind == AggregateType.StDev)
					{
						return Math.Sqrt(sqlDouble.Value);
					}
					return sqlDouble;
				}
				return _nullValue;
			}
			case AggregateType.Min:
			{
				SqlByte sqlByte = SqlByte.MaxValue;
				foreach (int num2 in records)
				{
					if (!IsNull(num2))
					{
						if (SqlByte.LessThan(_values[num2], sqlByte).IsTrue)
						{
							sqlByte = _values[num2];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlByte;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				SqlByte sqlByte3 = SqlByte.MinValue;
				foreach (int num7 in records)
				{
					if (!IsNull(num7))
					{
						if (SqlByte.GreaterThan(_values[num7], sqlByte3).IsTrue)
						{
							sqlByte3 = _values[num7];
						}
						flag = true;
					}
				}
				if (flag)
				{
					return sqlByte3;
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
			throw ExprException.Overflow(typeof(SqlByte));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		return _values[recordNo1].CompareTo(_values[recordNo2]);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		return _values[recordNo].CompareTo((SqlByte)value);
	}

	public override object ConvertValue(object value)
	{
		if (value != null)
		{
			return SqlConvert.ConvertToSqlByte(value);
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
		_values[record] = SqlConvert.ConvertToSqlByte(value);
	}

	public override void SetCapacity(int capacity)
	{
		SqlByte[] array = new SqlByte[capacity];
		if (_values != null)
		{
			Array.Copy(_values, array, Math.Min(capacity, _values.Length));
		}
		_values = array;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		SqlByte sqlByte = default(SqlByte);
		string s2 = "<col>" + s + "</col>";
		StringReader input = new StringReader(s2);
		IXmlSerializable xmlSerializable = sqlByte;
		using (XmlTextReader reader = new XmlTextReader(input))
		{
			xmlSerializable.ReadXml(reader);
		}
		return (SqlByte)(object)xmlSerializable;
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
		return new SqlByte[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		SqlByte[] array = (SqlByte[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(record, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (SqlByte[])store;
	}
}
