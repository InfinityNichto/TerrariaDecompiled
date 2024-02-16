using System.Xml.XPath;

namespace System.Xml;

public class XmlText : XmlCharacterData
{
	public override string Name => OwnerDocument.strTextName;

	public override string LocalName => OwnerDocument.strTextName;

	public override XmlNodeType NodeType => XmlNodeType.Text;

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

	public override string? Value
	{
		get
		{
			return Data;
		}
		set
		{
			Data = value;
			XmlNode xmlNode = parentNode;
			if (xmlNode != null && xmlNode.NodeType == XmlNodeType.Attribute && xmlNode is XmlUnspecifiedAttribute { Specified: false } xmlUnspecifiedAttribute)
			{
				xmlUnspecifiedAttribute.SetSpecified(f: true);
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

	internal XmlText(string strData)
		: this(strData, null)
	{
	}

	protected internal XmlText(string? strData, XmlDocument doc)
		: base(strData, doc)
	{
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateTextNode(Data);
	}

	public virtual XmlText SplitText(int offset)
	{
		XmlNode xmlNode = ParentNode;
		int length = Length;
		if (offset > length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (xmlNode == null)
		{
			throw new InvalidOperationException(System.SR.Xdom_TextNode_SplitText);
		}
		int count = length - offset;
		string text = Substring(offset, count);
		DeleteData(offset, count);
		XmlText xmlText = OwnerDocument.CreateTextNode(text);
		xmlNode.InsertAfter(xmlText, this);
		return xmlText;
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteString(Data);
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}
}
