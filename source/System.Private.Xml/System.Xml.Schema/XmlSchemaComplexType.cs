using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaComplexType : XmlSchemaType
{
	private static readonly XmlSchemaComplexType s_anyTypeLax = CreateAnyType(XmlSchemaContentProcessing.Lax);

	private static readonly XmlSchemaComplexType s_anyTypeSkip = CreateAnyType(XmlSchemaContentProcessing.Skip);

	private static readonly XmlSchemaComplexType s_untypedAnyType = CreateUntypedAnyType();

	private XmlSchemaDerivationMethod _block = XmlSchemaDerivationMethod.None;

	private XmlSchemaContentModel _contentModel;

	private XmlSchemaParticle _particle;

	private XmlSchemaObjectCollection _attributes;

	private XmlSchemaAnyAttribute _anyAttribute;

	private XmlSchemaParticle _contentTypeParticle = XmlSchemaParticle.Empty;

	private XmlSchemaDerivationMethod _blockResolved;

	private XmlSchemaObjectTable _localElements;

	private XmlSchemaObjectTable _attributeUses;

	private XmlSchemaAnyAttribute _attributeWildcard;

	private byte _pvFlags;

	[XmlIgnore]
	internal static XmlSchemaComplexType AnyType => s_anyTypeLax;

	[XmlIgnore]
	internal static XmlSchemaComplexType UntypedAnyType => s_untypedAnyType;

	[XmlIgnore]
	internal static XmlSchemaComplexType AnyTypeSkip => s_anyTypeSkip;

	internal static ContentValidator AnyTypeContentValidator => s_anyTypeLax.ElementDecl.ContentValidator;

	[XmlAttribute("abstract")]
	[DefaultValue(false)]
	public bool IsAbstract
	{
		get
		{
			return (_pvFlags & 4) != 0;
		}
		set
		{
			if (value)
			{
				_pvFlags |= 4;
			}
			else
			{
				_pvFlags = (byte)(_pvFlags & 0xFFFFFFFBu);
			}
		}
	}

	[XmlAttribute("block")]
	[DefaultValue(XmlSchemaDerivationMethod.None)]
	public XmlSchemaDerivationMethod Block
	{
		get
		{
			return _block;
		}
		set
		{
			_block = value;
		}
	}

	[XmlAttribute("mixed")]
	[DefaultValue(false)]
	public override bool IsMixed
	{
		get
		{
			return (_pvFlags & 2) != 0;
		}
		set
		{
			if (value)
			{
				_pvFlags |= 2;
			}
			else
			{
				_pvFlags = (byte)(_pvFlags & 0xFFFFFFFDu);
			}
		}
	}

	[XmlElement("simpleContent", typeof(XmlSchemaSimpleContent))]
	[XmlElement("complexContent", typeof(XmlSchemaComplexContent))]
	public XmlSchemaContentModel? ContentModel
	{
		get
		{
			return _contentModel;
		}
		set
		{
			_contentModel = value;
		}
	}

	[XmlElement("group", typeof(XmlSchemaGroupRef))]
	[XmlElement("choice", typeof(XmlSchemaChoice))]
	[XmlElement("all", typeof(XmlSchemaAll))]
	[XmlElement("sequence", typeof(XmlSchemaSequence))]
	public XmlSchemaParticle? Particle
	{
		get
		{
			return _particle;
		}
		set
		{
			_particle = value;
		}
	}

	[XmlElement("attribute", typeof(XmlSchemaAttribute))]
	[XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroupRef))]
	public XmlSchemaObjectCollection Attributes
	{
		get
		{
			if (_attributes == null)
			{
				_attributes = new XmlSchemaObjectCollection();
			}
			return _attributes;
		}
	}

	[XmlElement("anyAttribute")]
	public XmlSchemaAnyAttribute? AnyAttribute
	{
		get
		{
			return _anyAttribute;
		}
		set
		{
			_anyAttribute = value;
		}
	}

	[XmlIgnore]
	public XmlSchemaContentType ContentType => base.SchemaContentType;

	[XmlIgnore]
	public XmlSchemaParticle ContentTypeParticle => _contentTypeParticle;

	[XmlIgnore]
	public XmlSchemaDerivationMethod BlockResolved => _blockResolved;

	[XmlIgnore]
	public XmlSchemaObjectTable AttributeUses
	{
		get
		{
			if (_attributeUses == null)
			{
				_attributeUses = new XmlSchemaObjectTable();
			}
			return _attributeUses;
		}
	}

	[XmlIgnore]
	public XmlSchemaAnyAttribute? AttributeWildcard => _attributeWildcard;

	[XmlIgnore]
	internal XmlSchemaObjectTable LocalElements
	{
		get
		{
			if (_localElements == null)
			{
				_localElements = new XmlSchemaObjectTable();
			}
			return _localElements;
		}
	}

	internal bool HasWildCard
	{
		get
		{
			return (_pvFlags & 1) != 0;
		}
		set
		{
			if (value)
			{
				_pvFlags |= 1;
			}
			else
			{
				_pvFlags = (byte)(_pvFlags & 0xFFFFFFFEu);
			}
		}
	}

	internal override XmlQualifiedName DerivedFrom
	{
		get
		{
			if (_contentModel == null)
			{
				return XmlQualifiedName.Empty;
			}
			if (_contentModel.Content is XmlSchemaComplexContentRestriction)
			{
				return ((XmlSchemaComplexContentRestriction)_contentModel.Content).BaseTypeName;
			}
			if (_contentModel.Content is XmlSchemaComplexContentExtension)
			{
				return ((XmlSchemaComplexContentExtension)_contentModel.Content).BaseTypeName;
			}
			if (_contentModel.Content is XmlSchemaSimpleContentRestriction)
			{
				return ((XmlSchemaSimpleContentRestriction)_contentModel.Content).BaseTypeName;
			}
			if (_contentModel.Content is XmlSchemaSimpleContentExtension)
			{
				return ((XmlSchemaSimpleContentExtension)_contentModel.Content).BaseTypeName;
			}
			return XmlQualifiedName.Empty;
		}
	}

	private static XmlSchemaComplexType CreateUntypedAnyType()
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.SetQualifiedName(new XmlQualifiedName("untypedAny", "http://www.w3.org/2003/11/xpath-datatypes"));
		xmlSchemaComplexType.IsMixed = true;
		xmlSchemaComplexType.SetContentTypeParticle(s_anyTypeLax.ContentTypeParticle);
		xmlSchemaComplexType.SetContentType(XmlSchemaContentType.Mixed);
		xmlSchemaComplexType.ElementDecl = SchemaElementDecl.CreateAnyTypeElementDecl();
		xmlSchemaComplexType.ElementDecl.SchemaType = xmlSchemaComplexType;
		xmlSchemaComplexType.ElementDecl.ContentValidator = AnyTypeContentValidator;
		return xmlSchemaComplexType;
	}

	private static XmlSchemaComplexType CreateAnyType(XmlSchemaContentProcessing processContents)
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.SetQualifiedName(DatatypeImplementation.QnAnyType);
		XmlSchemaAny xmlSchemaAny = new XmlSchemaAny();
		xmlSchemaAny.MinOccurs = 0m;
		xmlSchemaAny.MaxOccurs = decimal.MaxValue;
		xmlSchemaAny.ProcessContents = processContents;
		xmlSchemaAny.BuildNamespaceList(null);
		XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
		xmlSchemaSequence.Items.Add(xmlSchemaAny);
		xmlSchemaComplexType.SetContentTypeParticle(xmlSchemaSequence);
		xmlSchemaComplexType.SetContentType(XmlSchemaContentType.Mixed);
		xmlSchemaComplexType.ElementDecl = SchemaElementDecl.CreateAnyTypeElementDecl();
		xmlSchemaComplexType.ElementDecl.SchemaType = xmlSchemaComplexType;
		ParticleContentValidator particleContentValidator = new ParticleContentValidator(XmlSchemaContentType.Mixed);
		particleContentValidator.Start();
		particleContentValidator.OpenGroup();
		particleContentValidator.AddNamespaceList(xmlSchemaAny.NamespaceList, xmlSchemaAny);
		particleContentValidator.AddStar();
		particleContentValidator.CloseGroup();
		ContentValidator contentValidator = particleContentValidator.Finish(useDFA: true);
		xmlSchemaComplexType.ElementDecl.ContentValidator = contentValidator;
		XmlSchemaAnyAttribute xmlSchemaAnyAttribute = new XmlSchemaAnyAttribute();
		xmlSchemaAnyAttribute.ProcessContents = processContents;
		xmlSchemaAnyAttribute.BuildNamespaceList(null);
		xmlSchemaComplexType.SetAttributeWildcard(xmlSchemaAnyAttribute);
		xmlSchemaComplexType.ElementDecl.AnyAttribute = xmlSchemaAnyAttribute;
		return xmlSchemaComplexType;
	}

	internal void SetContentTypeParticle(XmlSchemaParticle value)
	{
		_contentTypeParticle = value;
	}

	internal void SetBlockResolved(XmlSchemaDerivationMethod value)
	{
		_blockResolved = value;
	}

	internal void SetAttributeWildcard(XmlSchemaAnyAttribute value)
	{
		_attributeWildcard = value;
	}

	internal void SetAttributes(XmlSchemaObjectCollection newAttributes)
	{
		_attributes = newAttributes;
	}

	internal bool ContainsIdAttribute(bool findAll)
	{
		int num = 0;
		foreach (XmlSchemaAttribute value in AttributeUses.Values)
		{
			if (value.Use == XmlSchemaUse.Prohibited)
			{
				continue;
			}
			XmlSchemaDatatype datatype = value.Datatype;
			if (datatype != null && datatype.TypeCode == XmlTypeCode.Id)
			{
				num++;
				if (num > 1)
				{
					break;
				}
			}
		}
		if (!findAll)
		{
			return num > 0;
		}
		return num > 1;
	}

	internal override XmlSchemaObject Clone()
	{
		return Clone(null);
	}

	internal XmlSchemaObject Clone(XmlSchema parentSchema)
	{
		XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)MemberwiseClone();
		if (xmlSchemaComplexType.ContentModel != null)
		{
			if (xmlSchemaComplexType.ContentModel is XmlSchemaSimpleContent xmlSchemaSimpleContent)
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent2 = (XmlSchemaSimpleContent)xmlSchemaSimpleContent.Clone();
				if (xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension)
				{
					XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension2 = (XmlSchemaSimpleContentExtension)xmlSchemaSimpleContentExtension.Clone();
					xmlSchemaSimpleContentExtension2.BaseTypeName = xmlSchemaSimpleContentExtension.BaseTypeName.Clone();
					xmlSchemaSimpleContentExtension2.SetAttributes(CloneAttributes(xmlSchemaSimpleContentExtension.Attributes));
					xmlSchemaSimpleContent2.Content = xmlSchemaSimpleContentExtension2;
				}
				else
				{
					XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction = (XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContent.Content;
					XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction2 = (XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContentRestriction.Clone();
					xmlSchemaSimpleContentRestriction2.BaseTypeName = xmlSchemaSimpleContentRestriction.BaseTypeName.Clone();
					xmlSchemaSimpleContentRestriction2.SetAttributes(CloneAttributes(xmlSchemaSimpleContentRestriction.Attributes));
					xmlSchemaSimpleContent2.Content = xmlSchemaSimpleContentRestriction2;
				}
				xmlSchemaComplexType.ContentModel = xmlSchemaSimpleContent2;
			}
			else
			{
				XmlSchemaComplexContent xmlSchemaComplexContent = (XmlSchemaComplexContent)xmlSchemaComplexType.ContentModel;
				XmlSchemaComplexContent xmlSchemaComplexContent2 = (XmlSchemaComplexContent)xmlSchemaComplexContent.Clone();
				if (xmlSchemaComplexContent.Content is XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension)
				{
					XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension2 = (XmlSchemaComplexContentExtension)xmlSchemaComplexContentExtension.Clone();
					xmlSchemaComplexContentExtension2.BaseTypeName = xmlSchemaComplexContentExtension.BaseTypeName.Clone();
					xmlSchemaComplexContentExtension2.SetAttributes(CloneAttributes(xmlSchemaComplexContentExtension.Attributes));
					if (HasParticleRef(xmlSchemaComplexContentExtension.Particle, parentSchema))
					{
						xmlSchemaComplexContentExtension2.Particle = CloneParticle(xmlSchemaComplexContentExtension.Particle, parentSchema);
					}
					xmlSchemaComplexContent2.Content = xmlSchemaComplexContentExtension2;
				}
				else
				{
					XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction = xmlSchemaComplexContent.Content as XmlSchemaComplexContentRestriction;
					XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction2 = (XmlSchemaComplexContentRestriction)xmlSchemaComplexContentRestriction.Clone();
					xmlSchemaComplexContentRestriction2.BaseTypeName = xmlSchemaComplexContentRestriction.BaseTypeName.Clone();
					xmlSchemaComplexContentRestriction2.SetAttributes(CloneAttributes(xmlSchemaComplexContentRestriction.Attributes));
					if (HasParticleRef(xmlSchemaComplexContentRestriction2.Particle, parentSchema))
					{
						xmlSchemaComplexContentRestriction2.Particle = CloneParticle(xmlSchemaComplexContentRestriction2.Particle, parentSchema);
					}
					xmlSchemaComplexContent2.Content = xmlSchemaComplexContentRestriction2;
				}
				xmlSchemaComplexType.ContentModel = xmlSchemaComplexContent2;
			}
		}
		else
		{
			if (HasParticleRef(xmlSchemaComplexType.Particle, parentSchema))
			{
				xmlSchemaComplexType.Particle = CloneParticle(xmlSchemaComplexType.Particle, parentSchema);
			}
			xmlSchemaComplexType.SetAttributes(CloneAttributes(xmlSchemaComplexType.Attributes));
		}
		xmlSchemaComplexType.ClearCompiledState();
		return xmlSchemaComplexType;
	}

	private void ClearCompiledState()
	{
		_attributeUses = null;
		_localElements = null;
		_attributeWildcard = null;
		_contentTypeParticle = XmlSchemaParticle.Empty;
		_blockResolved = XmlSchemaDerivationMethod.None;
	}

	internal static XmlSchemaObjectCollection CloneAttributes(XmlSchemaObjectCollection attributes)
	{
		if (HasAttributeQNameRef(attributes))
		{
			XmlSchemaObjectCollection xmlSchemaObjectCollection = attributes.Clone();
			for (int i = 0; i < attributes.Count; i++)
			{
				XmlSchemaObject xmlSchemaObject = attributes[i];
				if (xmlSchemaObject is XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef)
				{
					XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef2 = (XmlSchemaAttributeGroupRef)xmlSchemaAttributeGroupRef.Clone();
					xmlSchemaAttributeGroupRef2.RefName = xmlSchemaAttributeGroupRef.RefName.Clone();
					xmlSchemaObjectCollection[i] = xmlSchemaAttributeGroupRef2;
					continue;
				}
				XmlSchemaAttribute xmlSchemaAttribute = xmlSchemaObject as XmlSchemaAttribute;
				if (!xmlSchemaAttribute.RefName.IsEmpty || !xmlSchemaAttribute.SchemaTypeName.IsEmpty)
				{
					xmlSchemaObjectCollection[i] = xmlSchemaAttribute.Clone();
				}
			}
			return xmlSchemaObjectCollection;
		}
		return attributes;
	}

	private static XmlSchemaObjectCollection CloneGroupBaseParticles(XmlSchemaObjectCollection groupBaseParticles, XmlSchema parentSchema)
	{
		XmlSchemaObjectCollection xmlSchemaObjectCollection = groupBaseParticles.Clone();
		for (int i = 0; i < groupBaseParticles.Count; i++)
		{
			XmlSchemaParticle particle = (XmlSchemaParticle)groupBaseParticles[i];
			xmlSchemaObjectCollection[i] = CloneParticle(particle, parentSchema);
		}
		return xmlSchemaObjectCollection;
	}

	[return: NotNullIfNotNull("particle")]
	internal static XmlSchemaParticle CloneParticle(XmlSchemaParticle particle, XmlSchema parentSchema)
	{
		if (particle is XmlSchemaGroupBase xmlSchemaGroupBase)
		{
			XmlSchemaGroupBase xmlSchemaGroupBase2 = xmlSchemaGroupBase;
			XmlSchemaObjectCollection items = CloneGroupBaseParticles(xmlSchemaGroupBase.Items, parentSchema);
			xmlSchemaGroupBase2 = (XmlSchemaGroupBase)xmlSchemaGroupBase.Clone();
			xmlSchemaGroupBase2.SetItems(items);
			return xmlSchemaGroupBase2;
		}
		if (particle is XmlSchemaGroupRef)
		{
			XmlSchemaGroupRef xmlSchemaGroupRef = (XmlSchemaGroupRef)particle.Clone();
			xmlSchemaGroupRef.RefName = xmlSchemaGroupRef.RefName.Clone();
			return xmlSchemaGroupRef;
		}
		if (particle is XmlSchemaElement xmlSchemaElement && (!xmlSchemaElement.RefName.IsEmpty || !xmlSchemaElement.SchemaTypeName.IsEmpty || GetResolvedElementForm(parentSchema, xmlSchemaElement) == XmlSchemaForm.Qualified))
		{
			return (XmlSchemaElement)xmlSchemaElement.Clone(parentSchema);
		}
		return particle;
	}

	private static XmlSchemaForm GetResolvedElementForm(XmlSchema parentSchema, XmlSchemaElement element)
	{
		if (element.Form == XmlSchemaForm.None && parentSchema != null)
		{
			return parentSchema.ElementFormDefault;
		}
		return element.Form;
	}

	internal static bool HasParticleRef(XmlSchemaParticle particle, XmlSchema parentSchema)
	{
		if (particle is XmlSchemaGroupBase xmlSchemaGroupBase)
		{
			bool flag = false;
			int num = 0;
			while (num < xmlSchemaGroupBase.Items.Count && !flag)
			{
				XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)xmlSchemaGroupBase.Items[num++];
				flag = xmlSchemaParticle is XmlSchemaGroupRef || (xmlSchemaParticle is XmlSchemaElement xmlSchemaElement && (!xmlSchemaElement.RefName.IsEmpty || !xmlSchemaElement.SchemaTypeName.IsEmpty || GetResolvedElementForm(parentSchema, xmlSchemaElement) == XmlSchemaForm.Qualified)) || HasParticleRef(xmlSchemaParticle, parentSchema);
			}
			return flag;
		}
		if (particle is XmlSchemaGroupRef)
		{
			return true;
		}
		return false;
	}

	internal static bool HasAttributeQNameRef(XmlSchemaObjectCollection attributes)
	{
		for (int i = 0; i < attributes.Count; i++)
		{
			if (attributes[i] is XmlSchemaAttributeGroupRef)
			{
				return true;
			}
			XmlSchemaAttribute xmlSchemaAttribute = attributes[i] as XmlSchemaAttribute;
			if (!xmlSchemaAttribute.RefName.IsEmpty || !xmlSchemaAttribute.SchemaTypeName.IsEmpty)
			{
				return true;
			}
		}
		return false;
	}
}
