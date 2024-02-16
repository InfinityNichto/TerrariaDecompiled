namespace System.Xml;

public abstract class XmlLinkedNode : XmlNode
{
	internal XmlLinkedNode next;

	public override XmlNode? PreviousSibling
	{
		get
		{
			XmlNode xmlNode = ParentNode;
			if (xmlNode != null)
			{
				XmlNode xmlNode2 = xmlNode.FirstChild;
				while (xmlNode2 != null)
				{
					XmlNode nextSibling = xmlNode2.NextSibling;
					if (nextSibling == this)
					{
						break;
					}
					xmlNode2 = nextSibling;
				}
				return xmlNode2;
			}
			return null;
		}
	}

	public override XmlNode? NextSibling
	{
		get
		{
			XmlNode xmlNode = ParentNode;
			if (xmlNode != null && next != xmlNode.FirstChild)
			{
				return next;
			}
			return null;
		}
	}

	internal XmlLinkedNode(XmlDocument doc)
		: base(doc)
	{
		next = null;
	}
}
