using System.Xml.XPath;

namespace System.Xml;

public class XmlSignificantWhitespace : XmlCharacterData
{
	public override string Name => OwnerDocument.strSignificantWhitespaceName;

	public override string LocalName => OwnerDocument.strSignificantWhitespaceName;

	public override XmlNodeType NodeType => XmlNodeType.SignificantWhitespace;

	public override XmlNode? ParentNode
	{
		get
		{
			switch (parentNode.NodeType)
			{
			case XmlNodeType.Document:
				return base.ParentNode;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
			{
				XmlNode xmlNode = parentNode.parentNode;
				while (xmlNode.IsText)
				{
					xmlNode = xmlNode.parentNode;
				}
				return xmlNode;
			}
			default:
				return parentNode;
			}
		}
	}

	public override string? Value
	{
		get
		{
			return Data;
		}
		set
		{
			if (CheckOnData(value))
			{
				Data = value;
				return;
			}
			throw new ArgumentException(System.SR.Xdom_WS_Char);
		}
	}

	internal override XPathNodeType XPNodeType
	{
		get
		{
			XPathNodeType xnt = XPathNodeType.SignificantWhitespace;
			DecideXPNodeTypeForTextNodes(this, ref xnt);
			return xnt;
		}
	}

	internal override bool IsText => true;

	public override XmlNode? PreviousText
	{
		get
		{
			if (parentNode != null && parentNode.IsText)
			{
				return parentNode;
			}
			return null;
		}
	}

	protected internal XmlSignificantWhitespace(string? strData, XmlDocument doc)
		: base(strData, doc)
	{
		if (!doc.IsLoading && !CheckOnData(strData))
		{
			throw new ArgumentException(System.SR.Xdom_WS_Char);
		}
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateSignificantWhitespace(Data);
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteString(Data);
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}
}
