using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathNodeInfoAtom : IEquatable<XPathNodeInfoAtom>
{
	private string _localName;

	private string _namespaceUri;

	private string _prefix;

	private string _baseUri;

	private XPathNode[] _pageParent;

	private XPathNode[] _pageSibling;

	private XPathNode[] _pageSimilar;

	private XPathDocument _doc;

	private int _lineNumBase;

	private int _linePosBase;

	private int _hashCode;

	private int _localNameHash;

	private XPathNodeInfoAtom _next;

	private XPathNodePageInfo _pageInfo;

	public XPathNodePageInfo PageInfo => _pageInfo;

	public string LocalName => _localName;

	public string NamespaceUri => _namespaceUri;

	public string Prefix => _prefix;

	public string BaseUri => _baseUri;

	public XPathNode[] SiblingPage => _pageSibling;

	public XPathNode[] SimilarElementPage => _pageSimilar;

	public XPathNode[] ParentPage => _pageParent;

	public XPathDocument Document => _doc;

	public int LineNumberBase => _lineNumBase;

	public int LinePositionBase => _linePosBase;

	public int LocalNameHashCode => _localNameHash;

	public XPathNodeInfoAtom Next
	{
		get
		{
			return _next;
		}
		set
		{
			_next = value;
		}
	}

	public XPathNodeInfoAtom(XPathNodePageInfo pageInfo)
	{
		_pageInfo = pageInfo;
	}

	public XPathNodeInfoAtom(string localName, string namespaceUri, string prefix, string baseUri, XPathNode[] pageParent, XPathNode[] pageSibling, XPathNode[] pageSimilar, XPathDocument doc, int lineNumBase, int linePosBase)
	{
		Init(localName, namespaceUri, prefix, baseUri, pageParent, pageSibling, pageSimilar, doc, lineNumBase, linePosBase);
	}

	public void Init(string localName, string namespaceUri, string prefix, string baseUri, XPathNode[] pageParent, XPathNode[] pageSibling, XPathNode[] pageSimilar, XPathDocument doc, int lineNumBase, int linePosBase)
	{
		_localName = localName;
		_namespaceUri = namespaceUri;
		_prefix = prefix;
		_baseUri = baseUri;
		_pageParent = pageParent;
		_pageSibling = pageSibling;
		_pageSimilar = pageSimilar;
		_doc = doc;
		_lineNumBase = lineNumBase;
		_linePosBase = linePosBase;
		_next = null;
		_pageInfo = null;
		_hashCode = 0;
		_localNameHash = 0;
		for (int i = 0; i < _localName.Length; i++)
		{
			_localNameHash += (_localNameHash << 7) ^ _localName[i];
		}
	}

	public override int GetHashCode()
	{
		if (_hashCode == 0)
		{
			int num = _localNameHash;
			if (_pageSibling != null)
			{
				num += (num << 7) ^ _pageSibling[0].PageInfo.PageNumber;
			}
			if (_pageParent != null)
			{
				num += (num << 7) ^ _pageParent[0].PageInfo.PageNumber;
			}
			if (_pageSimilar != null)
			{
				num += (num << 7) ^ _pageSimilar[0].PageInfo.PageNumber;
			}
			_hashCode = ((num == 0) ? 1 : num);
		}
		return _hashCode;
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		return Equals(other as XPathNodeInfoAtom);
	}

	public bool Equals(XPathNodeInfoAtom other)
	{
		if (GetHashCode() == other.GetHashCode() && (object)_localName == other._localName && _pageSibling == other._pageSibling && (object)_namespaceUri == other._namespaceUri && _pageParent == other._pageParent && _pageSimilar == other._pageSimilar && (object)_prefix == other._prefix && (object)_baseUri == other._baseUri && _lineNumBase == other._lineNumBase && _linePosBase == other._linePosBase)
		{
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("hash=");
		stringBuilder.Append(GetHashCode());
		stringBuilder.Append(", ");
		if (_localName.Length != 0)
		{
			stringBuilder.Append('{');
			stringBuilder.Append(_namespaceUri);
			stringBuilder.Append('}');
			if (_prefix.Length != 0)
			{
				stringBuilder.Append(_prefix);
				stringBuilder.Append(':');
			}
			stringBuilder.Append(_localName);
			stringBuilder.Append(", ");
		}
		if (_pageParent != null)
		{
			stringBuilder.Append("parent=");
			stringBuilder.Append(_pageParent[0].PageInfo.PageNumber);
			stringBuilder.Append(", ");
		}
		if (_pageSibling != null)
		{
			stringBuilder.Append("sibling=");
			stringBuilder.Append(_pageSibling[0].PageInfo.PageNumber);
			stringBuilder.Append(", ");
		}
		if (_pageSimilar != null)
		{
			stringBuilder.Append("similar=");
			stringBuilder.Append(_pageSimilar[0].PageInfo.PageNumber);
			stringBuilder.Append(", ");
		}
		stringBuilder.Append("lineNum=");
		stringBuilder.Append(_lineNumBase);
		stringBuilder.Append(", ");
		stringBuilder.Append("linePos=");
		stringBuilder.Append(_linePosBase);
		return stringBuilder.ToString();
	}
}
