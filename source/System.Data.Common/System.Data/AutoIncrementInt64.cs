using System.Data.Common;
using System.Globalization;
using System.Numerics;

namespace System.Data;

internal sealed class AutoIncrementInt64 : AutoIncrementValue
{
	private long _current;

	private long _seed;

	private long _step = 1L;

	internal override object Current
	{
		get
		{
			return _current;
		}
		set
		{
			_current = (long)value;
		}
	}

	internal override Type DataType => typeof(long);

	internal override long Seed
	{
		get
		{
			return _seed;
		}
		set
		{
			if (_current == _seed || BoundaryCheck(value))
			{
				_current = value;
			}
			_seed = value;
		}
	}

	internal override long Step
	{
		get
		{
			return _step;
		}
		set
		{
			if (value == 0L)
			{
				throw ExceptionBuilder.AutoIncrementSeed();
			}
			if (_step != value)
			{
				if (_current != Seed)
				{
					_current = _current - _step + value;
				}
				_step = value;
			}
		}
	}

	internal override void MoveAfter()
	{
		_current += _step;
	}

	internal override void SetCurrent(object value, IFormatProvider formatProvider)
	{
		_current = Convert.ToInt64(value, formatProvider);
	}

	internal override void SetCurrentAndIncrement(object value)
	{
		long num = (long)SqlConvert.ChangeType2(value, StorageType.Int64, typeof(long), CultureInfo.InvariantCulture);
		if (BoundaryCheck(num))
		{
			_current = num + _step;
		}
	}

	private bool BoundaryCheck(BigInteger value)
	{
		if (_step >= 0 || !(value <= _current))
		{
			if (0 < _step)
			{
				return _current <= value;
			}
			return false;
		}
		return true;
	}
}
