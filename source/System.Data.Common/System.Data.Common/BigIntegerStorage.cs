using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace System.Data.Common;

internal sealed class BigIntegerStorage : DataStorage
{
	private BigInteger[] _values;

	internal BigIntegerStorage(DataColumn column)
		: base(column, typeof(BigInteger), BigInteger.Zero, StorageType.BigInteger)
	{
	}

	public override object Aggregate(int[] records, AggregateType kind)
	{
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		BigInteger bigInteger = _values[recordNo1];
		BigInteger other = _values[recordNo2];
		if (bigInteger.IsZero || other.IsZero)
		{
			int num = CompareBits(recordNo1, recordNo2);
			if (num != 0)
			{
				return num;
			}
		}
		return bigInteger.CompareTo(other);
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
		BigInteger bigInteger = _values[recordNo];
		if (bigInteger.IsZero && !HasValue(recordNo))
		{
			return -1;
		}
		return bigInteger.CompareTo((BigInteger)value);
	}

	internal static BigInteger ConvertToBigInteger(object value, IFormatProvider formatProvider)
	{
		if (value.GetType() == typeof(BigInteger))
		{
			return (BigInteger)value;
		}
		if (value.GetType() == typeof(string))
		{
			return BigInteger.Parse((string)value, formatProvider);
		}
		if (value.GetType() == typeof(long))
		{
			return (long)value;
		}
		if (value.GetType() == typeof(int))
		{
			return (int)value;
		}
		if (value.GetType() == typeof(short))
		{
			return (short)value;
		}
		if (value.GetType() == typeof(sbyte))
		{
			return (sbyte)value;
		}
		if (value.GetType() == typeof(ulong))
		{
			return (ulong)value;
		}
		if (value.GetType() == typeof(uint))
		{
			return (uint)value;
		}
		if (value.GetType() == typeof(ushort))
		{
			return (ushort)value;
		}
		if (value.GetType() == typeof(byte))
		{
			return (byte)value;
		}
		throw ExceptionBuilder.ConvertFailed(value.GetType(), typeof(BigInteger));
	}

	internal static object ConvertFromBigInteger(BigInteger value, Type type, IFormatProvider formatProvider)
	{
		if (type == typeof(string))
		{
			return value.ToString("D", formatProvider);
		}
		if (type == typeof(sbyte))
		{
			return (sbyte)value;
		}
		if (type == typeof(short))
		{
			return (short)value;
		}
		if (type == typeof(int))
		{
			return (int)value;
		}
		if (type == typeof(long))
		{
			return (long)value;
		}
		if (type == typeof(byte))
		{
			return (byte)value;
		}
		if (type == typeof(ushort))
		{
			return (ushort)value;
		}
		if (type == typeof(uint))
		{
			return (uint)value;
		}
		if (type == typeof(ulong))
		{
			return (ulong)value;
		}
		if (type == typeof(float))
		{
			return (float)value;
		}
		if (type == typeof(double))
		{
			return (double)value;
		}
		if (type == typeof(decimal))
		{
			return (decimal)value;
		}
		if (type == typeof(BigInteger))
		{
			return value;
		}
		throw ExceptionBuilder.ConvertFailed(typeof(BigInteger), type);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)ConvertToBigInteger(value, base.FormatProvider)));
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
		BigInteger bigInteger = _values[record];
		if (!bigInteger.IsZero)
		{
			return bigInteger;
		}
		return GetBits(record);
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = BigInteger.Zero;
			SetNullBit(record, flag: true);
		}
		else
		{
			_values[record] = ConvertToBigInteger(value, base.FormatProvider);
			SetNullBit(record, flag: false);
		}
	}

	public override void SetCapacity(int capacity)
	{
		BigInteger[] array = new BigInteger[capacity];
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
		return BigInteger.Parse(s, CultureInfo.InvariantCulture);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return ((BigInteger)value).ToString("D", CultureInfo.InvariantCulture);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new BigInteger[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		BigInteger[] array = (BigInteger[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, !HasValue(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (BigInteger[])store;
		SetNullStorage(nullbits);
	}
}
