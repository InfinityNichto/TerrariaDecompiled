namespace System.Xml.Schema;

internal sealed class AutoValidator : BaseValidator
{
	public override bool PreserveWhitespace => false;

	public AutoValidator(XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling eventHandling)
		: base(reader, schemaCollection, eventHandling)
	{
		schemaInfo = new SchemaInfo();
	}

	public override void Validate()
	{
		switch (DetectValidationType())
		{
		case ValidationType.XDR:
			reader.Validator = new XdrValidator(this);
			reader.Validator.Validate();
			break;
		case ValidationType.Schema:
			reader.Validator = new XsdValidator(this);
			reader.Validator.Validate();
			break;
		case ValidationType.Auto:
		case ValidationType.DTD:
			break;
		}
	}

	public override void CompleteValidation()
	{
	}

	public override object FindId(string name)
	{
		return null;
	}

	private ValidationType DetectValidationType()
	{
		if (reader.Schemas != null && reader.Schemas.Count > 0)
		{
			XmlSchemaCollectionEnumerator enumerator = reader.Schemas.GetEnumerator();
			while (enumerator.MoveNext())
			{
				XmlSchemaCollectionNode currentNode = enumerator.CurrentNode;
				SchemaInfo schemaInfo = currentNode.SchemaInfo;
				if (schemaInfo.SchemaType == SchemaType.XSD)
				{
					return ValidationType.Schema;
				}
				if (schemaInfo.SchemaType == SchemaType.XDR)
				{
					return ValidationType.XDR;
				}
			}
		}
		if (reader.NodeType == XmlNodeType.Element)
		{
			switch (base.SchemaNames.SchemaTypeFromRoot(reader.LocalName, reader.NamespaceURI))
			{
			case SchemaType.XSD:
				return ValidationType.Schema;
			case SchemaType.XDR:
				return ValidationType.XDR;
			}
			int attributeCount = reader.AttributeCount;
			for (int i = 0; i < attributeCount; i++)
			{
				reader.MoveToAttribute(i);
				string namespaceURI = reader.NamespaceURI;
				string localName = reader.LocalName;
				if (Ref.Equal(namespaceURI, base.SchemaNames.NsXmlNs))
				{
					if (XdrBuilder.IsXdrSchema(reader.Value))
					{
						reader.MoveToElement();
						return ValidationType.XDR;
					}
					continue;
				}
				if (Ref.Equal(namespaceURI, base.SchemaNames.NsXsi))
				{
					reader.MoveToElement();
					return ValidationType.Schema;
				}
				if (Ref.Equal(namespaceURI, base.SchemaNames.QnDtDt.Namespace) && Ref.Equal(localName, base.SchemaNames.QnDtDt.Name))
				{
					reader.SchemaTypeObject = XmlSchemaDatatype.FromXdrName(reader.Value);
					reader.MoveToElement();
					return ValidationType.XDR;
				}
			}
			if (attributeCount > 0)
			{
				reader.MoveToElement();
			}
		}
		return ValidationType.Auto;
	}
}
