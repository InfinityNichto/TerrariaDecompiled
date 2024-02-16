using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Xml.Schema;

internal sealed class Compiler : BaseProcessor
{
	private string _restrictionErrorMsg;

	private readonly XmlSchemaObjectTable _attributes = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _attributeGroups = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _elements = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _schemaTypes = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _groups = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _notations = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _examplars = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _identityConstraints = new XmlSchemaObjectTable();

	private readonly Stack<XmlSchemaComplexType> _complexTypeStack = new Stack<XmlSchemaComplexType>();

	private readonly Hashtable _schemasToCompile = new Hashtable();

	private readonly XmlSchema _schemaForSchema;

	public Compiler(XmlNameTable nameTable, ValidationEventHandler eventHandler, XmlSchema schemaForSchema, XmlSchemaCompilationSettings compilationSettings)
		: base(nameTable, null, eventHandler, compilationSettings)
	{
		_schemaForSchema = schemaForSchema;
	}

	public bool Execute(XmlSchemaSet schemaSet, SchemaInfo schemaCompiledInfo)
	{
		Compile();
		if (!base.HasErrors)
		{
			Output(schemaCompiledInfo);
			schemaSet.elements = _elements;
			schemaSet.attributes = _attributes;
			schemaSet.schemaTypes = _schemaTypes;
			schemaSet.substitutionGroups = _examplars;
		}
		return !base.HasErrors;
	}

	internal void Prepare(XmlSchema schema, bool cleanup)
	{
		if (_schemasToCompile[schema] != null)
		{
			return;
		}
		_schemasToCompile.Add(schema, schema);
		foreach (XmlSchemaElement value in schema.Elements.Values)
		{
			if (cleanup)
			{
				CleanupElement(value);
			}
			AddToTable(_elements, value.QualifiedName, value);
		}
		foreach (XmlSchemaAttribute value2 in schema.Attributes.Values)
		{
			if (cleanup)
			{
				CleanupAttribute(value2);
			}
			AddToTable(_attributes, value2.QualifiedName, value2);
		}
		foreach (XmlSchemaGroup value3 in schema.Groups.Values)
		{
			if (cleanup)
			{
				CleanupGroup(value3);
			}
			AddToTable(_groups, value3.QualifiedName, value3);
		}
		foreach (XmlSchemaAttributeGroup value4 in schema.AttributeGroups.Values)
		{
			if (cleanup)
			{
				CleanupAttributeGroup(value4);
			}
			AddToTable(_attributeGroups, value4.QualifiedName, value4);
		}
		foreach (XmlSchemaType value5 in schema.SchemaTypes.Values)
		{
			if (cleanup)
			{
				if (value5 is XmlSchemaComplexType complexType)
				{
					CleanupComplexType(complexType);
				}
				else
				{
					CleanupSimpleType(value5 as XmlSchemaSimpleType);
				}
			}
			AddToTable(_schemaTypes, value5.QualifiedName, value5);
		}
		foreach (XmlSchemaNotation value6 in schema.Notations.Values)
		{
			AddToTable(_notations, value6.QualifiedName, value6);
		}
		foreach (XmlSchemaIdentityConstraint value7 in schema.IdentityConstraints.Values)
		{
			AddToTable(_identityConstraints, value7.QualifiedName, value7);
		}
	}

	private void UpdateSForSSimpleTypes()
	{
		XmlSchemaSimpleType[] builtInTypes = DatatypeImplementation.GetBuiltInTypes();
		int num = builtInTypes.Length - 3;
		for (int i = 12; i < num; i++)
		{
			XmlSchemaSimpleType xmlSchemaSimpleType = builtInTypes[i];
			_schemaForSchema.SchemaTypes.Replace(xmlSchemaSimpleType.QualifiedName, xmlSchemaSimpleType);
			_schemaTypes.Replace(xmlSchemaSimpleType.QualifiedName, xmlSchemaSimpleType);
		}
	}

	private void Output(SchemaInfo schemaInfo)
	{
		foreach (XmlSchema value in _schemasToCompile.Values)
		{
			string text = value.TargetNamespace;
			if (text == null)
			{
				text = string.Empty;
			}
			schemaInfo.TargetNamespaces[text] = true;
		}
		foreach (XmlSchemaElement value2 in _elements.Values)
		{
			schemaInfo.ElementDecls.Add(value2.QualifiedName, value2.ElementDecl);
		}
		foreach (XmlSchemaAttribute value3 in _attributes.Values)
		{
			schemaInfo.AttributeDecls.Add(value3.QualifiedName, value3.AttDef);
		}
		foreach (XmlSchemaType value4 in _schemaTypes.Values)
		{
			schemaInfo.ElementDeclsByType.Add(value4.QualifiedName, value4.ElementDecl);
		}
		foreach (XmlSchemaNotation value5 in _notations.Values)
		{
			SchemaNotation schemaNotation = new SchemaNotation(value5.QualifiedName);
			schemaNotation.SystemLiteral = value5.System;
			schemaNotation.Pubid = value5.Public;
			if (!schemaInfo.Notations.ContainsKey(schemaNotation.Name.Name))
			{
				schemaInfo.Notations.Add(schemaNotation.Name.Name, schemaNotation);
			}
		}
	}

	internal void ImportAllCompiledSchemas(XmlSchemaSet schemaSet)
	{
		SortedList sortedSchemas = schemaSet.SortedSchemas;
		for (int i = 0; i < sortedSchemas.Count; i++)
		{
			XmlSchema xmlSchema = (XmlSchema)sortedSchemas.GetByIndex(i);
			if (xmlSchema.IsCompiledBySet)
			{
				Prepare(xmlSchema, cleanup: false);
			}
		}
	}

	internal bool Compile()
	{
		_schemaTypes.Insert(DatatypeImplementation.QnAnyType, XmlSchemaComplexType.AnyType);
		if (_schemaForSchema != null)
		{
			_schemaForSchema.SchemaTypes.Replace(DatatypeImplementation.QnAnyType, XmlSchemaComplexType.AnyType);
			UpdateSForSSimpleTypes();
		}
		foreach (XmlSchemaGroup value in _groups.Values)
		{
			CompileGroup(value);
		}
		foreach (XmlSchemaAttributeGroup value2 in _attributeGroups.Values)
		{
			CompileAttributeGroup(value2);
		}
		foreach (XmlSchemaType value3 in _schemaTypes.Values)
		{
			if (value3 is XmlSchemaComplexType complexType)
			{
				CompileComplexType(complexType);
			}
			else
			{
				CompileSimpleType((XmlSchemaSimpleType)value3);
			}
		}
		foreach (XmlSchemaElement value4 in _elements.Values)
		{
			if (value4.ElementDecl == null)
			{
				CompileElement(value4);
			}
		}
		foreach (XmlSchemaAttribute value5 in _attributes.Values)
		{
			if (value5.AttDef == null)
			{
				CompileAttribute(value5);
			}
		}
		foreach (XmlSchemaIdentityConstraint value6 in _identityConstraints.Values)
		{
			if (value6.CompiledConstraint == null)
			{
				CompileIdentityConstraint(value6);
			}
		}
		while (_complexTypeStack.Count > 0)
		{
			XmlSchemaComplexType complexType2 = _complexTypeStack.Pop();
			CompileComplexTypeElements(complexType2);
		}
		ProcessSubstitutionGroups();
		foreach (XmlSchemaType value7 in _schemaTypes.Values)
		{
			if (value7 is XmlSchemaComplexType complexType3)
			{
				CheckParticleDerivation(complexType3);
			}
		}
		foreach (XmlSchemaElement value8 in _elements.Values)
		{
			if (value8.ElementSchemaType is XmlSchemaComplexType complexType4 && value8.SchemaTypeName == XmlQualifiedName.Empty)
			{
				CheckParticleDerivation(complexType4);
			}
		}
		foreach (XmlSchemaGroup value9 in _groups.Values)
		{
			XmlSchemaGroup redefined = value9.Redefined;
			if (redefined != null)
			{
				RecursivelyCheckRedefinedGroups(value9, redefined);
			}
		}
		foreach (XmlSchemaAttributeGroup value10 in _attributeGroups.Values)
		{
			XmlSchemaAttributeGroup redefined2 = value10.Redefined;
			if (redefined2 != null)
			{
				RecursivelyCheckRedefinedAttributeGroups(value10, redefined2);
			}
		}
		return !base.HasErrors;
	}

