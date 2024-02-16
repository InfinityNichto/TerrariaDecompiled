using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common;

internal sealed class DateTimeStorage : DataStorage
{
	private static readonly DateTime s_defaultValue = DateTime.MinValue;

	private DateTime[] _values;

	internal DateTimeStorage(DataColumn column)
		: base(column, typeof(DateTime), s_defaultValue, StorageType.DateTime)
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
				DateTime dateTime2 = DateTime.MaxValue;
				foreach (int num3 in records)
				{
					if (HasValue(num3))
					{
						dateTime2 = ((DateTime.Compare(_values[num3], dateTime2) < 0) ? _values[num3] : dateTime2);
						flag = true;
					}
				}
				if (flag)
				{
					return dateTime2;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				DateTime dateTime = DateTime.MinValue;
				foreach (int num2 in records)
				{
					if (HasValue(num2))
					{
						dateTime = ((DateTime.Compare(_values[num2], dateTime) >= 0) ? _values[num2] : dateTime);
						flag = true;
					}
				}
				if (flag)
				{
					return dateTime;
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
			throw ExprException.Overflow(typeof(DateTime));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		DateTime dateTime = _values[recordNo1];
		DateTime dateTime2 = _values[recordNo2];
		if (dateTime == s_defaultValue || dateTime2 == s_defaultValue)
		{
			int num = CompareBits(recordNo1, recordNo2);
			if (num != 0)
			{
				return num;
			}
		}
		return DateTime.Compare(dateTime, dateTime2);
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
		DateTime dateTime = _values[recordNo];
		if (s_defaultValue == dateTime && !HasValue(recordNo))
		{
			return -1;
		}
		return DateTime.Compare(dateTime, (DateTime)value);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)((IConvertible)value).ToDateTime(base.FormatProvider)));
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
		DateTime dateTime = _values[record];
		if (dateTime != s_defaultValue || HasValue(record))
		{
			return dateTime;
		}
		return _nullValue;
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = s_defaultValue;
			SetNullBit(record, flag: true);
			return;
		}
		DateTime dateTime = ((IConvertible)value).ToDateTime(base.FormatProvider);
		DateTime dateTime2;
		switch (base.DateTimeMode)
		{
		case DataSetDateTime.Utc:
			dateTime2 = ((dateTime.Kind != DateTimeKind.Utc) ? ((dateTime.Kind != DateTimeKind.Local) ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime()) : dateTime);
			break;
		case DataSetDateTime.Local:
			dateTime2 = ((dateTime.Kind != DateTimeKind.Local) ? ((dateTime.Kind != DateTimeKind.Utc) ? DateTime.SpecifyKind(dateTime, DateTimeKind.Local) : dateTime.ToLocalTime()) : dateTime);
			break;
		case DataSetDateTime.Unspecified:
		case DataSetDateTime.UnspecifiedLocal:
			dateTime2 = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
			break;
		default:
			throw ExceptionBuilder.InvalidDateTimeMode(base.DateTimeMode);
		}
		_values[record] = dateTime2;
		SetNullBit(record, flag: false);
	}

	public override void SetCapacity(int capacity)
	{
		DateTime[] array = new DateTime[capacity];
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
		if (base.DateTimeMode == DataSetDateTime.UnspecifiedLocal)
		{
			return XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.Unspecified);
		}
		return XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		if (base.DateTimeMode == DataSetDateTime.UnspecifiedLocal)
		{
			return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Local);
		}
		return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new DateTime[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		DateTime[] array = (DateTime[])store;
		bool flag = !HasValue(record);
		if (flag || (base.DateTimeMode & DataSetDateTime.Local) == 0)
		{
			array[storeIndex] = _values[record];
		}
		else
		{
			array[storeIndex] = _values[record].ToUniversalTime();
		}
		nullbits.Set(storeIndex, flag);
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (DateTime[])store;
		SetNullStorage(nullbits);
		if (base.DateTimeMode == DataSetDateTime.UnspecifiedLocal)
		{
			for (int i = 0; i < _values.Length; i++)
			{
				if (HasValue(i))
				{
					_values[i] = DateTime.SpecifyKind(_values[i].ToLocalTime(), DateTimeKind.Unspecified);
				}
			}
		}
		else
		{
			if (base.DateTimeMode != DataSetDateTime.Local)
			{
				return;
			}
			for (int j = 0; j < _values.Length; j++)
			{
				if (HasValue(j))
				{
					_values[j] = _values[j].ToLocalTime();
				}
			}
		}
	}
}
