namespace System.Xml.XPath;

internal sealed class XmlEmptyNavigator : XPathNavigator
{
	private static volatile XmlEmptyNavigator s_singleton;

	public static XmlEmptyNavigator Singleton
	{
		get
		{
			if (s_singleton == null)
			{
				s_singleton = new XmlEmptyNavigator();
			}
			return s_singleton;
		}
	}

	public override XPathNodeType NodeType => XPathNodeType.All;

	public override string NamespaceURI => string.Empty;

	public override string LocalName => string.Empty;

	public override string Name => string.Empty;

	public override string Prefix => string.Empty;

	public override string BaseURI => string.Empty;

	public override string Value => string.Empty;

	public override bool IsEmptyElement => false;

	public override string XmlLang => string.Empty;

	public override bool HasAttributes => false;

	public override bool HasChildren => false;

	public override XmlNameTable NameTable => new NameTable();

	private XmlEmptyNavigator()
	{
	}

	public override bool MoveToFirstChild()
	{
		return false;
	}

	public override void MoveToRoot()
	{
	}

	public override bool MoveToNext()
	{
		return false;
	}

	public override bool MoveToPrevious()
	{
		return false;
	}

	public override bool MoveToFirst()
	{
		return false;
	}

	public override bool MoveToFirstAttribute()
	{
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		return false;
	}

	public override bool MoveToId(string id)
	{
		return false;
	}

	public override string GetAttribute(string localName, string namespaceName)
	{
		return null;
	}

	public override bool MoveToAttribute(string localName, string namespaceName)
	{
		return false;
	}

	public override string GetNamespace(string name)
	{
		return null;
	}

	public override bool MoveToNamespace(string prefix)
	{
		return false;
	}

	public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
	{
		return false;
	}

	public override bool MoveToNextNamespace(XPathNamespaceScope scope)
	{
		return false;
	}

	public override bool MoveToParent()
	{
		return false;
	}

	public override bool MoveTo(XPathNavigator other)
	{
		return this == other;
	}

	public override XmlNodeOrder ComparePosition(XPathNavigator other)
	{
		if (this != other)
		{
			return XmlNodeOrder.Unknown;
		}
		return XmlNodeOrder.Same;
	}

	public override bool IsSamePosition(XPathNavigator other)
	{
		return this == other;
	}

	public override XPathNavigator Clone()
	{
		return this;
	}
}