	private void CleanupAttribute(XmlSchemaAttribute attribute)
	{
		if (attribute.SchemaType != null)
		{
			CleanupSimpleType(attribute.SchemaType);
		}
		attribute.AttDef = null;
	}

	private void CleanupAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
	{
		CleanupAttributes(attributeGroup.Attributes);
		attributeGroup.AttributeUses.Clear();
		attributeGroup.AttributeWildcard = null;
		if (attributeGroup.Redefined != null)
		{
			CleanupAttributeGroup(attributeGroup.Redefined);
		}
	}

	private void CleanupComplexType(XmlSchemaComplexType complexType)
	{
		if (complexType.QualifiedName == DatatypeImplementation.QnAnyType)
		{
			return;
		}
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
		complexType.HasWildCard = false;
		if (complexType.Redefined != null)
		{
			CleanupComplexType(complexType.Redefined as XmlSchemaComplexType);
		}
	}

	private void CleanupSimpleType(XmlSchemaSimpleType simpleType)
	{
		if (simpleType != XmlSchemaType.GetBuiltInSimpleType(simpleType.TypeCode))
		{
			simpleType.ElementDecl = null;
			if (simpleType.Redefined != null)
			{
				CleanupSimpleType(simpleType.Redefined as XmlSchemaSimpleType);
			}
		}
	}

