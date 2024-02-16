using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics;

internal sealed class DiagLinkedList<T> : IEnumerable<T>, IEnumerable
{
	private DiagNode<T> _first;

	private DiagNode<T> _last;

	public DiagNode<T> First => _first;

	public DiagLinkedList()
	{
	}

	public DiagLinkedList(T firstValue)
	{
		_last = (_first = new DiagNode<T>(firstValue));
	}

	public DiagLinkedList(IEnumerator<T> e)
	{
		_last = (_first = new DiagNode<T>(e.Current));
		while (e.MoveNext())
		{
			_last.Next = new DiagNode<T>(e.Current);
			_last = _last.Next;
		}
	}

	public void Clear()
	{
		lock (this)
		{
			_first = (_last = null);
		}
	}

	private void UnsafeAdd(DiagNode<T> newNode)
	{
		if (_first == null)
		{
			_first = (_last = newNode);
			return;
		}
		_last.Next = newNode;
		_last = newNode;
	}

	public void Add(T value)
	{
		DiagNode<T> newNode = new DiagNode<T>(value);
		lock (this)
		{
			UnsafeAdd(newNode);
		}
	}

	public bool AddIfNotExist(T value, Func<T, T, bool> compare)
	{
		lock (this)
		{
			for (DiagNode<T> diagNode = _first; diagNode != null; diagNode = diagNode.Next)
			{
				if (compare(value, diagNode.Value))
				{
					return false;
				}
			}
			DiagNode<T> newNode = new DiagNode<T>(value);
			UnsafeAdd(newNode);
			return true;
		}
	}

	public T Remove(T value, Func<T, T, bool> compare)
	{
		lock (this)
		{
			DiagNode<T> diagNode = _first;
			if (diagNode == null)
			{
				return default(T);
			}
			if (compare(diagNode.Value, value))
			{
				_first = diagNode.Next;
				if (_first == null)
				{
					_last = null;
				}
				return diagNode.Value;
			}
			for (DiagNode<T> next = diagNode.Next; next != null; next = next.Next)
			{
				if (compare(next.Value, value))
				{
					diagNode.Next = next.Next;
					if (_last == next)
					{
						_last = diagNode;
					}
					return next.Value;
				}
				diagNode = next;
			}
			return default(T);
		}
	}

	public Enumerator<T> GetEnumerator()
	{
		return new Enumerator<T>(_first);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
