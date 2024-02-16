using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class RtfTextNavigator : RtfNavigator
{
	private string _text;

	private string _baseUri;

	private NavigatorConstructor _constr;

	public override string Value => _text;

	public override string BaseURI => _baseUri;

	public RtfTextNavigator(string text, string baseUri)
	{
		_text = text;
		_baseUri = baseUri;
		_constr = new NavigatorConstructor();
	}

	public RtfTextNavigator(RtfTextNavigator that)
	{
		_text = that._text;
		_baseUri = that._baseUri;
		_constr = that._constr;
	}

	public override void CopyToWriter(XmlWriter writer)
	{
		writer.WriteString(Value);
	}

	public override XPathNavigator ToNavigator()
	{
		return _constr.GetNavigator(_text, _baseUri, new NameTable());
	}

	public override XPathNavigator Clone()
	{
		return new RtfTextNavigator(this);
	}

	public override bool MoveTo(XPathNavigator other)
	{
		if (other is RtfTextNavigator rtfTextNavigator)
		{
			_text = rtfTextNavigator._text;
			_baseUri = rtfTextNavigator._baseUri;
			_constr = rtfTextNavigator._constr;
			return true;
		}
		return false;
	}
}
