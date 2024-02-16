using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel;

internal abstract class QueryOperatorEnumerator<TElement, TKey>
{
	private sealed class QueryOperatorClassicEnumerator : IEnumerator<TElement>, IEnumerator, IDisposable
	{
		private QueryOperatorEnumerator<TElement, TKey> _operatorEnumerator;

		private TElement _current;

		public TElement Current => _current;

		object IEnumerator.Current => _current;

		internal QueryOperatorClassicEnumerator(QueryOperatorEnumerator<TElement, TKey> operatorEnumerator)
		{
			_operatorEnumerator = operatorEnumerator;
		}

		public bool MoveNext()
		{
			TKey currentKey = default(TKey);
			return _operatorEnumerator.MoveNext(ref _current, ref currentKey);
		}

		public void Dispose()
		{
			_operatorEnumerator.Dispose();
			_operatorEnumerator = null;
		}

		public void Reset()
		{
			_operatorEnumerator.Reset();
		}
	}

	internal abstract bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TElement currentElement, [AllowNull] ref TKey currentKey);

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	internal virtual void Reset()
	{
	}

	internal IEnumerator<TElement> AsClassicEnumerator()
	{
		return new QueryOperatorClassicEnumerator(this);
	}
}
