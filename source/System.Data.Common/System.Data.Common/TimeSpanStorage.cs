using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common;

internal sealed class TimeSpanStorage : DataStorage
{
	private static readonly TimeSpan s_defaultValue = TimeSpan.Zero;

	private TimeSpan[] _values;

	public TimeSpanStorage(DataColumn column)
		: base(column, typeof(TimeSpan), s_defaultValue, StorageType.TimeSpan)
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
				TimeSpan timeSpan = TimeSpan.MaxValue;
				foreach (int num9 in records)
				{
					if (!IsNull(num9))
					{
						timeSpan = ((TimeSpan.Compare(_values[num9], timeSpan) < 0) ? _values[num9] : timeSpan);
						flag = true;
					}
				}
				if (flag)
				{
					return timeSpan;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				TimeSpan timeSpan2 = TimeSpan.MinValue;
				foreach (int num13 in records)
				{
					if (!IsNull(num13))
					{
						timeSpan2 = ((TimeSpan.Compare(_values[num13], timeSpan2) >= 0) ? _values[num13] : timeSpan2);
						flag = true;
					}
				}
				if (flag)
				{
					return timeSpan2;
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
			case AggregateType.Sum:
			{
				decimal d = default(decimal);
				foreach (int num14 in records)
				{
					if (!IsNull(num14))
					{
						d += (decimal)_values[num14].Ticks;
						flag = true;
					}
				}
				if (flag)
				{
					return TimeSpan.FromTicks((long)Math.Round(d));
				}
				return null;
			}
			case AggregateType.Mean:
			{
				decimal num10 = default(decimal);
				int num11 = 0;
				foreach (int num12 in records)
				{
					if (!IsNull(num12))
					{
						num10 += (decimal)_values[num12].Ticks;
						num11++;
					}
				}
				if (num11 > 0)
				{
					return TimeSpan.FromTicks((long)Math.Round(num10 / (decimal)num11));
				}
				return null;
			}
			case AggregateType.StDev:
			{
				int num = 0;
				decimal num2 = default(decimal);
				foreach (int num3 in records)
				{
					if (!IsNull(num3))
					{
						num2 += (decimal)_values[num3].Ticks;
						num++;
					}
				}
				if (num > 1)
				{
					double num4 = 0.0;
					decimal num5 = num2 / (decimal)num;
					foreach (int num6 in records)
					{
						if (!IsNull(num6))
						{
							double num7 = (double)((decimal)_values[num6].Ticks - num5);
							num4 += num7 * num7;
						}
					}
					ulong num8 = (ulong)Math.Round(Math.Sqrt(num4 / (double)(num - 1)));
					if (num8 > long.MaxValue)
					{
						num8 = 9223372036854775807uL;
					}
					return TimeSpan.FromTicks((long)num8);
				}
				return null;
			}
			case AggregateType.Var:
				break;
			}
		}
		catch (OverflowException)
		{
			throw ExprException.Overflow(typeof(TimeSpan));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		TimeSpan timeSpan = _values[recordNo1];
		TimeSpan timeSpan2 = _values[recordNo2];
		if (timeSpan == s_defaultValue || timeSpan2 == s_defaultValue)
		{
			int num = CompareBits(recordNo1, recordNo2);
			if (num != 0)
			{
				return num;
			}
		}
		return TimeSpan.Compare(timeSpan, timeSpan2);
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
		TimeSpan timeSpan = _values[recordNo];
		if (s_defaultValue == timeSpan && IsNull(recordNo))
		{
			return -1;
		}
		return timeSpan.CompareTo((TimeSpan)value);
	}

	private static TimeSpan ConvertToTimeSpan(object value)
	{
		Type type = value.GetType();
		if (type == typeof(string))
		{
			return TimeSpan.Parse((string)value);
		}
		if (type == typeof(int))
		{
			return new TimeSpan((int)value);
		}
		if (type == typeof(long))
		{
			return new TimeSpan((long)value);
		}
		return (TimeSpan)value;
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)ConvertToTimeSpan(value)));
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
		TimeSpan timeSpan = _values[record];
		if (timeSpan != s_defaultValue)
		{
			return timeSpan;
		}
		return GetBits(record);
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
			_values[record] = ConvertToTimeSpan(value);
			SetNullBit(record, flag: false);
		}
	}

	public override void SetCapacity(int capacity)
	{
		TimeSpan[] array = new TimeSpan[capacity];
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
		return XmlConvert.ToTimeSpan(s);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return XmlConvert.ToString((TimeSpan)value);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new TimeSpan[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		TimeSpan[] array = (TimeSpan[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (TimeSpan[])store;
		SetNullStorage(nullbits);
	}
}
