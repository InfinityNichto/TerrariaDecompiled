using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal struct XPathNode
{
	private XPathNodeInfoAtom _info;

	private ushort _idxSibling;

	private ushort _idxParent;

	private ushort _idxSimilar;

	private ushort _posOffset;

	private uint _props;

	private string _value;

	public XPathNodeType NodeType => (XPathNodeType)((int)_props & 0xF);

	public string Prefix => _info.Prefix;

	public string LocalName => _info.LocalName;

	public string Name
	{
		get
		{
			if (Prefix.Length == 0)
			{
				return LocalName;
			}
			return Prefix + ":" + LocalName;
		}
	}

	public string NamespaceUri => _info.NamespaceUri;

	public XPathDocument Document => _info.Document;

	public string BaseUri => _info.BaseUri ?? string.Empty;

	public int LineNumber => _info.LineNumberBase + (int)((_props & 0xFFFC00) >> 10);

	public int LinePosition => _info.LinePositionBase + _posOffset;

	public int CollapsedLinePosition => LinePosition + (int)(_props >> 24);

	public XPathNodePageInfo PageInfo => _info.PageInfo;

	public bool IsXmlNamespaceNode
	{
		get
		{
			string localName = _info.LocalName;
			if (NodeType == XPathNodeType.Namespace && localName.Length == 3)
			{
				return localName == "xml";
			}
			return false;
		}
	}

	public bool HasSibling => _idxSibling != 0;

	public bool HasCollapsedText => (_props & 0x80) != 0;

	public bool HasAttribute => (_props & 0x10) != 0;

	public bool HasContentChild => (_props & 0x20) != 0;

	public bool HasElementChild => (_props & 0x40) != 0;

	public bool IsAttrNmsp
	{
		get
		{
			XPathNodeType nodeType = NodeType;
			if (nodeType != XPathNodeType.Attribute)
			{
				return nodeType == XPathNodeType.Namespace;
			}
			return true;
		}
	}

	public bool IsText => XPathNavigator.IsText(NodeType);

	public bool HasNamespaceDecls
	{
		get
		{
			return (_props & 0x200) != 0;
		}
		set
		{
			if (value)
			{
				_props |= 512u;
			}
			else
			{
				_props &= 255u;
			}
		}
	}

	public bool AllowShortcutTag => (_props & 0x100) != 0;

	public int LocalNameHashCode => _info.LocalNameHashCode;

	public string Value => _value;

	public int GetRoot(out XPathNode[] pageNode)
	{
		return _info.Document.GetRootNode(out pageNode);
	}

	public int GetParent(out XPathNode[] pageNode)
	{
		pageNode = _info.ParentPage;
		return _idxParent;
	}

	public int GetSibling(out XPathNode[] pageNode)
	{
		pageNode = _info.SiblingPage;
		return _idxSibling;
	}

	public int GetSimilarElement(out XPathNode[] pageNode)
	{
		pageNode = _info.SimilarElementPage;
		return _idxSimilar;
	}

	public bool NameMatch(string localName, string namespaceName)
	{
		if ((object)_info.LocalName == localName)
		{
			return _info.NamespaceUri == namespaceName;
		}
		return false;
	}

	public bool ElementMatch(string localName, string namespaceName)
	{
		if (NodeType == XPathNodeType.Element && (object)_info.LocalName == localName)
		{
			return _info.NamespaceUri == namespaceName;
		}
		return false;
	}

	public void Create(XPathNodePageInfo pageInfo)
	{
		_info = new XPathNodeInfoAtom(pageInfo);
	}

	public void Create(XPathNodeInfoAtom info, XPathNodeType xptyp, int idxParent)
	{
		_info = info;
		_props = (uint)xptyp;
		_idxParent = (ushort)idxParent;
	}

	public void SetLineInfoOffsets(int lineNumOffset, int linePosOffset)
	{
		_props |= (uint)(lineNumOffset << 10);
		_posOffset = (ushort)linePosOffset;
	}

	public void SetCollapsedLineInfoOffset(int posOffset)
	{
		_props |= (uint)(posOffset << 24);
	}

	public void SetValue(string value)
	{
		_value = value;
	}

	public void SetEmptyValue(bool allowShortcutTag)
	{
		_value = string.Empty;
		if (allowShortcutTag)
		{
			_props |= 256u;
		}
	}

	public void SetCollapsedValue(string value)
	{
		_value = value;
		_props |= 160u;
	}

	public void SetParentProperties(XPathNodeType xptyp)
	{
		if (xptyp == XPathNodeType.Attribute)
		{
			_props |= 16u;
			return;
		}
		_props |= 32u;
		if (xptyp == XPathNodeType.Element)
		{
			_props |= 64u;
		}
	}

	public void SetSibling(XPathNodeInfoTable infoTable, XPathNode[] pageSibling, int idxSibling)
	{
		_idxSibling = (ushort)idxSibling;
		if (pageSibling != _info.SiblingPage)
		{
			_info = infoTable.Create(_info.LocalName, _info.NamespaceUri, _info.Prefix, _info.BaseUri, _info.ParentPage, pageSibling, _info.SimilarElementPage, _info.Document, _info.LineNumberBase, _info.LinePositionBase);
		}
	}

	public void SetSimilarElement(XPathNodeInfoTable infoTable, XPathNode[] pageSimilar, int idxSimilar)
	{
		_idxSimilar = (ushort)idxSimilar;
		if (pageSimilar != _info.SimilarElementPage)
		{
			_info = infoTable.Create(_info.LocalName, _info.NamespaceUri, _info.Prefix, _info.BaseUri, _info.ParentPage, _info.SiblingPage, pageSimilar, _info.Document, _info.LineNumberBase, _info.LinePositionBase);
		}
	}
}
