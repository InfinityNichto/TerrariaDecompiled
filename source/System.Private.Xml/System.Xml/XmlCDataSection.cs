using System.Xml.XPath;

namespace System.Xml;

public class XmlCDataSection : XmlCharacterData
{
	public override string Name => OwnerDocument.strCDataSectionName;

	public override string LocalName => OwnerDocument.strCDataSectionName;

	public override XmlNodeType NodeType => XmlNodeType.CDATA;

	public override XmlNode? ParentNode
	{
		get
		{
			switch (parentNode.NodeType)
			{
			case XmlNodeType.Document:
				return null;
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

	internal override XPathNodeType XPNodeType => XPathNodeType.Text;

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

	protected internal XmlCDataSection(string? data, XmlDocument doc)
		: base(data, doc)
	{
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateCDataSection(Data);
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteCData(Data);
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}
}
