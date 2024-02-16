using System.Xml.XPath;
using System.Xml.Xsl.Xslt;

namespace System.Xml.Xsl.XsltOld;

internal sealed class NavigatorInput
{
	private XPathNavigator _Navigator;

	private PositionInfo _PositionInfo;

	private readonly InputScopeManager _Manager;

	private NavigatorInput _Next;

	private readonly string _Href;

	private readonly KeywordsTable _Atoms;

	internal NavigatorInput Next
	{
		get
		{
			return _Next;
		}
		set
		{
			_Next = value;
		}
	}

	internal string Href => _Href;

	internal KeywordsTable Atoms => _Atoms;

	internal XPathNavigator Navigator => _Navigator;

	internal InputScopeManager InputScopeManager => _Manager;

	internal int LineNumber => _PositionInfo.LineNumber;

	internal int LinePosition => _PositionInfo.LinePosition;

	internal XPathNodeType NodeType => _Navigator.NodeType;

	internal string Name => _Navigator.Name;

	internal string LocalName => _Navigator.LocalName;

	internal string NamespaceURI => _Navigator.NamespaceURI;

	internal string Prefix => _Navigator.Prefix;

	internal string Value => _Navigator.Value;

	internal bool IsEmptyTag => _Navigator.IsEmptyElement;

	internal string BaseURI => _Navigator.BaseURI;

	internal bool Advance()
	{
		return _Navigator.MoveToNext();
	}

	internal bool Recurse()
	{
		return _Navigator.MoveToFirstChild();
	}

	internal bool ToParent()
	{
		return _Navigator.MoveToParent();
	}

	internal void Close()
	{
		_Navigator = null;
		_PositionInfo = null;
	}

	internal bool MoveToFirstAttribute()
	{
		return _Navigator.MoveToFirstAttribute();
	}

	internal bool MoveToNextAttribute()
	{
		return _Navigator.MoveToNextAttribute();
	}

	internal bool MoveToFirstNamespace()
	{
		return _Navigator.MoveToFirstNamespace(XPathNamespaceScope.ExcludeXml);
	}

	internal bool MoveToNextNamespace()
	{
		return _Navigator.MoveToNextNamespace(XPathNamespaceScope.ExcludeXml);
	}

	internal NavigatorInput(XPathNavigator navigator, string baseUri, InputScope rootScope)
	{
		if (navigator == null)
		{
			throw new ArgumentNullException("navigator");
		}
		if (baseUri == null)
		{
			throw new ArgumentNullException("baseUri");
		}
		_Next = null;
		_Href = baseUri;
		_Atoms = new KeywordsTable(navigator.NameTable);
		_Navigator = navigator;
		_Manager = new InputScopeManager(_Navigator, rootScope);
		_PositionInfo = PositionInfo.GetPositionInfo(_Navigator);
		if (NodeType == XPathNodeType.Root)
		{
			_Navigator.MoveToFirstChild();
		}
	}

	internal NavigatorInput(XPathNavigator navigator)
		: this(navigator, navigator.BaseURI, null)
	{
	}
}
