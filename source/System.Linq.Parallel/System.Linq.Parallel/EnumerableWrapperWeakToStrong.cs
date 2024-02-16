using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class EnumerableWrapperWeakToStrong : IEnumerable<object>, IEnumerable
{
	private sealed class WrapperEnumeratorWeakToStrong : IEnumerator<object>, IEnumerator, IDisposable
	{
		private readonly IEnumerator _wrappedEnumerator;

		object IEnumerator.Current => _wrappedEnumerator.Current;

		object IEnumerator<object>.Current => _wrappedEnumerator.Current;

		internal WrapperEnumeratorWeakToStrong(IEnumerator wrappedEnumerator)
		{
			_wrappedEnumerator = wrappedEnumerator;
		}

		void IDisposable.Dispose()
		{
			if (_wrappedEnumerator is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}

		bool IEnumerator.MoveNext()
		{
			return _wrappedEnumerator.MoveNext();
		}

		void IEnumerator.Reset()
		{
			_wrappedEnumerator.Reset();
		}
	}

	private readonly IEnumerable _wrappedEnumerable;

	internal EnumerableWrapperWeakToStrong(IEnumerable wrappedEnumerable)
	{
		_wrappedEnumerable = wrappedEnumerable;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<object>)this).GetEnumerator();
	}

	public IEnumerator<object> GetEnumerator()
	{
		return new WrapperEnumeratorWeakToStrong(_wrappedEnumerable.GetEnumerator());
	}
}