	private void CleanupElement(XmlSchemaElement element)
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
		element.IsLocalTypeDerivationChecked = false;
	}

	private void CleanupAttributes(XmlSchemaObjectCollection attributes)
	{
		for (int i = 0; i < attributes.Count; i++)
		{
			if (attributes[i] is XmlSchemaAttribute attribute)
			{
				CleanupAttribute(attribute);
			}
		}
	}

	private void CleanupGroup(XmlSchemaGroup group)
	{
		CleanupParticle(group.Particle);
		group.CanonicalParticle = null;
		if (group.Redefined != null)
		{
			CleanupGroup(group.Redefined);
		}
	}

	private void CleanupParticle(XmlSchemaParticle particle)
	{
		if (particle is XmlSchemaElement element)
		{
			CleanupElement(element);
		}
		else if (particle is XmlSchemaGroupBase xmlSchemaGroupBase)
		{
			for (int i = 0; i < xmlSchemaGroupBase.Items.Count; i++)
			{
				CleanupParticle((XmlSchemaParticle)xmlSchemaGroupBase.Items[i]);
			}
		}
	}

	private void ProcessSubstitutionGroups()
	{
		foreach (XmlSchemaElement value in _elements.Values)
		{
			if (value.SubstitutionGroup.IsEmpty)
			{
				continue;
			}
			if (!(_elements[value.SubstitutionGroup] is XmlSchemaElement xmlSchemaElement2))
			{
				SendValidationEvent(System.SR.Sch_NoExamplar, value);
				continue;
			}
			if (!XmlSchemaType.IsDerivedFrom(value.ElementSchemaType, xmlSchemaElement2.ElementSchemaType, xmlSchemaElement2.FinalResolved))
			{
				SendValidationEvent(System.SR.Sch_InvalidSubstitutionMember, value.QualifiedName.ToString(), xmlSchemaElement2.QualifiedName.ToString(), value);
			}
			XmlSchemaSubstitutionGroup xmlSchemaSubstitutionGroup = (XmlSchemaSubstitutionGroup)_examplars[value.SubstitutionGroup];
			if (xmlSchemaSubstitutionGroup == null)
			{
				xmlSchemaSubstitutionGroup = new XmlSchemaSubstitutionGroup();
				xmlSchemaSubstitutionGroup.Examplar = value.SubstitutionGroup;
				_examplars.Add(value.SubstitutionGroup, xmlSchemaSubstitutionGroup);
			}
			ArrayList members = xmlSchemaSubstitutionGroup.Members;
			if (!members.Contains(value))
			{
				members.Add(value);
			}
		}
		foreach (XmlSchemaSubstitutionGroup value2 in _examplars.Values)
		{
			CompileSubstitutionGroup(value2);
		}
	}

	private void CompileSubstitutionGroup(XmlSchemaSubstitutionGroup substitutionGroup)
	{
		if (substitutionGroup.IsProcessing && substitutionGroup.Members.Count > 0)
		{
			SendValidationEvent(System.SR.Sch_SubstitutionCircularRef, (XmlSchemaElement)substitutionGroup.Members[0]);
			return;
		}
		XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)_elements[substitutionGroup.Examplar];
		if (substitutionGroup.Members.Contains(xmlSchemaElement))
		{
			return;
		}
		substitutionGroup.IsProcessing = true;
		try
		{
			if (xmlSchemaElement.FinalResolved == XmlSchemaDerivationMethod.All)
			{
				SendValidationEvent(System.SR.Sch_InvalidExamplar, xmlSchemaElement.Name, xmlSchemaElement);
			}
			ArrayList arrayList = null;
			for (int i = 0; i < substitutionGroup.Members.Count; i++)
			{
				XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)substitutionGroup.Members[i];
				if ((xmlSchemaElement2.ElementDecl.Block & XmlSchemaDerivationMethod.Substitution) != 0)
				{
					continue;
				}
				XmlSchemaSubstitutionGroup xmlSchemaSubstitutionGroup = (XmlSchemaSubstitutionGroup)_examplars[xmlSchemaElement2.QualifiedName];
				if (xmlSchemaSubstitutionGroup == null)
				{
					continue;
				}
				CompileSubstitutionGroup(xmlSchemaSubstitutionGroup);
				for (int j = 0; j < xmlSchemaSubstitutionGroup.Members.Count; j++)
				{
					if (xmlSchemaSubstitutionGroup.Members[j] != xmlSchemaElement2)
					{
						if (arrayList == null)
						{
							arrayList = new ArrayList();
						}
						arrayList.Add(xmlSchemaSubstitutionGroup.Members[j]);
					}
				}
			}
			if (arrayList != null)
			{
				for (int k = 0; k < arrayList.Count; k++)
				{
					substitutionGroup.Members.Add(arrayList[k]);
				}
			}
			substitutionGroup.Members.Add(xmlSchemaElement);
		}
		finally
		{
			substitutionGroup.IsProcessing = false;
		}
	}

	private void RecursivelyCheckRedefinedGroups(XmlSchemaGroup redefinedGroup, XmlSchemaGroup baseGroup)
	{
		if (baseGroup.Redefined != null)
		{
			RecursivelyCheckRedefinedGroups(baseGroup, baseGroup.Redefined);
		}
		if (redefinedGroup.SelfReferenceCount == 0)
		{
			if (baseGroup.CanonicalParticle == null)
			{
				baseGroup.CanonicalParticle = CannonicalizeParticle(baseGroup.Particle, root: true);
			}
			if (redefinedGroup.CanonicalParticle == null)
			{
				redefinedGroup.CanonicalParticle = CannonicalizeParticle(redefinedGroup.Particle, root: true);
			}
			CompileParticleElements(redefinedGroup.CanonicalParticle);
			CompileParticleElements(baseGroup.CanonicalParticle);
			CheckParticleDerivation(redefinedGroup.CanonicalParticle, baseGroup.CanonicalParticle);
		}
	}

	private void RecursivelyCheckRedefinedAttributeGroups(XmlSchemaAttributeGroup attributeGroup, XmlSchemaAttributeGroup baseAttributeGroup)
	{
		if (baseAttributeGroup.Redefined != null)
		{
			RecursivelyCheckRedefinedAttributeGroups(baseAttributeGroup, baseAttributeGroup.Redefined);
		}
		if (attributeGroup.SelfReferenceCount == 0)
		{
			CompileAttributeGroup(baseAttributeGroup);
			CompileAttributeGroup(attributeGroup);
			CheckAtrributeGroupRestriction(baseAttributeGroup, attributeGroup);
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
			group.CanonicalParticle = CannonicalizeParticle(group.Particle, root: true);
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
						throw new XmlSchemaException(System.SR.Sch_UndeclaredSimpleType, xmlSchemaSimpleTypeList.ItemTypeName.ToString(), xmlSchemaSimpleTypeList);
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
						XmlSchema parentSchema = Preprocessor.GetParentSchema(simpleType);
						if (parentSchema.TargetNamespace != "http://www.w3.org/2001/XMLSchema")
						{
							throw new XmlSchemaException(System.SR.Sch_InvalidSimpleTypeRestriction, xmlSchemaSimpleTypeRestriction.BaseTypeName.ToString(), simpleType);
						}
					}
					XmlSchemaSimpleType simpleType3 = GetSimpleType(xmlSchemaSimpleTypeRestriction.BaseTypeName);
					if (simpleType3 == null)
					{
						throw new XmlSchemaException(System.SR.Sch_UndeclaredSimpleType, xmlSchemaSimpleTypeRestriction.BaseTypeName.ToString(), xmlSchemaSimpleTypeRestriction);
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
				throw new XmlSchemaException(System.SR.Sch_UndeclaredSimpleType, memberTypes[i].ToString(), xmlSchemaSimpleTypeUnion);
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
		try
		{
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
				complexType.SetContentTypeParticle(CompileContentTypeParticle(complexType.Particle));
				complexType.SetContentType(GetSchemaContentType(complexType, null, complexType.ContentTypeParticle));
			}
			if (complexType.ContainsIdAttribute(findAll: true))
			{
				SendValidationEvent(System.SR.Sch_TwoIdAttrUses, complexType);
			}
			SchemaElementDecl schemaElementDecl = new SchemaElementDecl();
			schemaElementDecl.ContentValidator = CompileComplexContent(complexType);
			schemaElementDecl.SchemaType = complexType;
			schemaElementDecl.IsAbstract = complexType.IsAbstract;
			schemaElementDecl.Datatype = complexType.Datatype;
			schemaElementDecl.Block = complexType.BlockResolved;
			schemaElementDecl.AnyAttribute = complexType.AttributeWildcard;
			foreach (XmlSchemaAttribute value in complexType.AttributeUses.Values)
			{
				if (value.Use == XmlSchemaUse.Prohibited)
				{
					if (!schemaElementDecl.ProhibitedAttributes.ContainsKey(value.QualifiedName))
					{
						schemaElementDecl.ProhibitedAttributes.Add(value.QualifiedName, value.QualifiedName);
					}
				}
				else if (!schemaElementDecl.AttDefs.ContainsKey(value.QualifiedName) && value.AttDef != null && value.AttDef.Name != XmlQualifiedName.Empty && value.AttDef != SchemaAttDef.Empty)
				{
					schemaElementDecl.AddAttDef(value.AttDef);
				}
			}
			complexType.ElementDecl = schemaElementDecl;
		}
		finally
		{
			complexType.IsProcessing = false;
		}
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
				SendValidationEvent(System.SR.Sch_UndeclaredType, simpleExtension.BaseTypeName.ToString(), simpleExtension);
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
		if ((xmlSchemaComplexType.FinalResolved & XmlSchemaDerivationMethod.Extension) != 0)
		{
			SendValidationEvent(System.SR.Sch_BaseFinalExtension, complexType);
		}
		CompileLocalAttributes(xmlSchemaComplexType, complexType, complexExtension.Attributes, complexExtension.AnyAttribute, XmlSchemaDerivationMethod.Extension);
		XmlSchemaParticle contentTypeParticle = xmlSchemaComplexType.ContentTypeParticle;
		XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle(complexExtension.Particle, root: true);
		if (contentTypeParticle != XmlSchemaParticle.Empty)
		{
			if (xmlSchemaParticle != XmlSchemaParticle.Empty)
			{
				XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
				xmlSchemaSequence.Items.Add(contentTypeParticle);
				xmlSchemaSequence.Items.Add(xmlSchemaParticle);
				complexType.SetContentTypeParticle(CompileContentTypeParticle(xmlSchemaSequence));
			}
			else
			{
				complexType.SetContentTypeParticle(contentTypeParticle);
			}
		}
		else
		{
			complexType.SetContentTypeParticle(xmlSchemaParticle);
		}
		XmlSchemaContentType xmlSchemaContentType = GetSchemaContentType(complexType, complexContent, xmlSchemaParticle);
		if (xmlSchemaContentType == XmlSchemaContentType.Empty)
		{
			xmlSchemaContentType = xmlSchemaComplexType.ContentType;
			if (xmlSchemaContentType == XmlSchemaContentType.TextOnly)
			{
				complexType.SetDatatype(xmlSchemaComplexType.Datatype);
			}
		}
		complexType.SetContentType(xmlSchemaContentType);
		if (xmlSchemaComplexType.ContentType != XmlSchemaContentType.Empty && complexType.ContentType != xmlSchemaComplexType.ContentType)
		{
			SendValidationEvent(System.SR.Sch_DifContentType, complexType);
			return;
		}
		complexType.SetBaseSchemaType(xmlSchemaComplexType);
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
		complexType.SetBaseSchemaType(xmlSchemaComplexType);
		if ((xmlSchemaComplexType.FinalResolved & XmlSchemaDerivationMethod.Restriction) != 0)
		{
			SendValidationEvent(System.SR.Sch_BaseFinalRestriction, complexType);
		}
		CompileLocalAttributes(xmlSchemaComplexType, complexType, complexRestriction.Attributes, complexRestriction.AnyAttribute, XmlSchemaDerivationMethod.Restriction);
		complexType.SetContentTypeParticle(CompileContentTypeParticle(complexRestriction.Particle));
		XmlSchemaContentType schemaContentType = GetSchemaContentType(complexType, complexContent, complexType.ContentTypeParticle);
		complexType.SetContentType(schemaContentType);
		switch (schemaContentType)
		{
		case XmlSchemaContentType.Empty:
			if (xmlSchemaComplexType.ElementDecl != null && !xmlSchemaComplexType.ElementDecl.ContentValidator.IsEmptiable)
			{
				SendValidationEvent(System.SR.Sch_InvalidContentRestrictionDetailed, System.SR.Sch_InvalidBaseToEmpty, complexType);
			}
			break;
		case XmlSchemaContentType.Mixed:
			if (xmlSchemaComplexType.ContentType != XmlSchemaContentType.Mixed)
			{
				SendValidationEvent(System.SR.Sch_InvalidContentRestrictionDetailed, System.SR.Sch_InvalidBaseToMixed, complexType);
			}
			break;
		}
		complexType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
	}

	private void CheckParticleDerivation(XmlSchemaComplexType complexType)
	{
		XmlSchemaComplexType xmlSchemaComplexType = complexType.BaseXmlSchemaType as XmlSchemaComplexType;
		_restrictionErrorMsg = null;
		if (xmlSchemaComplexType != null && xmlSchemaComplexType != XmlSchemaComplexType.AnyType && complexType.DerivedBy == XmlSchemaDerivationMethod.Restriction)
		{
			XmlSchemaParticle derivedParticle = CannonicalizePointlessRoot(complexType.ContentTypeParticle);
			XmlSchemaParticle baseParticle = CannonicalizePointlessRoot(xmlSchemaComplexType.ContentTypeParticle);
			if (!IsValidRestriction(derivedParticle, baseParticle))
			{
				if (_restrictionErrorMsg != null)
				{
					SendValidationEvent(System.SR.Sch_InvalidParticleRestrictionDetailed, _restrictionErrorMsg, complexType);
				}
				else
				{
					SendValidationEvent(System.SR.Sch_InvalidParticleRestriction, complexType);
				}
			}
		}
		else
		{
			if (xmlSchemaComplexType != XmlSchemaComplexType.AnyType)
			{
				return;
			}
			foreach (XmlSchemaElement value in complexType.LocalElements.Values)
			{
				if (!value.IsLocalTypeDerivationChecked && value.ElementSchemaType is XmlSchemaComplexType complexType2 && value.SchemaTypeName == XmlQualifiedName.Empty && value.RefName == XmlQualifiedName.Empty)
				{
					value.IsLocalTypeDerivationChecked = true;
					CheckParticleDerivation(complexType2);
				}
			}
		}
	}

	private void CheckParticleDerivation(XmlSchemaParticle derivedParticle, XmlSchemaParticle baseParticle)
	{
		_restrictionErrorMsg = null;
		derivedParticle = CannonicalizePointlessRoot(derivedParticle);
		baseParticle = CannonicalizePointlessRoot(baseParticle);
		if (!IsValidRestriction(derivedParticle, baseParticle))
		{
			if (_restrictionErrorMsg != null)
			{
				SendValidationEvent(System.SR.Sch_InvalidParticleRestrictionDetailed, _restrictionErrorMsg, derivedParticle);
			}
			else
			{
				SendValidationEvent(System.SR.Sch_InvalidParticleRestriction, derivedParticle);
			}
		}
	}

	private XmlSchemaParticle CompileContentTypeParticle(XmlSchemaParticle particle)
	{
		XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle(particle, root: true);
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

	private XmlSchemaParticle CannonicalizeParticle(XmlSchemaParticle particle, bool root)
	{
		if (particle == null || particle.IsEmpty)
		{
			return XmlSchemaParticle.Empty;
		}
		if (particle is XmlSchemaElement)
		{
			return particle;
		}
		if (particle is XmlSchemaGroupRef)
		{
			return CannonicalizeGroupRef((XmlSchemaGroupRef)particle, root);
		}
		if (particle is XmlSchemaAll)
		{
			return CannonicalizeAll((XmlSchemaAll)particle, root);
		}
		if (particle is XmlSchemaChoice)
		{
			return CannonicalizeChoice((XmlSchemaChoice)particle, root);
		}
		if (particle is XmlSchemaSequence)
		{
			return CannonicalizeSequence((XmlSchemaSequence)particle, root);
		}
		return particle;
	}

	private XmlSchemaParticle CannonicalizeElement(XmlSchemaElement element)
	{
		if (!element.RefName.IsEmpty && (element.ElementDecl.Block & XmlSchemaDerivationMethod.Substitution) == 0)
		{
			XmlSchemaSubstitutionGroup xmlSchemaSubstitutionGroup = (XmlSchemaSubstitutionGroup)_examplars[element.QualifiedName];
			if (xmlSchemaSubstitutionGroup == null)
			{
				return element;
			}
			XmlSchemaChoice xmlSchemaChoice = new XmlSchemaChoice();
			for (int i = 0; i < xmlSchemaSubstitutionGroup.Members.Count; i++)
			{
				xmlSchemaChoice.Items.Add((XmlSchemaElement)xmlSchemaSubstitutionGroup.Members[i]);
			}
			xmlSchemaChoice.MinOccurs = element.MinOccurs;
			xmlSchemaChoice.MaxOccurs = element.MaxOccurs;
			CopyPosition(xmlSchemaChoice, element, copyParent: false);
			return xmlSchemaChoice;
		}
		return element;
	}

	private XmlSchemaParticle CannonicalizeGroupRef(XmlSchemaGroupRef groupRef, bool root)
	{
		XmlSchemaGroup xmlSchemaGroup = ((groupRef.Redefined == null) ? ((XmlSchemaGroup)_groups[groupRef.RefName]) : groupRef.Redefined);
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
			if (groupRef.MinOccurs > 1m || groupRef.MaxOccurs != 1m)
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
		CopyPosition(xmlSchemaGroupBase2, groupRef, copyParent: true);
		for (int i = 0; i < xmlSchemaGroupBase.Items.Count; i++)
		{
			xmlSchemaGroupBase2.Items.Add(xmlSchemaGroupBase.Items[i]);
		}
		groupRef.SetParticle(xmlSchemaGroupBase2);
		return xmlSchemaGroupBase2;
	}

	private XmlSchemaParticle CannonicalizeAll(XmlSchemaAll all, bool root)
	{
		if (all.Items.Count > 0)
		{
			XmlSchemaAll xmlSchemaAll = new XmlSchemaAll();
			xmlSchemaAll.MinOccurs = all.MinOccurs;
			xmlSchemaAll.MaxOccurs = all.MaxOccurs;
			CopyPosition(xmlSchemaAll, all, copyParent: true);
			for (int i = 0; i < all.Items.Count; i++)
			{
				XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle((XmlSchemaElement)all.Items[i], root: false);
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
		if (!root)
		{
			SendValidationEvent(System.SR.Sch_NotAllAlone, all);
			return XmlSchemaParticle.Empty;
		}
		return all;
	}

	private XmlSchemaParticle CannonicalizeChoice(XmlSchemaChoice choice, bool root)
	{
		XmlSchemaChoice source = choice;
		if (choice.Items.Count > 0)
		{
			XmlSchemaChoice xmlSchemaChoice = new XmlSchemaChoice();
			xmlSchemaChoice.MinOccurs = choice.MinOccurs;
			xmlSchemaChoice.MaxOccurs = choice.MaxOccurs;
			CopyPosition(xmlSchemaChoice, choice, copyParent: true);
			for (int i = 0; i < choice.Items.Count; i++)
			{
				XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle((XmlSchemaParticle)choice.Items[i], root: false);
				if (xmlSchemaParticle == XmlSchemaParticle.Empty)
				{
					continue;
				}
				if (xmlSchemaParticle.MinOccurs == 1m && xmlSchemaParticle.MaxOccurs == 1m && xmlSchemaParticle is XmlSchemaChoice)
				{
					XmlSchemaChoice xmlSchemaChoice2 = xmlSchemaParticle as XmlSchemaChoice;
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

	private XmlSchemaParticle CannonicalizeSequence(XmlSchemaSequence sequence, bool root)
	{
		if (sequence.Items.Count > 0)
		{
			XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
			xmlSchemaSequence.MinOccurs = sequence.MinOccurs;
			xmlSchemaSequence.MaxOccurs = sequence.MaxOccurs;
			CopyPosition(xmlSchemaSequence, sequence, copyParent: true);
			for (int i = 0; i < sequence.Items.Count; i++)
			{
				XmlSchemaParticle xmlSchemaParticle = CannonicalizeParticle((XmlSchemaParticle)sequence.Items[i], root: false);
				if (xmlSchemaParticle == XmlSchemaParticle.Empty)
				{
					continue;
				}
				XmlSchemaSequence xmlSchemaSequence2 = xmlSchemaParticle as XmlSchemaSequence;
				if (xmlSchemaParticle.MinOccurs == 1m && xmlSchemaParticle.MaxOccurs == 1m && xmlSchemaSequence2 != null)
				{
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

	[return: NotNullIfNotNull("particle")]
	private XmlSchemaParticle CannonicalizePointlessRoot(XmlSchemaParticle particle)
	{
		if (particle == null)
		{
			return null;
		}
		decimal num = 1m;
		if (particle is XmlSchemaSequence xmlSchemaSequence)
		{
			XmlSchemaObjectCollection items = xmlSchemaSequence.Items;
			int count = items.Count;
			if (count == 1 && xmlSchemaSequence.MinOccurs == num && xmlSchemaSequence.MaxOccurs == num)
			{
				return (XmlSchemaParticle)items[0];
			}
		}
		else if (particle is XmlSchemaChoice xmlSchemaChoice)
		{
			XmlSchemaObjectCollection items2 = xmlSchemaChoice.Items;
			switch (items2.Count)
			{
			case 1:
				if (xmlSchemaChoice.MinOccurs == num && xmlSchemaChoice.MaxOccurs == num)
				{
					return (XmlSchemaParticle)items2[0];
				}
				break;
			case 0:
				return XmlSchemaParticle.Empty;
			}
		}
		else if (particle is XmlSchemaAll xmlSchemaAll)
		{
			XmlSchemaObjectCollection items3 = xmlSchemaAll.Items;
			int count2 = items3.Count;
			if (count2 == 1 && xmlSchemaAll.MinOccurs == num && xmlSchemaAll.MaxOccurs == num)
			{
				return (XmlSchemaParticle)items3[0];
			}
		}
		return particle;
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
		if (derivedParticle is XmlSchemaElement)
		{
			XmlSchemaElement element = (XmlSchemaElement)derivedParticle;
			derivedParticle = CannonicalizeElement(element);
		}
		if (baseParticle is XmlSchemaElement)
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)baseParticle;
			XmlSchemaParticle xmlSchemaParticle = CannonicalizeElement(xmlSchemaElement);
			if (xmlSchemaParticle is XmlSchemaChoice)
			{
				return IsValidRestriction(derivedParticle, xmlSchemaParticle);
			}
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromElement((XmlSchemaElement)derivedParticle, xmlSchemaElement);
			}
			_restrictionErrorMsg = System.SR.Sch_ForbiddenDerivedParticleForElem;
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
				return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle);
			}
			if (derivedParticle is XmlSchemaAll)
			{
				if (IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: true))
				{
					return true;
				}
			}
			else if (derivedParticle is XmlSchemaSequence)
			{
				if (IsSequenceFromAll((XmlSchemaSequence)derivedParticle, (XmlSchemaAll)baseParticle))
				{
					return true;
				}
				_restrictionErrorMsg = System.SR.Format(System.SR.Sch_SeqFromAll, derivedParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
			}
			else if (derivedParticle is XmlSchemaChoice || derivedParticle is XmlSchemaAny)
			{
				_restrictionErrorMsg = System.SR.Sch_ForbiddenDerivedParticleForAll;
			}
			return false;
		}
		if (baseParticle is XmlSchemaChoice)
		{
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle);
			}
			if (derivedParticle is XmlSchemaChoice)
			{
				XmlSchemaChoice xmlSchemaChoice = baseParticle as XmlSchemaChoice;
				XmlSchemaChoice xmlSchemaChoice2 = derivedParticle as XmlSchemaChoice;
				if (xmlSchemaChoice.Parent == null || xmlSchemaChoice2.Parent == null)
				{
					return IsChoiceFromChoiceSubstGroup(xmlSchemaChoice2, xmlSchemaChoice);
				}
				if (IsGroupBaseFromGroupBase(xmlSchemaChoice2, xmlSchemaChoice, skipEmptableOnly: false))
				{
					return true;
				}
			}
			else if (derivedParticle is XmlSchemaSequence)
			{
				if (IsSequenceFromChoice((XmlSchemaSequence)derivedParticle, (XmlSchemaChoice)baseParticle))
				{
					return true;
				}
				_restrictionErrorMsg = System.SR.Format(System.SR.Sch_SeqFromChoice, derivedParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseParticle.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
			}
			else
			{
				_restrictionErrorMsg = System.SR.Sch_ForbiddenDerivedParticleForChoice;
			}
			return false;
		}
		if (baseParticle is XmlSchemaSequence)
		{
			if (derivedParticle is XmlSchemaElement)
			{
				return IsElementFromGroupBase((XmlSchemaElement)derivedParticle, (XmlSchemaGroupBase)baseParticle);
			}
			if (derivedParticle is XmlSchemaSequence || (derivedParticle is XmlSchemaAll && ((XmlSchemaGroupBase)derivedParticle).Items.Count == 1))
			{
				if (IsGroupBaseFromGroupBase((XmlSchemaGroupBase)derivedParticle, (XmlSchemaGroupBase)baseParticle, skipEmptableOnly: true))
				{
					return true;
				}
			}
			else
			{
				_restrictionErrorMsg = System.SR.Sch_ForbiddenDerivedParticleForSeq;
			}
			return false;
		}
		return false;
	}

	private bool IsElementFromElement(XmlSchemaElement derivedElement, XmlSchemaElement baseElement)
	{
		XmlSchemaDerivationMethod xmlSchemaDerivationMethod = ((baseElement.ElementDecl.Block == XmlSchemaDerivationMethod.All) ? (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction) : baseElement.ElementDecl.Block);
		XmlSchemaDerivationMethod xmlSchemaDerivationMethod2 = ((derivedElement.ElementDecl.Block == XmlSchemaDerivationMethod.All) ? (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction) : derivedElement.ElementDecl.Block);
		if (!(derivedElement.QualifiedName == baseElement.QualifiedName) || (!baseElement.IsNillable && derivedElement.IsNillable) || !IsValidOccurrenceRangeRestriction(derivedElement, baseElement) || (baseElement.FixedValue != null && !IsFixedEqual(baseElement.ElementDecl, derivedElement.ElementDecl)) || (xmlSchemaDerivationMethod2 | xmlSchemaDerivationMethod) != xmlSchemaDerivationMethod2 || derivedElement.ElementSchemaType == null || baseElement.ElementSchemaType == null || !XmlSchemaType.IsDerivedFrom(derivedElement.ElementSchemaType, baseElement.ElementSchemaType, ~(XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union)))
		{
			_restrictionErrorMsg = System.SR.Format(System.SR.Sch_ElementFromElement, derivedElement.QualifiedName, baseElement.QualifiedName);
			return false;
		}
		return true;
	}

	private bool IsElementFromAny(XmlSchemaElement derivedElement, XmlSchemaAny baseAny)
	{
		if (!baseAny.Allows(derivedElement.QualifiedName))
		{
			_restrictionErrorMsg = System.SR.Format(System.SR.Sch_ElementFromAnyRule1, derivedElement.QualifiedName.ToString());
			return false;
		}
		if (!IsValidOccurrenceRangeRestriction(derivedElement, baseAny))
		{
			_restrictionErrorMsg = System.SR.Format(System.SR.Sch_ElementFromAnyRule2, derivedElement.QualifiedName.ToString());
			return false;
		}
		return true;
	}

	private bool IsAnyFromAny(XmlSchemaAny derivedAny, XmlSchemaAny baseAny)
	{
		if (!IsValidOccurrenceRangeRestriction(derivedAny, baseAny))
		{
			_restrictionErrorMsg = System.SR.Sch_AnyFromAnyRule1;
			return false;
		}
		if (!NamespaceList.IsSubset(derivedAny.NamespaceList, baseAny.NamespaceList))
		{
			_restrictionErrorMsg = System.SR.Sch_AnyFromAnyRule2;
			return false;
		}
		if (derivedAny.ProcessContentsCorrect < baseAny.ProcessContentsCorrect)
		{
			_restrictionErrorMsg = System.SR.Sch_AnyFromAnyRule3;
			return false;
		}
		return true;
	}

	private bool IsGroupBaseFromAny(XmlSchemaGroupBase derivedGroupBase, XmlSchemaAny baseAny)
	{
		CalculateEffectiveTotalRange(derivedGroupBase, out var minOccurs, out var maxOccurs);
		if (!IsValidOccurrenceRangeRestriction(minOccurs, maxOccurs, baseAny.MinOccurs, baseAny.MaxOccurs))
		{
			_restrictionErrorMsg = System.SR.Format(System.SR.Sch_GroupBaseFromAny2, derivedGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseAny.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseAny.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
			return false;
		}
		string minOccursString = baseAny.MinOccursString;
		baseAny.MinOccurs = 0m;
		for (int i = 0; i < derivedGroupBase.Items.Count; i++)
		{
			if (!IsValidRestriction((XmlSchemaParticle)derivedGroupBase.Items[i], baseAny))
			{
				_restrictionErrorMsg = System.SR.Sch_GroupBaseFromAny1;
				baseAny.MinOccursString = minOccursString;
				return false;
			}
		}
		baseAny.MinOccursString = minOccursString;
		return true;
	}

	private bool IsElementFromGroupBase(XmlSchemaElement derivedElement, XmlSchemaGroupBase baseGroupBase)
	{
		if (baseGroupBase is XmlSchemaSequence)
		{
			XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
			xmlSchemaSequence.MinOccurs = 1m;
			xmlSchemaSequence.MaxOccurs = 1m;
			xmlSchemaSequence.Items.Add(derivedElement);
			if (IsGroupBaseFromGroupBase(xmlSchemaSequence, baseGroupBase, skipEmptableOnly: true))
			{
				return true;
			}
			_restrictionErrorMsg = System.SR.Format(System.SR.Sch_ElementFromGroupBase1, derivedElement.QualifiedName.ToString(), derivedElement.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedElement.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
		}
		else if (baseGroupBase is XmlSchemaChoice)
		{
			XmlSchemaChoice xmlSchemaChoice = new XmlSchemaChoice();
			xmlSchemaChoice.MinOccurs = 1m;
			xmlSchemaChoice.MaxOccurs = 1m;
			xmlSchemaChoice.Items.Add(derivedElement);
			if (IsGroupBaseFromGroupBase(xmlSchemaChoice, baseGroupBase, skipEmptableOnly: false))
			{
				return true;
			}
			_restrictionErrorMsg = System.SR.Format(System.SR.Sch_ElementFromGroupBase2, derivedElement.QualifiedName.ToString(), derivedElement.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedElement.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
		}
		else if (baseGroupBase is XmlSchemaAll)
		{
			XmlSchemaAll xmlSchemaAll = new XmlSchemaAll();
			xmlSchemaAll.MinOccurs = 1m;
			xmlSchemaAll.MaxOccurs = 1m;
			xmlSchemaAll.Items.Add(derivedElement);
			if (IsGroupBaseFromGroupBase(xmlSchemaAll, baseGroupBase, skipEmptableOnly: true))
			{
				return true;
			}
			_restrictionErrorMsg = System.SR.Format(System.SR.Sch_ElementFromGroupBase3, derivedElement.QualifiedName.ToString(), derivedElement.LineNumber.ToString(NumberFormatInfo.InvariantInfo), derivedElement.LinePosition.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LineNumber.ToString(NumberFormatInfo.InvariantInfo), baseGroupBase.LinePosition.ToString(NumberFormatInfo.InvariantInfo));
		}
		return false;
	}

	private bool IsChoiceFromChoiceSubstGroup(XmlSchemaChoice derivedChoice, XmlSchemaChoice baseChoice)
	{
		if (!IsValidOccurrenceRangeRestriction(derivedChoice, baseChoice))
		{
			_restrictionErrorMsg = System.SR.Sch_GroupBaseRestRangeInvalid;
			return false;
		}
		for (int i = 0; i < derivedChoice.Items.Count; i++)
		{
			if (GetMappingParticle((XmlSchemaParticle)derivedChoice.Items[i], baseChoice.Items) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsGroupBaseFromGroupBase(XmlSchemaGroupBase derivedGroupBase, XmlSchemaGroupBase baseGroupBase, bool skipEmptableOnly)
	{
		if (!IsValidOccurrenceRangeRestriction(derivedGroupBase, baseGroupBase))
		{
			_restrictionErrorMsg = System.SR.Sch_GroupBaseRestRangeInvalid;
			return false;
		}
		if (derivedGroupBase.Items.Count > baseGroupBase.Items.Count)
		{
			_restrictionErrorMsg = System.SR.Sch_GroupBaseRestNoMap;
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
				if (_restrictionErrorMsg == null)
				{
					_restrictionErrorMsg = System.SR.Sch_GroupBaseRestNotEmptiable;
				}
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
		decimal minOccurs = derivedSequence.MinOccurs * (decimal)derivedSequence.Items.Count;
		decimal maxOccurs = ((!(derivedSequence.MaxOccurs == decimal.MaxValue)) ? (derivedSequence.MaxOccurs * (decimal)derivedSequence.Items.Count) : decimal.MaxValue);
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
		XmlSchemaChoice xmlSchemaChoice = particle as XmlSchemaChoice;
		if (particle is XmlSchemaElement || particle is XmlSchemaAny)
		{
			minOccurs = particle.MinOccurs;
			maxOccurs = particle.MaxOccurs;
			return;
		}
		if (xmlSchemaChoice != null)
		{
			if (xmlSchemaChoice.Items.Count == 0)
			{
				minOccurs = (maxOccurs = 0m);
				return;
			}
			minOccurs = decimal.MaxValue;
			maxOccurs = default(decimal);
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
			try
			{
				for (int i = 0; i < attributeGroup.Attributes.Count; i++)
				{
					if (attributeGroup.Attributes[i] is XmlSchemaAttribute xmlSchemaAttribute)
					{
						if (xmlSchemaAttribute.Use != XmlSchemaUse.Prohibited)
						{
							CompileAttribute(xmlSchemaAttribute);
							if (attributeGroup.AttributeUses[xmlSchemaAttribute.QualifiedName] == null)
							{
								attributeGroup.AttributeUses.Add(xmlSchemaAttribute.QualifiedName, xmlSchemaAttribute);
							}
							else
							{
								SendValidationEvent(System.SR.Sch_DupAttributeUse, xmlSchemaAttribute.QualifiedName.ToString(), xmlSchemaAttribute);
							}
						}
						continue;
					}
					XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef = (XmlSchemaAttributeGroupRef)attributeGroup.Attributes[i];
					XmlSchemaAttributeGroup xmlSchemaAttributeGroup = ((attributeGroup.Redefined == null || !(xmlSchemaAttributeGroupRef.RefName == attributeGroup.Redefined.QualifiedName)) ? ((XmlSchemaAttributeGroup)_attributeGroups[xmlSchemaAttributeGroupRef.RefName]) : attributeGroup.Redefined);
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
			}
			finally
			{
				attributeGroup.IsProcessing = false;
			}
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
			XmlSchemaAttributeGroup xmlSchemaAttributeGroup = (XmlSchemaAttributeGroup)_attributeGroups[xmlSchemaAttributeGroupRef.RefName];
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
						if (xmlSchemaAttribute4 == null)
						{
							derivedType.AttributeUses.Add(value2.QualifiedName, value2);
						}
						else if (value2.Use != XmlSchemaUse.Prohibited && xmlSchemaAttribute4.AttributeSchemaType != value2.AttributeSchemaType)
						{
							SendValidationEvent(System.SR.Sch_InvalidAttributeExtension, xmlSchemaAttribute4);
						}
					}
					return;
				}
			}
			if (anyAttribute != null && (xmlSchemaAnyAttribute == null || !XmlSchemaAnyAttribute.IsSubset(anyAttribute, xmlSchemaAnyAttribute) || !IsProcessContentsRestricted(baseType, anyAttribute, xmlSchemaAnyAttribute)))
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
				else if (value3.Use == XmlSchemaUse.Required && xmlSchemaAttribute6.Use != XmlSchemaUse.Required)
				{
					SendValidationEvent(System.SR.Sch_AttributeUseInvalid, xmlSchemaAttribute6);
				}
				else if (xmlSchemaAttribute6.Use != XmlSchemaUse.Prohibited)
				{
					if (value3.AttributeSchemaType == null || xmlSchemaAttribute6.AttributeSchemaType == null || !XmlSchemaType.IsDerivedFrom(xmlSchemaAttribute6.AttributeSchemaType, value3.AttributeSchemaType, XmlSchemaDerivationMethod.Empty))
					{
						SendValidationEvent(System.SR.Sch_AttributeRestrictionInvalid, xmlSchemaAttribute6);
					}
					else if (!IsFixedEqual(value3.AttDef, xmlSchemaAttribute6.AttDef))
					{
						SendValidationEvent(System.SR.Sch_AttributeFixedInvalid, xmlSchemaAttribute6);
					}
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

	private void CheckAtrributeGroupRestriction(XmlSchemaAttributeGroup baseAttributeGroup, XmlSchemaAttributeGroup derivedAttributeGroup)
	{
		XmlSchemaAnyAttribute attributeWildcard = baseAttributeGroup.AttributeWildcard;
		XmlSchemaAnyAttribute attributeWildcard2 = derivedAttributeGroup.AttributeWildcard;
		if (attributeWildcard2 != null && (attributeWildcard == null || !XmlSchemaAnyAttribute.IsSubset(attributeWildcard2, attributeWildcard) || !IsProcessContentsRestricted(null, attributeWildcard2, attributeWildcard)))
		{
			SendValidationEvent(System.SR.Sch_InvalidAnyAttributeRestriction, derivedAttributeGroup);
		}
		foreach (XmlSchemaAttribute value in baseAttributeGroup.AttributeUses.Values)
		{
			XmlSchemaAttribute xmlSchemaAttribute2 = (XmlSchemaAttribute)derivedAttributeGroup.AttributeUses[value.QualifiedName];
			if (xmlSchemaAttribute2 != null)
			{
				if (value.Use == XmlSchemaUse.Prohibited && xmlSchemaAttribute2.Use != XmlSchemaUse.Prohibited)
				{
					SendValidationEvent(System.SR.Sch_AttributeRestrictionProhibited, xmlSchemaAttribute2);
				}
				else if (value.Use == XmlSchemaUse.Required && xmlSchemaAttribute2.Use != XmlSchemaUse.Required)
				{
					SendValidationEvent(System.SR.Sch_AttributeUseInvalid, xmlSchemaAttribute2);
				}
				else if (xmlSchemaAttribute2.Use != XmlSchemaUse.Prohibited)
				{
					if (value.AttributeSchemaType == null || xmlSchemaAttribute2.AttributeSchemaType == null || !XmlSchemaType.IsDerivedFrom(xmlSchemaAttribute2.AttributeSchemaType, value.AttributeSchemaType, XmlSchemaDerivationMethod.Empty))
					{
						SendValidationEvent(System.SR.Sch_AttributeRestrictionInvalid, xmlSchemaAttribute2);
					}
					else if (!IsFixedEqual(value.AttDef, xmlSchemaAttribute2.AttDef))
					{
						SendValidationEvent(System.SR.Sch_AttributeFixedInvalid, xmlSchemaAttribute2);
					}
				}
			}
			else if (value.Use == XmlSchemaUse.Required)
			{
				SendValidationEvent(System.SR.Sch_NoDerivedAttribute, value.QualifiedName.ToString(), baseAttributeGroup.QualifiedName.ToString(), derivedAttributeGroup);
			}
		}
		foreach (XmlSchemaAttribute value2 in derivedAttributeGroup.AttributeUses.Values)
		{
			XmlSchemaAttribute xmlSchemaAttribute4 = (XmlSchemaAttribute)baseAttributeGroup.AttributeUses[value2.QualifiedName];
			if (xmlSchemaAttribute4 == null && (attributeWildcard == null || !attributeWildcard.Allows(value2.QualifiedName)))
			{
				SendValidationEvent(System.SR.Sch_AttributeRestrictionInvalidFromWildcard, value2);
			}
		}
	}

	private bool IsProcessContentsRestricted(XmlSchemaComplexType baseType, XmlSchemaAnyAttribute derivedAttributeWildcard, XmlSchemaAnyAttribute baseAttributeWildcard)
	{
		if (baseType == XmlSchemaComplexType.AnyType)
		{
			return true;
		}
		if (derivedAttributeWildcard.ProcessContentsCorrect >= baseAttributeWildcard.ProcessContentsCorrect)
		{
			return true;
		}
		return false;
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
		XmlSchemaAnyAttribute xmlSchemaAnyAttribute = XmlSchemaAnyAttribute.Union(a, b, v1Compat: false);
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
		XmlSchemaAnyAttribute xmlSchemaAnyAttribute = XmlSchemaAnyAttribute.Intersection(a, b, v1Compat: false);
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
					XmlSchemaAttribute xmlSchemaAttribute = (XmlSchemaAttribute)_attributes[xa.RefName];
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
					XmlSchemaDatatype datatype = schemaAttDef.Datatype;
					if (datatype != null)
					{
						if (xmlSchemaAttribute.FixedValue == null && xmlSchemaAttribute.DefaultValue == null)
						{
							SetDefaultFixed(xa, schemaAttDef);
						}
						else if (xmlSchemaAttribute.FixedValue != null)
						{
							if (xa.DefaultValue != null)
							{
								throw new XmlSchemaException(System.SR.Sch_FixedDefaultInRef, xa.RefName.ToString(), xa);
							}
							if (xa.FixedValue != null)
							{
								object o = datatype.ParseValue(xa.FixedValue, base.NameTable, new SchemaNamespaceManager(xa), createAtomicValue: true);
								if (!datatype.IsEqual(schemaAttDef.DefaultValueTyped, o))
								{
									throw new XmlSchemaException(System.SR.Sch_FixedInRef, xa.RefName.ToString(), xa);
								}
							}
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
					if (schemaAttDef.Datatype != null)
					{
						schemaAttDef.Datatype.VerifySchemaValid(_notations, xa);
					}
					SetDefaultFixed(xa, schemaAttDef);
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

	private void SetDefaultFixed(XmlSchemaAttribute xa, SchemaAttDef decl)
	{
		if (xa.DefaultValue != null || xa.FixedValue != null)
		{
			if (xa.DefaultValue != null)
			{
				decl.Presence = SchemaDeclBase.Use.Default;
				string defaultValueRaw = (decl.DefaultValueExpanded = xa.DefaultValue);
				decl.DefaultValueRaw = defaultValueRaw;
			}
			else
			{
				if (xa.Use == XmlSchemaUse.Required)
				{
					decl.Presence = SchemaDeclBase.Use.RequiredFixed;
				}
				else
				{
					decl.Presence = SchemaDeclBase.Use.Fixed;
				}
				string defaultValueRaw = (decl.DefaultValueExpanded = xa.FixedValue);
				decl.DefaultValueRaw = defaultValueRaw;
			}
			if (decl.Datatype != null)
			{
				if (decl.Datatype.TypeCode == XmlTypeCode.Id)
				{
					SendValidationEvent(System.SR.Sch_DefaultIdValue, xa);
				}
				else
				{
					decl.DefaultValueTyped = decl.Datatype.ParseValue(decl.DefaultValueRaw, base.NameTable, new SchemaNamespaceManager(xa), createAtomicValue: true);
				}
			}
		}
		else
		{
			switch (xa.Use)
			{
			case XmlSchemaUse.None:
			case XmlSchemaUse.Optional:
				decl.Presence = SchemaDeclBase.Use.Implied;
				break;
			case XmlSchemaUse.Required:
				decl.Presence = SchemaDeclBase.Use.Required;
				break;
			case XmlSchemaUse.Prohibited:
				break;
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
					XmlSchemaIdentityConstraint xmlSchemaIdentityConstraint = (XmlSchemaIdentityConstraint)_identityConstraints[((XmlSchemaKeyref)xi).Refer];
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
					XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)_elements[xe.RefName];
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
						XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)_elements[xe.SubstitutionGroup];
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
					schemaElementDecl.Datatype.VerifySchemaValid(_notations, xe);
				}
				if ((xe.DefaultValue != null || xe.FixedValue != null) && schemaElementDecl.ContentValidator != null)
				{
					if (schemaElementDecl.ContentValidator.ContentType != 0 && (schemaElementDecl.ContentValidator.ContentType != XmlSchemaContentType.Mixed || !schemaElementDecl.ContentValidator.IsEmptiable))
					{
						throw new XmlSchemaException(System.SR.Sch_ElementCannotHaveValue, xe);
					}
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
						if (schemaElementDecl.Datatype.TypeCode == XmlTypeCode.Id)
						{
							SendValidationEvent(System.SR.Sch_DefaultIdValue, xe);
						}
						else
						{
							schemaElementDecl.DefaultValueTyped = schemaElementDecl.Datatype.ParseValue(schemaElementDecl.DefaultValueRaw, base.NameTable, new SchemaNamespaceManager(xe), createAtomicValue: true);
						}
					}
					else
					{
						schemaElementDecl.DefaultValueTyped = DatatypeImplementation.AnySimpleType.Datatype.ParseValue(schemaElementDecl.DefaultValueRaw, base.NameTable, new SchemaNamespaceManager(xe));
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
		ParticleContentValidator particleContentValidator = new ParticleContentValidator(complexType.ContentType, base.CompilationSettings.EnableUpaCheck);
		try
		{
			particleContentValidator.Start();
			complexType.HasWildCard = BuildParticleContentModel(particleContentValidator, contentTypeParticle);
			return particleContentValidator.Finish(useDFA: true);
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
					SendValidationEvent(System.SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)ex.Particle2).ResolvedNamespace, ((XmlSchemaElement)ex.Particle1).QualifiedName.ToString(), (XmlSchemaAny)ex.Particle2);
				}
			}
			else if (ex.Particle2 is XmlSchemaElement)
			{
				SendValidationEvent(System.SR.Sch_NonDeterministicAnyEx, ((XmlSchemaAny)ex.Particle1).ResolvedNamespace, ((XmlSchemaElement)ex.Particle2).QualifiedName.ToString(), (XmlSchemaElement)ex.Particle2);
			}
			else
			{
				SendValidationEvent(System.SR.Sch_NonDeterministicAnyAny, ((XmlSchemaAny)ex.Particle1).ResolvedNamespace, ((XmlSchemaAny)ex.Particle2).ResolvedNamespace, (XmlSchemaAny)ex.Particle2);
			}
			return XmlSchemaComplexType.AnyTypeContentValidator;
		}
		catch (NotSupportedException)
		{
			SendValidationEvent(System.SR.Sch_ComplexContentModel, complexType, XmlSeverityType.Warning);
			return XmlSchemaComplexType.AnyTypeContentValidator;
		}
	}

	private bool BuildParticleContentModel(ParticleContentValidator contentValidator, XmlSchemaParticle particle)
	{
		bool result = false;
		if (particle is XmlSchemaElement)
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)particle;
			contentValidator.AddName(xmlSchemaElement.QualifiedName, xmlSchemaElement);
		}
		else if (particle is XmlSchemaAny)
		{
			result = true;
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
				result = BuildParticleContentModel(contentValidator, (XmlSchemaParticle)items[i]);
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
		return result;
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

	private void CompileParticleElements(XmlSchemaParticle particle)
	{
		if (particle is XmlSchemaElement)
		{
			XmlSchemaElement xe = (XmlSchemaElement)particle;
			CompileElement(xe);
		}
		else if (particle is XmlSchemaGroupBase)
		{
			XmlSchemaObjectCollection items = ((XmlSchemaGroupBase)particle).Items;
			for (int i = 0; i < items.Count; i++)
			{
				CompileParticleElements((XmlSchemaParticle)items[i]);
			}
		}
	}

	private void CompileComplexTypeElements(XmlSchemaComplexType complexType)
	{
		if (complexType.IsProcessing)
		{
			SendValidationEvent(System.SR.Sch_TypeCircularRef, complexType);
			return;
		}
		complexType.IsProcessing = true;
		try
		{
			if (complexType.ContentTypeParticle != XmlSchemaParticle.Empty)
			{
				CompileParticleElements(complexType, complexType.ContentTypeParticle);
			}
		}
		finally
		{
			complexType.IsProcessing = false;
		}
	}

	private XmlSchemaSimpleType GetSimpleType(XmlQualifiedName name)
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = _schemaTypes[name] as XmlSchemaSimpleType;
		if (xmlSchemaSimpleType != null)
		{
			CompileSimpleType(xmlSchemaSimpleType);
		}
		else
		{
			xmlSchemaSimpleType = DatatypeImplementation.GetSimpleTypeFromXsdType(name);
		}
		return xmlSchemaSimpleType;
	}

	private XmlSchemaComplexType GetComplexType(XmlQualifiedName name)
	{
		XmlSchemaComplexType xmlSchemaComplexType = _schemaTypes[name] as XmlSchemaComplexType;
		if (xmlSchemaComplexType != null)
		{
			CompileComplexType(xmlSchemaComplexType);
		}
		return xmlSchemaComplexType;
	}

	private XmlSchemaType GetAnySchemaType(XmlQualifiedName name)
	{
		XmlSchemaType xmlSchemaType = (XmlSchemaType)_schemaTypes[name];
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

	private void CopyPosition(XmlSchemaAnnotated to, XmlSchemaAnnotated from, bool copyParent)
	{
		to.SourceUri = from.SourceUri;
		to.LinePosition = from.LinePosition;
		to.LineNumber = from.LineNumber;
		to.SetUnhandledAttributes(from.UnhandledAttributes);
		if (copyParent)
		{
			to.Parent = from.Parent;
		}
	}

	private bool IsFixedEqual(SchemaDeclBase baseDecl, SchemaDeclBase derivedDecl)
	{
		if (baseDecl.Presence == SchemaDeclBase.Use.Fixed || baseDecl.Presence == SchemaDeclBase.Use.RequiredFixed)
		{
			object defaultValueTyped = baseDecl.DefaultValueTyped;
			if (derivedDecl.Presence != SchemaDeclBase.Use.Fixed && derivedDecl.Presence != SchemaDeclBase.Use.RequiredFixed)
			{
				return false;
			}
			object defaultValueTyped2 = derivedDecl.DefaultValueTyped;
			XmlSchemaDatatype datatype = baseDecl.Datatype;
			XmlSchemaDatatype datatype2 = derivedDecl.Datatype;
			if (datatype.Variety == XmlSchemaDatatypeVariety.Union)
			{
				if (datatype2.Variety == XmlSchemaDatatypeVariety.Union)
				{
					if (!datatype2.IsEqual(defaultValueTyped, defaultValueTyped2))
					{
						return false;
					}
				}
				else
				{
					XsdSimpleValue xsdSimpleValue = baseDecl.DefaultValueTyped as XsdSimpleValue;
					XmlSchemaDatatype datatype3 = xsdSimpleValue.XmlType.Datatype;
					if (!datatype3.IsComparable(datatype2) || !datatype2.IsEqual(xsdSimpleValue.TypedValue, defaultValueTyped2))
					{
						return false;
					}
				}
			}
			else if (!datatype2.IsEqual(defaultValueTyped, defaultValueTyped2))
			{
				return false;
			}
		}
		return true;
	}
}
