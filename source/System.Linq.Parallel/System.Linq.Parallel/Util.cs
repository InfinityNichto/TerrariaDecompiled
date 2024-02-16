using System.Collections.Generic;

namespace System.Linq.Parallel;

internal static class Util
{
	private sealed class FastIntComparer : Comparer<int>
	{
		public override int Compare(int x, int y)
		{
			return x.CompareTo(y);
		}
	}

	private sealed class FastLongComparer : Comparer<long>
	{
		public override int Compare(long x, long y)
		{
			return x.CompareTo(y);
		}
	}

	private sealed class FastFloatComparer : Comparer<float>
	{
		public override int Compare(float x, float y)
		{
			return x.CompareTo(y);
		}
	}

	private sealed class FastDoubleComparer : Comparer<double>
	{
		public override int Compare(double x, double y)
		{
			return x.CompareTo(y);
		}
	}

	private sealed class FastDateTimeComparer : Comparer<DateTime>
	{
		public override int Compare(DateTime x, DateTime y)
		{
			return x.CompareTo(y);
		}
	}

	private static readonly FastIntComparer s_fastIntComparer = new FastIntComparer();

	private static readonly FastLongComparer s_fastLongComparer = new FastLongComparer();

	private static readonly FastFloatComparer s_fastFloatComparer = new FastFloatComparer();

	private static readonly FastDoubleComparer s_fastDoubleComparer = new FastDoubleComparer();

	private static readonly FastDateTimeComparer s_fastDateTimeComparer = new FastDateTimeComparer();

	internal static int Sign(int x)
	{
		if (x >= 0)
		{
			if (x != 0)
			{
				return 1;
			}
			return 0;
		}
		return -1;
	}

	internal static Comparer<TKey> GetDefaultComparer<TKey>()
	{
		if (typeof(TKey) == typeof(int))
		{
			return (Comparer<TKey>)(object)s_fastIntComparer;
		}
		if (typeof(TKey) == typeof(long))
		{
			return (Comparer<TKey>)(object)s_fastLongComparer;
		}
		if (typeof(TKey) == typeof(float))
		{
			return (Comparer<TKey>)(object)s_fastFloatComparer;
		}
		if (typeof(TKey) == typeof(double))
		{
			return (Comparer<TKey>)(object)s_fastDoubleComparer;
		}
		if (typeof(TKey) == typeof(DateTime))
		{
			return (Comparer<TKey>)(object)s_fastDateTimeComparer;
		}
		return Comparer<TKey>.Default;
	}
}
