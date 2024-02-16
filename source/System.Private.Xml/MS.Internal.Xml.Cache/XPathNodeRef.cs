namespace MS.Internal.Xml.Cache;

internal struct XPathNodeRef
{
	private readonly XPathNode[] _page;

	private readonly int _idx;

	public XPathNode[] Page => _page;

	public int Index => _idx;

	public XPathNodeRef(XPathNode[] page, int idx)
	{
		_page = page;
		_idx = idx;
	}

	public override int GetHashCode()
	{
		return XPathNodeHelper.GetLocation(_page, _idx);
	}
}
