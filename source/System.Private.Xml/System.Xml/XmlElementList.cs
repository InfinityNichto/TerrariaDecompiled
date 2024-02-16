using System.Collections;

namespace System.Xml;

internal sealed class XmlElementList : XmlNodeList
{
	private readonly string _asterisk;

	private int _changeCount;

	private readonly string _name;

	private string _localName;

	private string _namespaceURI;

	private readonly XmlNode _rootNode;

	private int _curInd;

	private XmlNode _curElem;

	private bool _empty;

	private bool _atomized;

	private int _matchCount;

	private WeakReference<XmlElementListListener> _listener;

	internal int ChangeCount => _changeCount;

	public override int Count
	{
		get
		{
			if (_empty)
			{
				return 0;
			}
			if (_matchCount < 0)
			{
				int num = 0;
				int changeCount = _changeCount;
				XmlNode n = _rootNode;
				while ((n = GetMatchingNode(n, bNext: true)) != null)
				{
					num++;
				}
				if (changeCount != _changeCount)
				{
					return num;
				}
				_matchCount = num;
			}
			return _matchCount;
		}
	}

	private XmlElementList(XmlNode parent)
	{
		_rootNode = parent;
		_curInd = -1;
		_curElem = _rootNode;
		_changeCount = 0;
		_empty = false;
		_atomized = true;
		_matchCount = -1;
		_listener = new WeakReference<XmlElementListListener>(new XmlElementListListener(parent.Document, this));
	}

	~XmlElementList()
	{
		Dispose(disposing: false);
	}

	internal void ConcurrencyCheck(XmlNodeChangedEventArgs args)
	{
		if (!_atomized)
		{
			XmlNameTable nameTable = _rootNode.Document.NameTable;
			_localName = nameTable.Add(_localName);
			_namespaceURI = nameTable.Add(_namespaceURI);
			_atomized = true;
		}
		if (IsMatch(args.Node))
		{
			_changeCount++;
			_curInd = -1;
			_curElem = _rootNode;
			if (args.Action == XmlNodeChangedAction.Insert)
			{
				_empty = false;
			}
		}
		_matchCount = -1;
	}

	internal XmlElementList(XmlNode parent, string name)
		: this(parent)
	{
		XmlNameTable nameTable = parent.Document.NameTable;
		_asterisk = nameTable.Add("*");
		_name = nameTable.Add(name);
		_localName = null;
		_namespaceURI = null;
	}

	internal XmlElementList(XmlNode parent, string localName, string namespaceURI)
		: this(parent)
	{
		XmlNameTable nameTable = parent.Document.NameTable;
		_asterisk = nameTable.Add("*");
		_localName = nameTable.Get(localName);
		_namespaceURI = nameTable.Get(namespaceURI);
		if (_localName == null || _namespaceURI == null)
		{
			_empty = true;
			_atomized = false;
			_localName = localName;
			_namespaceURI = namespaceURI;
		}
		_name = null;
	}

	private XmlNode NextElemInPreOrder(XmlNode curNode)
	{
		XmlNode xmlNode = curNode.FirstChild;
		if (xmlNode == null)
		{
			xmlNode = curNode;
			while (xmlNode != null && xmlNode != _rootNode && xmlNode.NextSibling == null)
			{
				xmlNode = xmlNode.ParentNode;
			}
			if (xmlNode != null && xmlNode != _rootNode)
			{
				xmlNode = xmlNode.NextSibling;
			}
		}
		if (xmlNode == _rootNode)
		{
			xmlNode = null;
		}
		return xmlNode;
	}

	private XmlNode PrevElemInPreOrder(XmlNode curNode)
	{
		XmlNode xmlNode = curNode.PreviousSibling;
		while (xmlNode != null && xmlNode.LastChild != null)
		{
			xmlNode = xmlNode.LastChild;
		}
		if (xmlNode == null)
		{
			xmlNode = curNode.ParentNode;
		}
		if (xmlNode == _rootNode)
		{
			xmlNode = null;
		}
		return xmlNode;
	}

	private bool IsMatch(XmlNode curNode)
	{
		if (curNode.NodeType == XmlNodeType.Element)
		{
			if (_name != null)
			{
				if (Ref.Equal(_name, _asterisk) || Ref.Equal(curNode.Name, _name))
				{
					return true;
				}
			}
			else if ((Ref.Equal(_localName, _asterisk) || Ref.Equal(curNode.LocalName, _localName)) && (Ref.Equal(_namespaceURI, _asterisk) || curNode.NamespaceURI == _namespaceURI))
			{
				return true;
			}
		}
		return false;
	}

	private XmlNode GetMatchingNode(XmlNode n, bool bNext)
	{
		XmlNode xmlNode = n;
		do
		{
			xmlNode = ((!bNext) ? PrevElemInPreOrder(xmlNode) : NextElemInPreOrder(xmlNode));
		}
		while (xmlNode != null && !IsMatch(xmlNode));
		return xmlNode;
	}

	private XmlNode GetNthMatchingNode(XmlNode n, bool bNext, int nCount)
	{
		XmlNode xmlNode = n;
		for (int i = 0; i < nCount; i++)
		{
			xmlNode = GetMatchingNode(xmlNode, bNext);
			if (xmlNode == null)
			{
				return null;
			}
		}
		return xmlNode;
	}

	public XmlNode GetNextNode(XmlNode n)
	{
		if (_empty)
		{
			return null;
		}
		XmlNode n2 = ((n == null) ? _rootNode : n);
		return GetMatchingNode(n2, bNext: true);
	}

	public override XmlNode Item(int index)
	{
		if (_rootNode == null || index < 0)
		{
			return null;
		}
		if (_empty)
		{
			return null;
		}
		if (_curInd == index)
		{
			return _curElem;
		}
		int num = index - _curInd;
		bool bNext = num > 0;
		if (num < 0)
		{
			num = -num;
		}
		XmlNode nthMatchingNode;
		if ((nthMatchingNode = GetNthMatchingNode(_curElem, bNext, num)) != null)
		{
			_curInd = index;
			_curElem = nthMatchingNode;
			return _curElem;
		}
		return null;
	}

	public override IEnumerator GetEnumerator()
	{
		if (_empty)
		{
			return new XmlEmptyElementListEnumerator(this);
		}
		return new XmlElementListEnumerator(this);
	}

	protected override void PrivateDisposeNodeList()
	{
		GC.SuppressFinalize(this);
		Dispose(disposing: true);
	}

	private void Dispose(bool disposing)
	{
		if (_listener != null)
		{
			if (_listener.TryGetTarget(out var target))
			{
				target.Unregister();
			}
			_listener = null;
		}
	}
}
