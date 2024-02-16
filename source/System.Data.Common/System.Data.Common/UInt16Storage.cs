using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common;

internal sealed class UInt16Storage : DataStorage
{
	private ushort[] _values;

	public UInt16Storage(DataColumn column)
		: base(column, typeof(ushort), (ushort)0, StorageType.UInt16)
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
				ulong num4 = 0uL;
				foreach (int num5 in records)
				{
					if (HasValue(num5))
					{
						num4 = checked(num4 + _values[num5]);
						flag = true;
					}
				}
				if (flag)
				{
					return num4;
				}
				return _nullValue;
			}
			case AggregateType.Mean:
			{
				long num11 = 0L;
				int num12 = 0;
				foreach (int num13 in records)
				{
					if (HasValue(num13))
					{
						num11 = checked(num11 + _values[num13]);
						num12++;
						flag = true;
					}
				}
				checked
				{
					if (flag)
					{
						ushort num14 = (ushort)unchecked(num11 / num12);
						return num14;
					}
					return _nullValue;
				}
			}
			case AggregateType.Var:
			case AggregateType.StDev:
			{
				int num = 0;
				double num6 = 0.0;
				double num7 = 0.0;
				double num8 = 0.0;
				double num9 = 0.0;
				foreach (int num10 in records)
				{
					if (HasValue(num10))
					{
						num8 += (double)(int)_values[num10];
						num9 += (double)(int)_values[num10] * (double)(int)_values[num10];
						num++;
					}
				}
				if (num > 1)
				{
					num6 = (double)num * num9 - num8 * num8;
					num7 = num6 / (num8 * num8);
					num6 = ((!(num7 < 1E-15) && !(num6 < 0.0)) ? (num6 / (double)(num * (num - 1))) : 0.0);
					if (kind == AggregateType.StDev)
					{
						return Math.Sqrt(num6);
					}
					return num6;
				}
				return _nullValue;
			}
			case AggregateType.Min:
			{
				ushort num2 = ushort.MaxValue;
				foreach (int num3 in records)
				{
					if (HasValue(num3))
					{
						num2 = Math.Min(_values[num3], num2);
						flag = true;
					}
				}
				if (flag)
				{
					return num2;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				ushort num15 = 0;
				foreach (int num16 in records)
				{
					if (HasValue(num16))
					{
						num15 = Math.Max(_values[num16], num15);
						flag = true;
					}
				}
				if (flag)
				{
					return num15;
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
			throw ExprException.Overflow(typeof(ushort));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		ushort num = _values[recordNo1];
		ushort num2 = _values[recordNo2];
		if (num == 0 || num2 == 0)
		{
			int num3 = CompareBits(recordNo1, recordNo2);
			if (num3 != 0)
			{
				return num3;
			}
		}
		return num - num2;
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
		ushort num = _values[recordNo];
		if (num == 0 && !HasValue(recordNo))
		{
			return -1;
		}
		return num.CompareTo((ushort)value);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)((IConvertible)value).ToUInt16(base.FormatProvider)));
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
		ushort num = _values[record];
		if (!num.Equals(0))
		{
			return num;
		}
		return GetBits(record);
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = 0;
			SetNullBit(record, flag: true);
		}
		else
		{
			_values[record] = ((IConvertible)value).ToUInt16(base.FormatProvider);
			SetNullBit(record, flag: false);
		}
	}

	public override void SetCapacity(int capacity)
	{
		ushort[] array = new ushort[capacity];
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
		return XmlConvert.ToUInt16(s);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return XmlConvert.ToString((ushort)value);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new ushort[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		ushort[] array = (ushort[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, !HasValue(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (ushort[])store;
		SetNullStorage(nullbits);
	}
}
