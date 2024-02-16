using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class RtfTreeNavigator : RtfNavigator
{
	private XmlEventCache _events;

	private NavigatorConstructor _constr;

	private XmlNameTable _nameTable;

	public override string Value => _events.EventsToString();

	public override string BaseURI => _events.BaseUri;

	public RtfTreeNavigator(XmlEventCache events, XmlNameTable nameTable)
	{
		_events = events;
		_constr = new NavigatorConstructor();
		_nameTable = nameTable;
	}

	public RtfTreeNavigator(RtfTreeNavigator that)
	{
		_events = that._events;
		_constr = that._constr;
		_nameTable = that._nameTable;
	}

	public override void CopyToWriter(XmlWriter writer)
	{
		_events.EventsToWriter(writer);
	}

	public override XPathNavigator ToNavigator()
	{
		return _constr.GetNavigator(_events, _nameTable);
	}

	public override XPathNavigator Clone()
	{
		return new RtfTreeNavigator(this);
	}

	public override bool MoveTo(XPathNavigator other)
	{
		if (other is RtfTreeNavigator rtfTreeNavigator)
		{
			_events = rtfTreeNavigator._events;
			_constr = rtfTreeNavigator._constr;
			_nameTable = rtfTreeNavigator._nameTable;
			return true;
		}
		return false;
	}
}
