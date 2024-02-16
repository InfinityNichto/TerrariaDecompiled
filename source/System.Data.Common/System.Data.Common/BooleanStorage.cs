using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common;

internal sealed class BooleanStorage : DataStorage
{
	private bool[] _values;

	internal BooleanStorage(DataColumn column)
		: base(column, typeof(bool), false, StorageType.Boolean)
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
				bool flag3 = true;
				foreach (int num2 in records)
				{
					if (!IsNull(num2))
					{
						flag3 = _values[num2] && flag3;
						flag = true;
					}
				}
				if (flag)
				{
					return flag3;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				bool flag2 = false;
				foreach (int num in records)
				{
					if (!IsNull(num))
					{
						flag2 = _values[num] || flag2;
						flag = true;
					}
				}
				if (flag)
				{
					return flag2;
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
				return base.Aggregate(records, kind);
			}
		}
		catch (OverflowException)
		{
			throw ExprException.Overflow(typeof(bool));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		bool flag = _values[recordNo1];
		bool flag2 = _values[recordNo2];
		if (!flag || !flag2)
		{
			int num = CompareBits(recordNo1, recordNo2);
			if (num != 0)
			{
				return num;
			}
		}
		return flag.CompareTo(flag2);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		if (_nullValue == value)
		{
			if (IsNull(recordNo))
			{
				return 0;
			}
			return 1;
		}
		bool flag = _values[recordNo];
		if (!flag && IsNull(recordNo))
		{
			return -1;
		}
		return flag.CompareTo((bool)value);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)((IConvertible)value).ToBoolean(base.FormatProvider)));
		}
		return value;
	}

	public override void Copy(int recordNo1, int recordNo2)
	{
		CopyBits(recordNo1, recordNo2);
		_values[recordNo2] = _values[recordNo1];
	}

	public override object Get(int record)
	{
		bool flag = _values[record];
		if (flag)
		{
			return flag;
		}
		return GetBits(record);
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = false;
			SetNullBit(record, flag: true);
		}
		else
		{
			_values[record] = ((IConvertible)value).ToBoolean(base.FormatProvider);
			SetNullBit(record, flag: false);
		}
	}

	public override void SetCapacity(int capacity)
	{
		bool[] array = new bool[capacity];
		if (_values != null)
		{
			Array.Copy(_values, array, Math.Min(capacity, _values.Length));
		}
		_values = array;
		base.SetCapacity(capacity);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override object ConvertXmlToObject(string s)
	{
		return XmlConvert.ToBoolean(s);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return XmlConvert.ToString((bool)value);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new bool[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		bool[] array = (bool[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (bool[])store;
		SetNullStorage(nullbits);
	}
}
