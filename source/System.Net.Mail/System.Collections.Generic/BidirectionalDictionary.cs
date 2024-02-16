using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

internal sealed class BidirectionalDictionary<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>, IEnumerable
{
	private readonly Dictionary<T1, T2> _forward;

	private readonly Dictionary<T2, T1> _backward;

	public BidirectionalDictionary(int capacity)
	{
		_forward = new Dictionary<T1, T2>(capacity);
		_backward = new Dictionary<T2, T1>(capacity);
	}

	public void Add(T1 item1, T2 item2)
	{
		_forward.Add(item1, item2);
		_backward.Add(item2, item1);
	}

	public bool TryGetForward(T1 item1, [MaybeNullWhen(false)] out T2 item2)
	{
		return _forward.TryGetValue(item1, out item2);
	}

	public bool TryGetBackward(T2 item2, [MaybeNullWhen(false)] out T1 item1)
	{
		return _backward.TryGetValue(item2, out item1);
	}

	public Dictionary<T1, T2>.Enumerator GetEnumerator()
	{
		return _forward.GetEnumerator();
	}

	IEnumerator<KeyValuePair<T1, T2>> IEnumerable<KeyValuePair<T1, T2>>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
