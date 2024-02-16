using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class XPathSelectionIterator : ResetableIterator
{
	private XPathNavigator _nav;

	private readonly Query _query;

	private int _position;

	public override int Count => _query.Count;

	public override XPathNavigator Current => _nav;

	public override int CurrentPosition => _position;

	internal XPathSelectionIterator(XPathNavigator nav, Query query)
	{
		_nav = nav.Clone();
		_query = query;
	}

	private XPathSelectionIterator(XPathSelectionIterator it)
	{
		_nav = it._nav.Clone();
		_query = (Query)it._query.Clone();
		_position = it._position;
	}

	public override void Reset()
	{
		_query.Reset();
	}

	public override bool MoveNext()
	{
		XPathNavigator xPathNavigator = _query.Advance();
		if (xPathNavigator != null)
		{
			_position++;
			if (!_nav.MoveTo(xPathNavigator))
			{
				_nav = xPathNavigator.Clone();
			}
			return true;
		}
		return false;
	}

	public override XPathNodeIterator Clone()
	{
		return new XPathSelectionIterator(this);
	}
}
