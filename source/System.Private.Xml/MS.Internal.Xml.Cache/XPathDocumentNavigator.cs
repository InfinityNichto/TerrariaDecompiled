using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathDocumentNavigator : XPathNavigator, IXmlLineInfo
{
	private XPathNode[] _pageCurrent;

	private XPathNode[] _pageParent;

	private int _idxCurrent;

	private int _idxParent;

	private string _atomizedLocalName;

	public override string Value
	{
		get
		{
			string value = _pageCurrent[_idxCurrent].Value;
			if (value != null)
			{
				return value;
			}
			if (_idxParent != 0)
			{
				return _pageParent[_idxParent].Value;
			}
			string text = string.Empty;
			StringBuilder stringBuilder = null;
			XPathNode[] pageNode;
			XPathNode[] pageCurrent = (pageNode = _pageCurrent);
			int idxNode;
			int idxCurrent = (idxNode = _idxCurrent);
			if (!XPathNodeHelper.GetNonDescendant(ref pageNode, ref idxNode))
			{
				pageNode = null;
				idxNode = 0;
			}
			while (XPathNodeHelper.GetTextFollowing(ref pageCurrent, ref idxCurrent, pageNode, idxNode))
			{
				value = pageCurrent[idxCurrent].Value;
				if (text.Length == 0)
				{
					text = value;
					continue;
				}
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder();
					stringBuilder.Append(text);
				}
				stringBuilder.Append(value);
			}
			if (stringBuilder == null)
			{
				return text;
			}
			return stringBuilder.ToString();
		}
	}

	public override XPathNodeType NodeType => _pageCurrent[_idxCurrent].NodeType;

	public override string LocalName => _pageCurrent[_idxCurrent].LocalName;

	public override string NamespaceURI => _pageCurrent[_idxCurrent].NamespaceUri;

	public override string Name => _pageCurrent[_idxCurrent].Name;

	public override string Prefix => _pageCurrent[_idxCurrent].Prefix;

	public override string BaseURI
	{
		get
		{
			XPathNode[] pageNode;
			int num;
			if (_idxParent != 0)
			{
				pageNode = _pageParent;
				num = _idxParent;
			}
			else
			{
				pageNode = _pageCurrent;
				num = _idxCurrent;
			}
			do
			{
				XPathNodeType nodeType = pageNode[num].NodeType;
				if ((uint)nodeType <= 1u || nodeType == XPathNodeType.ProcessingInstruction)
				{
					return pageNode[num].BaseUri;
				}
				num = pageNode[num].GetParent(out pageNode);
			}
			while (num != 0);
			return string.Empty;
		}
	}

	public override bool IsEmptyElement => _pageCurrent[_idxCurrent].AllowShortcutTag;

	public override XmlNameTable NameTable => _pageCurrent[_idxCurrent].Document.NameTable;

	public override bool HasAttributes => _pageCurrent[_idxCurrent].HasAttribute;

	public override bool HasChildren => _pageCurrent[_idxCurrent].HasContentChild;

	internal override string UniqueId
	{
		get
		{
			char[] array = new char[16];
			int length = 0;
			array[length++] = XPathNavigator.NodeTypeLetter[(int)_pageCurrent[_idxCurrent].NodeType];
			int num;
			if (_idxParent != 0)
			{
				num = (_pageParent[0].PageInfo.PageNumber - 1 << 16) | (_idxParent - 1);
				do
				{
					array[length++] = XPathNavigator.UniqueIdTbl[num & 0x1F];
					num >>= 5;
				}
				while (num != 0);
				array[length++] = '0';
			}
			num = (_pageCurrent[0].PageInfo.PageNumber - 1 << 16) | (_idxCurrent - 1);
			do
			{
				array[length++] = XPathNavigator.UniqueIdTbl[num & 0x1F];
				num >>= 5;
			}
			while (num != 0);
			return new string(array, 0, length);
		}
	}

	public override object UnderlyingObject => Clone();

	public int LineNumber
	{
		get
		{
			if (_idxParent != 0 && NodeType == XPathNodeType.Text)
			{
				return _pageParent[_idxParent].LineNumber;
			}
			return _pageCurrent[_idxCurrent].LineNumber;
		}
	}

	public int LinePosition
	{
		get
		{
			if (_idxParent != 0 && NodeType == XPathNodeType.Text)
			{
				return _pageParent[_idxParent].CollapsedLinePosition;
			}
			return _pageCurrent[_idxCurrent].LinePosition;
		}
	}

	public XPathDocumentNavigator(XPathNode[] pageCurrent, int idxCurrent, XPathNode[] pageParent, int idxParent)
	{
		_pageCurrent = pageCurrent;
		_pageParent = pageParent;
		_idxCurrent = idxCurrent;
		_idxParent = idxParent;
	}

	public XPathDocumentNavigator(XPathDocumentNavigator nav)
		: this(nav._pageCurrent, nav._idxCurrent, nav._pageParent, nav._idxParent)
	{
		_atomizedLocalName = nav._atomizedLocalName;
	}

	public override XPathNavigator Clone()
	{
		return new XPathDocumentNavigator(_pageCurrent, _idxCurrent, _pageParent, _idxParent);
	}

	public override bool MoveToFirstAttribute()
	{
		XPathNode[] pageCurrent = _pageCurrent;
		int idxCurrent = _idxCurrent;
		if (XPathNodeHelper.GetFirstAttribute(ref _pageCurrent, ref _idxCurrent))
		{
			_pageParent = pageCurrent;
			_idxParent = idxCurrent;
			return true;
		}
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		return XPathNodeHelper.GetNextAttribute(ref _pageCurrent, ref _idxCurrent);
	}

	public override bool MoveToAttribute(string localName, string namespaceURI)
	{
		XPathNode[] pageCurrent = _pageCurrent;
		int idxCurrent = _idxCurrent;
		if ((object)localName != _atomizedLocalName)
		{
			_atomizedLocalName = ((localName != null) ? NameTable.Get(localName) : null);
		}
		if (XPathNodeHelper.GetAttribute(ref _pageCurrent, ref _idxCurrent, _atomizedLocalName, namespaceURI))
		{
			_pageParent = pageCurrent;
			_idxParent = idxCurrent;
			return true;
		}
		return false;
	}

	public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
	{
		XPathNode[] pageNmsp;
		for (int num = ((namespaceScope != XPathNamespaceScope.Local) ? XPathNodeHelper.GetInScopeNamespaces(_pageCurrent, _idxCurrent, out pageNmsp) : XPathNodeHelper.GetLocalNamespaces(_pageCurrent, _idxCurrent, out pageNmsp)); num != 0; num = pageNmsp[num].GetSibling(out pageNmsp))
		{
			if (namespaceScope != XPathNamespaceScope.ExcludeXml || !pageNmsp[num].IsXmlNamespaceNode)
			{
				_pageParent = _pageCurrent;
				_idxParent = _idxCurrent;
				_pageCurrent = pageNmsp;
				_idxCurrent = num;
				return true;
			}
		}
		return false;
	}

	public override bool MoveToNextNamespace(XPathNamespaceScope scope)
	{
		XPathNode[] pageNode = _pageCurrent;
		int num = _idxCurrent;
		if (pageNode[num].NodeType != XPathNodeType.Namespace)
		{
			return false;
		}
		do
		{
			num = pageNode[num].GetSibling(out pageNode);
			if (num == 0)
			{
				return false;
			}
			switch (scope)
			{
			case XPathNamespaceScope.Local:
			{
				XPathNode[] pageNode2;
				int parent = pageNode[num].GetParent(out pageNode2);
				if (parent != _idxParent || pageNode2 != _pageParent)
				{
					return false;
				}
				break;
			}
			case XPathNamespaceScope.ExcludeXml:
				continue;
			}
			break;
		}
		while (pageNode[num].IsXmlNamespaceNode);
		_pageCurrent = pageNode;
		_idxCurrent = num;
		return true;
	}

	public override bool MoveToNext()
	{
		return XPathNodeHelper.GetContentSibling(ref _pageCurrent, ref _idxCurrent);
	}

	public override bool MoveToPrevious()
	{
		if (_idxParent != 0)
		{
			return false;
		}
		return XPathNodeHelper.GetPreviousContentSibling(ref _pageCurrent, ref _idxCurrent);
	}

	public override bool MoveToFirstChild()
	{
		if (_pageCurrent[_idxCurrent].HasCollapsedText)
		{
			_pageParent = _pageCurrent;
			_idxParent = _idxCurrent;
			_idxCurrent = _pageCurrent[_idxCurrent].Document.GetCollapsedTextNode(out _pageCurrent);
			return true;
		}
		return XPathNodeHelper.GetContentChild(ref _pageCurrent, ref _idxCurrent);
	}

	public override bool MoveToParent()
	{
		if (_idxParent != 0)
		{
			_pageCurrent = _pageParent;
			_idxCurrent = _idxParent;
			_pageParent = null;
			_idxParent = 0;
			return true;
		}
		return XPathNodeHelper.GetParent(ref _pageCurrent, ref _idxCurrent);
	}

	public override bool MoveTo(XPathNavigator other)
	{
		if (other is XPathDocumentNavigator xPathDocumentNavigator)
		{
			_pageCurrent = xPathDocumentNavigator._pageCurrent;
			_idxCurrent = xPathDocumentNavigator._idxCurrent;
			_pageParent = xPathDocumentNavigator._pageParent;
			_idxParent = xPathDocumentNavigator._idxParent;
			return true;
		}
		return false;
	}

	public override bool MoveToId(string id)
	{
		XPathNode[] pageElem;
		int num = _pageCurrent[_idxCurrent].Document.LookupIdElement(id, out pageElem);
		if (num != 0)
		{
			_pageCurrent = pageElem;
			_idxCurrent = num;
			_pageParent = null;
			_idxParent = 0;
			return true;
		}
		return false;
	}

	public override bool IsSamePosition(XPathNavigator other)
	{
		if (other is XPathDocumentNavigator xPathDocumentNavigator)
		{
			if (_idxCurrent == xPathDocumentNavigator._idxCurrent && _pageCurrent == xPathDocumentNavigator._pageCurrent && _idxParent == xPathDocumentNavigator._idxParent)
			{
				return _pageParent == xPathDocumentNavigator._pageParent;
			}
			return false;
		}
		return false;
	}

	public override void MoveToRoot()
	{
		if (_idxParent != 0)
		{
			_pageParent = null;
			_idxParent = 0;
		}
		_idxCurrent = _pageCurrent[_idxCurrent].GetRoot(out _pageCurrent);
	}

	public override bool MoveToChild(string localName, string namespaceURI)
	{
		if ((object)localName != _atomizedLocalName)
		{
			_atomizedLocalName = ((localName != null) ? NameTable.Get(localName) : null);
		}
		return XPathNodeHelper.GetElementChild(ref _pageCurrent, ref _idxCurrent, _atomizedLocalName, namespaceURI);
	}

	public override bool MoveToNext(string localName, string namespaceURI)
	{
		if ((object)localName != _atomizedLocalName)
		{
			_atomizedLocalName = ((localName != null) ? NameTable.Get(localName) : null);
		}
		return XPathNodeHelper.GetElementSibling(ref _pageCurrent, ref _idxCurrent, _atomizedLocalName, namespaceURI);
	}

	public override bool MoveToChild(XPathNodeType type)
	{
		if (_pageCurrent[_idxCurrent].HasCollapsedText)
		{
			if (type != XPathNodeType.Text && type != XPathNodeType.All)
			{
				return false;
			}
			_pageParent = _pageCurrent;
			_idxParent = _idxCurrent;
			_idxCurrent = _pageCurrent[_idxCurrent].Document.GetCollapsedTextNode(out _pageCurrent);
			return true;
		}
		return XPathNodeHelper.GetContentChild(ref _pageCurrent, ref _idxCurrent, type);
	}

	public override bool MoveToNext(XPathNodeType type)
	{
		return XPathNodeHelper.GetContentSibling(ref _pageCurrent, ref _idxCurrent, type);
	}

	public override bool MoveToFollowing(string localName, string namespaceURI, XPathNavigator end)
	{
		if ((object)localName != _atomizedLocalName)
		{
			_atomizedLocalName = ((localName != null) ? NameTable.Get(localName) : null);
		}
		XPathNode[] pageEnd;
		int followingEnd = GetFollowingEnd(end as XPathDocumentNavigator, useParentOfVirtual: false, out pageEnd);
		if (_idxParent != 0)
		{
			if (!XPathNodeHelper.GetElementFollowing(ref _pageParent, ref _idxParent, pageEnd, followingEnd, _atomizedLocalName, namespaceURI))
			{
				return false;
			}
			_pageCurrent = _pageParent;
			_idxCurrent = _idxParent;
			_pageParent = null;
			_idxParent = 0;
			return true;
		}
		return XPathNodeHelper.GetElementFollowing(ref _pageCurrent, ref _idxCurrent, pageEnd, followingEnd, _atomizedLocalName, namespaceURI);
	}

	public override bool MoveToFollowing(XPathNodeType type, XPathNavigator end)
	{
		XPathDocumentNavigator xPathDocumentNavigator = end as XPathDocumentNavigator;
		XPathNode[] pageEnd;
		int followingEnd;
		if (type == XPathNodeType.Text || type == XPathNodeType.All)
		{
			if (_pageCurrent[_idxCurrent].HasCollapsedText)
			{
				if (xPathDocumentNavigator != null && _idxCurrent == xPathDocumentNavigator._idxParent && _pageCurrent == xPathDocumentNavigator._pageParent)
				{
					return false;
				}
				_pageParent = _pageCurrent;
				_idxParent = _idxCurrent;
				_idxCurrent = _pageCurrent[_idxCurrent].Document.GetCollapsedTextNode(out _pageCurrent);
				return true;
			}
			if (type == XPathNodeType.Text)
			{
				followingEnd = GetFollowingEnd(xPathDocumentNavigator, useParentOfVirtual: true, out pageEnd);
				XPathNode[] pageCurrent;
				int idxCurrent;
				if (_idxParent != 0)
				{
					pageCurrent = _pageParent;
					idxCurrent = _idxParent;
				}
				else
				{
					pageCurrent = _pageCurrent;
					idxCurrent = _idxCurrent;
				}
				if (xPathDocumentNavigator != null && xPathDocumentNavigator._idxParent != 0 && idxCurrent == followingEnd && pageCurrent == pageEnd)
				{
					return false;
				}
				if (!XPathNodeHelper.GetTextFollowing(ref pageCurrent, ref idxCurrent, pageEnd, followingEnd))
				{
					return false;
				}
				if (pageCurrent[idxCurrent].NodeType == XPathNodeType.Element)
				{
					_idxCurrent = pageCurrent[idxCurrent].Document.GetCollapsedTextNode(out _pageCurrent);
					_pageParent = pageCurrent;
					_idxParent = idxCurrent;
				}
				else
				{
					_pageCurrent = pageCurrent;
					_idxCurrent = idxCurrent;
					_pageParent = null;
					_idxParent = 0;
				}
				return true;
			}
		}
		followingEnd = GetFollowingEnd(xPathDocumentNavigator, useParentOfVirtual: false, out pageEnd);
		if (_idxParent != 0)
		{
			if (!XPathNodeHelper.GetContentFollowing(ref _pageParent, ref _idxParent, pageEnd, followingEnd, type))
			{
				return false;
			}
			_pageCurrent = _pageParent;
			_idxCurrent = _idxParent;
			_pageParent = null;
			_idxParent = 0;
			return true;
		}
		return XPathNodeHelper.GetContentFollowing(ref _pageCurrent, ref _idxCurrent, pageEnd, followingEnd, type);
	}

	public override XPathNodeIterator SelectChildren(XPathNodeType type)
	{
		return new XPathDocumentKindChildIterator(this, type);
	}

	public override XPathNodeIterator SelectChildren(string name, string namespaceURI)
	{
		if (name.Length == 0)
		{
			return base.SelectChildren(name, namespaceURI);
		}
		return new XPathDocumentElementChildIterator(this, name, namespaceURI);
	}

	public override XPathNodeIterator SelectDescendants(XPathNodeType type, bool matchSelf)
	{
		return new XPathDocumentKindDescendantIterator(this, type, matchSelf);
	}

	public override XPathNodeIterator SelectDescendants(string name, string namespaceURI, bool matchSelf)
	{
		if (name.Length == 0)
		{
			return base.SelectDescendants(name, namespaceURI, matchSelf);
		}
		return new XPathDocumentElementDescendantIterator(this, name, namespaceURI, matchSelf);
	}

	public override XmlNodeOrder ComparePosition(XPathNavigator other)
	{
		if (other is XPathDocumentNavigator xPathDocumentNavigator)
		{
			XPathDocument document = _pageCurrent[_idxCurrent].Document;
			XPathDocument document2 = xPathDocumentNavigator._pageCurrent[xPathDocumentNavigator._idxCurrent].Document;
			if (document == document2)
			{
				int num = GetPrimaryLocation();
				int num2 = xPathDocumentNavigator.GetPrimaryLocation();
				if (num == num2)
				{
					num = GetSecondaryLocation();
					num2 = xPathDocumentNavigator.GetSecondaryLocation();
					if (num == num2)
					{
						return XmlNodeOrder.Same;
					}
				}
				if (num >= num2)
				{
					return XmlNodeOrder.After;
				}
				return XmlNodeOrder.Before;
			}
		}
		return XmlNodeOrder.Unknown;
	}

	public override bool IsDescendant([NotNullWhen(true)] XPathNavigator other)
	{
		if (other is XPathDocumentNavigator xPathDocumentNavigator)
		{
			XPathNode[] pageNode;
			int num;
			if (xPathDocumentNavigator._idxParent != 0)
			{
				pageNode = xPathDocumentNavigator._pageParent;
				num = xPathDocumentNavigator._idxParent;
			}
			else
			{
				num = xPathDocumentNavigator._pageCurrent[xPathDocumentNavigator._idxCurrent].GetParent(out pageNode);
			}
			while (num != 0)
			{
				if (num == _idxCurrent && pageNode == _pageCurrent)
				{
					return true;
				}
				num = pageNode[num].GetParent(out pageNode);
			}
		}
		return false;
	}

	private int GetPrimaryLocation()
	{
		if (_idxParent == 0)
		{
			return XPathNodeHelper.GetLocation(_pageCurrent, _idxCurrent);
		}
		return XPathNodeHelper.GetLocation(_pageParent, _idxParent);
	}

	private int GetSecondaryLocation()
	{
		if (_idxParent == 0)
		{
			return int.MinValue;
		}
		return _pageCurrent[_idxCurrent].NodeType switch
		{
			XPathNodeType.Namespace => -2147483647 + XPathNodeHelper.GetLocation(_pageCurrent, _idxCurrent), 
			XPathNodeType.Attribute => XPathNodeHelper.GetLocation(_pageCurrent, _idxCurrent), 
			_ => int.MaxValue, 
		};
	}

	public bool HasLineInfo()
	{
		return _pageCurrent[_idxCurrent].Document.HasLineInfo;
	}

	public int GetPositionHashCode()
	{
		return _idxCurrent ^ _idxParent;
	}

	public bool IsElementMatch(string localName, string namespaceURI)
	{
		if ((object)localName != _atomizedLocalName)
		{
			_atomizedLocalName = ((localName != null) ? NameTable.Get(localName) : null);
		}
		if (_idxParent != 0)
		{
			return false;
		}
		return _pageCurrent[_idxCurrent].ElementMatch(_atomizedLocalName, namespaceURI);
	}

	public bool IsKindMatch(XPathNodeType typ)
	{
		return ((1 << (int)_pageCurrent[_idxCurrent].NodeType) & XPathNavigator.GetKindMask(typ)) != 0;
	}

	private int GetFollowingEnd(XPathDocumentNavigator end, bool useParentOfVirtual, out XPathNode[] pageEnd)
	{
		if (end != null && _pageCurrent[_idxCurrent].Document == end._pageCurrent[end._idxCurrent].Document)
		{
			if (end._idxParent == 0)
			{
				pageEnd = end._pageCurrent;
				return end._idxCurrent;
			}
			pageEnd = end._pageParent;
			if (!useParentOfVirtual)
			{
				return end._idxParent + 1;
			}
			return end._idxParent;
		}
		pageEnd = null;
		return 0;
	}
}
