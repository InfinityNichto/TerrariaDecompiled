using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class SchemaGraph
{
	private readonly ArrayList _empty = new ArrayList();

	private readonly XmlSchemas _schemas;

	private readonly Hashtable _scope;

	private readonly int _items;

	[RequiresUnreferencedCode("Calls Compile")]
	internal SchemaGraph(Hashtable scope, XmlSchemas schemas)
	{
		_scope = scope;
		schemas.Compile(null, fullCompile: false);
		_schemas = schemas;
		_items = 0;
		foreach (XmlSchema schema in schemas)
		{
			_items += schema.Items.Count;
			foreach (XmlSchemaObject item in schema.Items)
			{
				Depends(item);
			}
		}
	}

	internal ArrayList GetItems()
	{
		return new ArrayList(_scope.Keys);
	}

	internal void AddRef(ArrayList list, XmlSchemaObject o)
	{
		if (o != null && !_schemas.IsReference(o) && o.Parent is XmlSchema xmlSchema)
		{
			string targetNamespace = xmlSchema.TargetNamespace;
			if (!(targetNamespace == "http://www.w3.org/2001/XMLSchema") && !list.Contains(o))
			{
				list.Add(o);
			}
		}
	}

	internal ArrayList Depends(XmlSchemaObject item)
	{
		if (item.Parent is XmlSchema)
		{
			if (_scope[item] != null)
			{
				return (ArrayList)_scope[item];
			}
			ArrayList arrayList = new ArrayList();
			Depends(item, arrayList);
			_scope.Add(item, arrayList);
			return arrayList;
		}
		return _empty;
	}

	internal void Depends(XmlSchemaObject item, ArrayList refs)
	{
		if (item == null || _scope[item] != null)
		{
			return;
		}
		Type type = item.GetType();
		if (typeof(XmlSchemaType).IsAssignableFrom(type))
		{
			XmlQualifiedName xmlQualifiedName = XmlQualifiedName.Empty;
			XmlSchemaType xmlSchemaType = null;
			XmlSchemaParticle xmlSchemaParticle = null;
			XmlSchemaObjectCollection xmlSchemaObjectCollection = null;
			if (item is XmlSchemaComplexType)
			{
				XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)item;
				if (xmlSchemaComplexType.ContentModel != null)
				{
					XmlSchemaContent content = xmlSchemaComplexType.ContentModel.Content;
					if (content is XmlSchemaComplexContentRestriction)
					{
						xmlQualifiedName = ((XmlSchemaComplexContentRestriction)content).BaseTypeName;
						xmlSchemaObjectCollection = ((XmlSchemaComplexContentRestriction)content).Attributes;
					}
					else if (content is XmlSchemaSimpleContentRestriction)
					{
						XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction = (XmlSchemaSimpleContentRestriction)content;
						if (xmlSchemaSimpleContentRestriction.BaseType != null)
						{
							xmlSchemaType = xmlSchemaSimpleContentRestriction.BaseType;
						}
						else
						{
							xmlQualifiedName = xmlSchemaSimpleContentRestriction.BaseTypeName;
						}
						xmlSchemaObjectCollection = xmlSchemaSimpleContentRestriction.Attributes;
					}
					else if (content is XmlSchemaComplexContentExtension)
					{
						XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = (XmlSchemaComplexContentExtension)content;
						xmlSchemaObjectCollection = xmlSchemaComplexContentExtension.Attributes;
						xmlSchemaParticle = xmlSchemaComplexContentExtension.Particle;
						xmlQualifiedName = xmlSchemaComplexContentExtension.BaseTypeName;
					}
					else if (content is XmlSchemaSimpleContentExtension)
					{
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)content;
						xmlSchemaObjectCollection = xmlSchemaSimpleContentExtension.Attributes;
						xmlQualifiedName = xmlSchemaSimpleContentExtension.BaseTypeName;
					}
				}
				else
				{
					xmlSchemaObjectCollection = xmlSchemaComplexType.Attributes;
					xmlSchemaParticle = xmlSchemaComplexType.Particle;
				}
				if (xmlSchemaParticle is XmlSchemaGroupRef)
				{
					XmlSchemaGroupRef xmlSchemaGroupRef = (XmlSchemaGroupRef)xmlSchemaParticle;
					xmlSchemaParticle = ((XmlSchemaGroup)_schemas.Find(xmlSchemaGroupRef.RefName, typeof(XmlSchemaGroup), checkCache: false)).Particle;
				}
				else if (xmlSchemaParticle is XmlSchemaGroupBase)
				{
					xmlSchemaParticle = (XmlSchemaGroupBase)xmlSchemaParticle;
				}
			}
			else if (item is XmlSchemaSimpleType)
			{
				XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)item;
				XmlSchemaSimpleTypeContent content2 = xmlSchemaSimpleType.Content;
				if (content2 is XmlSchemaSimpleTypeRestriction)
				{
					xmlSchemaType = ((XmlSchemaSimpleTypeRestriction)content2).BaseType;
					xmlQualifiedName = ((XmlSchemaSimpleTypeRestriction)content2).BaseTypeName;
				}
				else if (content2 is XmlSchemaSimpleTypeList)
				{
					XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = (XmlSchemaSimpleTypeList)content2;
					if (xmlSchemaSimpleTypeList.ItemTypeName != null && !xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
					{
						xmlQualifiedName = xmlSchemaSimpleTypeList.ItemTypeName;
					}
					if (xmlSchemaSimpleTypeList.ItemType != null)
					{
						xmlSchemaType = xmlSchemaSimpleTypeList.ItemType;
					}
				}
				else if (type == typeof(XmlSchemaSimpleTypeUnion))
				{
					XmlQualifiedName[] memberTypes = ((XmlSchemaSimpleTypeUnion)item).MemberTypes;
					if (memberTypes != null)
					{
						for (int i = 0; i < memberTypes.Length; i++)
						{
							XmlSchemaType o = (XmlSchemaType)_schemas.Find(memberTypes[i], typeof(XmlSchemaType), checkCache: false);
							AddRef(refs, o);
						}
					}
				}
			}
			if (xmlSchemaType == null && !xmlQualifiedName.IsEmpty && xmlQualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				xmlSchemaType = (XmlSchemaType)_schemas.Find(xmlQualifiedName, typeof(XmlSchemaType), checkCache: false);
			}
			if (xmlSchemaType != null)
			{
				AddRef(refs, xmlSchemaType);
			}
			if (xmlSchemaParticle != null)
			{
				Depends(xmlSchemaParticle, refs);
			}
			if (xmlSchemaObjectCollection != null)
			{
				for (int j = 0; j < xmlSchemaObjectCollection.Count; j++)
				{
					Depends(xmlSchemaObjectCollection[j], refs);
				}
			}
		}
		else if (type == typeof(XmlSchemaElement))
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)item;
			if (!xmlSchemaElement.SubstitutionGroup.IsEmpty && xmlSchemaElement.SubstitutionGroup.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				XmlSchemaElement o2 = (XmlSchemaElement)_schemas.Find(xmlSchemaElement.SubstitutionGroup, typeof(XmlSchemaElement), checkCache: false);
				AddRef(refs, o2);
			}
			if (!xmlSchemaElement.RefName.IsEmpty)
			{
				xmlSchemaElement = (XmlSchemaElement)_schemas.Find(xmlSchemaElement.RefName, typeof(XmlSchemaElement), checkCache: false);
				AddRef(refs, xmlSchemaElement);
			}
			else if (!xmlSchemaElement.SchemaTypeName.IsEmpty)
			{
				XmlSchemaType o3 = (XmlSchemaType)_schemas.Find(xmlSchemaElement.SchemaTypeName, typeof(XmlSchemaType), checkCache: false);
				AddRef(refs, o3);
			}
			else
			{
				Depends(xmlSchemaElement.SchemaType, refs);
			}
		}
		else if (type == typeof(XmlSchemaGroup))
		{
			Depends(((XmlSchemaGroup)item).Particle);
		}
		else if (type == typeof(XmlSchemaGroupRef))
		{
			XmlSchemaGroup o4 = (XmlSchemaGroup)_schemas.Find(((XmlSchemaGroupRef)item).RefName, typeof(XmlSchemaGroup), checkCache: false);
			AddRef(refs, o4);
		}
		else if (typeof(XmlSchemaGroupBase).IsAssignableFrom(type))
		{
			foreach (XmlSchemaObject item2 in ((XmlSchemaGroupBase)item).Items)
			{
				Depends(item2, refs);
			}
		}
		else if (type == typeof(XmlSchemaAttributeGroupRef))
		{
			XmlSchemaAttributeGroup o5 = (XmlSchemaAttributeGroup)_schemas.Find(((XmlSchemaAttributeGroupRef)item).RefName, typeof(XmlSchemaAttributeGroup), checkCache: false);
			AddRef(refs, o5);
		}
		else if (type == typeof(XmlSchemaAttributeGroup))
		{
			foreach (XmlSchemaObject attribute in ((XmlSchemaAttributeGroup)item).Attributes)
			{
				Depends(attribute, refs);
			}
		}
		else if (type == typeof(XmlSchemaAttribute))
		{
			XmlSchemaAttribute xmlSchemaAttribute = (XmlSchemaAttribute)item;
			if (!xmlSchemaAttribute.RefName.IsEmpty)
			{
				xmlSchemaAttribute = (XmlSchemaAttribute)_schemas.Find(xmlSchemaAttribute.RefName, typeof(XmlSchemaAttribute), checkCache: false);
				AddRef(refs, xmlSchemaAttribute);
			}
			else if (!xmlSchemaAttribute.SchemaTypeName.IsEmpty)
			{
				XmlSchemaType o6 = (XmlSchemaType)_schemas.Find(xmlSchemaAttribute.SchemaTypeName, typeof(XmlSchemaType), checkCache: false);
				AddRef(refs, o6);
			}
			else
			{
				Depends(xmlSchemaAttribute.SchemaType, refs);
			}
		}
		if (!typeof(XmlSchemaAnnotated).IsAssignableFrom(type))
		{
			return;
		}
		XmlAttribute[] unhandledAttributes = ((XmlSchemaAnnotated)item).UnhandledAttributes;
		if (unhandledAttributes == null)
		{
			return;
		}
		foreach (XmlAttribute xmlAttribute in unhandledAttributes)
		{
			if (xmlAttribute.LocalName == "arrayType" && xmlAttribute.NamespaceURI == "http://schemas.xmlsoap.org/wsdl/")
			{
				string dims;
				XmlQualifiedName name = TypeScope.ParseWsdlArrayType(xmlAttribute.Value, out dims, item);
				XmlSchemaType o7 = (XmlSchemaType)_schemas.Find(name, typeof(XmlSchemaType), checkCache: false);
				AddRef(refs, o7);
			}
		}
	}
}
