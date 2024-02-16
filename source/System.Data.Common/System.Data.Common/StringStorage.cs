using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common;

internal sealed class StringStorage : DataStorage
{
	private string[] _values;

	public StringStorage(DataColumn column)
		: base(column, typeof(string), string.Empty, StorageType.String)
	{
	}

	public override object Aggregate(int[] recordNos, AggregateType kind)
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
				object obj = _values[recordNos[i]];
				if (obj != null)
				{
					num++;
				}
			}
			return num;
		}
		default:
			throw ExceptionBuilder.AggregateException(kind, _dataType);
		}
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		string text = _values[recordNo1];
		string text2 = _values[recordNo2];
		if ((object)text == text2)
		{
			return 0;
		}
		if (text == null)
		{
			return -1;
		}
		if (text2 == null)
		{
			return 1;
		}
		return _table.Compare(text, text2);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		string text = _values[recordNo];
		if (text == null)
		{
			if (_nullValue == value)
			{
				return 0;
			}
			return -1;
		}
		if (_nullValue == value)
		{
			return 1;
		}
		return _table.Compare(text, (string)value);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : value.ToString());
		}
		return value;
	}

	public override void Copy(int recordNo1, int recordNo2)
	{
		_values[recordNo2] = _values[recordNo1];
	}

	public override object Get(int recordNo)
	{
		string text = _values[recordNo];
		if (text != null)
		{
			return text;
		}
		return _nullValue;
	}

	public override int GetStringLength(int record)
	{
		return _values[record]?.Length ?? 0;
	}

	public override bool IsNull(int record)
	{
		return _values[record] == null;
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = null;
		}
		else
		{
			_values[record] = value.ToString();
		}
	}

	public override void SetCapacity(int capacity)
	{
		string[] array = new string[capacity];
		if (_values != null)
		{
			Array.Copy(_values, array, Math.Min(capacity, _values.Length));
		}
		_values = array;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		return s;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return (string)value;
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new string[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		string[] array = (string[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (string[])store;
	}
}
