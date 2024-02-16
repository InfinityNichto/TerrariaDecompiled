using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq;

[DebuggerDisplay("Key = {Key}")]
[DebuggerTypeProxy(typeof(SystemLinq_GroupingDebugView<, >))]
public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IEnumerable<TElement>, IEnumerable, IList<TElement>, ICollection<TElement>
{
	internal readonly TKey _key;

	internal readonly int _hashCode;

	internal TElement[] _elements;

	internal int _count;

	internal Grouping<TKey, TElement> _hashNext;

	internal Grouping<TKey, TElement> _next;

	public TKey Key => _key;

	int ICollection<TElement>.Count => _count;

	bool ICollection<TElement>.IsReadOnly => true;

	TElement IList<TElement>.this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
			}
			return _elements[index];
		}
		set
		{
			ThrowHelper.ThrowNotSupportedException();
		}
	}

	internal Grouping(TKey key, int hashCode)
	{
		_key = key;
		_hashCode = hashCode;
		_elements = new TElement[1];
	}

	internal void Add(TElement element)
	{
		if (_elements.Length == _count)
		{
			Array.Resize(ref _elements, checked(_count * 2));
		}
		_elements[_count] = element;
		_count++;
	}

	internal void Trim()
	{
		if (_elements.Length != _count)
		{
			Array.Resize(ref _elements, _count);
		}
	}

	public IEnumerator<TElement> GetEnumerator()
	{
		for (int i = 0; i < _count; i++)
		{
			yield return _elements[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	void ICollection<TElement>.Add(TElement item)
	{
		ThrowHelper.ThrowNotSupportedException();
	}

	void ICollection<TElement>.Clear()
	{
		ThrowHelper.ThrowNotSupportedException();
	}

	bool ICollection<TElement>.Contains(TElement item)
	{
		return Array.IndexOf(_elements, item, 0, _count) >= 0;
	}

	void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex)
	{
		Array.Copy(_elements, 0, array, arrayIndex, _count);
	}

	bool ICollection<TElement>.Remove(TElement item)
	{
		ThrowHelper.ThrowNotSupportedException();
		return false;
	}

	int IList<TElement>.IndexOf(TElement item)
	{
		return Array.IndexOf(_elements, item, 0, _count);
	}

	void IList<TElement>.Insert(int index, TElement item)
	{
		ThrowHelper.ThrowNotSupportedException();
	}

	void IList<TElement>.RemoveAt(int index)
	{
		ThrowHelper.ThrowNotSupportedException();
	}
}
