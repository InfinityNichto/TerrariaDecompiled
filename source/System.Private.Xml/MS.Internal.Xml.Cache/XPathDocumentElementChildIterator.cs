using System;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathDocumentElementChildIterator : XPathDocumentBaseIterator
{
	private readonly string _localName;

	private readonly string _namespaceUri;

	public XPathDocumentElementChildIterator(XPathDocumentNavigator parent, string name, string namespaceURI)
		: base(parent)
	{
		if (namespaceURI == null)
		{
			throw new ArgumentNullException("namespaceURI");
		}
		_localName = parent.NameTable.Get(name);
		_namespaceUri = namespaceURI;
	}

	public XPathDocumentElementChildIterator(XPathDocumentElementChildIterator iter)
		: base(iter)
	{
		_localName = iter._localName;
		_namespaceUri = iter._namespaceUri;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathDocumentElementChildIterator(this);
	}

	public override bool MoveNext()
	{
		if (pos == 0)
		{
			if (!ctxt.MoveToChild(_localName, _namespaceUri))
			{
				return false;
			}
		}
		else if (!ctxt.MoveToNext(_localName, _namespaceUri))
		{
			return false;
		}
		pos++;
		return true;
	}
}
