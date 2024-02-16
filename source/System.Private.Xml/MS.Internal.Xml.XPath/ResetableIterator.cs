using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal abstract class ResetableIterator : XPathNodeIterator
{
	public abstract override int CurrentPosition { get; }

	public ResetableIterator()
	{
		count = -1;
	}

	protected ResetableIterator(ResetableIterator other)
	{
		count = other.count;
	}

	protected void ResetCount()
	{
		count = -1;
	}

	public abstract void Reset();
}
