using System.Collections.Generic;

namespace MS.Internal.Xml.XPath;

internal sealed class ClonableStack<T> : List<T>
{
	public ClonableStack()
	{
	}

	private ClonableStack(IEnumerable<T> collection)
		: base(collection)
	{
	}

	public void Push(T value)
	{
		Add(value);
	}

	public T Pop()
	{
		int index = base.Count - 1;
		T result = base[index];
		RemoveAt(index);
		return result;
	}

	public T Peek()
	{
		return base[base.Count - 1];
	}

	public ClonableStack<T> Clone()
	{
		return new ClonableStack<T>(this);
	}
}
