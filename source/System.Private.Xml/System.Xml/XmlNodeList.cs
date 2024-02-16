using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Xml;

public abstract class XmlNodeList : IEnumerable, IDisposable
{
	public abstract int Count { get; }

	[IndexerName("ItemOf")]
	public virtual XmlNode? this[int i] => Item(i);

	public abstract XmlNode? Item(int index);

	public abstract IEnumerator GetEnumerator();

	void IDisposable.Dispose()
	{
		PrivateDisposeNodeList();
	}

	protected virtual void PrivateDisposeNodeList()
	{
	}
}
