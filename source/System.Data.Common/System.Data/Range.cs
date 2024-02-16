namespace System.Data;

internal struct Range
{
	private readonly int _min;

	private readonly int _max;

	private readonly bool _isNotNull;

	public int Count
	{
		get
		{
			if (!IsNull)
			{
				return _max - _min + 1;
			}
			return 0;
		}
	}

	public bool IsNull => !_isNotNull;

	public int Max
	{
		get
		{
			CheckNull();
			return _max;
		}
	}

	public int Min
	{
		get
		{
			CheckNull();
			return _min;
		}
	}

	public Range(int min, int max)
	{
		if (min > max)
		{
			throw ExceptionBuilder.RangeArgument(min, max);
		}
		_min = min;
		_max = max;
		_isNotNull = true;
	}

	internal void CheckNull()
	{
		if (IsNull)
		{
			throw ExceptionBuilder.NullRange();
		}
	}
}
