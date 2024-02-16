using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class XmlQueryItemSequence : XmlQuerySequence<XPathItem>
{
	public new static readonly XmlQueryItemSequence Empty = new XmlQueryItemSequence();

	public static XmlQueryItemSequence CreateOrReuse(XmlQueryItemSequence seq)
	{
		if (seq != null)
		{
			seq.Clear();
			return seq;
		}
		return new XmlQueryItemSequence();
	}

	public static XmlQueryItemSequence CreateOrReuse(XmlQueryItemSequence seq, XPathItem item)
	{
		if (seq != null)
		{
			seq.Clear();
			seq.Add(item);
			return seq;
		}
		return new XmlQueryItemSequence(item);
	}

	public XmlQueryItemSequence()
	{
	}

	public XmlQueryItemSequence(int capacity)
		: base(capacity)
	{
	}

	public XmlQueryItemSequence(XPathItem item)
		: base(1)
	{
		AddClone(item);
	}

	public void AddClone(XPathItem item)
	{
		if (item.IsNode)
		{
			Add(((XPathNavigator)item).Clone());
		}
		else
		{
			Add(item);
		}
	}
}
