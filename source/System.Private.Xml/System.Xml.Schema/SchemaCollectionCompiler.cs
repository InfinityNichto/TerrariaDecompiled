using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Schema;

internal sealed class SchemaCollectionCompiler : BaseProcessor
{
	private bool _compileContentModel;

	private readonly XmlSchemaObjectTable _examplars = new XmlSchemaObjectTable();

	private readonly Stack<XmlSchemaComplexType> _complexTypeStack = new Stack<XmlSchemaComplexType>();

	private XmlSchema _schema;

	public SchemaCollectionCompiler(XmlNameTable nameTable, ValidationEventHandler eventHandler)
		: base(nameTable, null, eventHandler)
	{
	}

	public bool Execute(XmlSchema schema, SchemaInfo schemaInfo, bool compileContentModel)
	{
		_compileContentModel = compileContentModel;
		_schema = schema;
		Prepare();
		Cleanup();
		Compile();
		if (!base.HasErrors)
		{
			Output(schemaInfo);
		}
		return !base.HasErrors;
	}

	private void Prepare()
	{
		foreach (XmlSchemaElement value in _schema.Elements.Values)
		{
			if (!value.SubstitutionGroup.IsEmpty)
			{
				XmlSchemaSubstitutionGroup xmlSchemaSubstitutionGroup = (XmlSchemaSubstitutionGroup)_examplars[value.SubstitutionGroup];
				if (xmlSchemaSubstitutionGroup == null)
				{
					xmlSchemaSubstitutionGroup = new XmlSchemaSubstitutionGroupV1Compat();
					xmlSchemaSubstitutionGroup.Examplar = value.SubstitutionGroup;
					_examplars.Add(value.SubstitutionGroup, xmlSchemaSubstitutionGroup);
				}
				ArrayList members = xmlSchemaSubstitutionGroup.Members;
				members.Add(value);
			}
		}
	}

	private void Cleanup()
	{
		foreach (XmlSchemaGroup value in _schema.Groups.Values)
		{
			CleanupGroup(value);
		}
		foreach (XmlSchemaAttributeGroup value2 in _schema.AttributeGroups.Values)
		{
			CleanupAttributeGroup(value2);
		}
		foreach (XmlSchemaType value3 in _schema.SchemaTypes.Values)
		{
			if (value3 is XmlSchemaComplexType)
			{
				CleanupComplexType((XmlSchemaComplexType)value3);
			}
			else
			{
				CleanupSimpleType((XmlSchemaSimpleType)value3);
			}
		}
		foreach (XmlSchemaElement value4 in _schema.Elements.Values)
		{
			CleanupElement(value4);
		}
		foreach (XmlSchemaAttribute value5 in _schema.Attributes.Values)
		{
			CleanupAttribute(value5);
		}
	}

