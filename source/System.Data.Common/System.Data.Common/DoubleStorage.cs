using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common;

internal sealed class DoubleStorage : DataStorage
{
	private double[] _values;

	internal DoubleStorage(DataColumn column)
		: base(column, typeof(double), 0.0, StorageType.Double)
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
				double num15 = 0.0;
				foreach (int num16 in records)
				{
					if (!IsNull(num16))
					{
						num15 += _values[num16];
						flag = true;
					}
				}
				if (flag)
				{
					return num15;
				}
				return _nullValue;
			}
			case AggregateType.Mean:
			{
				double num7 = 0.0;
				int num8 = 0;
				foreach (int num9 in records)
				{
					if (!IsNull(num9))
					{
						num7 += _values[num9];
						num8++;
						flag = true;
					}
				}
				if (flag)
				{
					double num10 = num7 / (double)num8;
					return num10;
				}
				return _nullValue;
			}
			case AggregateType.Var:
			case AggregateType.StDev:
			{
				int num = 0;
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				foreach (int num6 in records)
				{
					if (!IsNull(num6))
					{
						num4 += _values[num6];
						num5 += _values[num6] * _values[num6];
						num++;
					}
				}
				if (num > 1)
				{
					num2 = (double)num * num5 - num4 * num4;
					num3 = num2 / (num4 * num4);
					num2 = ((!(num3 < 1E-15) && !(num2 < 0.0)) ? (num2 / (double)(num * (num - 1))) : 0.0);
					if (kind == AggregateType.StDev)
					{
						return Math.Sqrt(num2);
					}
					return num2;
				}
				return _nullValue;
			}
			case AggregateType.Min:
			{
				double num13 = double.MaxValue;
				foreach (int num14 in records)
				{
					if (!IsNull(num14))
					{
						num13 = Math.Min(_values[num14], num13);
						flag = true;
					}
				}
				if (flag)
				{
					return num13;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				double num11 = double.MinValue;
				foreach (int num12 in records)
				{
					if (!IsNull(num12))
					{
						num11 = Math.Max(_values[num12], num11);
						flag = true;
					}
				}
				if (flag)
				{
					return num11;
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
			throw ExprException.Overflow(typeof(double));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		double num = _values[recordNo1];
		double num2 = _values[recordNo2];
		if (num == 0.0 || num2 == 0.0)
		{
			int num3 = CompareBits(recordNo1, recordNo2);
			if (num3 != 0)
			{
				return num3;
			}
		}
		return num.CompareTo(num2);
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
		double num = _values[recordNo];
		if (0.0 == num && IsNull(recordNo))
		{
			return -1;
		}
		return num.CompareTo((double)value);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)((IConvertible)value).ToDouble(base.FormatProvider)));
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
		double num = _values[record];
		if (num != 0.0)
		{
			return num;
		}
		return GetBits(record);
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = 0.0;
			SetNullBit(record, flag: true);
		}
		else
		{
			_values[record] = ((IConvertible)value).ToDouble(base.FormatProvider);
			SetNullBit(record, flag: false);
		}
	}

	public override void SetCapacity(int capacity)
	{
		double[] array = new double[capacity];
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
		return XmlConvert.ToDouble(s);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return XmlConvert.ToString((double)value);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new double[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		double[] array = (double[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (double[])store;
		SetNullStorage(nullbits);
	}
}
