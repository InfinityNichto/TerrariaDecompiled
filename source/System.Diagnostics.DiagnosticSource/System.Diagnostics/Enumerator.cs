using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

internal struct Enumerator<T> : IEnumerator<T>, IEnumerator, IDisposable
{
	private DiagNode<T> _nextNode;

	[AllowNull]
	[MaybeNull]
	private T _currentItem;

	public T Current => _currentItem;

	object IEnumerator.Current => Current;

	public Enumerator(DiagNode<T> head)
	{
		_nextNode = head;
		_currentItem = default(T);
	}

	public bool MoveNext()
	{
		if (_nextNode == null)
		{
			_currentItem = default(T);
			return false;
		}
		_currentItem = _nextNode.Value;
		_nextNode = _nextNode.Next;
		return true;
	}

	public void Reset()
	{
		throw new NotSupportedException();
	}

	public void Dispose()
	{
	}
}