	internal static void Cleanup(XmlSchema schema)
	{
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			if (xmlSchemaExternal.Schema != null)
			{
				Cleanup(xmlSchemaExternal.Schema);
			}
			if (!(xmlSchemaExternal is XmlSchemaRedefine xmlSchemaRedefine))
			{
				continue;
			}
			xmlSchemaRedefine.AttributeGroups.Clear();
			xmlSchemaRedefine.Groups.Clear();
			xmlSchemaRedefine.SchemaTypes.Clear();
			for (int j = 0; j < xmlSchemaRedefine.Items.Count; j++)
			{
				object obj = xmlSchemaRedefine.Items[j];
				if (obj is XmlSchemaAttribute attribute)
				{
					CleanupAttribute(attribute);
				}
				else if (obj is XmlSchemaAttributeGroup attributeGroup)
				{
					CleanupAttributeGroup(attributeGroup);
				}
				else if (obj is XmlSchemaComplexType complexType)
				{
					CleanupComplexType(complexType);
				}
				else if (obj is XmlSchemaSimpleType simpleType)
				{
					CleanupSimpleType(simpleType);
				}
				else if (obj is XmlSchemaElement element)
				{
					CleanupElement(element);
				}
				else if (obj is XmlSchemaGroup group)
				{
					CleanupGroup(group);
				}
			}
		}
		for (int k = 0; k < schema.Items.Count; k++)
		{
			object obj2 = schema.Items[k];
			if (obj2 is XmlSchemaAttribute attribute2)
			{
				CleanupAttribute(attribute2);
			}
			else if (schema.Items[k] is XmlSchemaAttributeGroup attributeGroup2)
			{
				CleanupAttributeGroup(attributeGroup2);
			}
			else if (schema.Items[k] is XmlSchemaComplexType complexType2)
			{
				CleanupComplexType(complexType2);
			}
			else if (schema.Items[k] is XmlSchemaSimpleType simpleType2)
			{
				CleanupSimpleType(simpleType2);
			}
			else if (schema.Items[k] is XmlSchemaElement element2)
			{
				CleanupElement(element2);
			}
			else if (schema.Items[k] is XmlSchemaGroup group2)
			{
				CleanupGroup(group2);
			}
		}
		schema.Attributes.Clear();
		schema.AttributeGroups.Clear();
		schema.SchemaTypes.Clear();
		schema.Elements.Clear();
		schema.Groups.Clear();
		schema.Notations.Clear();
		schema.Ids.Clear();
		schema.IdentityConstraints.Clear();
	}

	private void Compile()
	{
		_schema.SchemaTypes.Insert(DatatypeImplementation.QnAnyType, XmlSchemaComplexType.AnyType);
		foreach (XmlSchemaSubstitutionGroupV1Compat value in _examplars.Values)
		{
			CompileSubstitutionGroup(value);
		}
		foreach (XmlSchemaGroup value2 in _schema.Groups.Values)
		{
			CompileGroup(value2);
		}
		foreach (XmlSchemaAttributeGroup value3 in _schema.AttributeGroups.Values)
		{
			CompileAttributeGroup(value3);
		}
		foreach (XmlSchemaType value4 in _schema.SchemaTypes.Values)
		{
			if (value4 is XmlSchemaComplexType)
			{
				CompileComplexType((XmlSchemaComplexType)value4);
			}
			else
			{
				CompileSimpleType((XmlSchemaSimpleType)value4);
			}
		}
		foreach (XmlSchemaElement value5 in _schema.Elements.Values)
		{
			if (value5.ElementDecl == null)
			{
				CompileElement(value5);
			}
		}
		foreach (XmlSchemaAttribute value6 in _schema.Attributes.Values)
		{
			if (value6.AttDef == null)
			{
				CompileAttribute(value6);
			}
		}
		foreach (XmlSchemaIdentityConstraint value7 in _schema.IdentityConstraints.Values)
		{
			if (value7.CompiledConstraint == null)
			{
				CompileIdentityConstraint(value7);
			}
		}
		while (_complexTypeStack.Count > 0)
		{
			XmlSchemaComplexType complexType = _complexTypeStack.Pop();
			CompileCompexTypeElements(complexType);
		}
		foreach (XmlSchemaType value8 in _schema.SchemaTypes.Values)
		{
			if (value8 is XmlSchemaComplexType)
			{
				CheckParticleDerivation((XmlSchemaComplexType)value8);
			}
		}
		foreach (XmlSchemaElement value9 in _schema.Elements.Values)
		{
			if (value9.ElementSchemaType is XmlSchemaComplexType && value9.SchemaTypeName == XmlQualifiedName.Empty)
			{
				CheckParticleDerivation((XmlSchemaComplexType)value9.ElementSchemaType);
			}
		}
		foreach (XmlSchemaSubstitutionGroup value10 in _examplars.Values)
		{
			CheckSubstitutionGroup(value10);
		}
		_schema.SchemaTypes.Remove(DatatypeImplementation.QnAnyType);
	}

	private void Output(SchemaInfo schemaInfo)
	{
		foreach (XmlSchemaElement value in _schema.Elements.Values)
		{
			schemaInfo.TargetNamespaces[value.QualifiedName.Namespace] = true;
			schemaInfo.ElementDecls.Add(value.QualifiedName, value.ElementDecl);
		}
		foreach (XmlSchemaAttribute value2 in _schema.Attributes.Values)
		{
			schemaInfo.TargetNamespaces[value2.QualifiedName.Namespace] = true;
			schemaInfo.AttributeDecls.Add(value2.QualifiedName, value2.AttDef);
		}
		foreach (XmlSchemaType value3 in _schema.SchemaTypes.Values)
		{
			schemaInfo.TargetNamespaces[value3.QualifiedName.Namespace] = true;
			if (!(value3 is XmlSchemaComplexType xmlSchemaComplexType) || (!xmlSchemaComplexType.IsAbstract && value3 != XmlSchemaComplexType.AnyType))
			{
				schemaInfo.ElementDeclsByType.Add(value3.QualifiedName, value3.ElementDecl);
			}
		}
		foreach (XmlSchemaNotation value4 in _schema.Notations.Values)
		{
			schemaInfo.TargetNamespaces[value4.QualifiedName.Namespace] = true;
			SchemaNotation schemaNotation = new SchemaNotation(value4.QualifiedName);
			schemaNotation.SystemLiteral = value4.System;
			schemaNotation.Pubid = value4.Public;
			if (!schemaInfo.Notations.ContainsKey(schemaNotation.Name.Name))
			{
				schemaInfo.Notations.Add(schemaNotation.Name.Name, schemaNotation);
			}
		}
	}

	private static void CleanupAttribute(XmlSchemaAttribute attribute)
	{
		if (attribute.SchemaType != null)
		{
			CleanupSimpleType(attribute.SchemaType);
		}
		attribute.AttDef = null;
	}

	private static void CleanupAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
	{
		CleanupAttributes(attributeGroup.Attributes);
		attributeGroup.AttributeUses.Clear();
		attributeGroup.AttributeWildcard = null;
	}

	private static void CleanupComplexType(XmlSchemaComplexType complexType)
	{
		if (complexType.ContentModel != null)
		{
			if (complexType.ContentModel is XmlSchemaSimpleContent)
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;
				if (xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentExtension)
				{
					XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)xmlSchemaSimpleContent.Content;
					CleanupAttributes(xmlSchemaSimpleContentExtension.Attributes);
				}
				else
				{
					XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction = (XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContent.Content;
					CleanupAttributes(xmlSchemaSimpleContentRestriction.Attributes);
				}
			}
			else
			{
				XmlSchemaComplexContent xmlSchemaComplexContent = (XmlSchemaComplexContent)complexType.ContentModel;
				if (xmlSchemaComplexContent.Content is XmlSchemaComplexContentExtension)
				{
					XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = (XmlSchemaComplexContentExtension)xmlSchemaComplexContent.Content;
					CleanupParticle(xmlSchemaComplexContentExtension.Particle);
					CleanupAttributes(xmlSchemaComplexContentExtension.Attributes);
				}
				else
				{
					XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction = (XmlSchemaComplexContentRestriction)xmlSchemaComplexContent.Content;
					CleanupParticle(xmlSchemaComplexContentRestriction.Particle);
					CleanupAttributes(xmlSchemaComplexContentRestriction.Attributes);
				}
			}
		}
		else
		{
			CleanupParticle(complexType.Particle);
			CleanupAttributes(complexType.Attributes);
		}
		complexType.LocalElements.Clear();
		complexType.AttributeUses.Clear();
		complexType.SetAttributeWildcard(null);
		complexType.SetContentTypeParticle(XmlSchemaParticle.Empty);
		complexType.ElementDecl = null;
	}

	private static void CleanupSimpleType(XmlSchemaSimpleType simpleType)
	{
		simpleType.ElementDecl = null;
	}

	private static void CleanupElement(XmlSchemaElement element)
	{
		if (element.SchemaType != null)
		{
			if (element.SchemaType is XmlSchemaComplexType complexType)
			{
				CleanupComplexType(complexType);
			}
			else
			{
				CleanupSimpleType((XmlSchemaSimpleType)element.SchemaType);
			}
		}
		for (int i = 0; i < element.Constraints.Count; i++)
		{
			((XmlSchemaIdentityConstraint)element.Constraints[i]).CompiledConstraint = null;
		}
		element.ElementDecl = null;
	}

	private static void CleanupAttributes(XmlSchemaObjectCollection attributes)
	{
		for (int i = 0; i < attributes.Count; i++)
		{
			if (attributes[i] is XmlSchemaAttribute attribute)
			{
				CleanupAttribute(attribute);
			}
		}
	}

	private static void CleanupGroup(XmlSchemaGroup group)
	{
		CleanupParticle(group.Particle);
		group.CanonicalParticle = null;
	}

	private static void CleanupParticle(XmlSchemaParticle particle)
	{
		if (particle is XmlSchemaElement)
		{
			CleanupElement((XmlSchemaElement)particle);
		}
		else if (particle is XmlSchemaGroupBase)
		{
			XmlSchemaObjectCollection items = ((XmlSchemaGroupBase)particle).Items;
			for (int i = 0; i < items.Count; i++)
			{
				CleanupParticle((XmlSchemaParticle)items[i]);
			}
		}
	}

	private void CompileSubstitutionGroup(XmlSchemaSubstitutionGroupV1Compat substitutionGroup)
	{
		if (substitutionGroup.IsProcessing && substitutionGroup.Members.Count > 0)
		{
			SendValidationEvent(System.SR.Sch_SubstitutionCircularRef, (XmlSchemaElement)substitutionGroup.Members[0]);
			return;
		}
		XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)_schema.Elements[substitutionGroup.Examplar];
		if (substitutionGroup.Members.Contains(xmlSchemaElement))
		{
			return;
		}
		substitutionGroup.IsProcessing = true;
		if (xmlSchemaElement != null)
		{
			if (xmlSchemaElement.FinalResolved == XmlSchemaDerivationMethod.All)
			{
				SendValidationEvent(System.SR.Sch_InvalidExamplar, xmlSchemaElement.Name, xmlSchemaElement);
			}
			for (int i = 0; i < substitutionGroup.Members.Count; i++)
			{
				XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)substitutionGroup.Members[i];
				XmlSchemaSubstitutionGroupV1Compat xmlSchemaSubstitutionGroupV1Compat = (XmlSchemaSubstitutionGroupV1Compat)_examplars[xmlSchemaElement2.QualifiedName];
				if (xmlSchemaSubstitutionGroupV1Compat != null)
				{
					CompileSubstitutionGroup(xmlSchemaSubstitutionGroupV1Compat);
					for (int j = 0; j < xmlSchemaSubstitutionGroupV1Compat.Choice.Items.Count; j++)
					{
						substitutionGroup.Choice.Items.Add(xmlSchemaSubstitutionGroupV1Compat.Choice.Items[j]);
					}
				}
				else
				{
					substitutionGroup.Choice.Items.Add(xmlSchemaElement2);
				}
			}
			substitutionGroup.Choice.Items.Add(xmlSchemaElement);
			substitutionGroup.Members.Add(xmlSchemaElement);
		}
		else if (substitutionGroup.Members.Count > 0)
		{
			SendValidationEvent(System.SR.Sch_NoExamplar, (XmlSchemaElement)substitutionGroup.Members[0]);
		}
		substitutionGroup.IsProcessing = false;
	}

	private void CheckSubstitutionGroup(XmlSchemaSubstitutionGroup substitutionGroup)
	{
		XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)_schema.Elements[substitutionGroup.Examplar];
		if (xmlSchemaElement == null)
		{
			return;
		}
		for (int i = 0; i < substitutionGroup.Members.Count; i++)
		{
			XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)substitutionGroup.Members[i];
			if (xmlSchemaElement2 != xmlSchemaElement && !XmlSchemaType.IsDerivedFrom(xmlSchemaElement2.ElementSchemaType, xmlSchemaElement.ElementSchemaType, xmlSchemaElement.FinalResolved))
			{
				SendValidationEvent(System.SR.Sch_InvalidSubstitutionMember, xmlSchemaElement2.QualifiedName.ToString(), xmlSchemaElement.QualifiedName.ToString(), xmlSchemaElement2);
			}
		}
	}

	private void CompileGroup(XmlSchemaGroup group)
	{
		if (group.IsProcessing)
		{
			SendValidationEvent(System.SR.Sch_GroupCircularRef, group);
			group.CanonicalParticle = XmlSchemaParticle.Empty;
			return;
		}
		group.IsProcessing = true;
		if (group.CanonicalParticle == null)
		{
			group.CanonicalParticle = CannonicalizeParticle(group.Particle, root: true, substitution: true);
		}
		group.IsProcessing = false;
	}

	private void CompileSimpleType(XmlSchemaSimpleType simpleType)
	{
		if (simpleType.IsProcessing)
		{
			throw new XmlSchemaException(System.SR.Sch_TypeCircularRef, simpleType);
		}
		if (simpleType.ElementDecl != null)
		{
			return;
		}
		simpleType.IsProcessing = true;
		try
		{
			if (simpleType.Content is XmlSchemaSimpleTypeList)
			{
				XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = (XmlSchemaSimpleTypeList)simpleType.Content;
				simpleType.SetBaseSchemaType(DatatypeImplementation.AnySimpleType);
				XmlSchemaDatatype datatype;
				if (xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
				{
					CompileSimpleType(xmlSchemaSimpleTypeList.ItemType);
					xmlSchemaSimpleTypeList.BaseItemType = xmlSchemaSimpleTypeList.ItemType;
					datatype = xmlSchemaSimpleTypeList.ItemType.Datatype;
				}
				else
				{
					XmlSchemaSimpleType simpleType2 = GetSimpleType(xmlSchemaSimpleTypeList.ItemTypeName);
					if (simpleType2 == null)
					{
						throw new XmlSchemaException(System.SR.Sch_UndeclaredSimpleType, xmlSchemaSimpleTypeList.ItemTypeName.ToString(), simpleType);
					}
					if ((simpleType2.FinalResolved & XmlSchemaDerivationMethod.List) != 0)
					{
						SendValidationEvent(System.SR.Sch_BaseFinalList, simpleType);
					}
					xmlSchemaSimpleTypeList.BaseItemType = simpleType2;
					datatype = simpleType2.Datatype;
				}
				simpleType.SetDatatype(datatype.DeriveByList(simpleType));
				simpleType.SetDerivedBy(XmlSchemaDerivationMethod.List);
			}
			else if (simpleType.Content is XmlSchemaSimpleTypeRestriction)
			{
				XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
				XmlSchemaDatatype datatype2;
				if (xmlSchemaSimpleTypeRestriction.BaseTypeName.IsEmpty)
				{
					CompileSimpleType(xmlSchemaSimpleTypeRestriction.BaseType);
					simpleType.SetBaseSchemaType(xmlSchemaSimpleTypeRestriction.BaseType);
					datatype2 = xmlSchemaSimpleTypeRestriction.BaseType.Datatype;
				}
				else if (simpleType.Redefined != null && xmlSchemaSimpleTypeRestriction.BaseTypeName == simpleType.Redefined.QualifiedName)
				{
					CompileSimpleType((XmlSchemaSimpleType)simpleType.Redefined);
					simpleType.SetBaseSchemaType(simpleType.Redefined.BaseXmlSchemaType);
					datatype2 = simpleType.Redefined.Datatype;
				}
				else
				{
					if (xmlSchemaSimpleTypeRestriction.BaseTypeName.Equals(DatatypeImplementation.QnAnySimpleType))
					{
						throw new XmlSchemaException(System.SR.Sch_InvalidSimpleTypeRestriction, xmlSchemaSimpleTypeRestriction.BaseTypeName.ToString(), simpleType);
					}
					XmlSchemaSimpleType simpleType3 = GetSimpleType(xmlSchemaSimpleTypeRestriction.BaseTypeName);
					if (simpleType3 == null)
					{
						throw new XmlSchemaException(System.SR.Sch_UndeclaredSimpleType, xmlSchemaSimpleTypeRestriction.BaseTypeName.ToString(), simpleType);
					}
					if ((simpleType3.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
					{
						SendValidationEvent(System.SR.Sch_BaseFinalRestriction, simpleType);
					}
					simpleType.SetBaseSchemaType(simpleType3);
					datatype2 = simpleType3.Datatype;
				}
				simpleType.SetDatatype(datatype2.DeriveByRestriction(xmlSchemaSimpleTypeRestriction.Facets, base.NameTable, simpleType));
				simpleType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
			}
			else
			{
				XmlSchemaSimpleType[] types = CompileBaseMemberTypes(simpleType);
				simpleType.SetBaseSchemaType(DatatypeImplementation.AnySimpleType);
				simpleType.SetDatatype(XmlSchemaDatatype.DeriveByUnion(types, simpleType));
				simpleType.SetDerivedBy(XmlSchemaDerivationMethod.Union);
			}
		}
		catch (XmlSchemaException ex)
		{
			if (ex.SourceSchemaObject == null)
			{
				ex.SetSource(simpleType);
			}
			SendValidationEvent(ex);
			simpleType.SetDatatype(DatatypeImplementation.AnySimpleType.Datatype);
		}
		finally
		{
			SchemaElementDecl schemaElementDecl = new SchemaElementDecl();
			schemaElementDecl.ContentValidator = ContentValidator.TextOnly;
			schemaElementDecl.SchemaType = simpleType;
			schemaElementDecl.Datatype = simpleType.Datatype;
			simpleType.ElementDecl = schemaElementDecl;
			simpleType.IsProcessing = false;
		}
	}

	private XmlSchemaSimpleType[] CompileBaseMemberTypes(XmlSchemaSimpleType simpleType)
	{
		List<XmlSchemaSimpleType> list = new List<XmlSchemaSimpleType>();
		XmlSchemaSimpleTypeUnion xmlSchemaSimpleTypeUnion = (XmlSchemaSimpleTypeUnion)simpleType.Content;
		XmlQualifiedName[] memberTypes = xmlSchemaSimpleTypeUnion.MemberTypes;
		if (memberTypes != null)
		{
			for (int i = 0; i < memberTypes.Length; i++)
			{
				XmlSchemaSimpleType simpleType2 = GetSimpleType(memberTypes[i]);
				if (simpleType2 != null)
				{
					if (simpleType2.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
					{
						CheckUnionType(simpleType2, list, simpleType);
					}
					else
					{
						list.Add(simpleType2);
					}
					if ((simpleType2.FinalResolved & XmlSchemaDerivationMethod.Union) != 0)
					{
						SendValidationEvent(System.SR.Sch_BaseFinalUnion, simpleType);
					}
					continue;
				}
				throw new XmlSchemaException(System.SR.Sch_UndeclaredSimpleType, memberTypes[i].ToString(), simpleType);
			}
		}
		XmlSchemaObjectCollection baseTypes = xmlSchemaSimpleTypeUnion.BaseTypes;
		if (baseTypes != null)
		{
			for (int j = 0; j < baseTypes.Count; j++)
			{
				XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)baseTypes[j];
				CompileSimpleType(xmlSchemaSimpleType);
				if (xmlSchemaSimpleType.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
				{
					CheckUnionType(xmlSchemaSimpleType, list, simpleType);
				}
				else
				{
					list.Add(xmlSchemaSimpleType);
				}
			}
		}
		xmlSchemaSimpleTypeUnion.SetBaseMemberTypes(list.ToArray());
		return xmlSchemaSimpleTypeUnion.BaseMemberTypes;
	}

	private void CheckUnionType(XmlSchemaSimpleType unionMember, List<XmlSchemaSimpleType> memberTypeDefinitions, XmlSchemaSimpleType parentType)
	{
		XmlSchemaDatatype datatype = unionMember.Datatype;
		if (unionMember.DerivedBy == XmlSchemaDerivationMethod.Restriction && (datatype.HasLexicalFacets || datatype.HasValueFacets))
		{
			SendValidationEvent(System.SR.Sch_UnionFromUnion, parentType);
			return;
		}
		Datatype_union datatype_union = unionMember.Datatype as Datatype_union;
		memberTypeDefinitions.AddRange(datatype_union.BaseMemberTypes);
	}

	private void CompileComplexType(XmlSchemaComplexType complexType)
	{
		if (complexType.ElementDecl != null)
		{
			return;
		}
		if (complexType.IsProcessing)
		{
			SendValidationEvent(System.SR.Sch_TypeCircularRef, complexType);
			return;
		}
		complexType.IsProcessing = true;
		if (complexType.ContentModel != null)
		{
			if (complexType.ContentModel is XmlSchemaSimpleContent)
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;
				complexType.SetContentType(XmlSchemaContentType.TextOnly);
				if (xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentExtension)
				{
					CompileSimpleContentExtension(complexType, (XmlSchemaSimpleContentExtension)xmlSchemaSimpleContent.Content);
				}
				else
				{
					CompileSimpleContentRestriction(complexType, (XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContent.Content);
				}
			}
			else
			{
				XmlSchemaComplexContent xmlSchemaComplexContent = (XmlSchemaComplexContent)complexType.ContentModel;
				if (xmlSchemaComplexContent.Content is XmlSchemaComplexContentExtension)
				{
					CompileComplexContentExtension(complexType, xmlSchemaComplexContent, (XmlSchemaComplexContentExtension)xmlSchemaComplexContent.Content);
				}
				else
				{
					CompileComplexContentRestriction(complexType, xmlSchemaComplexContent, (XmlSchemaComplexContentRestriction)xmlSchemaComplexContent.Content);
				}
			}
		}
		else
		{
			complexType.SetBaseSchemaType(XmlSchemaComplexType.AnyType);
			CompileLocalAttributes(XmlSchemaComplexType.AnyType, complexType, complexType.Attributes, complexType.AnyAttribute, XmlSchemaDerivationMethod.Restriction);
			complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
			complexType.SetContentTypeParticle(CompileContentTypeParticle(complexType.Particle, substitution: true));
			complexType.SetContentType(GetSchemaContentType(complexType, null, complexType.ContentTypeParticle));
		}
		bool flag = false;
		foreach (XmlSchemaAttribute value in complexType.AttributeUses.Values)
		{
			if (value.Use == XmlSchemaUse.Prohibited)
			{
				continue;
			}
			XmlSchemaDatatype datatype = value.Datatype;
			if (datatype != null && datatype.TokenizedType == XmlTokenizedType.ID)
			{
				if (flag)
				{
					SendValidationEvent(System.SR.Sch_TwoIdAttrUses, complexType);
				}
				else
				{
					flag = true;
				}
			}
		}
		SchemaElementDecl schemaElementDecl = new SchemaElementDecl();
		schemaElementDecl.ContentValidator = CompileComplexContent(complexType);
		schemaElementDecl.SchemaType = complexType;
		schemaElementDecl.IsAbstract = complexType.IsAbstract;
		schemaElementDecl.Datatype = complexType.Datatype;
		schemaElementDecl.Block = complexType.BlockResolved;
		schemaElementDecl.AnyAttribute = complexType.AttributeWildcard;
		foreach (XmlSchemaAttribute value2 in complexType.AttributeUses.Values)
		{
			if (value2.Use == XmlSchemaUse.Prohibited)
			{
				if (!schemaElementDecl.ProhibitedAttributes.ContainsKey(value2.QualifiedName))
				{
					schemaElementDecl.ProhibitedAttributes.Add(value2.QualifiedName, value2.QualifiedName);
				}
			}
			else if (!schemaElementDecl.AttDefs.ContainsKey(value2.QualifiedName) && value2.AttDef != null && value2.AttDef.Name != XmlQualifiedName.Empty && value2.AttDef != SchemaAttDef.Empty)
			{
				schemaElementDecl.AddAttDef(value2.AttDef);
			}
		}
		complexType.ElementDecl = schemaElementDecl;
		complexType.IsProcessing = false;
	}

	private void CompileSimpleContentExtension(XmlSchemaComplexType complexType, XmlSchemaSimpleContentExtension simpleExtension)
	{
		XmlSchemaComplexType xmlSchemaComplexType = null;
		if (complexType.Redefined != null && simpleExtension.BaseTypeName == complexType.Redefined.QualifiedName)
		{
			xmlSchemaComplexType = (XmlSchemaComplexType)complexType.Redefined;
			CompileComplexType(xmlSchemaComplexType);
			complexType.SetBaseSchemaType(xmlSchemaComplexType);
			complexType.SetDatatype(xmlSchemaComplexType.Datatype);
		}
		else
		{
			XmlSchemaType anySchemaType = GetAnySchemaType(simpleExtension.BaseTypeName);
			if (anySchemaType == null)
			{
				SendValidationEvent(System.SR.Sch_UndeclaredType, simpleExtension.BaseTypeName.ToString(), complexType);
			}
			else
			{
				complexType.SetBaseSchemaType(anySchemaType);
				complexType.SetDatatype(anySchemaType.Datatype);
			}
			xmlSchemaComplexType = anySchemaType as XmlSchemaComplexType;
		}
		if (xmlSchemaComplexType != null)
		{
			if ((xmlSchemaComplexType.FinalResolved & XmlSchemaDerivationMethod.Extension) != 0)
			{
				SendValidationEvent(System.SR.Sch_BaseFinalExtension, complexType);
			}
			if (xmlSchemaComplexType.ContentType != 0)
			{
				SendValidationEvent(System.SR.Sch_NotSimpleContent, complexType);
			}
		}
		complexType.SetDerivedBy(XmlSchemaDerivationMethod.Extension);
		CompileLocalAttributes(xmlSchemaComplexType, complexType, simpleExtension.Attributes, simpleExtension.AnyAttribute, XmlSchemaDerivationMethod.Extension);
	}

	private void CompileSimpleContentRestriction(XmlSchemaComplexType complexType, XmlSchemaSimpleContentRestriction simpleRestriction)
	{
		XmlSchemaComplexType xmlSchemaComplexType = null;
		XmlSchemaDatatype xmlSchemaDatatype = null;
		if (complexType.Redefined != null && simpleRestriction.BaseTypeName == complexType.Redefined.QualifiedName)
		{
			xmlSchemaComplexType = (XmlSchemaComplexType)complexType.Redefined;
			CompileComplexType(xmlSchemaComplexType);
			xmlSchemaDatatype = xmlSchemaComplexType.Datatype;
		}
		else
		{
			xmlSchemaComplexType = GetComplexType(simpleRestriction.BaseTypeName);
			if (xmlSchemaComplexType == null)
			{
				SendValidationEvent(System.SR.Sch_UndefBaseRestriction, simpleRestriction.BaseTypeName.ToString(), simpleRestriction);
				return;
			}
			if (xmlSchemaComplexType.ContentType == XmlSchemaContentType.TextOnly)
			{
				if (simpleRestriction.BaseType == null)
				{
					xmlSchemaDatatype = xmlSchemaComplexType.Datatype;
				}
				else
				{
					CompileSimpleType(simpleRestriction.BaseType);
					if (!XmlSchemaType.IsDerivedFromDatatype(simpleRestriction.BaseType.Datatype, xmlSchemaComplexType.Datatype, XmlSchemaDerivationMethod.None))
					{
						SendValidationEvent(System.SR.Sch_DerivedNotFromBase, simpleRestriction);
					}
					xmlSchemaDatatype = simpleRestriction.BaseType.Datatype;
				}
			}
			else if (xmlSchemaComplexType.ContentType == XmlSchemaContentType.Mixed && xmlSchemaComplexType.ElementDecl.ContentValidator.IsEmptiable)
			{
				if (simpleRestriction.BaseType != null)
				{
					CompileSimpleType(simpleRestriction.BaseType);
					complexType.SetBaseSchemaType(simpleRestriction.BaseType);
					xmlSchemaDatatype = simpleRestriction.BaseType.Datatype;
				}
				else
				{
					SendValidationEvent(System.SR.Sch_NeedSimpleTypeChild, simpleRestriction);
				}
			}
			else
			{
				SendValidationEvent(System.SR.Sch_NotSimpleContent, complexType);
			}
		}
		if (xmlSchemaComplexType != null && xmlSchemaComplexType.ElementDecl != null && (xmlSchemaComplexType.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
		{
			SendValidationEvent(System.SR.Sch_BaseFinalRestriction, complexType);
		}
		if (xmlSchemaComplexType != null)
		{
			complexType.SetBaseSchemaType(xmlSchemaComplexType);
		}
		if (xmlSchemaDatatype != null)
		{
			try
			{
				complexType.SetDatatype(xmlSchemaDatatype.DeriveByRestriction(simpleRestriction.Facets, base.NameTable, complexType));
			}
			catch (XmlSchemaException ex)
			{
				if (ex.SourceSchemaObject == null)
				{
					ex.SetSource(complexType);
				}
				SendValidationEvent(ex);
				complexType.SetDatatype(DatatypeImplementation.AnySimpleType.Datatype);
			}
		}
		complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
		CompileLocalAttributes(xmlSchemaComplexType, complexType, simpleRestriction.Attributes, simpleRestriction.AnyAttribute, XmlSchemaDerivationMethod.Restriction);
	}

	private void CompileComplexContentExtension(XmlSchemaComplexType complexType, XmlSchemaComplexContent complexContent, XmlSchemaComplexContentExtension complexExtension)
	{
		XmlSchemaComplexType xmlSchemaComplexType = null;
		if (complexType.Redefined != null && complexExtension.BaseTypeName == complexType.Redefined.QualifiedName)
		{
			xmlSchemaComplexType = (XmlSchemaComplexType)complexType.Redefined;
			CompileComplexType(xmlSchemaComplexType);
		}
		else
		{
			xmlSchemaComplexType = GetComplexType(complexExtension.BaseTypeName);
			if (xmlSchemaComplexType == null)
			{
				SendValidationEvent(System.SR.Sch_UndefBaseExtension, complexExtension.BaseTypeName.ToString(), complexExtension);
				return;
			}
		}
		if (xmlSchemaComplexType != null && xmlSchemaComplexType.ElementDecl != null && xmlSchemaComplexType.ContentType == XmlSchemaContentType.TextOnly)
		{
			SendValidationEvent(System.SR.Sch_NotComplexContent, complexType);
			return;
		}
		complexType.SetBaseSchemaType(xmlSchemaComplexType);
		if ((xmlSchemaComplexType.FinalResolved & XmlSchemaDerivationMethod.Extension) != 0)
		{
			SendValidationEvent(System.SR.Sch_BaseFinalExtension, complexType);
		}
		CompileLocalAttributes(xmlSchemaComplexType, complexType, complexExtension.Attributes, complexExtension.AnyAttribute, XmlSchemaDerivationMethod.Extension);
		XmlSchemaParticle contentTypeParticle = xmlSchemaComplexType.ContentTypeParticle;
		XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle(complexExtension.Particle, root: true, substitution: true);
		if (contentTypeParticle != XmlSchemaParticle.Empty)
		{
			if (xmlSchemaParticle != XmlSchemaParticle.Empty)
			{
				XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
				xmlSchemaSequence.Items.Add(contentTypeParticle);
				xmlSchemaSequence.Items.Add(xmlSchemaParticle);
				complexType.SetContentTypeParticle(CompileContentTypeParticle(xmlSchemaSequence, substitution: false));
			}
			else
			{
				complexType.SetContentTypeParticle(contentTypeParticle);
			}
			XmlSchemaContentType xmlSchemaContentType = GetSchemaContentType(complexType, complexContent, xmlSchemaParticle);
			if (xmlSchemaContentType == XmlSchemaContentType.Empty)
			{
				xmlSchemaContentType = xmlSchemaComplexType.ContentType;
			}
			complexType.SetContentType(xmlSchemaContentType);
			if (complexType.ContentType != xmlSchemaComplexType.ContentType)
			{
				SendValidationEvent(System.SR.Sch_DifContentType, complexType);
			}
		}
		else
		{
			complexType.SetContentTypeParticle(xmlSchemaParticle);
			complexType.SetContentType(GetSchemaContentType(complexType, complexContent, complexType.ContentTypeParticle));
		}
		complexType.SetDerivedBy(XmlSchemaDerivationMethod.Extension);
	}

	private void CompileComplexContentRestriction(XmlSchemaComplexType complexType, XmlSchemaComplexContent complexContent, XmlSchemaComplexContentRestriction complexRestriction)
	{
		XmlSchemaComplexType xmlSchemaComplexType = null;
		if (complexType.Redefined != null && complexRestriction.BaseTypeName == complexType.Redefined.QualifiedName)
		{
			xmlSchemaComplexType = (XmlSchemaComplexType)complexType.Redefined;
			CompileComplexType(xmlSchemaComplexType);
		}
		else
		{
			xmlSchemaComplexType = GetComplexType(complexRestriction.BaseTypeName);
			if (xmlSchemaComplexType == null)
			{
				SendValidationEvent(System.SR.Sch_UndefBaseRestriction, complexRestriction.BaseTypeName.ToString(), complexRestriction);
				return;
			}
		}
		if (xmlSchemaComplexType != null && xmlSchemaComplexType.ElementDecl != null && xmlSchemaComplexType.ContentType == XmlSchemaContentType.TextOnly)
		{
			SendValidationEvent(System.SR.Sch_NotComplexContent, complexType);
			return;
		}
		complexType.SetBaseSchemaType(xmlSchemaComplexType);
		if ((xmlSchemaComplexType.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
		{
			SendValidationEvent(System.SR.Sch_BaseFinalRestriction, complexType);
		}
		CompileLocalAttributes(xmlSchemaComplexType, complexType, complexRestriction.Attributes, complexRestriction.AnyAttribute, XmlSchemaDerivationMethod.Restriction);
		complexType.SetContentTypeParticle(CompileContentTypeParticle(complexRestriction.Particle, substitution: true));
		complexType.SetContentType(GetSchemaContentType(complexType, complexContent, complexType.ContentTypeParticle));
		if (complexType.ContentType == XmlSchemaContentType.Empty)
		{
			_ = xmlSchemaComplexType.ElementDecl;
			if (xmlSchemaComplexType.ElementDecl != null && !xmlSchemaComplexType.ElementDecl.ContentValidator.IsEmptiable)
			{
				SendValidationEvent(System.SR.Sch_InvalidContentRestriction, complexType);
			}
		}
		complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
	}

	private void CheckParticleDerivation(XmlSchemaComplexType complexType)
	{
		if (complexType.BaseXmlSchemaType is XmlSchemaComplexType xmlSchemaComplexType && xmlSchemaComplexType != XmlSchemaComplexType.AnyType && complexType.DerivedBy == XmlSchemaDerivationMethod.Restriction && !IsValidRestriction(complexType.ContentTypeParticle, xmlSchemaComplexType.ContentTypeParticle))
		{
			SendValidationEvent(System.SR.Sch_InvalidParticleRestriction, complexType);
		}
	}

	private XmlSchemaParticle CompileContentTypeParticle(XmlSchemaParticle particle, bool substitution)
	{
		XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle(particle, root: true, substitution);
		if (xmlSchemaParticle is XmlSchemaChoice xmlSchemaChoice && xmlSchemaChoice.Items.Count == 0)
		{
			if (xmlSchemaChoice.MinOccurs != 0m)
			{
				SendValidationEvent(System.SR.Sch_EmptyChoice, xmlSchemaChoice, XmlSeverityType.Warning);
			}
			return XmlSchemaParticle.Empty;
		}
		return xmlSchemaParticle;
	}

	private XmlSchemaParticle CannonicalizeParticle(XmlSchemaParticle particle, bool root, bool substitution)
	{
		if (particle == null || particle.IsEmpty)
		{
			return XmlSchemaParticle.Empty;
		}
		if (particle is XmlSchemaElement)
		{
			return CannonicalizeElement((XmlSchemaElement)particle, substitution);
		}
		if (particle is XmlSchemaGroupRef)
		{
			return CannonicalizeGroupRef((XmlSchemaGroupRef)particle, root, substitution);
		}
		if (particle is XmlSchemaAll)
		{
			return CannonicalizeAll((XmlSchemaAll)particle, root, substitution);
		}
		if (particle is XmlSchemaChoice)
		{
			return CannonicalizeChoice((XmlSchemaChoice)particle, root, substitution);
		}
		if (particle is XmlSchemaSequence)
		{
			return CannonicalizeSequence((XmlSchemaSequence)particle, root, substitution);
		}
		return particle;
	}

	private XmlSchemaParticle CannonicalizeElement(XmlSchemaElement element, bool substitution)
	{
		if (!element.RefName.IsEmpty && substitution && (element.BlockResolved & XmlSchemaDerivationMethod.Substitution) == 0)
		{
			XmlSchemaSubstitutionGroupV1Compat xmlSchemaSubstitutionGroupV1Compat = (XmlSchemaSubstitutionGroupV1Compat)_examplars[element.QualifiedName];
			if (xmlSchemaSubstitutionGroupV1Compat == null)
			{
				return element;
			}
			XmlSchemaChoice xmlSchemaChoice = (XmlSchemaChoice)xmlSchemaSubstitutionGroupV1Compat.Choice.Clone();
			xmlSchemaChoice.MinOccurs = element.MinOccurs;
			xmlSchemaChoice.MaxOccurs = element.MaxOccurs;
			return xmlSchemaChoice;
		}
		return element;
	}

	private XmlSchemaParticle CannonicalizeGroupRef(XmlSchemaGroupRef groupRef, bool root, bool substitution)
	{
		XmlSchemaGroup xmlSchemaGroup = ((groupRef.Redefined == null) ? ((XmlSchemaGroup)_schema.Groups[groupRef.RefName]) : groupRef.Redefined);
		if (xmlSchemaGroup == null)
		{
			SendValidationEvent(System.SR.Sch_UndefGroupRef, groupRef.RefName.ToString(), groupRef);
			return XmlSchemaParticle.Empty;
		}
		if (xmlSchemaGroup.CanonicalParticle == null)
		{
			CompileGroup(xmlSchemaGroup);
		}
		if (xmlSchemaGroup.CanonicalParticle == XmlSchemaParticle.Empty)
		{
			return XmlSchemaParticle.Empty;
		}
		XmlSchemaGroupBase xmlSchemaGroupBase = (XmlSchemaGroupBase)xmlSchemaGroup.CanonicalParticle;
		if (xmlSchemaGroupBase is XmlSchemaAll)
		{
			if (!root)
			{
				SendValidationEvent(System.SR.Sch_AllRefNotRoot, "", groupRef);
				return XmlSchemaParticle.Empty;
			}
			if (groupRef.MinOccurs != 1m || groupRef.MaxOccurs != 1m)
			{
				SendValidationEvent(System.SR.Sch_AllRefMinMax, groupRef);
				return XmlSchemaParticle.Empty;
			}
		}
		else if (xmlSchemaGroupBase is XmlSchemaChoice && xmlSchemaGroupBase.Items.Count == 0)
		{
			if (groupRef.MinOccurs != 0m)
			{
				SendValidationEvent(System.SR.Sch_EmptyChoice, groupRef, XmlSeverityType.Warning);
			}
			return XmlSchemaParticle.Empty;
		}
		XmlSchemaGroupBase xmlSchemaGroupBase2 = ((xmlSchemaGroupBase is XmlSchemaSequence) ? new XmlSchemaSequence() : ((xmlSchemaGroupBase is XmlSchemaChoice) ? ((XmlSchemaGroupBase)new XmlSchemaChoice()) : ((XmlSchemaGroupBase)new XmlSchemaAll())));
		xmlSchemaGroupBase2.MinOccurs = groupRef.MinOccurs;
		xmlSchemaGroupBase2.MaxOccurs = groupRef.MaxOccurs;
		for (int i = 0; i < xmlSchemaGroupBase.Items.Count; i++)
		{
			xmlSchemaGroupBase2.Items.Add((XmlSchemaParticle)xmlSchemaGroupBase.Items[i]);
		}
		groupRef.SetParticle(xmlSchemaGroupBase2);
		return xmlSchemaGroupBase2;
	}

	private XmlSchemaParticle CannonicalizeAll(XmlSchemaAll all, bool root, bool substitution)
	{
		if (all.Items.Count > 0)
		{
			XmlSchemaAll xmlSchemaAll = new XmlSchemaAll();
			xmlSchemaAll.MinOccurs = all.MinOccurs;
			xmlSchemaAll.MaxOccurs = all.MaxOccurs;
			xmlSchemaAll.SourceUri = all.SourceUri;
			xmlSchemaAll.LineNumber = all.LineNumber;
			xmlSchemaAll.LinePosition = all.LinePosition;
			for (int i = 0; i < all.Items.Count; i++)
			{
				XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle((XmlSchemaElement)all.Items[i], root: false, substitution);
				if (xmlSchemaParticle != XmlSchemaParticle.Empty)
				{
					xmlSchemaAll.Items.Add(xmlSchemaParticle);
				}
			}
			all = xmlSchemaAll;
		}
		if (all.Items.Count == 0)
		{
			return XmlSchemaParticle.Empty;
		}
		if (root && all.Items.Count == 1)
		{
			XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
			xmlSchemaSequence.MinOccurs = all.MinOccurs;
			xmlSchemaSequence.MaxOccurs = all.MaxOccurs;
			xmlSchemaSequence.Items.Add((XmlSchemaParticle)all.Items[0]);
			return xmlSchemaSequence;
		}
		if (!root && all.Items.Count == 1 && all.MinOccurs == 1m && all.MaxOccurs == 1m)
		{
			return (XmlSchemaParticle)all.Items[0];
		}
		if (!root)
		{
			SendValidationEvent(System.SR.Sch_NotAllAlone, all);
			return XmlSchemaParticle.Empty;
		}
		return all;
	}

	private XmlSchemaParticle CannonicalizeChoice(XmlSchemaChoice choice, bool root, bool substitution)
	{
		XmlSchemaChoice source = choice;
		if (choice.Items.Count > 0)
		{
			XmlSchemaChoice xmlSchemaChoice = new XmlSchemaChoice();
			xmlSchemaChoice.MinOccurs = choice.MinOccurs;
			xmlSchemaChoice.MaxOccurs = choice.MaxOccurs;
			for (int i = 0; i < choice.Items.Count; i++)
			{
				XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle((XmlSchemaParticle)choice.Items[i], root: false, substitution);
				if (xmlSchemaParticle == XmlSchemaParticle.Empty)
				{
					continue;
				}
				if (xmlSchemaParticle.MinOccurs == 1m && xmlSchemaParticle.MaxOccurs == 1m && xmlSchemaParticle is XmlSchemaChoice)
				{
					XmlSchemaChoice xmlSchemaChoice2 = (XmlSchemaChoice)xmlSchemaParticle;
					for (int j = 0; j < xmlSchemaChoice2.Items.Count; j++)
					{
						xmlSchemaChoice.Items.Add(xmlSchemaChoice2.Items[j]);
					}
				}
				else
				{
					xmlSchemaChoice.Items.Add(xmlSchemaParticle);
				}
			}
			choice = xmlSchemaChoice;
		}
		if (!root && choice.Items.Count == 0)
		{
			if (choice.MinOccurs != 0m)
			{
				SendValidationEvent(System.SR.Sch_EmptyChoice, source, XmlSeverityType.Warning);
			}
			return XmlSchemaParticle.Empty;
		}
		if (!root && choice.Items.Count == 1 && choice.MinOccurs == 1m && choice.MaxOccurs == 1m)
		{
			return (XmlSchemaParticle)choice.Items[0];
		}
		return choice;
	}

	private XmlSchemaParticle CannonicalizeSequence(XmlSchemaSequence sequence, bool root, bool substitution)
	{
		if (sequence.Items.Count > 0)
		{
			XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
			xmlSchemaSequence.MinOccurs = sequence.MinOccurs;
			xmlSchemaSequence.MaxOccurs = sequence.MaxOccurs;
			for (int i = 0; i < sequence.Items.Count; i++)
			{
				XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle((XmlSchemaParticle)sequence.Items[i], root: false, substitution);
				if (xmlSchemaParticle == XmlSchemaParticle.Empty)
				{
					continue;
				}
				if (xmlSchemaParticle.MinOccurs == 1m && xmlSchemaParticle.MaxOccurs == 1m && xmlSchemaParticle is XmlSchemaSequence)
				{
					XmlSchemaSequence xmlSchemaSequence2 = (XmlSchemaSequence)xmlSchemaParticle;
					for (int j = 0; j < xmlSchemaSequence2.Items.Count; j++)
					{
						xmlSchemaSequence.Items.Add(xmlSchemaSequence2.Items[j]);
					}
				}
				else
				{
					xmlSchemaSequence.Items.Add(xmlSchemaParticle);
				}
			}
			sequence = xmlSchemaSequence;
		}
		if (sequence.Items.Count == 0)
		{
			return XmlSchemaParticle.Empty;
		}
		if (!root && sequence.Items.Count == 1 && sequence.MinOccurs == 1m && sequence.MaxOccurs == 1m)
		{
			return (XmlSchemaParticle)sequence.Items[0];
		}
		return sequence;
	}

	private bool IsValidRestriction(XmlSchemaParticle derivedParticle, XmlSchemaParticle baseParticle)
	{
		if (derivedParticle == baseParticle)
		{
			return true;
		}
		if (derivedParticle == null || derivedParticle == XmlSchemaParticle.Empty)
		{
			return IsParticleEmptiable(baseParticle);
		}
		if (baseParticle == null || baseParticle == XmlSchemaParticle.Empty)
		{
			return false;
		}
		if (baseParticle is XmlSchemaElement)
		{
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromElement((XmlSchemaElement)derivedParticle, (XmlSchemaElement)baseParticle);
			}
			return false;
		}
		if (baseParticle is XmlSchemaAny)
		{
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromAny((XmlSchemaElement)derivedParticle, (XmlSchemaAny)baseParticle);
			}
			if (derivedParticle is XmlSchemaAny)
			{
				return IsAnyFromAny((XmlSchemaAny)derivedParticle, (XmlSchemaAny)baseParticle);
			}
			return IsGroupBaseFromAny((XmlSchemaGroupBase)derivedParticle, (XmlSchemaAny)baseParticle);
		}
		if (baseParticle is XmlSchemaAll)
		{
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: true);
			}
			if (derivedParticle is XmlSchemaAll)
			{
				return IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: true);
			}
			if (derivedParticle is XmlSchemaSequence)
			{
				return IsSequenceFromAll((XmlSchemaSequence)derivedParticle, (XmlSchemaAll)baseParticle);
			}
		}
		else if (baseParticle is XmlSchemaChoice)
		{
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: false);
			}
			if (derivedParticle is XmlSchemaChoice)
			{
				return IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: false);
			}
			if (derivedParticle is XmlSchemaSequence)
			{
				return IsSequenceFromChoice((XmlSchemaSequence)derivedParticle, (XmlSchemaChoice)baseParticle);
			}
		}
		else if (baseParticle is XmlSchemaSequence)
		{
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: true);
			}
			if (derivedParticle is XmlSchemaSequence)
			{
				return IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: true);
			}
		}
		return false;
	}

	private bool IsElementFromElement(XmlSchemaElement derivedElement, XmlSchemaElement baseElement)
	{
		if (derivedElement.QualifiedName == baseElement.QualifiedName && derivedElement.IsNillable == baseElement.IsNillable && IsValidOccurrenceRangeRestriction(derivedElement, baseElement) && (baseElement.FixedValue == null || baseElement.FixedValue == derivedElement.FixedValue) && (derivedElement.BlockResolved | baseElement.BlockResolved) == derivedElement.BlockResolved && derivedElement.ElementSchemaType != null && baseElement.ElementSchemaType != null)
		{
			return XmlSchemaType.IsDerivedFrom(derivedElement.ElementSchemaType, baseElement.ElementSchemaType, ~XmlSchemaDerivationMethod.Restriction);
		}
		return false;
	}

	private bool IsElementFromAny(XmlSchemaElement derivedElement, XmlSchemaAny baseAny)
	{
		if (baseAny.Allows(derivedElement.QualifiedName))
		{
			return IsValidOccurrenceRangeRestriction(derivedElement, baseAny);
		}
		return false;
	}

	private bool IsAnyFromAny(XmlSchemaAny derivedAny, XmlSchemaAny baseAny)
	{
		if (IsValidOccurrenceRangeRestriction(derivedAny, baseAny))
		{
			return NamespaceList.IsSubset(derivedAny.NamespaceList, baseAny.NamespaceList);
		}
		return false;
	}

	private bool IsGroupBaseFromAny(XmlSchemaGroupBase derivedGroupBase, XmlSchemaAny baseAny)
	{
		CalculateEffectiveTotalRange(derivedGroupBase, out var minOccurs, out var maxOccurs);
		if (!IsValidOccurrenceRangeRestriction(minOccurs, maxOccurs, baseAny.MinOccurs, baseAny.MaxOccurs))
		{
			return false;
		}
		string minOccursString = baseAny.MinOccursString;
		baseAny.MinOccurs = 0m;
		for (int i = 0; i < derivedGroupBase.Items.Count; i++)
		{
			if (!IsValidRestriction((XmlSchemaParticle)derivedGroupBase.Items[i], baseAny))
			{
				baseAny.MinOccursString = minOccursString;
				return false;
			}
		}
		baseAny.MinOccursString = minOccursString;
		return true;
	}

	private bool IsElementFromGroupBase(XmlSchemaElement derivedElement, XmlSchemaGroupBase baseGroupBase, bool skipEmptableOnly)
	{
		bool flag = false;
		for (int i = 0; i < baseGroupBase.Items.Count; i++)
		{
			XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)baseGroupBase.Items[i];
			if (!flag)
			{
				string minOccursString = xmlSchemaParticle.MinOccursString;
				string maxOccursString = xmlSchemaParticle.MaxOccursString;
				xmlSchemaParticle.MinOccurs *= baseGroupBase.MinOccurs;
				if (xmlSchemaParticle.MaxOccurs != decimal.MaxValue)
				{
					if (baseGroupBase.MaxOccurs == decimal.MaxValue)
					{
						xmlSchemaParticle.MaxOccurs = decimal.MaxValue;
					}
					else
					{
						xmlSchemaParticle.MaxOccurs *= baseGroupBase.MaxOccurs;
					}
				}
				flag = IsValidRestriction(derivedElement, xmlSchemaParticle);
				xmlSchemaParticle.MinOccursString = minOccursString;
				xmlSchemaParticle.MaxOccursString = maxOccursString;
			}
			else if (skipEmptableOnly && !IsParticleEmptiable(xmlSchemaParticle))
			{
				return false;
			}
		}
		return flag;
	}

	private bool IsGroupBaseFromGroupBase(XmlSchemaGroupBase derivedGroupBase, XmlSchemaGroupBase baseGroupBase, bool skipEmptableOnly)
	{
		if (!IsValidOccurrenceRangeRestriction(derivedGroupBase, baseGroupBase) || derivedGroupBase.Items.Count > baseGroupBase.Items.Count)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < baseGroupBase.Items.Count; i++)
		{
			XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)baseGroupBase.Items[i];
			if (num < derivedGroupBase.Items.Count && IsValidRestriction((XmlSchemaParticle)derivedGroupBase.Items[num], xmlSchemaParticle))
			{
				num++;
			}
			else if (skipEmptableOnly && !IsParticleEmptiable(xmlSchemaParticle))
			{
				return false;
			}
		}
		if (num < derivedGroupBase.Items.Count)
		{
			return false;
		}
		return true;
	}

	private bool IsSequenceFromAll(XmlSchemaSequence derivedSequence, XmlSchemaAll baseAll)
	{
		if (!IsValidOccurrenceRangeRestriction(derivedSequence, baseAll) || derivedSequence.Items.Count > baseAll.Items.Count)
		{
			return false;
		}
		BitSet bitSet = new BitSet(baseAll.Items.Count);
		for (int i = 0; i < derivedSequence.Items.Count; i++)
		{
			int mappingParticle = GetMappingParticle((XmlSchemaParticle)derivedSequence.Items[i], baseAll.Items);
			if (mappingParticle >= 0)
			{
				if (bitSet[mappingParticle])
				{
					return false;
				}
				bitSet.Set(mappingParticle);
				continue;
			}
			return false;
		}
		for (int j = 0; j < baseAll.Items.Count; j++)
		{
			if (!bitSet[j] && !IsParticleEmptiable((XmlSchemaParticle)baseAll.Items[j]))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsSequenceFromChoice(XmlSchemaSequence derivedSequence, XmlSchemaChoice baseChoice)
	{
		CalculateSequenceRange(derivedSequence, out var minOccurs, out var maxOccurs);
		if (!IsValidOccurrenceRangeRestriction(minOccurs, maxOccurs, baseChoice.MinOccurs, baseChoice.MaxOccurs))
		{
			return false;
		}
		for (int i = 0; i < derivedSequence.Items.Count; i++)
		{
			if (GetMappingParticle((XmlSchemaParticle)derivedSequence.Items[i], baseChoice.Items) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private void CalculateSequenceRange(XmlSchemaSequence sequence, out decimal minOccurs, out decimal maxOccurs)
	{
		minOccurs = default(decimal);
		maxOccurs = default(decimal);
		for (int i = 0; i < sequence.Items.Count; i++)
		{
			XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)sequence.Items[i];
			minOccurs += xmlSchemaParticle.MinOccurs;
			if (xmlSchemaParticle.MaxOccurs == decimal.MaxValue)
			{
				maxOccurs = decimal.MaxValue;
			}
			else if (maxOccurs != decimal.MaxValue)
			{
				maxOccurs += xmlSchemaParticle.MaxOccurs;
			}
		}
		minOccurs *= sequence.MinOccurs;
		if (sequence.MaxOccurs == decimal.MaxValue)
		{
			maxOccurs = decimal.MaxValue;
		}
		else if (maxOccurs != decimal.MaxValue)
		{
			maxOccurs *= sequence.MaxOccurs;
		}
	}

	private bool IsValidOccurrenceRangeRestriction(XmlSchemaParticle derivedParticle, XmlSchemaParticle baseParticle)
	{
		return IsValidOccurrenceRangeRestriction(derivedParticle.MinOccurs, derivedParticle.MaxOccurs, baseParticle.MinOccurs, baseParticle.MaxOccurs);
	}

	private bool IsValidOccurrenceRangeRestriction(decimal minOccurs, decimal maxOccurs, decimal baseMinOccurs, decimal baseMaxOccurs)
	{
		if (baseMinOccurs <= minOccurs)
		{
			return maxOccurs <= baseMaxOccurs;
		}
		return false;
	}

	private int GetMappingParticle(XmlSchemaParticle particle, XmlSchemaObjectCollection collection)
	{
		for (int i = 0; i < collection.Count; i++)
		{
			if (IsValidRestriction(particle, (XmlSchemaParticle)collection[i]))
			{
				return i;
			}
		}
		return -1;
	}

	private bool IsParticleEmptiable(XmlSchemaParticle particle)
	{
		CalculateEffectiveTotalRange(particle, out var minOccurs, out var _);
		return minOccurs == 0m;
	}

	private void CalculateEffectiveTotalRange(XmlSchemaParticle particle, out decimal minOccurs, out decimal maxOccurs)
	{
		if (particle is XmlSchemaElement || particle is XmlSchemaAny)
		{
			minOccurs = particle.MinOccurs;
			maxOccurs = particle.MaxOccurs;
			return;
		}
		if (particle is XmlSchemaChoice)
		{
			if (((XmlSchemaChoice)particle).Items.Count == 0)
			{
				minOccurs = (maxOccurs = 0m);
				return;
			}
			minOccurs = decimal.MaxValue;
			maxOccurs = default(decimal);
			XmlSchemaChoice xmlSchemaChoice = (XmlSchemaChoice)particle;
			for (int i = 0; i < xmlSchemaChoice.Items.Count; i++)
			{
				CalculateEffectiveTotalRange((XmlSchemaParticle)xmlSchemaChoice.Items[i], out var minOccurs2, out var maxOccurs2);
				if (minOccurs2 < minOccurs)
				{
					minOccurs = minOccurs2;
				}
				if (maxOccurs2 > maxOccurs)
				{
					maxOccurs = maxOccurs2;
				}
			}
			minOccurs *= particle.MinOccurs;
			if (maxOccurs != decimal.MaxValue)
			{
				if (particle.MaxOccurs == decimal.MaxValue)
				{
					maxOccurs = decimal.MaxValue;
				}
				else
				{
					maxOccurs *= particle.MaxOccurs;
				}
			}
			return;
		}
		XmlSchemaObjectCollection items = ((XmlSchemaGroupBase)particle).Items;
		if (items.Count == 0)
		{
			minOccurs = (maxOccurs = 0m);
			return;
		}
		minOccurs = default(decimal);
		maxOccurs = default(decimal);
		for (int j = 0; j < items.Count; j++)
		{
			CalculateEffectiveTotalRange((XmlSchemaParticle)items[j], out var minOccurs3, out var maxOccurs3);
			minOccurs += minOccurs3;
			if (maxOccurs != decimal.MaxValue)
			{
				if (maxOccurs3 == decimal.MaxValue)
				{
					maxOccurs = decimal.MaxValue;
				}
				else
				{
					maxOccurs += maxOccurs3;
				}
			}
		}
		minOccurs *= particle.MinOccurs;
		if (maxOccurs != decimal.MaxValue)
		{
			if (particle.MaxOccurs == decimal.MaxValue)
			{
				maxOccurs = decimal.MaxValue;
			}
			else
			{
				maxOccurs *= particle.MaxOccurs;
			}
		}
	}

	private void PushComplexType(XmlSchemaComplexType complexType)
	{
		_complexTypeStack.Push(complexType);
	}

	private XmlSchemaContentType GetSchemaContentType(XmlSchemaComplexType complexType, XmlSchemaComplexContent complexContent, XmlSchemaParticle particle)
	{
		if ((complexContent != null && complexContent.IsMixed) || (complexContent == null && complexType.IsMixed))
		{
			return XmlSchemaContentType.Mixed;
		}
		if (particle != null && !particle.IsEmpty)
		{
			return XmlSchemaContentType.ElementOnly;
		}
		return XmlSchemaContentType.Empty;
	}

	private void CompileAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
	{
		if (attributeGroup.IsProcessing)
		{
			SendValidationEvent(System.SR.Sch_AttributeGroupCircularRef, attributeGroup);
		}
		else
		{
			if (attributeGroup.AttributeUses.Count > 0)
			{
				return;
			}
			attributeGroup.IsProcessing = true;
			XmlSchemaAnyAttribute xmlSchemaAnyAttribute = attributeGroup.AnyAttribute;
			for (int i = 0; i < attributeGroup.Attributes.Count; i++)
			{
				if (attributeGroup.Attributes[i] is XmlSchemaAttribute xmlSchemaAttribute)
				{
					if (xmlSchemaAttribute.Use != XmlSchemaUse.Prohibited)
					{
						CompileAttribute(xmlSchemaAttribute);
					}
					if (attributeGroup.AttributeUses[xmlSchemaAttribute.QualifiedName] == null)
					{
						attributeGroup.AttributeUses.Add(xmlSchemaAttribute.QualifiedName, xmlSchemaAttribute);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_DupAttributeUse, xmlSchemaAttribute.QualifiedName.ToString(), xmlSchemaAttribute);
					}
					continue;
				}
				XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef = (XmlSchemaAttributeGroupRef)attributeGroup.Attributes[i];
				XmlSchemaAttributeGroup xmlSchemaAttributeGroup = ((attributeGroup.Redefined == null || !(xmlSchemaAttributeGroupRef.RefName == attributeGroup.Redefined.QualifiedName)) ? ((XmlSchemaAttributeGroup)_schema.AttributeGroups[xmlSchemaAttributeGroupRef.RefName]) : attributeGroup.Redefined);
				if (xmlSchemaAttributeGroup != null)
				{
					CompileAttributeGroup(xmlSchemaAttributeGroup);
					foreach (XmlSchemaAttribute value in xmlSchemaAttributeGroup.AttributeUses.Values)
					{
						if (attributeGroup.AttributeUses[value.QualifiedName] == null)
						{
							attributeGroup.AttributeUses.Add(value.QualifiedName, value);
						}
						else
						{
							SendValidationEvent(System.SR.Sch_DupAttributeUse, value.QualifiedName.ToString(), value);
						}
					}
					xmlSchemaAnyAttribute = CompileAnyAttributeIntersection(xmlSchemaAnyAttribute, xmlSchemaAttributeGroup.AttributeWildcard);
				}
				else
				{
					SendValidationEvent(System.SR.Sch_UndefAttributeGroupRef, xmlSchemaAttributeGroupRef.RefName.ToString(), xmlSchemaAttributeGroupRef);
				}
			}
			attributeGroup.AttributeWildcard = xmlSchemaAnyAttribute;
			attributeGroup.IsProcessing = false;
		}
	}

	private void CompileLocalAttributes(XmlSchemaComplexType baseType, XmlSchemaComplexType derivedType, XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute anyAttribute, XmlSchemaDerivationMethod derivedBy)
	{
		XmlSchemaAnyAttribute xmlSchemaAnyAttribute = baseType?.AttributeWildcard;
		for (int i = 0; i < attributes.Count; i++)
		{
			if (attributes[i] is XmlSchemaAttribute xmlSchemaAttribute)
			{
				if (xmlSchemaAttribute.Use != XmlSchemaUse.Prohibited)
				{
					CompileAttribute(xmlSchemaAttribute);
				}
				if (xmlSchemaAttribute.Use != XmlSchemaUse.Prohibited || (xmlSchemaAttribute.Use == XmlSchemaUse.Prohibited && derivedBy == XmlSchemaDerivationMethod.Restriction && baseType != XmlSchemaComplexType.AnyType))
				{
					if (derivedType.AttributeUses[xmlSchemaAttribute.QualifiedName] == null)
					{
						derivedType.AttributeUses.Add(xmlSchemaAttribute.QualifiedName, xmlSchemaAttribute);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_DupAttributeUse, xmlSchemaAttribute.QualifiedName.ToString(), xmlSchemaAttribute);
					}
				}
				else
				{
					SendValidationEvent(System.SR.Sch_AttributeIgnored, xmlSchemaAttribute.QualifiedName.ToString(), xmlSchemaAttribute, XmlSeverityType.Warning);
				}
				continue;
			}
			XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef = (XmlSchemaAttributeGroupRef)attributes[i];
			XmlSchemaAttributeGroup xmlSchemaAttributeGroup = (XmlSchemaAttributeGroup)_schema.AttributeGroups[xmlSchemaAttributeGroupRef.RefName];
			if (xmlSchemaAttributeGroup != null)
			{
				CompileAttributeGroup(xmlSchemaAttributeGroup);
				foreach (XmlSchemaAttribute value in xmlSchemaAttributeGroup.AttributeUses.Values)
				{
					if (value.Use != XmlSchemaUse.Prohibited || (value.Use == XmlSchemaUse.Prohibited && derivedBy == XmlSchemaDerivationMethod.Restriction && baseType != XmlSchemaComplexType.AnyType))
					{
						if (derivedType.AttributeUses[value.QualifiedName] == null)
						{
							derivedType.AttributeUses.Add(value.QualifiedName, value);
						}
						else
						{
							SendValidationEvent(System.SR.Sch_DupAttributeUse, value.QualifiedName.ToString(), xmlSchemaAttributeGroupRef);
						}
					}
					else
					{
						SendValidationEvent(System.SR.Sch_AttributeIgnored, value.QualifiedName.ToString(), value, XmlSeverityType.Warning);
					}
				}
				anyAttribute = CompileAnyAttributeIntersection(anyAttribute, xmlSchemaAttributeGroup.AttributeWildcard);
			}
			else
			{
				SendValidationEvent(System.SR.Sch_UndefAttributeGroupRef, xmlSchemaAttributeGroupRef.RefName.ToString(), xmlSchemaAttributeGroupRef);
			}
		}
		if (baseType != null)
		{
			if (derivedBy == XmlSchemaDerivationMethod.Extension)
			{
				derivedType.SetAttributeWildcard(CompileAnyAttributeUnion(anyAttribute, xmlSchemaAnyAttribute));
				{
					foreach (XmlSchemaAttribute value2 in baseType.AttributeUses.Values)
					{
						XmlSchemaAttribute xmlSchemaAttribute4 = (XmlSchemaAttribute)derivedType.AttributeUses[value2.QualifiedName];
						if (xmlSchemaAttribute4 != null)
						{
							if (xmlSchemaAttribute4.AttributeSchemaType != value2.AttributeSchemaType || value2.Use == XmlSchemaUse.Prohibited)
							{
								SendValidationEvent(System.SR.Sch_InvalidAttributeExtension, xmlSchemaAttribute4);
							}
						}
						else
						{
							derivedType.AttributeUses.Add(value2.QualifiedName, value2);
						}
					}
					return;
				}
			}
			if (anyAttribute != null && (xmlSchemaAnyAttribute == null || !XmlSchemaAnyAttribute.IsSubset(anyAttribute, xmlSchemaAnyAttribute)))
			{
				SendValidationEvent(System.SR.Sch_InvalidAnyAttributeRestriction, derivedType);
			}
			else
			{
				derivedType.SetAttributeWildcard(anyAttribute);
			}
			foreach (XmlSchemaAttribute value3 in baseType.AttributeUses.Values)
			{
				XmlSchemaAttribute xmlSchemaAttribute6 = (XmlSchemaAttribute)derivedType.AttributeUses[value3.QualifiedName];
				if (xmlSchemaAttribute6 == null)
				{
					derivedType.AttributeUses.Add(value3.QualifiedName, value3);
				}
				else if (value3.Use == XmlSchemaUse.Prohibited && xmlSchemaAttribute6.Use != XmlSchemaUse.Prohibited)
				{
					SendValidationEvent(System.SR.Sch_AttributeRestrictionProhibited, xmlSchemaAttribute6);
				}
				else if (xmlSchemaAttribute6.Use != XmlSchemaUse.Prohibited && (value3.AttributeSchemaType == null || xmlSchemaAttribute6.AttributeSchemaType == null || !XmlSchemaType.IsDerivedFrom(xmlSchemaAttribute6.AttributeSchemaType, value3.AttributeSchemaType, XmlSchemaDerivationMethod.Empty)))
				{
					SendValidationEvent(System.SR.Sch_AttributeRestrictionInvalid, xmlSchemaAttribute6);
				}
			}
			{
				foreach (XmlSchemaAttribute value4 in derivedType.AttributeUses.Values)
				{
					XmlSchemaAttribute xmlSchemaAttribute8 = (XmlSchemaAttribute)baseType.AttributeUses[value4.QualifiedName];
					if (xmlSchemaAttribute8 == null && (xmlSchemaAnyAttribute == null || !xmlSchemaAnyAttribute.Allows(value4.QualifiedName)))
					{
						SendValidationEvent(System.SR.Sch_AttributeRestrictionInvalidFromWildcard, value4);
					}
				}
				return;
			}
		}
		derivedType.SetAttributeWildcard(anyAttribute);
	}

	private XmlSchemaAnyAttribute CompileAnyAttributeUnion(XmlSchemaAnyAttribute a, XmlSchemaAnyAttribute b)
	{
		if (a == null)
		{
			return b;
		}
		if (b == null)
		{
			return a;
		}
		XmlSchemaAnyAttribute xmlSchemaAnyAttribute = XmlSchemaAnyAttribute.Union(a, b, v1Compat: true);
		if (xmlSchemaAnyAttribute == null)
		{
			SendValidationEvent(System.SR.Sch_UnexpressibleAnyAttribute, a);
		}
		return xmlSchemaAnyAttribute;
	}

	private XmlSchemaAnyAttribute CompileAnyAttributeIntersection(XmlSchemaAnyAttribute a, XmlSchemaAnyAttribute b)
	{
		if (a == null)
		{
			return b;
		}
		if (b == null)
		{
			return a;
		}
		XmlSchemaAnyAttribute xmlSchemaAnyAttribute = XmlSchemaAnyAttribute.Intersection(a, b, v1Compat: true);
		if (xmlSchemaAnyAttribute == null)
		{
			SendValidationEvent(System.SR.Sch_UnexpressibleAnyAttribute, a);
		}
		return xmlSchemaAnyAttribute;
	}

	private void CompileAttribute(XmlSchemaAttribute xa)
	{
		if (xa.IsProcessing)
		{
			SendValidationEvent(System.SR.Sch_AttributeCircularRef, xa);
		}
		else
		{
			if (xa.AttDef != null)
			{
				return;
			}
			xa.IsProcessing = true;
			SchemaAttDef schemaAttDef = null;
			try
			{
				if (!xa.RefName.IsEmpty)
				{
					XmlSchemaAttribute xmlSchemaAttribute = (XmlSchemaAttribute)_schema.Attributes[xa.RefName];
					if (xmlSchemaAttribute == null)
					{
						throw new XmlSchemaException(System.SR.Sch_UndeclaredAttribute, xa.RefName.ToString(), xa);
					}
					CompileAttribute(xmlSchemaAttribute);
					if (xmlSchemaAttribute.AttDef == null)
					{
						throw new XmlSchemaException(System.SR.Sch_RefInvalidAttribute, xa.RefName.ToString(), xa);
					}
					schemaAttDef = xmlSchemaAttribute.AttDef.Clone();
					if (schemaAttDef.Datatype != null)
					{
						if (xmlSchemaAttribute.FixedValue != null)
						{
							if (xa.DefaultValue != null)
							{
								throw new XmlSchemaException(System.SR.Sch_FixedDefaultInRef, xa.RefName.ToString(), xa);
							}
							if (xa.FixedValue != null)
							{
								if (xa.FixedValue != xmlSchemaAttribute.FixedValue)
								{
									throw new XmlSchemaException(System.SR.Sch_FixedInRef, xa.RefName.ToString(), xa);
								}
							}
							else
							{
								schemaAttDef.Presence = SchemaDeclBase.Use.Fixed;
								SchemaAttDef schemaAttDef2 = schemaAttDef;
								string defaultValueRaw = (schemaAttDef.DefaultValueExpanded = xmlSchemaAttribute.FixedValue);
								schemaAttDef2.DefaultValueRaw = defaultValueRaw;
								schemaAttDef.DefaultValueTyped = schemaAttDef.Datatype.ParseValue(schemaAttDef.DefaultValueRaw, base.NameTable, new SchemaNamespaceManager(xa), createAtomicValue: true);
							}
						}
						else if (xmlSchemaAttribute.DefaultValue != null && xa.DefaultValue == null && xa.FixedValue == null)
						{
							schemaAttDef.Presence = SchemaDeclBase.Use.Default;
							SchemaAttDef schemaAttDef3 = schemaAttDef;
							string defaultValueRaw = (schemaAttDef.DefaultValueExpanded = xmlSchemaAttribute.DefaultValue);
							schemaAttDef3.DefaultValueRaw = defaultValueRaw;
							schemaAttDef.DefaultValueTyped = schemaAttDef.Datatype.ParseValue(schemaAttDef.DefaultValueRaw, base.NameTable, new SchemaNamespaceManager(xa), createAtomicValue: true);
						}
					}
					xa.SetAttributeType(xmlSchemaAttribute.AttributeSchemaType);
				}
				else
				{
					schemaAttDef = new SchemaAttDef(xa.QualifiedName);
					if (xa.SchemaType != null)
					{
						CompileSimpleType(xa.SchemaType);
						xa.SetAttributeType(xa.SchemaType);
						schemaAttDef.SchemaType = xa.SchemaType;
						schemaAttDef.Datatype = xa.SchemaType.Datatype;
					}
					else if (!xa.SchemaTypeName.IsEmpty)
					{
						XmlSchemaSimpleType simpleType = GetSimpleType(xa.SchemaTypeName);
						if (simpleType == null)
						{
							throw new XmlSchemaException(System.SR.Sch_UndeclaredSimpleType, xa.SchemaTypeName.ToString(), xa);
						}
						xa.SetAttributeType(simpleType);
						schemaAttDef.Datatype = simpleType.Datatype;
						schemaAttDef.SchemaType = simpleType;
					}
					else
					{
						schemaAttDef.SchemaType = DatatypeImplementation.AnySimpleType;
						schemaAttDef.Datatype = DatatypeImplementation.AnySimpleType.Datatype;
						xa.SetAttributeType(DatatypeImplementation.AnySimpleType);
					}
				}
				if (schemaAttDef.Datatype != null)
				{
					schemaAttDef.Datatype.VerifySchemaValid(_schema.Notations, xa);
				}
				if (xa.DefaultValue != null || xa.FixedValue != null)
				{
					if (xa.DefaultValue != null)
					{
						schemaAttDef.Presence = SchemaDeclBase.Use.Default;
						SchemaAttDef schemaAttDef4 = schemaAttDef;
						string defaultValueRaw = (schemaAttDef.DefaultValueExpanded = xa.DefaultValue);
						schemaAttDef4.DefaultValueRaw = defaultValueRaw;
					}
					else
					{
						schemaAttDef.Presence = SchemaDeclBase.Use.Fixed;
						SchemaAttDef schemaAttDef5 = schemaAttDef;
						string defaultValueRaw = (schemaAttDef.DefaultValueExpanded = xa.FixedValue);
						schemaAttDef5.DefaultValueRaw = defaultValueRaw;
					}
					if (schemaAttDef.Datatype != null)
					{
						schemaAttDef.DefaultValueTyped = schemaAttDef.Datatype.ParseValue(schemaAttDef.DefaultValueRaw, base.NameTable, new SchemaNamespaceManager(xa), createAtomicValue: true);
					}
				}
				else
				{
					switch (xa.Use)
					{
					case XmlSchemaUse.None:
					case XmlSchemaUse.Optional:
						schemaAttDef.Presence = SchemaDeclBase.Use.Implied;
						break;
					case XmlSchemaUse.Required:
						schemaAttDef.Presence = SchemaDeclBase.Use.Required;
						break;
					}
				}
				schemaAttDef.SchemaAttribute = xa;
				xa.AttDef = schemaAttDef;
			}
			catch (XmlSchemaException ex)
			{
				if (ex.SourceSchemaObject == null)
				{
					ex.SetSource(xa);
				}
				SendValidationEvent(ex);
				xa.AttDef = SchemaAttDef.Empty;
			}
			finally
			{
				xa.IsProcessing = false;
			}
		}
	}

	private void CompileIdentityConstraint(XmlSchemaIdentityConstraint xi)
	{
		if (xi.IsProcessing)
		{
			xi.CompiledConstraint = CompiledIdentityConstraint.Empty;
			SendValidationEvent(System.SR.Sch_IdentityConstraintCircularRef, xi);
		}
		else
		{
			if (xi.CompiledConstraint != null)
			{
				return;
			}
			xi.IsProcessing = true;
			CompiledIdentityConstraint compiledIdentityConstraint = null;
			try
			{
				SchemaNamespaceManager nsmgr = new SchemaNamespaceManager(xi);
				compiledIdentityConstraint = new CompiledIdentityConstraint(xi, nsmgr);
				if (xi is XmlSchemaKeyref)
				{
					XmlSchemaIdentityConstraint xmlSchemaIdentityConstraint = (XmlSchemaIdentityConstraint)_schema.IdentityConstraints[((XmlSchemaKeyref)xi).Refer];
					if (xmlSchemaIdentityConstraint == null)
					{
						throw new XmlSchemaException(System.SR.Sch_UndeclaredIdentityConstraint, ((XmlSchemaKeyref)xi).Refer.ToString(), xi);
					}
					CompileIdentityConstraint(xmlSchemaIdentityConstraint);
					if (xmlSchemaIdentityConstraint.CompiledConstraint == null)
					{
						throw new XmlSchemaException(System.SR.Sch_RefInvalidIdentityConstraint, ((XmlSchemaKeyref)xi).Refer.ToString(), xi);
					}
					if (xmlSchemaIdentityConstraint.Fields.Count != xi.Fields.Count)
					{
						throw new XmlSchemaException(System.SR.Sch_RefInvalidCardin, xi.QualifiedName.ToString(), xi);
					}
					if (xmlSchemaIdentityConstraint.CompiledConstraint.Role == CompiledIdentityConstraint.ConstraintRole.Keyref)
					{
						throw new XmlSchemaException(System.SR.Sch_ReftoKeyref, xi.QualifiedName.ToString(), xi);
					}
				}
				xi.CompiledConstraint = compiledIdentityConstraint;
			}
			catch (XmlSchemaException ex)
			{
				if (ex.SourceSchemaObject == null)
				{
					ex.SetSource(xi);
				}
				SendValidationEvent(ex);
				xi.CompiledConstraint = CompiledIdentityConstraint.Empty;
			}
			finally
			{
				xi.IsProcessing = false;
			}
		}
	}

	private void CompileElement(XmlSchemaElement xe)
	{
		if (xe.IsProcessing)
		{
			SendValidationEvent(System.SR.Sch_ElementCircularRef, xe);
		}
		else
		{
			if (xe.ElementDecl != null)
			{
				return;
			}
			xe.IsProcessing = true;
			SchemaElementDecl schemaElementDecl = null;
			try
			{
				if (!xe.RefName.IsEmpty)
				{
					XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)_schema.Elements[xe.RefName];
					if (xmlSchemaElement == null)
					{
						throw new XmlSchemaException(System.SR.Sch_UndeclaredElement, xe.RefName.ToString(), xe);
					}
					CompileElement(xmlSchemaElement);
					if (xmlSchemaElement.ElementDecl == null)
					{
						throw new XmlSchemaException(System.SR.Sch_RefInvalidElement, xe.RefName.ToString(), xe);
					}
					xe.SetElementType(xmlSchemaElement.ElementSchemaType);
					schemaElementDecl = xmlSchemaElement.ElementDecl.Clone();
				}
				else
				{
					if (xe.SchemaType != null)
					{
						xe.SetElementType(xe.SchemaType);
					}
					else if (!xe.SchemaTypeName.IsEmpty)
					{
						xe.SetElementType(GetAnySchemaType(xe.SchemaTypeName));
						if (xe.ElementSchemaType == null)
						{
							throw new XmlSchemaException(System.SR.Sch_UndeclaredType, xe.SchemaTypeName.ToString(), xe);
						}
					}
					else if (!xe.SubstitutionGroup.IsEmpty)
					{
						XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)_schema.Elements[xe.SubstitutionGroup];
						if (xmlSchemaElement2 == null)
						{
							throw new XmlSchemaException(System.SR.Sch_UndeclaredEquivClass, xe.SubstitutionGroup.Name, xe);
						}
						if (xmlSchemaElement2.IsProcessing)
						{
							return;
						}
						CompileElement(xmlSchemaElement2);
						if (xmlSchemaElement2.ElementDecl == null)
						{
							xe.SetElementType(XmlSchemaComplexType.AnyType);
							schemaElementDecl = XmlSchemaComplexType.AnyType.ElementDecl.Clone();
						}
						else
						{
							xe.SetElementType(xmlSchemaElement2.ElementSchemaType);
							schemaElementDecl = xmlSchemaElement2.ElementDecl.Clone();
						}
					}
					else
					{
						xe.SetElementType(XmlSchemaComplexType.AnyType);
						schemaElementDecl = XmlSchemaComplexType.AnyType.ElementDecl.Clone();
					}
					if (schemaElementDecl == null)
					{
						if (xe.ElementSchemaType is XmlSchemaComplexType)
						{
							XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)xe.ElementSchemaType;
							CompileComplexType(xmlSchemaComplexType);
							if (xmlSchemaComplexType.ElementDecl != null)
							{
								schemaElementDecl = xmlSchemaComplexType.ElementDecl.Clone();
							}
						}
						else if (xe.ElementSchemaType is XmlSchemaSimpleType)
						{
							XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)xe.ElementSchemaType;
							CompileSimpleType(xmlSchemaSimpleType);
							if (xmlSchemaSimpleType.ElementDecl != null)
							{
								schemaElementDecl = xmlSchemaSimpleType.ElementDecl.Clone();
							}
						}
					}
					schemaElementDecl.Name = xe.QualifiedName;
					schemaElementDecl.IsAbstract = xe.IsAbstract;
					if (xe.ElementSchemaType is XmlSchemaComplexType xmlSchemaComplexType2)
					{
						schemaElementDecl.IsAbstract |= xmlSchemaComplexType2.IsAbstract;
					}
					schemaElementDecl.IsNillable = xe.IsNillable;
					schemaElementDecl.Block |= xe.BlockResolved;
				}
				if (schemaElementDecl.Datatype != null)
				{
					schemaElementDecl.Datatype.VerifySchemaValid(_schema.Notations, xe);
				}
				if ((xe.DefaultValue != null || xe.FixedValue != null) && schemaElementDecl.ContentValidator != null)
				{
					if (schemaElementDecl.ContentValidator.ContentType == XmlSchemaContentType.TextOnly)
					{
						if (xe.DefaultValue != null)
						{
							schemaElementDecl.Presence = SchemaDeclBase.Use.Default;
							schemaElementDecl.DefaultValueRaw = xe.DefaultValue;
						}
						else
						{
							schemaElementDecl.Presence = SchemaDeclBase.Use.Fixed;
							schemaElementDecl.DefaultValueRaw = xe.FixedValue;
						}
						if (schemaElementDecl.Datatype != null)
						{
							schemaElementDecl.DefaultValueTyped = schemaElementDecl.Datatype.ParseValue(schemaElementDecl.DefaultValueRaw, base.NameTable, new SchemaNamespaceManager(xe), createAtomicValue: true);
						}
					}
					else if (schemaElementDecl.ContentValidator.ContentType != XmlSchemaContentType.Mixed || !schemaElementDecl.ContentValidator.IsEmptiable)
					{
						throw new XmlSchemaException(System.SR.Sch_ElementCannotHaveValue, xe);
					}
				}
				if (xe.HasConstraints)
				{
					XmlSchemaObjectCollection constraints = xe.Constraints;
					CompiledIdentityConstraint[] array = new CompiledIdentityConstraint[constraints.Count];
					int num = 0;
					for (int i = 0; i < constraints.Count; i++)
					{
						XmlSchemaIdentityConstraint xmlSchemaIdentityConstraint = (XmlSchemaIdentityConstraint)constraints[i];
						CompileIdentityConstraint(xmlSchemaIdentityConstraint);
						array[num++] = xmlSchemaIdentityConstraint.CompiledConstraint;
					}
					schemaElementDecl.Constraints = array;
				}
				schemaElementDecl.SchemaElement = xe;
				xe.ElementDecl = schemaElementDecl;
			}
			catch (XmlSchemaException ex)
			{
				if (ex.SourceSchemaObject == null)
				{
					ex.SetSource(xe);
				}
				SendValidationEvent(ex);
				xe.ElementDecl = SchemaElementDecl.Empty;
			}
			finally
			{
				xe.IsProcessing = false;
			}
		}
	}

	private ContentValidator CompileComplexContent(XmlSchemaComplexType complexType)
	{
		if (complexType.ContentType == XmlSchemaContentType.Empty)
		{
			return ContentValidator.Empty;
		}
		if (complexType.ContentType == XmlSchemaContentType.TextOnly)
		{
			return ContentValidator.TextOnly;
		}
		XmlSchemaParticle contentTypeParticle = complexType.ContentTypeParticle;
		if (contentTypeParticle == null || contentTypeParticle == XmlSchemaParticle.Empty)
		{
			if (complexType.ContentType == XmlSchemaContentType.ElementOnly)
			{
				return ContentValidator.Empty;
			}
			return ContentValidator.Mixed;
		}
		PushComplexType(complexType);
		if (contentTypeParticle is XmlSchemaAll)
		{
			XmlSchemaAll xmlSchemaAll = (XmlSchemaAll)contentTypeParticle;
			AllElementsContentValidator allElementsContentValidator = new AllElementsContentValidator(complexType.ContentType, xmlSchemaAll.Items.Count, xmlSchemaAll.MinOccurs == 0m);
			for (int i = 0; i < xmlSchemaAll.Items.Count; i++)
			{
				XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)xmlSchemaAll.Items[i];
				if (!allElementsContentValidator.AddElement(xmlSchemaElement.QualifiedName, xmlSchemaElement, xmlSchemaElement.MinOccurs == 0m))
				{
					SendValidationEvent(System.SR.Sch_DupElement, xmlSchemaElement.QualifiedName.ToString(), xmlSchemaElement);
				}
			}
			return allElementsContentValidator;
		}
		ParticleContentValidator particleContentValidator = new ParticleContentValidator(complexType.ContentType);
		try
		{
			particleContentValidator.Start();
			BuildParticleContentModel(particleContentValidator, contentTypeParticle);
			return particleContentValidator.Finish(_compileContentModel);
		}
		catch (UpaException ex)
		{
			if (ex.Particle1 is XmlSchemaElement)
			{
				if (ex.Particle2 is XmlSchemaElement)
				{
					SendValidationEvent(System.SR.Sch_NonDeterministic, ((XmlSchemaElement)ex.Particle1).QualifiedName.ToString(), (XmlSchemaElement)ex.Particle2);
				}
				else
				{
					SendValidationEvent(System.SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)ex.Particle2).NamespaceList.ToString(), ((XmlSchemaElement)ex.Particle1).QualifiedName.ToString(), (XmlSchemaAny)ex.Particle2);
				}
			}
			else if (ex.Particle2 is XmlSchemaElement)
			{
				SendValidationEvent(System.SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)ex.Particle1).NamespaceList.ToString(), ((XmlSchemaElement)ex.Particle2).QualifiedName.ToString(), (XmlSchemaAny)ex.Particle1);
			}
			else
			{
				SendValidationEvent(System.SR.Sch_NonDeterministicAnyAny, ((XmlSchemaAny)ex.Particle1).NamespaceList.ToString(), ((XmlSchemaAny)ex.Particle2).NamespaceList.ToString(), (XmlSchemaAny)ex.Particle1);
			}
			return XmlSchemaComplexType.AnyTypeContentValidator;
		}
		catch (NotSupportedException)
		{
			SendValidationEvent(System.SR.Sch_ComplexContentModel, complexType, XmlSeverityType.Warning);
			return XmlSchemaComplexType.AnyTypeContentValidator;
		}
	}

	private void BuildParticleContentModel(ParticleContentValidator contentValidator, XmlSchemaParticle particle)
	{
		if (particle is XmlSchemaElement)
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)particle;
			contentValidator.AddName(xmlSchemaElement.QualifiedName, xmlSchemaElement);
		}
		else if (particle is XmlSchemaAny)
		{
			XmlSchemaAny xmlSchemaAny = (XmlSchemaAny)particle;
			contentValidator.AddNamespaceList(xmlSchemaAny.NamespaceList, xmlSchemaAny);
		}
		else if (particle is XmlSchemaGroupBase)
		{
			XmlSchemaObjectCollection items = ((XmlSchemaGroupBase)particle).Items;
			bool flag = particle is XmlSchemaChoice;
			contentValidator.OpenGroup();
			bool flag2 = true;
			for (int i = 0; i < items.Count; i++)
			{
				XmlSchemaParticle particle2 = (XmlSchemaParticle)items[i];
				if (flag2)
				{
					flag2 = false;
				}
				else if (flag)
				{
					contentValidator.AddChoice();
				}
				else
				{
					contentValidator.AddSequence();
				}
				BuildParticleContentModel(contentValidator, particle2);
			}
			contentValidator.CloseGroup();
		}
		if (!(particle.MinOccurs == 1m) || !(particle.MaxOccurs == 1m))
		{
			if (particle.MinOccurs == 0m && particle.MaxOccurs == 1m)
			{
				contentValidator.AddQMark();
			}
			else if (particle.MinOccurs == 0m && particle.MaxOccurs == decimal.MaxValue)
			{
				contentValidator.AddStar();
			}
			else if (particle.MinOccurs == 1m && particle.MaxOccurs == decimal.MaxValue)
			{
				contentValidator.AddPlus();
			}
			else
			{
				contentValidator.AddLeafRange(particle.MinOccurs, particle.MaxOccurs);
			}
		}
	}

	private void CompileParticleElements(XmlSchemaComplexType complexType, XmlSchemaParticle particle)
	{
		if (particle is XmlSchemaElement)
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)particle;
			CompileElement(xmlSchemaElement);
			if (complexType.LocalElements[xmlSchemaElement.QualifiedName] == null)
			{
				complexType.LocalElements.Add(xmlSchemaElement.QualifiedName, xmlSchemaElement);
				return;
			}
			XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)complexType.LocalElements[xmlSchemaElement.QualifiedName];
			if (xmlSchemaElement2.ElementSchemaType != xmlSchemaElement.ElementSchemaType)
			{
				SendValidationEvent(System.SR.Sch_ElementTypeCollision, particle);
			}
		}
		else if (particle is XmlSchemaGroupBase)
		{
			XmlSchemaObjectCollection items = ((XmlSchemaGroupBase)particle).Items;
			for (int i = 0; i < items.Count; i++)
			{
				CompileParticleElements(complexType, (XmlSchemaParticle)items[i]);
			}
		}
	}

	private void CompileCompexTypeElements(XmlSchemaComplexType complexType)
	{
		if (complexType.IsProcessing)
		{
			SendValidationEvent(System.SR.Sch_TypeCircularRef, complexType);
			return;
		}
		complexType.IsProcessing = true;
		if (complexType.ContentTypeParticle != XmlSchemaParticle.Empty)
		{
			CompileParticleElements(complexType, complexType.ContentTypeParticle);
		}
		complexType.IsProcessing = false;
	}

	private XmlSchemaSimpleType GetSimpleType(XmlQualifiedName name)
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = _schema.SchemaTypes[name] as XmlSchemaSimpleType;
		if (xmlSchemaSimpleType != null)
		{
			CompileSimpleType(xmlSchemaSimpleType);
		}
		else
		{
			xmlSchemaSimpleType = DatatypeImplementation.GetSimpleTypeFromXsdType(name);
			if (xmlSchemaSimpleType != null)
			{
				if (xmlSchemaSimpleType.TypeCode == XmlTypeCode.NormalizedString)
				{
					xmlSchemaSimpleType = DatatypeImplementation.GetNormalizedStringTypeV1Compat();
				}
				else if (xmlSchemaSimpleType.TypeCode == XmlTypeCode.Token)
				{
					xmlSchemaSimpleType = DatatypeImplementation.GetTokenTypeV1Compat();
				}
			}
		}
		return xmlSchemaSimpleType;
	}

	private XmlSchemaComplexType GetComplexType(XmlQualifiedName name)
	{
		XmlSchemaComplexType xmlSchemaComplexType = _schema.SchemaTypes[name] as XmlSchemaComplexType;
		if (xmlSchemaComplexType != null)
		{
			CompileComplexType(xmlSchemaComplexType);
		}
		return xmlSchemaComplexType;
	}

	private XmlSchemaType GetAnySchemaType(XmlQualifiedName name)
	{
		XmlSchemaType xmlSchemaType = (XmlSchemaType)_schema.SchemaTypes[name];
		if (xmlSchemaType != null)
		{
			if (xmlSchemaType is XmlSchemaComplexType)
			{
				CompileComplexType((XmlSchemaComplexType)xmlSchemaType);
			}
			else
			{
				CompileSimpleType((XmlSchemaSimpleType)xmlSchemaType);
			}
			return xmlSchemaType;
		}
		return DatatypeImplementation.GetSimpleTypeFromXsdType(name);
	}
}
