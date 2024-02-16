using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class EmptyEnumerable<T> : ParallelQuery<T>
{
	private static volatile EmptyEnumerable<T> s_instance;

	private static volatile EmptyEnumerator<T> s_enumeratorInstance;

	internal static EmptyEnumerable<T> Instance
	{
		get
		{
			if (s_instance == null)
			{
				s_instance = new EmptyEnumerable<T>();
			}
			return s_instance;
		}
	}

	private EmptyEnumerable()
		: base(QuerySettings.Empty)
	{
	}

	public override IEnumerator<T> GetEnumerator()
	{
		if (s_enumeratorInstance == null)
		{
			s_enumeratorInstance = new EmptyEnumerator<T>();
		}
		return s_enumeratorInstance;
	}
}
