namespace System.Xml.Xsl.Runtime;

internal sealed class WhitespaceRuleReader : XmlWrappingReader
{
	private readonly WhitespaceRuleLookup _wsRules;

	private readonly BitStack _stkStrip;

	private bool _shouldStrip;

	private bool _preserveAdjacent;

	private string _val;

	public override string Value
	{
		get
		{
			if (_val != null)
			{
				return _val;
			}
			return base.Value;
		}
	}

	public static XmlReader CreateReader(XmlReader baseReader, WhitespaceRuleLookup wsRules)
	{
		if (wsRules == null)
		{
			return baseReader;
		}
		XmlReaderSettings settings = baseReader.Settings;
		if (settings != null)
		{
			if (settings.IgnoreWhitespace)
			{
				return baseReader;
			}
		}
		else
		{
			if (baseReader is XmlTextReader { WhitespaceHandling: WhitespaceHandling.None })
			{
				return baseReader;
			}
			if (baseReader is XmlTextReaderImpl { WhitespaceHandling: WhitespaceHandling.None })
			{
				return baseReader;
			}
		}
		return new WhitespaceRuleReader(baseReader, wsRules);
	}

	private WhitespaceRuleReader(XmlReader baseReader, WhitespaceRuleLookup wsRules)
		: base(baseReader)
	{
		_val = null;
		_stkStrip = new BitStack();
		_shouldStrip = false;
		_preserveAdjacent = false;
		_wsRules = wsRules;
		_wsRules.Atomize(baseReader.NameTable);
	}

	public override bool Read()
	{
		string text = null;
		_val = null;
		while (base.Read())
		{
			switch (base.NodeType)
			{
			case XmlNodeType.Element:
				if (!base.IsEmptyElement)
				{
					_stkStrip.PushBit(_shouldStrip);
					_shouldStrip = _wsRules.ShouldStripSpace(base.LocalName, base.NamespaceURI) && base.XmlSpace != XmlSpace.Preserve;
				}
				break;
			case XmlNodeType.EndElement:
				_shouldStrip = _stkStrip.PopBit();
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				if (_preserveAdjacent)
				{
					return true;
				}
				if (!_shouldStrip)
				{
					break;
				}
				if (!XmlCharType.IsOnlyWhitespace(base.Value))
				{
					if (text != null)
					{
						_val = text + base.Value;
					}
					_preserveAdjacent = true;
					return true;
				}
				goto case XmlNodeType.Whitespace;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				if (_preserveAdjacent)
				{
					return true;
				}
				if (_shouldStrip)
				{
					text = ((text != null) ? (text + base.Value) : base.Value);
					continue;
				}
				break;
			case XmlNodeType.EndEntity:
				continue;
			}
			_preserveAdjacent = false;
			return true;
		}
		return false;
	}
}
