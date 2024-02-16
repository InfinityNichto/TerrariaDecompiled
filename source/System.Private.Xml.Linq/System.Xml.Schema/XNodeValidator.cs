using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace System.Xml.Schema;

internal sealed class XNodeValidator
{
	private readonly XmlSchemaSet schemas;

	private readonly ValidationEventHandler validationEventHandler;

	private XObject source;

	private bool addSchemaInfo;

	private XmlNamespaceManager namespaceManager;

	private XmlSchemaValidator validator;

	private Dictionary<XmlSchemaInfo, XmlSchemaInfo> schemaInfos;

	private ArrayList defaultAttributes;

	private readonly XName xsiTypeName;

	private readonly XName xsiNilName;

	public XNodeValidator(XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
	{
		this.schemas = schemas;
		this.validationEventHandler = validationEventHandler;
		XNamespace xNamespace = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
		xsiTypeName = xNamespace.GetName("type");
		xsiNilName = xNamespace.GetName("nil");
	}

	public void Validate(XObject source, XmlSchemaObject partialValidationType, bool addSchemaInfo)
	{
		this.source = source;
		this.addSchemaInfo = addSchemaInfo;
		XmlSchemaValidationFlags xmlSchemaValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes;
		XmlNodeType nodeType = source.NodeType;
		if (nodeType != XmlNodeType.Element)
		{
			if (nodeType != XmlNodeType.Attribute)
			{
				if (nodeType != XmlNodeType.Document)
				{
					goto IL_0066;
				}
				source = ((XDocument)source).Root;
				if (source == null)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_MissingRoot);
				}
				xmlSchemaValidationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
			}
			else
			{
				if (((XAttribute)source).IsNamespaceDeclaration)
				{
					goto IL_0066;
				}
				if (source.Parent == null)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_MissingParent);
				}
			}
		}
		namespaceManager = new XmlNamespaceManager(schemas.NameTable);
		PushAncestorsAndSelf(source.Parent);
		validator = new XmlSchemaValidator(schemas.NameTable, schemas, namespaceManager, xmlSchemaValidationFlags);
		validator.ValidationEventHandler += ValidationCallback;
		validator.XmlResolver = null;
		if (partialValidationType != null)
		{
			validator.Initialize(partialValidationType);
		}
		else
		{
			validator.Initialize();
		}
		IXmlLineInfo originalLineInfo = SaveLineInfo(source);
		if (nodeType == XmlNodeType.Attribute)
		{
			ValidateAttribute((XAttribute)source);
		}
		else
		{
			ValidateElement((XElement)source);
		}
		validator.EndValidation();
		RestoreLineInfo(originalLineInfo);
		return;
		IL_0066:
		throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_BadNodeType, nodeType));
	}

	private XmlSchemaInfo GetDefaultAttributeSchemaInfo(XmlSchemaAttribute sa)
	{
		XmlSchemaInfo xmlSchemaInfo = new XmlSchemaInfo();
		xmlSchemaInfo.IsDefault = true;
		xmlSchemaInfo.IsNil = false;
		xmlSchemaInfo.SchemaAttribute = sa;
		XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)(xmlSchemaInfo.SchemaType = sa.AttributeSchemaType);
		if (xmlSchemaSimpleType.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
		{
			string defaultValue = GetDefaultValue(sa);
			XmlSchemaSimpleType[] baseMemberTypes = ((XmlSchemaSimpleTypeUnion)xmlSchemaSimpleType.Content).BaseMemberTypes;
			foreach (XmlSchemaSimpleType xmlSchemaSimpleType2 in baseMemberTypes)
			{
				object obj = null;
				try
				{
					obj = xmlSchemaSimpleType2.Datatype.ParseValue(defaultValue, schemas.NameTable, namespaceManager);
				}
				catch (XmlSchemaException)
				{
				}
				if (obj != null)
				{
					xmlSchemaInfo.MemberType = xmlSchemaSimpleType2;
					break;
				}
			}
		}
		xmlSchemaInfo.Validity = XmlSchemaValidity.Valid;
		return xmlSchemaInfo;
	}

	private string GetDefaultValue(XmlSchemaAttribute sa)
	{
		XmlSchemaAttribute xmlSchemaAttribute = sa;
		XmlQualifiedName refName = xmlSchemaAttribute.RefName;
		if (!refName.IsEmpty)
		{
			xmlSchemaAttribute = schemas.GlobalAttributes[refName] as XmlSchemaAttribute;
			if (xmlSchemaAttribute == null)
			{
				return null;
			}
		}
		string fixedValue = xmlSchemaAttribute.FixedValue;
		if (fixedValue != null)
		{
			return fixedValue;
		}
		return xmlSchemaAttribute.DefaultValue;
	}

	private string GetDefaultValue(XmlSchemaElement se)
	{
		XmlSchemaElement xmlSchemaElement = se;
		XmlQualifiedName refName = xmlSchemaElement.RefName;
		if (!refName.IsEmpty)
		{
			xmlSchemaElement = schemas.GlobalElements[refName] as XmlSchemaElement;
			if (xmlSchemaElement == null)
			{
				return null;
			}
		}
		string fixedValue = xmlSchemaElement.FixedValue;
		if (fixedValue != null)
		{
			return fixedValue;
		}
		return xmlSchemaElement.DefaultValue;
	}

	private void ReplaceSchemaInfo(XObject o, XmlSchemaInfo schemaInfo)
	{
		if (schemaInfos == null)
		{
			schemaInfos = new Dictionary<XmlSchemaInfo, XmlSchemaInfo>(new XmlSchemaInfoEqualityComparer());
		}
		XmlSchemaInfo value = o.Annotation<XmlSchemaInfo>();
		if (value != null)
		{
			if (!schemaInfos.ContainsKey(value))
			{
				schemaInfos.Add(value, value);
			}
			o.RemoveAnnotations<XmlSchemaInfo>();
		}
		if (!schemaInfos.TryGetValue(schemaInfo, out value))
		{
			value = schemaInfo;
			schemaInfos.Add(value, value);
		}
		o.AddAnnotation(value);
	}

	private void PushAncestorsAndSelf(XElement e)
	{
		while (e != null)
		{
			XAttribute xAttribute = e.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if (xAttribute.IsNamespaceDeclaration)
					{
						string text = xAttribute.Name.LocalName;
						if (text == "xmlns")
						{
							text = string.Empty;
						}
						if (!namespaceManager.HasNamespace(text))
						{
							namespaceManager.AddNamespace(text, xAttribute.Value);
						}
					}
				}
				while (xAttribute != e.lastAttr);
			}
			e = e.parent as XElement;
		}
	}

	private void PushElement(XElement e, ref string xsiType, ref string xsiNil)
	{
		namespaceManager.PushScope();
		XAttribute xAttribute = e.lastAttr;
		if (xAttribute == null)
		{
			return;
		}
		do
		{
			xAttribute = xAttribute.next;
			if (xAttribute.IsNamespaceDeclaration)
			{
				string text = xAttribute.Name.LocalName;
				if (text == "xmlns")
				{
					text = string.Empty;
				}
				namespaceManager.AddNamespace(text, xAttribute.Value);
				continue;
			}
			XName name = xAttribute.Name;
			if (name == xsiTypeName)
			{
				xsiType = xAttribute.Value;
			}
			else if (name == xsiNilName)
			{
				xsiNil = xAttribute.Value;
			}
		}
		while (xAttribute != e.lastAttr);
	}

	private IXmlLineInfo SaveLineInfo(XObject source)
	{
		IXmlLineInfo lineInfoProvider = validator.LineInfoProvider;
		validator.LineInfoProvider = source;
		return lineInfoProvider;
	}

	private void RestoreLineInfo(IXmlLineInfo originalLineInfo)
	{
		validator.LineInfoProvider = originalLineInfo;
	}

	private void ValidateAttribute(XAttribute a)
	{
		IXmlLineInfo originalLineInfo = SaveLineInfo(a);
		XmlSchemaInfo schemaInfo = (addSchemaInfo ? new XmlSchemaInfo() : null);
		source = a;
		validator.ValidateAttribute(a.Name.LocalName, a.Name.NamespaceName, a.Value, schemaInfo);
		if (addSchemaInfo)
		{
			ReplaceSchemaInfo(a, schemaInfo);
		}
		RestoreLineInfo(originalLineInfo);
	}

	private void ValidateAttributes(XElement e)
	{
		XAttribute xAttribute = e.lastAttr;
		IXmlLineInfo originalLineInfo = SaveLineInfo(xAttribute);
		if (xAttribute != null)
		{
			do
			{
				xAttribute = xAttribute.next;
				if (!xAttribute.IsNamespaceDeclaration)
				{
					ValidateAttribute(xAttribute);
				}
			}
			while (xAttribute != e.lastAttr);
			source = e;
		}
		if (addSchemaInfo)
		{
			if (defaultAttributes == null)
			{
				defaultAttributes = new ArrayList();
			}
			else
			{
				defaultAttributes.Clear();
			}
			validator.GetUnspecifiedDefaultAttributes(defaultAttributes);
			foreach (XmlSchemaAttribute defaultAttribute in defaultAttributes)
			{
				xAttribute = new XAttribute(XNamespace.Get(defaultAttribute.QualifiedName.Namespace).GetName(defaultAttribute.QualifiedName.Name), GetDefaultValue(defaultAttribute));
				ReplaceSchemaInfo(xAttribute, GetDefaultAttributeSchemaInfo(defaultAttribute));
				e.Add(xAttribute);
			}
		}
		RestoreLineInfo(originalLineInfo);
	}

	private void ValidateElement(XElement e)
	{
		XmlSchemaInfo xmlSchemaInfo = (addSchemaInfo ? new XmlSchemaInfo() : null);
		string xsiType = null;
		string xsiNil = null;
		PushElement(e, ref xsiType, ref xsiNil);
		IXmlLineInfo originalLineInfo = SaveLineInfo(e);
		source = e;
		validator.ValidateElement(e.Name.LocalName, e.Name.NamespaceName, xmlSchemaInfo, xsiType, xsiNil, null, null);
		ValidateAttributes(e);
		validator.ValidateEndOfAttributes(xmlSchemaInfo);
		ValidateNodes(e);
		validator.ValidateEndElement(xmlSchemaInfo);
		if (addSchemaInfo)
		{
			if (xmlSchemaInfo.Validity == XmlSchemaValidity.Valid && xmlSchemaInfo.IsDefault)
			{
				e.Value = GetDefaultValue(xmlSchemaInfo.SchemaElement);
			}
			ReplaceSchemaInfo(e, xmlSchemaInfo);
		}
		RestoreLineInfo(originalLineInfo);
		namespaceManager.PopScope();
	}

	private void ValidateNodes(XElement e)
	{
		XNode xNode = e.content as XNode;
		IXmlLineInfo originalLineInfo = SaveLineInfo(xNode);
		if (xNode != null)
		{
			do
			{
				xNode = xNode.next;
				if (xNode is XElement e2)
				{
					ValidateElement(e2);
				}
				else if (xNode is XText xText)
				{
					string value = xText.Value;
					if (value.Length > 0)
					{
						validator.LineInfoProvider = xText;
						validator.ValidateText(value);
					}
				}
			}
			while (xNode != e.content);
			source = e;
		}
		else if (e.content is string { Length: >0 } text)
		{
			validator.ValidateText(text);
		}
		RestoreLineInfo(originalLineInfo);
	}

	private void ValidationCallback(object sender, ValidationEventArgs e)
	{
		if (validationEventHandler != null)
		{
			validationEventHandler(source, e);
		}
		else if (e.Severity == XmlSeverityType.Error)
		{
			throw e.Exception;
		}
	}
}
