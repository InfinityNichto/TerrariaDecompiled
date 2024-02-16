using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common;

internal sealed class DateTimeOffsetStorage : DataStorage
{
	private static readonly DateTimeOffset s_defaultValue = DateTimeOffset.MinValue;

	private DateTimeOffset[] _values;

	internal DateTimeOffsetStorage(DataColumn column)
		: base(column, typeof(DateTimeOffset), s_defaultValue, StorageType.DateTimeOffset)
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
				DateTimeOffset dateTimeOffset2 = DateTimeOffset.MaxValue;
				foreach (int num3 in records)
				{
					if (HasValue(num3))
					{
						dateTimeOffset2 = ((DateTimeOffset.Compare(_values[num3], dateTimeOffset2) < 0) ? _values[num3] : dateTimeOffset2);
						flag = true;
					}
				}
				if (flag)
				{
					return dateTimeOffset2;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				DateTimeOffset dateTimeOffset = DateTimeOffset.MinValue;
				foreach (int num2 in records)
				{
					if (HasValue(num2))
					{
						dateTimeOffset = ((DateTimeOffset.Compare(_values[num2], dateTimeOffset) >= 0) ? _values[num2] : dateTimeOffset);
						flag = true;
					}
				}
				if (flag)
				{
					return dateTimeOffset;
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
					if (HasValue(records[i]))
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
			throw ExprException.Overflow(typeof(DateTimeOffset));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		DateTimeOffset dateTimeOffset = _values[recordNo1];
		DateTimeOffset dateTimeOffset2 = _values[recordNo2];
		if (dateTimeOffset == s_defaultValue || dateTimeOffset2 == s_defaultValue)
		{
			int num = CompareBits(recordNo1, recordNo2);
			if (num != 0)
			{
				return num;
			}
		}
		return DateTimeOffset.Compare(dateTimeOffset, dateTimeOffset2);
	}

	public override int CompareValueTo(int recordNo, object value)
	{
		if (_nullValue == value)
		{
			if (!HasValue(recordNo))
			{
				return 0;
			}
			return 1;
		}
		DateTimeOffset dateTimeOffset = _values[recordNo];
		if (s_defaultValue == dateTimeOffset && !HasValue(recordNo))
		{
			return -1;
		}
		return DateTimeOffset.Compare(dateTimeOffset, (DateTimeOffset)value);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)(DateTimeOffset)value));
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
		DateTimeOffset dateTimeOffset = _values[record];
		if (dateTimeOffset != s_defaultValue || HasValue(record))
		{
			return dateTimeOffset;
		}
		return _nullValue;
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = s_defaultValue;
			SetNullBit(record, flag: true);
		}
		else
		{
			_values[record] = (DateTimeOffset)value;
			SetNullBit(record, flag: false);
		}
	}

	public override void SetCapacity(int capacity)
	{
		DateTimeOffset[] array = new DateTimeOffset[capacity];
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
		return XmlConvert.ToDateTimeOffset(s);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return XmlConvert.ToString((DateTimeOffset)value);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new DateTimeOffset[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		DateTimeOffset[] array = (DateTimeOffset[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, !HasValue(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (DateTimeOffset[])store;
		SetNullStorage(nullbits);
	}
}
