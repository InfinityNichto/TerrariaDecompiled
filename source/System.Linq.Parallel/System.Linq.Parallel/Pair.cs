namespace System.Linq.Parallel;

internal struct Pair<T, U>
{
	internal T _first;

	internal U _second;

	public T First
	{
		get
		{
			return _first;
		}
		set
		{
			_first = value;
		}
	}

	public U Second
	{
		get
		{
			return _second;
		}
		set
		{
			_second = value;
		}
	}

	public Pair(T first, U second)
	{
		_first = first;
		_second = second;
	}
}
