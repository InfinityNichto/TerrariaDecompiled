using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;

namespace System.Xml;

internal sealed class XPathNodeList : XmlNodeList
{
	private readonly List<XmlNode> _list;

	private readonly XPathNodeIterator _nodeIterator;

	private bool _done;

	public override int Count
	{
		get
		{
			if (!_done)
			{
				ReadUntil(int.MaxValue);
			}
			return _list.Count;
		}
	}

	public XPathNodeList(XPathNodeIterator nodeIterator)
	{
		_nodeIterator = nodeIterator;
		_list = new List<XmlNode>();
		_done = false;
	}

	private XmlNode GetNode(XPathNavigator n)
	{
		IHasXmlNode hasXmlNode = (IHasXmlNode)n;
		return hasXmlNode.GetNode();
	}

	internal int ReadUntil(int index)
	{
		int num = _list.Count;
		while (!_done && num <= index)
		{
			if (_nodeIterator.MoveNext())
			{
				XmlNode node = GetNode(_nodeIterator.Current);
				if (node != null)
				{
					_list.Add(node);
					num++;
				}
				continue;
			}
			_done = true;
			break;
		}
		return num;
	}

	public override XmlNode Item(int index)
	{
		if (_list.Count <= index)
		{
			ReadUntil(index);
		}
		if (index < 0 || _list.Count <= index)
		{
			return null;
		}
		return _list[index];
	}

	public override IEnumerator GetEnumerator()
	{
		return new XmlNodeListEnumerator(this);
	}
}
