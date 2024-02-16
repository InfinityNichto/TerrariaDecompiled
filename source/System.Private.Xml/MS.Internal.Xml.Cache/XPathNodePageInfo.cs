namespace MS.Internal.Xml.Cache;

internal sealed class XPathNodePageInfo
{
	private readonly int _pageNum;

	private int _nodeCount;

	private readonly XPathNode[] _pagePrev;

	private XPathNode[] _pageNext;

	public int PageNumber => _pageNum;

	public int NodeCount
	{
		get
		{
			return _nodeCount;
		}
		set
		{
			_nodeCount = value;
		}
	}

	public XPathNode[] PreviousPage => _pagePrev;

	public XPathNode[] NextPage
	{
		get
		{
			return _pageNext;
		}
		set
		{
			_pageNext = value;
		}
	}

	public XPathNodePageInfo(XPathNode[] pagePrev, int pageNum)
	{
		_pagePrev = pagePrev;
		_pageNum = pageNum;
		_nodeCount = 1;
	}
}
