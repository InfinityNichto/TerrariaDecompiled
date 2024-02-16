using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data.Common;

internal sealed class CharStorage : DataStorage
{
	private char[] _values;

	internal CharStorage(DataColumn column)
		: base(column, typeof(char), '\0', StorageType.Char)
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
				char c2 = '\uffff';
				foreach (int num2 in records)
				{
					if (!IsNull(num2))
					{
						c2 = ((_values[num2] < c2) ? _values[num2] : c2);
						flag = true;
					}
				}
				if (flag)
				{
					return c2;
				}
				return _nullValue;
			}
			case AggregateType.Max:
			{
				char c = '\0';
				foreach (int num in records)
				{
					if (!IsNull(num))
					{
						c = ((_values[num] > c) ? _values[num] : c);
						flag = true;
					}
				}
				if (flag)
				{
					return c;
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
			throw ExprException.Overflow(typeof(char));
		}
		throw ExceptionBuilder.AggregateException(kind, _dataType);
	}

	public override int Compare(int recordNo1, int recordNo2)
	{
		char c = _values[recordNo1];
		char c2 = _values[recordNo2];
		if (c == '\0' || c2 == '\0')
		{
			int num = CompareBits(recordNo1, recordNo2);
			if (num != 0)
			{
				return num;
			}
		}
		return c.CompareTo(c2);
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
		char c = _values[recordNo];
		if (c == '\0' && IsNull(recordNo))
		{
			return -1;
		}
		return c.CompareTo((char)value);
	}

	public override object ConvertValue(object value)
	{
		if (_nullValue != value)
		{
			value = ((value == null) ? _nullValue : ((object)((IConvertible)value).ToChar(base.FormatProvider)));
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
		char c = _values[record];
		if (c != 0)
		{
			return c;
		}
		return GetBits(record);
	}

	public override void Set(int record, object value)
	{
		if (_nullValue == value)
		{
			_values[record] = '\0';
			SetNullBit(record, flag: true);
			return;
		}
		char c = ((IConvertible)value).ToChar(base.FormatProvider);
		if (c < '\ud800' || c > '\udfff')
		{
			switch (c)
			{
			case '\t':
			case '\n':
			case '\r':
				break;
			default:
				_values[record] = c;
				SetNullBit(record, flag: false);
				return;
			}
		}
		throw ExceptionBuilder.ProblematicChars(c);
	}

	public override void SetCapacity(int capacity)
	{
		char[] array = new char[capacity];
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
		return XmlConvert.ToChar(s);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public override string ConvertObjectToXml(object value)
	{
		return XmlConvert.ToString((char)value);
	}

	protected override object GetEmptyStorage(int recordCount)
	{
		return new char[recordCount];
	}

	protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
	{
		char[] array = (char[])store;
		array[storeIndex] = _values[record];
		nullbits.Set(storeIndex, IsNull(record));
	}

	protected override void SetStorage(object store, BitArray nullbits)
	{
		_values = (char[])store;
		SetNullStorage(nullbits);
	}
}
