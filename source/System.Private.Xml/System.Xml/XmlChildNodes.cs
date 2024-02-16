using System.Collections;

namespace System.Xml;

internal sealed class XmlChildNodes : XmlNodeList
{
	private readonly XmlNode _container;

	public override int Count
	{
		get
		{
			int num = 0;
			for (XmlNode xmlNode = _container.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
			{
				num++;
			}
			return num;
		}
	}

	public XmlChildNodes(XmlNode container)
	{
		_container = container;
	}

	public override XmlNode Item(int i)
	{
		if (i < 0)
		{
			return null;
		}
		XmlNode xmlNode = _container.FirstChild;
		while (xmlNode != null)
		{
			if (i == 0)
			{
				return xmlNode;
			}
			xmlNode = xmlNode.NextSibling;
			i--;
		}
		return null;
	}

	public override IEnumerator GetEnumerator()
	{
		if (_container.FirstChild == null)
		{
			return XmlDocument.EmptyEnumerator;
		}
		return new XmlChildEnumerator(_container);
	}
}
