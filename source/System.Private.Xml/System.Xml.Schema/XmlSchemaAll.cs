using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaAll : XmlSchemaGroupBase
{
	private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();

	[XmlElement("element", typeof(XmlSchemaElement))]
	public override XmlSchemaObjectCollection Items => _items;

	internal override bool IsEmpty
	{
		get
		{
			if (!base.IsEmpty)
			{
				return _items.Count == 0;
			}
			return true;
		}
	}

	internal override void SetItems(XmlSchemaObjectCollection newItems)
	{
		_items = newItems;
	}
}
