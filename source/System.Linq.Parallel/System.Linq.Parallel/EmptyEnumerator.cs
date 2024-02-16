using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel;

internal sealed class EmptyEnumerator<T> : QueryOperatorEnumerator<T, int>, IEnumerator<T>, IEnumerator, IDisposable
{
	public T Current => default(T);

	object IEnumerator.Current => null;

	internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref T currentElement, ref int currentKey)
	{
		return false;
	}

	public bool MoveNext()
	{
		return false;
	}

	void IEnumerator.Reset()
	{
	}
}
