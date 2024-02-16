using System.Collections.Generic;

namespace System.Linq.Parallel;

internal struct WrapperEqualityComparer<T> : IEqualityComparer<Wrapper<T>>
{
	private readonly IEqualityComparer<T> _comparer;

	internal WrapperEqualityComparer(IEqualityComparer<T> comparer)
	{
		if (comparer == null)
		{
			_comparer = EqualityComparer<T>.Default;
		}
		else
		{
			_comparer = comparer;
		}
	}

	public bool Equals(Wrapper<T> x, Wrapper<T> y)
	{
		return _comparer.Equals(x.Value, y.Value);
	}

	public int GetHashCode(Wrapper<T> x)
	{
		T value = x.Value;
		if (value != null)
		{
			return _comparer.GetHashCode(value);
		}
		return 0;
	}
}
