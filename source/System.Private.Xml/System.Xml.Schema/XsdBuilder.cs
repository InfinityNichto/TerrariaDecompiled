using System.Collections.Generic;
using System.Xml.Serialization;

namespace System.Xml.Schema;

internal sealed class XsdBuilder : SchemaBuilder
{
	private enum State
	{
		Root,
		Schema,
		Annotation,
		Include,
		Import,
		Element,
		Attribute,
		AttributeGroup,
		AttributeGroupRef,
		AnyAttribute,
		Group,
		GroupRef,
		All,
		Choice,
		Sequence,
		Any,
		Notation,
		SimpleType,
		ComplexType,
		ComplexContent,
		ComplexContentRestriction,
		ComplexContentExtension,
		SimpleContent,
		SimpleContentExtension,
		SimpleContentRestriction,
		SimpleTypeUnion,
		SimpleTypeList,
		SimpleTypeRestriction,
		Unique,
		Key,
		KeyRef,
		Selector,
		Field,
		MinExclusive,
		MinInclusive,
		MaxExclusive,
		MaxInclusive,
		TotalDigits,
		FractionDigits,
		Length,
		MinLength,
		MaxLength,
		Enumeration,
		Pattern,
		WhiteSpace,
		AppInfo,
		Documentation,
		Redefine
	}

	private delegate void XsdBuildFunction(XsdBuilder builder, string value);

	private delegate void XsdInitFunction(XsdBuilder builder, string value);

	private delegate void XsdEndChildFunction(XsdBuilder builder);

	private sealed class XsdAttributeEntry
	{
		public SchemaNames.Token Attribute;

		public XsdBuildFunction BuildFunc;

		public XsdAttributeEntry(SchemaNames.Token a, XsdBuildFunction build)
		{
			Attribute = a;
			BuildFunc = build;
		}
	}

	private sealed class XsdEntry
	{
		public SchemaNames.Token Name;

		public State CurrentState;

		public State[] NextStates;

		public XsdAttributeEntry[] Attributes;

		public XsdInitFunction InitFunc;

		public XsdEndChildFunction EndChildFunc;

		public bool ParseContent;

		public XsdEntry(SchemaNames.Token n, State state, State[] nextStates, XsdAttributeEntry[] attributes, XsdInitFunction init, XsdEndChildFunction end, bool parseContent)
		{
			Name = n;
			CurrentState = state;
			NextStates = nextStates;
			Attributes = attributes;
			InitFunc = init;
			EndChildFunc = end;
			ParseContent = parseContent;
		}
	}

	private sealed class BuilderNamespaceManager : XmlNamespaceManager
	{
		private readonly XmlNamespaceManager _nsMgr;

		private readonly XmlReader _reader;

		public BuilderNamespaceManager(XmlNamespaceManager nsMgr, XmlReader reader)
		{
			_nsMgr = nsMgr;
			_reader = reader;
		}

		public override string LookupNamespace(string prefix)
		{
			string text = _nsMgr.LookupNamespace(prefix);
			if (text == null)
			{
				text = _reader.LookupNamespace(prefix);
			}
			return text;
		}
	}

	private static readonly State[] s_schemaElement = new State[1] { State.Schema };

	private static readonly State[] s_schemaSubelements = new State[11]
	{
		State.Annotation,
		State.Include,
		State.Import,
		State.Redefine,
		State.ComplexType,
		State.SimpleType,
		State.Element,
		State.Attribute,
		State.AttributeGroup,
		State.Group,
		State.Notation
	};

	private static readonly State[] s_attributeSubelements = new State[2]
	{
		State.Annotation,
		State.SimpleType
	};

	private static readonly State[] s_elementSubelements = new State[6]
	{
		State.Annotation,
		State.SimpleType,
		State.ComplexType,
		State.Unique,
		State.Key,
		State.KeyRef
	};

	private static readonly State[] s_complexTypeSubelements = new State[10]
	{
		State.Annotation,
		State.SimpleContent,
		State.ComplexContent,
		State.GroupRef,
		State.All,
		State.Choice,
		State.Sequence,
		State.Attribute,
		State.AttributeGroupRef,
		State.AnyAttribute
	};

	private static readonly State[] s_simpleContentSubelements = new State[3]
	{
		State.Annotation,
		State.SimpleContentRestriction,
		State.SimpleContentExtension
	};

	private static readonly State[] s_simpleContentExtensionSubelements = new State[4]
	{
		State.Annotation,
		State.Attribute,
		State.AttributeGroupRef,
		State.AnyAttribute
	};

	private static readonly State[] s_simpleContentRestrictionSubelements = new State[17]
	{
		State.Annotation,
		State.SimpleType,
		State.Enumeration,
		State.Length,
		State.MaxExclusive,
		State.MaxInclusive,
		State.MaxLength,
		State.MinExclusive,
		State.MinInclusive,
		State.MinLength,
		State.Pattern,
		State.TotalDigits,
		State.FractionDigits,
		State.WhiteSpace,
		State.Attribute,
		State.AttributeGroupRef,
		State.AnyAttribute
	};

	private static readonly State[] s_complexContentSubelements = new State[3]
	{
		State.Annotation,
		State.ComplexContentRestriction,
		State.ComplexContentExtension
	};

	private static readonly State[] s_complexContentExtensionSubelements = new State[8]
	{
		State.Annotation,
		State.GroupRef,
		State.All,
		State.Choice,
		State.Sequence,
		State.Attribute,
		State.AttributeGroupRef,
		State.AnyAttribute
	};

	private static readonly State[] s_complexContentRestrictionSubelements = new State[8]
	{
		State.Annotation,
		State.GroupRef,
		State.All,
		State.Choice,
		State.Sequence,
		State.Attribute,
		State.AttributeGroupRef,
		State.AnyAttribute
	};

	private static readonly State[] s_simpleTypeSubelements = new State[4]
	{
		State.Annotation,
		State.SimpleTypeList,
		State.SimpleTypeRestriction,
		State.SimpleTypeUnion
	};

	private static readonly State[] s_simpleTypeRestrictionSubelements = new State[14]
	{
		State.Annotation,
		State.SimpleType,
		State.Enumeration,
		State.Length,
		State.MaxExclusive,
		State.MaxInclusive,
		State.MaxLength,
		State.MinExclusive,
		State.MinInclusive,
		State.MinLength,
		State.Pattern,
		State.TotalDigits,
		State.FractionDigits,
		State.WhiteSpace
	};

	private static readonly State[] s_simpleTypeListSubelements = new State[2]
	{
		State.Annotation,
		State.SimpleType
	};

	private static readonly State[] s_simpleTypeUnionSubelements = new State[2]
	{
		State.Annotation,
		State.SimpleType
	};

	private static readonly State[] s_redefineSubelements = new State[5]
	{
		State.Annotation,
		State.AttributeGroup,
		State.ComplexType,
		State.Group,
		State.SimpleType
	};

	private static readonly State[] s_attributeGroupSubelements = new State[4]
	{
		State.Annotation,
		State.Attribute,
		State.AttributeGroupRef,
		State.AnyAttribute
	};

	private static readonly State[] s_groupSubelements = new State[4]
	{
		State.Annotation,
		State.All,
		State.Choice,
		State.Sequence
	};

	private static readonly State[] s_allSubelements = new State[2]
	{
		State.Annotation,
		State.Element
	};

	private static readonly State[] s_choiceSequenceSubelements = new State[6]
	{
		State.Annotation,
		State.Element,
		State.GroupRef,
		State.Choice,
		State.Sequence,
		State.Any
	};

	private static readonly State[] s_identityConstraintSubelements = new State[3]
	{
		State.Annotation,
		State.Selector,
		State.Field
	};

	private static readonly State[] s_annotationSubelements = new State[2]
	{
		State.AppInfo,
		State.Documentation
	};

	private static readonly State[] s_annotatedSubelements = new State[1] { State.Annotation };

	private static readonly XsdAttributeEntry[] s_schemaAttributes = new XsdAttributeEntry[7]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaAttributeFormDefault, BuildSchema_AttributeFormDefault),
		new XsdAttributeEntry(SchemaNames.Token.SchemaElementFormDefault, BuildSchema_ElementFormDefault),
		new XsdAttributeEntry(SchemaNames.Token.SchemaTargetNamespace, BuildSchema_TargetNamespace),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaVersion, BuildSchema_Version),
		new XsdAttributeEntry(SchemaNames.Token.SchemaFinalDefault, BuildSchema_FinalDefault),
		new XsdAttributeEntry(SchemaNames.Token.SchemaBlockDefault, BuildSchema_BlockDefault)
	};

	private static readonly XsdAttributeEntry[] s_attributeAttributes = new XsdAttributeEntry[8]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaDefault, BuildAttribute_Default),
		new XsdAttributeEntry(SchemaNames.Token.SchemaFixed, BuildAttribute_Fixed),
		new XsdAttributeEntry(SchemaNames.Token.SchemaForm, BuildAttribute_Form),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildAttribute_Name),
		new XsdAttributeEntry(SchemaNames.Token.SchemaRef, BuildAttribute_Ref),
		new XsdAttributeEntry(SchemaNames.Token.SchemaType, BuildAttribute_Type),
		new XsdAttributeEntry(SchemaNames.Token.SchemaUse, BuildAttribute_Use)
	};

	private static readonly XsdAttributeEntry[] s_elementAttributes = new XsdAttributeEntry[14]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaAbstract, BuildElement_Abstract),
		new XsdAttributeEntry(SchemaNames.Token.SchemaBlock, BuildElement_Block),
		new XsdAttributeEntry(SchemaNames.Token.SchemaDefault, BuildElement_Default),
		new XsdAttributeEntry(SchemaNames.Token.SchemaFinal, BuildElement_Final),
		new XsdAttributeEntry(SchemaNames.Token.SchemaFixed, BuildElement_Fixed),
		new XsdAttributeEntry(SchemaNames.Token.SchemaForm, BuildElement_Form),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, BuildElement_MaxOccurs),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs, BuildElement_MinOccurs),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildElement_Name),
		new XsdAttributeEntry(SchemaNames.Token.SchemaNillable, BuildElement_Nillable),
		new XsdAttributeEntry(SchemaNames.Token.SchemaRef, BuildElement_Ref),
		new XsdAttributeEntry(SchemaNames.Token.SchemaSubstitutionGroup, BuildElement_SubstitutionGroup),
		new XsdAttributeEntry(SchemaNames.Token.SchemaType, BuildElement_Type)
	};

	private static readonly XsdAttributeEntry[] s_complexTypeAttributes = new XsdAttributeEntry[6]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaAbstract, BuildComplexType_Abstract),
		new XsdAttributeEntry(SchemaNames.Token.SchemaBlock, BuildComplexType_Block),
		new XsdAttributeEntry(SchemaNames.Token.SchemaFinal, BuildComplexType_Final),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMixed, BuildComplexType_Mixed),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildComplexType_Name)
	};

	private static readonly XsdAttributeEntry[] s_simpleContentAttributes = new XsdAttributeEntry[1]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id)
	};

	private static readonly XsdAttributeEntry[] s_simpleContentExtensionAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaBase, BuildSimpleContentExtension_Base),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id)
	};

	private static readonly XsdAttributeEntry[] s_simpleContentRestrictionAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaBase, BuildSimpleContentRestriction_Base),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id)
	};

	private static readonly XsdAttributeEntry[] s_complexContentAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMixed, BuildComplexContent_Mixed)
	};

	private static readonly XsdAttributeEntry[] s_complexContentExtensionAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaBase, BuildComplexContentExtension_Base),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id)
	};

	private static readonly XsdAttributeEntry[] s_complexContentRestrictionAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaBase, BuildComplexContentRestriction_Base),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id)
	};

	private static readonly XsdAttributeEntry[] s_simpleTypeAttributes = new XsdAttributeEntry[3]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaFinal, BuildSimpleType_Final),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildSimpleType_Name)
	};

	private static readonly XsdAttributeEntry[] s_simpleTypeRestrictionAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaBase, BuildSimpleTypeRestriction_Base),
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id)
	};

	private static readonly XsdAttributeEntry[] s_simpleTypeUnionAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMemberTypes, BuildSimpleTypeUnion_MemberTypes)
	};

	private static readonly XsdAttributeEntry[] s_simpleTypeListAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaItemType, BuildSimpleTypeList_ItemType)
	};

	private static readonly XsdAttributeEntry[] s_attributeGroupAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildAttributeGroup_Name)
	};

	private static readonly XsdAttributeEntry[] s_attributeGroupRefAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaRef, BuildAttributeGroupRef_Ref)
	};

	private static readonly XsdAttributeEntry[] s_groupAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildGroup_Name)
	};

	private static readonly XsdAttributeEntry[] s_groupRefAttributes = new XsdAttributeEntry[4]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, BuildParticle_MaxOccurs),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs, BuildParticle_MinOccurs),
		new XsdAttributeEntry(SchemaNames.Token.SchemaRef, BuildGroupRef_Ref)
	};

	private static readonly XsdAttributeEntry[] s_particleAttributes = new XsdAttributeEntry[3]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, BuildParticle_MaxOccurs),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs, BuildParticle_MinOccurs)
	};

	private static readonly XsdAttributeEntry[] s_anyAttributes = new XsdAttributeEntry[5]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMaxOccurs, BuildParticle_MaxOccurs),
		new XsdAttributeEntry(SchemaNames.Token.SchemaMinOccurs, BuildParticle_MinOccurs),
		new XsdAttributeEntry(SchemaNames.Token.SchemaNamespace, BuildAny_Namespace),
		new XsdAttributeEntry(SchemaNames.Token.SchemaProcessContents, BuildAny_ProcessContents)
	};

	private static readonly XsdAttributeEntry[] s_identityConstraintAttributes = new XsdAttributeEntry[3]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildIdentityConstraint_Name),
		new XsdAttributeEntry(SchemaNames.Token.SchemaRefer, BuildIdentityConstraint_Refer)
	};

	private static readonly XsdAttributeEntry[] s_selectorAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaXPath, BuildSelector_XPath)
	};

	private static readonly XsdAttributeEntry[] s_fieldAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaXPath, BuildField_XPath)
	};

	private static readonly XsdAttributeEntry[] s_notationAttributes = new XsdAttributeEntry[4]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaName, BuildNotation_Name),
		new XsdAttributeEntry(SchemaNames.Token.SchemaPublic, BuildNotation_Public),
		new XsdAttributeEntry(SchemaNames.Token.SchemaSystem, BuildNotation_System)
	};

	private static readonly XsdAttributeEntry[] s_includeAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaSchemaLocation, BuildInclude_SchemaLocation)
	};

	private static readonly XsdAttributeEntry[] s_importAttributes = new XsdAttributeEntry[3]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaNamespace, BuildImport_Namespace),
		new XsdAttributeEntry(SchemaNames.Token.SchemaSchemaLocation, BuildImport_SchemaLocation)
	};

	private static readonly XsdAttributeEntry[] s_facetAttributes = new XsdAttributeEntry[3]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaFixed, BuildFacet_Fixed),
		new XsdAttributeEntry(SchemaNames.Token.SchemaValue, BuildFacet_Value)
	};

	private static readonly XsdAttributeEntry[] s_anyAttributeAttributes = new XsdAttributeEntry[3]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaNamespace, BuildAnyAttribute_Namespace),
		new XsdAttributeEntry(SchemaNames.Token.SchemaProcessContents, BuildAnyAttribute_ProcessContents)
	};

	private static readonly XsdAttributeEntry[] s_documentationAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaSource, BuildDocumentation_Source),
		new XsdAttributeEntry(SchemaNames.Token.XmlLang, BuildDocumentation_XmlLang)
	};

	private static readonly XsdAttributeEntry[] s_appinfoAttributes = new XsdAttributeEntry[1]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaSource, BuildAppinfo_Source)
	};

	private static readonly XsdAttributeEntry[] s_redefineAttributes = new XsdAttributeEntry[2]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id),
		new XsdAttributeEntry(SchemaNames.Token.SchemaSchemaLocation, BuildRedefine_SchemaLocation)
	};

	private static readonly XsdAttributeEntry[] s_annotationAttributes = new XsdAttributeEntry[1]
	{
		new XsdAttributeEntry(SchemaNames.Token.SchemaId, BuildAnnotated_Id)
	};

	private static readonly XsdEntry[] s_schemaEntries = new XsdEntry[48]
	{
		new XsdEntry(SchemaNames.Token.Empty, State.Root, s_schemaElement, null, null, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSchema, State.Schema, s_schemaSubelements, s_schemaAttributes, InitSchema, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdAnnotation, State.Annotation, s_annotationSubelements, s_annotationAttributes, InitAnnotation, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdInclude, State.Include, s_annotatedSubelements, s_includeAttributes, InitInclude, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdImport, State.Import, s_annotatedSubelements, s_importAttributes, InitImport, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdElement, State.Element, s_elementSubelements, s_elementAttributes, InitElement, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdAttribute, State.Attribute, s_attributeSubelements, s_attributeAttributes, InitAttribute, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.xsdAttributeGroup, State.AttributeGroup, s_attributeGroupSubelements, s_attributeGroupAttributes, InitAttributeGroup, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.xsdAttributeGroup, State.AttributeGroupRef, s_annotatedSubelements, s_attributeGroupRefAttributes, InitAttributeGroupRef, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdAnyAttribute, State.AnyAttribute, s_annotatedSubelements, s_anyAttributeAttributes, InitAnyAttribute, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdGroup, State.Group, s_groupSubelements, s_groupAttributes, InitGroup, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdGroup, State.GroupRef, s_annotatedSubelements, s_groupRefAttributes, InitGroupRef, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdAll, State.All, s_allSubelements, s_particleAttributes, InitAll, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdChoice, State.Choice, s_choiceSequenceSubelements, s_particleAttributes, InitChoice, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSequence, State.Sequence, s_choiceSequenceSubelements, s_particleAttributes, InitSequence, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdAny, State.Any, s_annotatedSubelements, s_anyAttributes, InitAny, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdNotation, State.Notation, s_annotatedSubelements, s_notationAttributes, InitNotation, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSimpleType, State.SimpleType, s_simpleTypeSubelements, s_simpleTypeAttributes, InitSimpleType, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdComplexType, State.ComplexType, s_complexTypeSubelements, s_complexTypeAttributes, InitComplexType, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdComplexContent, State.ComplexContent, s_complexContentSubelements, s_complexContentAttributes, InitComplexContent, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdComplexContentRestriction, State.ComplexContentRestriction, s_complexContentRestrictionSubelements, s_complexContentRestrictionAttributes, InitComplexContentRestriction, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdComplexContentExtension, State.ComplexContentExtension, s_complexContentExtensionSubelements, s_complexContentExtensionAttributes, InitComplexContentExtension, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSimpleContent, State.SimpleContent, s_simpleContentSubelements, s_simpleContentAttributes, InitSimpleContent, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSimpleContentExtension, State.SimpleContentExtension, s_simpleContentExtensionSubelements, s_simpleContentExtensionAttributes, InitSimpleContentExtension, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSimpleContentRestriction, State.SimpleContentRestriction, s_simpleContentRestrictionSubelements, s_simpleContentRestrictionAttributes, InitSimpleContentRestriction, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSimpleTypeUnion, State.SimpleTypeUnion, s_simpleTypeUnionSubelements, s_simpleTypeUnionAttributes, InitSimpleTypeUnion, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSimpleTypeList, State.SimpleTypeList, s_simpleTypeListSubelements, s_simpleTypeListAttributes, InitSimpleTypeList, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSimpleTypeRestriction, State.SimpleTypeRestriction, s_simpleTypeRestrictionSubelements, s_simpleTypeRestrictionAttributes, InitSimpleTypeRestriction, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdUnique, State.Unique, s_identityConstraintSubelements, s_identityConstraintAttributes, InitIdentityConstraint, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdKey, State.Key, s_identityConstraintSubelements, s_identityConstraintAttributes, InitIdentityConstraint, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdKeyref, State.KeyRef, s_identityConstraintSubelements, s_identityConstraintAttributes, InitIdentityConstraint, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdSelector, State.Selector, s_annotatedSubelements, s_selectorAttributes, InitSelector, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdField, State.Field, s_annotatedSubelements, s_fieldAttributes, InitField, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdMinExclusive, State.MinExclusive, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdMinInclusive, State.MinInclusive, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdMaxExclusive, State.MaxExclusive, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdMaxInclusive, State.MaxInclusive, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdTotalDigits, State.TotalDigits, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdFractionDigits, State.FractionDigits, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdLength, State.Length, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdMinLength, State.MinLength, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdMaxLength, State.MaxLength, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdEnumeration, State.Enumeration, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdPattern, State.Pattern, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdWhitespace, State.WhiteSpace, s_annotatedSubelements, s_facetAttributes, InitFacet, null, parseContent: true),
		new XsdEntry(SchemaNames.Token.XsdAppInfo, State.AppInfo, null, s_appinfoAttributes, InitAppinfo, EndAppinfo, parseContent: false),
		new XsdEntry(SchemaNames.Token.XsdDocumentation, State.Documentation, null, s_documentationAttributes, InitDocumentation, EndDocumentation, parseContent: false),
		new XsdEntry(SchemaNames.Token.XsdRedefine, State.Redefine, s_redefineSubelements, s_redefineAttributes, InitRedefine, EndRedefine, parseContent: true)
	};

	private static readonly int[] s_derivationMethodValues = new int[6] { 1, 2, 4, 8, 16, 255 };

	private static readonly string[] s_derivationMethodStrings = new string[6] { "substitution", "extension", "restriction", "list", "union", "#all" };

	private static readonly string[] s_formStringValues = new string[2] { "qualified", "unqualified" };

	private static readonly string[] s_useStringValues = new string[3] { "optional", "prohibited", "required" };

	private static readonly string[] s_processContentsStringValues = new string[3] { "skip", "lax", "strict" };

	private readonly XmlReader _reader;

	private readonly PositionInfo _positionInfo;

	private XsdEntry _currentEntry;

	private XsdEntry _nextEntry;

	private bool _hasChild;

	private readonly HWStack _stateHistory = new HWStack(10);

	private readonly Stack<XmlSchemaObject> _containerStack = new Stack<XmlSchemaObject>();

	private readonly XmlNameTable _nameTable;

	private readonly SchemaNames _schemaNames;

	private readonly XmlNamespaceManager _namespaceManager;

	private bool _canIncludeImport;

	private readonly XmlSchema _schema;

	private XmlSchemaObject _xso;

	private XmlSchemaElement _element;

	private XmlSchemaAny _anyElement;

	private XmlSchemaAttribute _attribute;

	private XmlSchemaAnyAttribute _anyAttribute;

	private XmlSchemaComplexType _complexType;

	private XmlSchemaSimpleType _simpleType;

	private XmlSchemaComplexContent _complexContent;

	private XmlSchemaComplexContentExtension _complexContentExtension;

	private XmlSchemaComplexContentRestriction _complexContentRestriction;

	private XmlSchemaSimpleContent _simpleContent;

	private XmlSchemaSimpleContentExtension _simpleContentExtension;

	private XmlSchemaSimpleContentRestriction _simpleContentRestriction;

	private XmlSchemaSimpleTypeUnion _simpleTypeUnion;

	private XmlSchemaSimpleTypeList _simpleTypeList;

	private XmlSchemaSimpleTypeRestriction _simpleTypeRestriction;

	private XmlSchemaGroup _group;

	private XmlSchemaGroupRef _groupRef;

	private XmlSchemaAll _all;

	private XmlSchemaChoice _choice;

	private XmlSchemaSequence _sequence;

	private XmlSchemaParticle _particle;

	private XmlSchemaAttributeGroup _attributeGroup;

	private XmlSchemaAttributeGroupRef _attributeGroupRef;

	private XmlSchemaNotation _notation;

	private XmlSchemaIdentityConstraint _identityConstraint;

	private XmlSchemaXPath _xpath;

	private XmlSchemaInclude _include;

	private XmlSchemaImport _import;

	private XmlSchemaAnnotation _annotation;

	private XmlSchemaAppInfo _appInfo;

	private XmlSchemaDocumentation _documentation;

	private XmlSchemaFacet _facet;

	private XmlNode[] _markup;

	private XmlSchemaRedefine _redefine;

	private readonly ValidationEventHandler _validationEventHandler;

	private readonly List<XmlAttribute> _unhandledAttributes = new List<XmlAttribute>();

	private List<XmlQualifiedName> _namespaces;

	private SchemaNames.Token CurrentElement => _currentEntry.Name;

	private SchemaNames.Token ParentElement => ((XsdEntry)_stateHistory[_stateHistory.Length - 1]).Name;

	private XmlSchemaObject ParentContainer => _containerStack.Peek();

	internal XsdBuilder(XmlReader reader, XmlNamespaceManager curmgr, XmlSchema schema, XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventhandler)
	{
		_reader = reader;
		_xso = (_schema = schema);
		_namespaceManager = new BuilderNamespaceManager(curmgr, reader);
		_validationEventHandler = eventhandler;
		_nameTable = nameTable;
		_schemaNames = schemaNames;
		_stateHistory = new HWStack(10);
		_currentEntry = s_schemaEntries[0];
		_positionInfo = PositionInfo.GetPositionInfo(reader);
	}

	internal override bool ProcessElement(string prefix, string name, string ns)
	{
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(name, ns);
		if (GetNextState(xmlQualifiedName))
		{
			Push();
			_xso = null;
			_currentEntry.InitFunc(this, null);
			RecordPosition();
			return true;
		}
		if (!IsSkipableElement(xmlQualifiedName))
		{
			SendValidationEvent(System.SR.Sch_UnsupportedElement, xmlQualifiedName.ToString());
		}
		return false;
	}

	internal override void ProcessAttribute(string prefix, string name, string ns, string value)
	{
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(name, ns);
		if (_currentEntry.Attributes != null)
		{
			for (int i = 0; i < _currentEntry.Attributes.Length; i++)
			{
				XsdAttributeEntry xsdAttributeEntry = _currentEntry.Attributes[i];
				if (_schemaNames.TokenToQName[(int)xsdAttributeEntry.Attribute].Equals(xmlQualifiedName))
				{
					try
					{
						xsdAttributeEntry.BuildFunc(this, value);
						return;
					}
					catch (XmlSchemaException ex)
					{
						ex.SetSource(_reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
						SendValidationEvent(System.SR.Sch_InvalidXsdAttributeDatatypeValue, new string[2] { name, ex.Message }, XmlSeverityType.Error);
						return;
					}
				}
			}
		}
		if (ns != _schemaNames.NsXs && ns.Length != 0)
		{
			if (ns == _schemaNames.NsXmlNs)
			{
				if (_namespaces == null)
				{
					_namespaces = new List<XmlQualifiedName>();
				}
				_namespaces.Add(new XmlQualifiedName((name == _schemaNames.QnXmlNs.Name) ? string.Empty : name, value));
			}
			else
			{
				XmlAttribute xmlAttribute = new XmlAttribute(prefix, name, ns, _schema.Document);
				xmlAttribute.Value = value;
				_unhandledAttributes.Add(xmlAttribute);
			}
		}
		else
		{
			SendValidationEvent(System.SR.Sch_UnsupportedAttribute, xmlQualifiedName.ToString());
		}
	}

	internal override bool IsContentParsed()
	{
		return _currentEntry.ParseContent;
	}

	internal override void ProcessMarkup(XmlNode[] markup)
	{
		_markup = markup;
	}

	internal override void ProcessCData(string value)
	{
		SendValidationEvent(System.SR.Sch_TextNotAllowed, value);
	}

	internal override void StartChildren()
	{
		if (_xso != null)
		{
			if (_namespaces != null && _namespaces.Count > 0)
			{
				_xso.Namespaces = new XmlSerializerNamespaces(_namespaces);
				_namespaces = null;
			}
			if (_unhandledAttributes.Count != 0)
			{
				_xso.SetUnhandledAttributes(_unhandledAttributes.ToArray());
				_unhandledAttributes.Clear();
			}
		}
	}

	internal override void EndChildren()
	{
		if (_currentEntry.EndChildFunc != null)
		{
			_currentEntry.EndChildFunc(this);
		}
		Pop();
	}

	private void Push()
	{
		_stateHistory.Push();
		_stateHistory[_stateHistory.Length - 1] = _currentEntry;
		_containerStack.Push(GetContainer(_currentEntry.CurrentState));
		_currentEntry = _nextEntry;
		if (_currentEntry.Name != SchemaNames.Token.XsdAnnotation)
		{
			_hasChild = false;
		}
	}

	private void Pop()
	{
		_currentEntry = (XsdEntry)_stateHistory.Pop();
		SetContainer(_currentEntry.CurrentState, _containerStack.Pop());
		_hasChild = true;
	}

	private XmlSchemaObject GetContainer(State state)
	{
		XmlSchemaObject result = null;
		switch (state)
		{
		case State.Schema:
			result = _schema;
			break;
		case State.Annotation:
			result = _annotation;
			break;
		case State.Include:
			result = _include;
			break;
		case State.Import:
			result = _import;
			break;
		case State.Element:
			result = _element;
			break;
		case State.Attribute:
			result = _attribute;
			break;
		case State.AttributeGroup:
			result = _attributeGroup;
			break;
		case State.AttributeGroupRef:
			result = _attributeGroupRef;
			break;
		case State.AnyAttribute:
			result = _anyAttribute;
			break;
		case State.Group:
			result = _group;
			break;
		case State.GroupRef:
			result = _groupRef;
			break;
		case State.All:
			result = _all;
			break;
		case State.Choice:
			result = _choice;
			break;
		case State.Sequence:
			result = _sequence;
			break;
		case State.Any:
			result = _anyElement;
			break;
		case State.Notation:
			result = _notation;
			break;
		case State.SimpleType:
			result = _simpleType;
			break;
		case State.ComplexType:
			result = _complexType;
			break;
		case State.ComplexContent:
			result = _complexContent;
			break;
		case State.ComplexContentExtension:
			result = _complexContentExtension;
			break;
		case State.ComplexContentRestriction:
			result = _complexContentRestriction;
			break;
		case State.SimpleContent:
			result = _simpleContent;
			break;
		case State.SimpleContentExtension:
			result = _simpleContentExtension;
			break;
		case State.SimpleContentRestriction:
			result = _simpleContentRestriction;
			break;
		case State.SimpleTypeUnion:
			result = _simpleTypeUnion;
			break;
		case State.SimpleTypeList:
			result = _simpleTypeList;
			break;
		case State.SimpleTypeRestriction:
			result = _simpleTypeRestriction;
			break;
		case State.Unique:
		case State.Key:
		case State.KeyRef:
			result = _identityConstraint;
			break;
		case State.Selector:
		case State.Field:
			result = _xpath;
			break;
		case State.MinExclusive:
		case State.MinInclusive:
		case State.MaxExclusive:
		case State.MaxInclusive:
		case State.TotalDigits:
		case State.FractionDigits:
		case State.Length:
		case State.MinLength:
		case State.MaxLength:
		case State.Enumeration:
		case State.Pattern:
		case State.WhiteSpace:
			result = _facet;
			break;
		case State.AppInfo:
			result = _appInfo;
			break;
		case State.Documentation:
			result = _documentation;
			break;
		case State.Redefine:
			result = _redefine;
			break;
		}
		return result;
	}

	private void SetContainer(State state, object container)
	{
		switch (state)
		{
		case State.Annotation:
			_annotation = (XmlSchemaAnnotation)container;
			break;
		case State.Include:
			_include = (XmlSchemaInclude)container;
			break;
		case State.Import:
			_import = (XmlSchemaImport)container;
			break;
		case State.Element:
			_element = (XmlSchemaElement)container;
			break;
		case State.Attribute:
			_attribute = (XmlSchemaAttribute)container;
			break;
		case State.AttributeGroup:
			_attributeGroup = (XmlSchemaAttributeGroup)container;
			break;
		case State.AttributeGroupRef:
			_attributeGroupRef = (XmlSchemaAttributeGroupRef)container;
			break;
		case State.AnyAttribute:
			_anyAttribute = (XmlSchemaAnyAttribute)container;
			break;
		case State.Group:
			_group = (XmlSchemaGroup)container;
			break;
		case State.GroupRef:
			_groupRef = (XmlSchemaGroupRef)container;
			break;
		case State.All:
			_all = (XmlSchemaAll)container;
			break;
		case State.Choice:
			_choice = (XmlSchemaChoice)container;
			break;
		case State.Sequence:
			_sequence = (XmlSchemaSequence)container;
			break;
		case State.Any:
			_anyElement = (XmlSchemaAny)container;
			break;
		case State.Notation:
			_notation = (XmlSchemaNotation)container;
			break;
		case State.SimpleType:
			_simpleType = (XmlSchemaSimpleType)container;
			break;
		case State.ComplexType:
			_complexType = (XmlSchemaComplexType)container;
			break;
		case State.ComplexContent:
			_complexContent = (XmlSchemaComplexContent)container;
			break;
		case State.ComplexContentExtension:
			_complexContentExtension = (XmlSchemaComplexContentExtension)container;
			break;
		case State.ComplexContentRestriction:
			_complexContentRestriction = (XmlSchemaComplexContentRestriction)container;
			break;
		case State.SimpleContent:
			_simpleContent = (XmlSchemaSimpleContent)container;
			break;
		case State.SimpleContentExtension:
			_simpleContentExtension = (XmlSchemaSimpleContentExtension)container;
			break;
		case State.SimpleContentRestriction:
			_simpleContentRestriction = (XmlSchemaSimpleContentRestriction)container;
			break;
		case State.SimpleTypeUnion:
			_simpleTypeUnion = (XmlSchemaSimpleTypeUnion)container;
			break;
		case State.SimpleTypeList:
			_simpleTypeList = (XmlSchemaSimpleTypeList)container;
			break;
		case State.SimpleTypeRestriction:
			_simpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)container;
			break;
		case State.Unique:
		case State.Key:
		case State.KeyRef:
			_identityConstraint = (XmlSchemaIdentityConstraint)container;
			break;
		case State.Selector:
		case State.Field:
			_xpath = (XmlSchemaXPath)container;
			break;
		case State.MinExclusive:
		case State.MinInclusive:
		case State.MaxExclusive:
		case State.MaxInclusive:
		case State.TotalDigits:
		case State.FractionDigits:
		case State.Length:
		case State.MinLength:
		case State.MaxLength:
		case State.Enumeration:
		case State.Pattern:
		case State.WhiteSpace:
			_facet = (XmlSchemaFacet)container;
			break;
		case State.AppInfo:
			_appInfo = (XmlSchemaAppInfo)container;
			break;
		case State.Documentation:
			_documentation = (XmlSchemaDocumentation)container;
			break;
		case State.Redefine:
			_redefine = (XmlSchemaRedefine)container;
			break;
		case State.Root:
		case State.Schema:
			break;
		}
	}

	private static void BuildAnnotated_Id(XsdBuilder builder, string value)
	{
		builder._xso.IdAttribute = value;
	}

	private static void BuildSchema_AttributeFormDefault(XsdBuilder builder, string value)
	{
		builder._schema.AttributeFormDefault = (XmlSchemaForm)builder.ParseEnum(value, "attributeFormDefault", s_formStringValues);
	}

	private static void BuildSchema_ElementFormDefault(XsdBuilder builder, string value)
	{
		builder._schema.ElementFormDefault = (XmlSchemaForm)builder.ParseEnum(value, "elementFormDefault", s_formStringValues);
	}

	private static void BuildSchema_TargetNamespace(XsdBuilder builder, string value)
	{
		builder._schema.TargetNamespace = value;
	}

	private static void BuildSchema_Version(XsdBuilder builder, string value)
	{
		builder._schema.Version = value;
	}

	private static void BuildSchema_FinalDefault(XsdBuilder builder, string value)
	{
		builder._schema.FinalDefault = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "finalDefault");
	}

	private static void BuildSchema_BlockDefault(XsdBuilder builder, string value)
	{
		builder._schema.BlockDefault = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "blockDefault");
	}

	private static void InitSchema(XsdBuilder builder, string value)
	{
		builder._canIncludeImport = true;
		builder._xso = builder._schema;
	}

	private static void InitInclude(XsdBuilder builder, string value)
	{
		if (!builder._canIncludeImport)
		{
			builder.SendValidationEvent(System.SR.Sch_IncludeLocation, null);
		}
		builder._xso = (builder._include = new XmlSchemaInclude());
		builder._schema.Includes.Add(builder._include);
	}

	private static void BuildInclude_SchemaLocation(XsdBuilder builder, string value)
	{
		builder._include.SchemaLocation = value;
	}

	private static void InitImport(XsdBuilder builder, string value)
	{
		if (!builder._canIncludeImport)
		{
			builder.SendValidationEvent(System.SR.Sch_ImportLocation, null);
		}
		builder._xso = (builder._import = new XmlSchemaImport());
		builder._schema.Includes.Add(builder._import);
	}

	private static void BuildImport_Namespace(XsdBuilder builder, string value)
	{
		builder._import.Namespace = value;
	}

	private static void BuildImport_SchemaLocation(XsdBuilder builder, string value)
	{
		builder._import.SchemaLocation = value;
	}

	private static void InitRedefine(XsdBuilder builder, string value)
	{
		if (!builder._canIncludeImport)
		{
			builder.SendValidationEvent(System.SR.Sch_RedefineLocation, null);
		}
		builder._xso = (builder._redefine = new XmlSchemaRedefine());
		builder._schema.Includes.Add(builder._redefine);
	}

	private static void BuildRedefine_SchemaLocation(XsdBuilder builder, string value)
	{
		builder._redefine.SchemaLocation = value;
	}

	private static void EndRedefine(XsdBuilder builder)
	{
		builder._canIncludeImport = true;
	}

	private static void InitAttribute(XsdBuilder builder, string value)
	{
		builder._xso = (builder._attribute = new XmlSchemaAttribute());
		if (builder.ParentElement == SchemaNames.Token.XsdSchema)
		{
			builder._schema.Items.Add(builder._attribute);
		}
		else
		{
			builder.AddAttribute(builder._attribute);
		}
		builder._canIncludeImport = false;
	}

	private static void BuildAttribute_Default(XsdBuilder builder, string value)
	{
		builder._attribute.DefaultValue = value;
	}

	private static void BuildAttribute_Fixed(XsdBuilder builder, string value)
	{
		builder._attribute.FixedValue = value;
	}

	private static void BuildAttribute_Form(XsdBuilder builder, string value)
	{
		builder._attribute.Form = (XmlSchemaForm)builder.ParseEnum(value, "form", s_formStringValues);
	}

	private static void BuildAttribute_Use(XsdBuilder builder, string value)
	{
		builder._attribute.Use = (XmlSchemaUse)builder.ParseEnum(value, "use", s_useStringValues);
	}

	private static void BuildAttribute_Ref(XsdBuilder builder, string value)
	{
		builder._attribute.RefName = builder.ParseQName(value, "ref");
	}

	private static void BuildAttribute_Name(XsdBuilder builder, string value)
	{
		builder._attribute.Name = value;
	}

	private static void BuildAttribute_Type(XsdBuilder builder, string value)
	{
		builder._attribute.SchemaTypeName = builder.ParseQName(value, "type");
	}

	private static void InitElement(XsdBuilder builder, string value)
	{
		builder._xso = (builder._element = new XmlSchemaElement());
		builder._canIncludeImport = false;
		switch (builder.ParentElement)
		{
		case SchemaNames.Token.XsdSchema:
			builder._schema.Items.Add(builder._element);
			break;
		case SchemaNames.Token.XsdAll:
			builder._all.Items.Add(builder._element);
			break;
		case SchemaNames.Token.XsdChoice:
			builder._choice.Items.Add(builder._element);
			break;
		case SchemaNames.Token.XsdSequence:
			builder._sequence.Items.Add(builder._element);
			break;
		}
	}

	private static void BuildElement_Abstract(XsdBuilder builder, string value)
	{
		builder._element.IsAbstract = builder.ParseBoolean(value, "abstract");
	}

	private static void BuildElement_Block(XsdBuilder builder, string value)
	{
		builder._element.Block = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "block");
	}

	private static void BuildElement_Default(XsdBuilder builder, string value)
	{
		builder._element.DefaultValue = value;
	}

	private static void BuildElement_Form(XsdBuilder builder, string value)
	{
		builder._element.Form = (XmlSchemaForm)builder.ParseEnum(value, "form", s_formStringValues);
	}

	private static void BuildElement_SubstitutionGroup(XsdBuilder builder, string value)
	{
		builder._element.SubstitutionGroup = builder.ParseQName(value, "substitutionGroup");
	}

	private static void BuildElement_Final(XsdBuilder builder, string value)
	{
		builder._element.Final = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "final");
	}

	private static void BuildElement_Fixed(XsdBuilder builder, string value)
	{
		builder._element.FixedValue = value;
	}

	private static void BuildElement_MaxOccurs(XsdBuilder builder, string value)
	{
		builder.SetMaxOccurs(builder._element, value);
	}

	private static void BuildElement_MinOccurs(XsdBuilder builder, string value)
	{
		builder.SetMinOccurs(builder._element, value);
	}

	private static void BuildElement_Name(XsdBuilder builder, string value)
	{
		builder._element.Name = value;
	}

	private static void BuildElement_Nillable(XsdBuilder builder, string value)
	{
		builder._element.IsNillable = builder.ParseBoolean(value, "nillable");
	}

	private static void BuildElement_Ref(XsdBuilder builder, string value)
	{
		builder._element.RefName = builder.ParseQName(value, "ref");
	}

	private static void BuildElement_Type(XsdBuilder builder, string value)
	{
		builder._element.SchemaTypeName = builder.ParseQName(value, "type");
	}

	private static void InitSimpleType(XsdBuilder builder, string value)
	{
		builder._xso = (builder._simpleType = new XmlSchemaSimpleType());
		switch (builder.ParentElement)
		{
		case SchemaNames.Token.XsdSchema:
			builder._canIncludeImport = false;
			builder._schema.Items.Add(builder._simpleType);
			break;
		case SchemaNames.Token.XsdRedefine:
			builder._redefine.Items.Add(builder._simpleType);
			break;
		case SchemaNames.Token.XsdAttribute:
			if (builder._attribute.SchemaType != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupXsdElement, "simpleType");
			}
			builder._attribute.SchemaType = builder._simpleType;
			break;
		case SchemaNames.Token.XsdElement:
			if (builder._element.SchemaType != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupXsdElement, "simpleType");
			}
			if (builder._element.Constraints.Count != 0)
			{
				builder.SendValidationEvent(System.SR.Sch_TypeAfterConstraints, null);
			}
			builder._element.SchemaType = builder._simpleType;
			break;
		case SchemaNames.Token.XsdSimpleTypeList:
			if (builder._simpleTypeList.ItemType != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupXsdElement, "simpleType");
			}
			builder._simpleTypeList.ItemType = builder._simpleType;
			break;
		case SchemaNames.Token.XsdSimpleTypeRestriction:
			if (builder._simpleTypeRestriction.BaseType != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupXsdElement, "simpleType");
			}
			builder._simpleTypeRestriction.BaseType = builder._simpleType;
			break;
		case SchemaNames.Token.XsdSimpleContentRestriction:
			if (builder._simpleContentRestriction.BaseType != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupXsdElement, "simpleType");
			}
			if (builder._simpleContentRestriction.Attributes.Count != 0 || builder._simpleContentRestriction.AnyAttribute != null || builder._simpleContentRestriction.Facets.Count != 0)
			{
				builder.SendValidationEvent(System.SR.Sch_SimpleTypeRestriction, null);
			}
			builder._simpleContentRestriction.BaseType = builder._simpleType;
			break;
		case SchemaNames.Token.XsdSimpleTypeUnion:
			builder._simpleTypeUnion.BaseTypes.Add(builder._simpleType);
			break;
		}
	}

	private static void BuildSimpleType_Name(XsdBuilder builder, string value)
	{
		builder._simpleType.Name = value;
	}

	private static void BuildSimpleType_Final(XsdBuilder builder, string value)
	{
		builder._simpleType.Final = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "final");
	}

	private static void InitSimpleTypeUnion(XsdBuilder builder, string value)
	{
		if (builder._simpleType.Content != null)
		{
			builder.SendValidationEvent(System.SR.Sch_DupSimpleTypeChild, null);
		}
		builder._xso = (builder._simpleTypeUnion = new XmlSchemaSimpleTypeUnion());
		builder._simpleType.Content = builder._simpleTypeUnion;
	}

	private static void BuildSimpleTypeUnion_MemberTypes(XsdBuilder builder, string value)
	{
		XmlSchemaDatatype xmlSchemaDatatype = XmlSchemaDatatype.FromXmlTokenizedTypeXsd(XmlTokenizedType.QName).DeriveByList(null);
		try
		{
			builder._simpleTypeUnion.MemberTypes = (XmlQualifiedName[])xmlSchemaDatatype.ParseValue(value, builder._nameTable, builder._namespaceManager);
		}
		catch (XmlSchemaException ex)
		{
			ex.SetSource(builder._reader.BaseURI, builder._positionInfo.LineNumber, builder._positionInfo.LinePosition);
			builder.SendValidationEvent(ex);
		}
	}

	private static void InitSimpleTypeList(XsdBuilder builder, string value)
	{
		if (builder._simpleType.Content != null)
		{
			builder.SendValidationEvent(System.SR.Sch_DupSimpleTypeChild, null);
		}
		builder._xso = (builder._simpleTypeList = new XmlSchemaSimpleTypeList());
		builder._simpleType.Content = builder._simpleTypeList;
	}

	private static void BuildSimpleTypeList_ItemType(XsdBuilder builder, string value)
	{
		builder._simpleTypeList.ItemTypeName = builder.ParseQName(value, "itemType");
	}

	private static void InitSimpleTypeRestriction(XsdBuilder builder, string value)
	{
		if (builder._simpleType.Content != null)
		{
			builder.SendValidationEvent(System.SR.Sch_DupSimpleTypeChild, null);
		}
		builder._xso = (builder._simpleTypeRestriction = new XmlSchemaSimpleTypeRestriction());
		builder._simpleType.Content = builder._simpleTypeRestriction;
	}

	private static void BuildSimpleTypeRestriction_Base(XsdBuilder builder, string value)
	{
		builder._simpleTypeRestriction.BaseTypeName = builder.ParseQName(value, "base");
	}

	private static void InitComplexType(XsdBuilder builder, string value)
	{
		builder._xso = (builder._complexType = new XmlSchemaComplexType());
		switch (builder.ParentElement)
		{
		case SchemaNames.Token.XsdSchema:
			builder._canIncludeImport = false;
			builder._schema.Items.Add(builder._complexType);
			break;
		case SchemaNames.Token.XsdRedefine:
			builder._redefine.Items.Add(builder._complexType);
			break;
		case SchemaNames.Token.XsdElement:
			if (builder._element.SchemaType != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupElement, "complexType");
			}
			if (builder._element.Constraints.Count != 0)
			{
				builder.SendValidationEvent(System.SR.Sch_TypeAfterConstraints, null);
			}
			builder._element.SchemaType = builder._complexType;
			break;
		}
	}

	private static void BuildComplexType_Abstract(XsdBuilder builder, string value)
	{
		builder._complexType.IsAbstract = builder.ParseBoolean(value, "abstract");
	}

	private static void BuildComplexType_Block(XsdBuilder builder, string value)
	{
		builder._complexType.Block = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "block");
	}

	private static void BuildComplexType_Final(XsdBuilder builder, string value)
	{
		builder._complexType.Final = (XmlSchemaDerivationMethod)builder.ParseBlockFinalEnum(value, "final");
	}

	private static void BuildComplexType_Mixed(XsdBuilder builder, string value)
	{
		builder._complexType.IsMixed = builder.ParseBoolean(value, "mixed");
	}

	private static void BuildComplexType_Name(XsdBuilder builder, string value)
	{
		builder._complexType.Name = value;
	}

	private static void InitComplexContent(XsdBuilder builder, string value)
	{
		if (builder._complexType.ContentModel != null || builder._complexType.Particle != null || builder._complexType.Attributes.Count != 0 || builder._complexType.AnyAttribute != null)
		{
			builder.SendValidationEvent(System.SR.Sch_ComplexTypeContentModel, "complexContent");
		}
		builder._xso = (builder._complexContent = new XmlSchemaComplexContent());
		builder._complexType.ContentModel = builder._complexContent;
	}

	private static void BuildComplexContent_Mixed(XsdBuilder builder, string value)
	{
		builder._complexContent.IsMixed = builder.ParseBoolean(value, "mixed");
	}

	private static void InitComplexContentExtension(XsdBuilder builder, string value)
	{
		if (builder._complexContent.Content != null)
		{
			builder.SendValidationEvent(System.SR.Sch_ComplexContentContentModel, "extension");
		}
		builder._xso = (builder._complexContentExtension = new XmlSchemaComplexContentExtension());
		builder._complexContent.Content = builder._complexContentExtension;
	}

	private static void BuildComplexContentExtension_Base(XsdBuilder builder, string value)
	{
		builder._complexContentExtension.BaseTypeName = builder.ParseQName(value, "base");
	}

	private static void InitComplexContentRestriction(XsdBuilder builder, string value)
	{
		builder._xso = (builder._complexContentRestriction = new XmlSchemaComplexContentRestriction());
		builder._complexContent.Content = builder._complexContentRestriction;
	}

	private static void BuildComplexContentRestriction_Base(XsdBuilder builder, string value)
	{
		builder._complexContentRestriction.BaseTypeName = builder.ParseQName(value, "base");
	}

	private static void InitSimpleContent(XsdBuilder builder, string value)
	{
		if (builder._complexType.ContentModel != null || builder._complexType.Particle != null || builder._complexType.Attributes.Count != 0 || builder._complexType.AnyAttribute != null)
		{
			builder.SendValidationEvent(System.SR.Sch_ComplexTypeContentModel, "simpleContent");
		}
		builder._xso = (builder._simpleContent = new XmlSchemaSimpleContent());
		builder._complexType.ContentModel = builder._simpleContent;
	}

	private static void InitSimpleContentExtension(XsdBuilder builder, string value)
	{
		if (builder._simpleContent.Content != null)
		{
			builder.SendValidationEvent(System.SR.Sch_DupElement, "extension");
		}
		builder._xso = (builder._simpleContentExtension = new XmlSchemaSimpleContentExtension());
		builder._simpleContent.Content = builder._simpleContentExtension;
	}

	private static void BuildSimpleContentExtension_Base(XsdBuilder builder, string value)
	{
		builder._simpleContentExtension.BaseTypeName = builder.ParseQName(value, "base");
	}

	private static void InitSimpleContentRestriction(XsdBuilder builder, string value)
	{
		if (builder._simpleContent.Content != null)
		{
			builder.SendValidationEvent(System.SR.Sch_DupElement, "restriction");
		}
		builder._xso = (builder._simpleContentRestriction = new XmlSchemaSimpleContentRestriction());
		builder._simpleContent.Content = builder._simpleContentRestriction;
	}

	private static void BuildSimpleContentRestriction_Base(XsdBuilder builder, string value)
	{
		builder._simpleContentRestriction.BaseTypeName = builder.ParseQName(value, "base");
	}

	private static void InitAttributeGroup(XsdBuilder builder, string value)
	{
		builder._canIncludeImport = false;
		builder._xso = (builder._attributeGroup = new XmlSchemaAttributeGroup());
		switch (builder.ParentElement)
		{
		case SchemaNames.Token.XsdSchema:
			builder._schema.Items.Add(builder._attributeGroup);
			break;
		case SchemaNames.Token.XsdRedefine:
			builder._redefine.Items.Add(builder._attributeGroup);
			break;
		}
	}

	private static void BuildAttributeGroup_Name(XsdBuilder builder, string value)
	{
		builder._attributeGroup.Name = value;
	}

	private static void InitAttributeGroupRef(XsdBuilder builder, string value)
	{
		builder._xso = (builder._attributeGroupRef = new XmlSchemaAttributeGroupRef());
		builder.AddAttribute(builder._attributeGroupRef);
	}

	private static void BuildAttributeGroupRef_Ref(XsdBuilder builder, string value)
	{
		builder._attributeGroupRef.RefName = builder.ParseQName(value, "ref");
	}

	private static void InitAnyAttribute(XsdBuilder builder, string value)
	{
		builder._xso = (builder._anyAttribute = new XmlSchemaAnyAttribute());
		switch (builder.ParentElement)
		{
		case SchemaNames.Token.XsdComplexType:
			if (builder._complexType.ContentModel != null)
			{
				builder.SendValidationEvent(System.SR.Sch_AttributeMutuallyExclusive, "anyAttribute");
			}
			if (builder._complexType.AnyAttribute != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupElement, "anyAttribute");
			}
			builder._complexType.AnyAttribute = builder._anyAttribute;
			break;
		case SchemaNames.Token.XsdSimpleContentRestriction:
			if (builder._simpleContentRestriction.AnyAttribute != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupElement, "anyAttribute");
			}
			builder._simpleContentRestriction.AnyAttribute = builder._anyAttribute;
			break;
		case SchemaNames.Token.XsdSimpleContentExtension:
			if (builder._simpleContentExtension.AnyAttribute != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupElement, "anyAttribute");
			}
			builder._simpleContentExtension.AnyAttribute = builder._anyAttribute;
			break;
		case SchemaNames.Token.XsdComplexContentExtension:
			if (builder._complexContentExtension.AnyAttribute != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupElement, "anyAttribute");
			}
			builder._complexContentExtension.AnyAttribute = builder._anyAttribute;
			break;
		case SchemaNames.Token.XsdComplexContentRestriction:
			if (builder._complexContentRestriction.AnyAttribute != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupElement, "anyAttribute");
			}
			builder._complexContentRestriction.AnyAttribute = builder._anyAttribute;
			break;
		case SchemaNames.Token.xsdAttributeGroup:
			if (builder._attributeGroup.AnyAttribute != null)
			{
				builder.SendValidationEvent(System.SR.Sch_DupElement, "anyAttribute");
			}
			builder._attributeGroup.AnyAttribute = builder._anyAttribute;
			break;
		}
	}

	private static void BuildAnyAttribute_Namespace(XsdBuilder builder, string value)
	{
		builder._anyAttribute.Namespace = value;
	}

	private static void BuildAnyAttribute_ProcessContents(XsdBuilder builder, string value)
	{
		builder._anyAttribute.ProcessContents = (XmlSchemaContentProcessing)builder.ParseEnum(value, "processContents", s_processContentsStringValues);
	}

	private static void InitGroup(XsdBuilder builder, string value)
	{
		builder._xso = (builder._group = new XmlSchemaGroup());
		builder._canIncludeImport = false;
		switch (builder.ParentElement)
		{
		case SchemaNames.Token.XsdSchema:
			builder._schema.Items.Add(builder._group);
			break;
		case SchemaNames.Token.XsdRedefine:
			builder._redefine.Items.Add(builder._group);
			break;
		}
	}

	private static void BuildGroup_Name(XsdBuilder builder, string value)
	{
		builder._group.Name = value;
	}

	private static void InitGroupRef(XsdBuilder builder, string value)
	{
		builder._xso = (builder._particle = (builder._groupRef = new XmlSchemaGroupRef()));
		builder.AddParticle(builder._groupRef);
	}

	private static void BuildParticle_MaxOccurs(XsdBuilder builder, string value)
	{
		builder.SetMaxOccurs(builder._particle, value);
	}

	private static void BuildParticle_MinOccurs(XsdBuilder builder, string value)
	{
		builder.SetMinOccurs(builder._particle, value);
	}

	private static void BuildGroupRef_Ref(XsdBuilder builder, string value)
	{
		builder._groupRef.RefName = builder.ParseQName(value, "ref");
	}

	private static void InitAll(XsdBuilder builder, string value)
	{
		builder._xso = (builder._particle = (builder._all = new XmlSchemaAll()));
		builder.AddParticle(builder._all);
	}

	private static void InitChoice(XsdBuilder builder, string value)
	{
		builder._xso = (builder._particle = (builder._choice = new XmlSchemaChoice()));
		builder.AddParticle(builder._choice);
	}

	private static void InitSequence(XsdBuilder builder, string value)
	{
		builder._xso = (builder._particle = (builder._sequence = new XmlSchemaSequence()));
		builder.AddParticle(builder._sequence);
	}

	private static void InitAny(XsdBuilder builder, string value)
	{
		builder._xso = (builder._particle = (builder._anyElement = new XmlSchemaAny()));
		builder.AddParticle(builder._anyElement);
	}

	private static void BuildAny_Namespace(XsdBuilder builder, string value)
	{
		builder._anyElement.Namespace = value;
	}

	private static void BuildAny_ProcessContents(XsdBuilder builder, string value)
	{
		builder._anyElement.ProcessContents = (XmlSchemaContentProcessing)builder.ParseEnum(value, "processContents", s_processContentsStringValues);
	}

	private static void InitNotation(XsdBuilder builder, string value)
	{
		builder._xso = (builder._notation = new XmlSchemaNotation());
		builder._canIncludeImport = false;
		builder._schema.Items.Add(builder._notation);
	}

	private static void BuildNotation_Name(XsdBuilder builder, string value)
	{
		builder._notation.Name = value;
	}

	private static void BuildNotation_Public(XsdBuilder builder, string value)
	{
		builder._notation.Public = value;
	}

	private static void BuildNotation_System(XsdBuilder builder, string value)
	{
		builder._notation.System = value;
	}

	private static void InitFacet(XsdBuilder builder, string value)
	{
		switch (builder.CurrentElement)
		{
		case SchemaNames.Token.XsdEnumeration:
			builder._facet = new XmlSchemaEnumerationFacet();
			break;
		case SchemaNames.Token.XsdLength:
			builder._facet = new XmlSchemaLengthFacet();
			break;
		case SchemaNames.Token.XsdMaxExclusive:
			builder._facet = new XmlSchemaMaxExclusiveFacet();
			break;
		case SchemaNames.Token.XsdMaxInclusive:
			builder._facet = new XmlSchemaMaxInclusiveFacet();
			break;
		case SchemaNames.Token.XsdMaxLength:
			builder._facet = new XmlSchemaMaxLengthFacet();
			break;
		case SchemaNames.Token.XsdMinExclusive:
			builder._facet = new XmlSchemaMinExclusiveFacet();
			break;
		case SchemaNames.Token.XsdMinInclusive:
			builder._facet = new XmlSchemaMinInclusiveFacet();
			break;
		case SchemaNames.Token.XsdMinLength:
			builder._facet = new XmlSchemaMinLengthFacet();
			break;
		case SchemaNames.Token.XsdPattern:
			builder._facet = new XmlSchemaPatternFacet();
			break;
		case SchemaNames.Token.XsdTotalDigits:
			builder._facet = new XmlSchemaTotalDigitsFacet();
			break;
		case SchemaNames.Token.XsdFractionDigits:
			builder._facet = new XmlSchemaFractionDigitsFacet();
			break;
		case SchemaNames.Token.XsdWhitespace:
			builder._facet = new XmlSchemaWhiteSpaceFacet();
			break;
		}
		builder._xso = builder._facet;
		if (SchemaNames.Token.XsdSimpleTypeRestriction == builder.ParentElement)
		{
			builder._simpleTypeRestriction.Facets.Add(builder._facet);
			return;
		}
		if (builder._simpleContentRestriction.Attributes.Count != 0 || builder._simpleContentRestriction.AnyAttribute != null)
		{
			builder.SendValidationEvent(System.SR.Sch_InvalidFacetPosition, null);
		}
		builder._simpleContentRestriction.Facets.Add(builder._facet);
	}

	private static void BuildFacet_Fixed(XsdBuilder builder, string value)
	{
		builder._facet.IsFixed = builder.ParseBoolean(value, "fixed");
	}

	private static void BuildFacet_Value(XsdBuilder builder, string value)
	{
		builder._facet.Value = value;
	}

	private static void InitIdentityConstraint(XsdBuilder builder, string value)
	{
		if (!builder._element.RefName.IsEmpty)
		{
			builder.SendValidationEvent(System.SR.Sch_ElementRef, null);
		}
		switch (builder.CurrentElement)
		{
		case SchemaNames.Token.XsdUnique:
			builder._xso = (builder._identityConstraint = new XmlSchemaUnique());
			break;
		case SchemaNames.Token.XsdKey:
			builder._xso = (builder._identityConstraint = new XmlSchemaKey());
			break;
		case SchemaNames.Token.XsdKeyref:
			builder._xso = (builder._identityConstraint = new XmlSchemaKeyref());
			break;
		}
		builder._element.Constraints.Add(builder._identityConstraint);
	}

	private static void BuildIdentityConstraint_Name(XsdBuilder builder, string value)
	{
		builder._identityConstraint.Name = value;
	}

	private static void BuildIdentityConstraint_Refer(XsdBuilder builder, string value)
	{
		if (builder._identityConstraint is XmlSchemaKeyref)
		{
			((XmlSchemaKeyref)builder._identityConstraint).Refer = builder.ParseQName(value, "refer");
		}
		else
		{
			builder.SendValidationEvent(System.SR.Sch_UnsupportedAttribute, "refer");
		}
	}

	private static void InitSelector(XsdBuilder builder, string value)
	{
		builder._xso = (builder._xpath = new XmlSchemaXPath());
		if (builder._identityConstraint.Selector == null)
		{
			builder._identityConstraint.Selector = builder._xpath;
		}
		else
		{
			builder.SendValidationEvent(System.SR.Sch_DupSelector, builder._identityConstraint.Name);
		}
	}

	private static void BuildSelector_XPath(XsdBuilder builder, string value)
	{
		builder._xpath.XPath = value;
	}

	private static void InitField(XsdBuilder builder, string value)
	{
		builder._xso = (builder._xpath = new XmlSchemaXPath());
		if (builder._identityConstraint.Selector == null)
		{
			builder.SendValidationEvent(System.SR.Sch_SelectorBeforeFields, builder._identityConstraint.Name);
		}
		builder._identityConstraint.Fields.Add(builder._xpath);
	}

	private static void BuildField_XPath(XsdBuilder builder, string value)
	{
		builder._xpath.XPath = value;
	}

	private static void InitAnnotation(XsdBuilder builder, string value)
	{
		if (builder._hasChild && builder.ParentElement != SchemaNames.Token.XsdSchema && builder.ParentElement != SchemaNames.Token.XsdRedefine)
		{
			builder.SendValidationEvent(System.SR.Sch_AnnotationLocation, null);
		}
		builder._xso = (builder._annotation = new XmlSchemaAnnotation());
		builder.ParentContainer.AddAnnotation(builder._annotation);
	}

	private static void InitAppinfo(XsdBuilder builder, string value)
	{
		builder._xso = (builder._appInfo = new XmlSchemaAppInfo());
		builder._annotation.Items.Add(builder._appInfo);
		builder._markup = Array.Empty<XmlNode>();
	}

	private static void BuildAppinfo_Source(XsdBuilder builder, string value)
	{
		builder._appInfo.Source = ParseUriReference(value);
	}

	private static void EndAppinfo(XsdBuilder builder)
	{
		builder._appInfo.Markup = builder._markup;
	}

	private static void InitDocumentation(XsdBuilder builder, string value)
	{
		builder._xso = (builder._documentation = new XmlSchemaDocumentation());
		builder._annotation.Items.Add(builder._documentation);
		builder._markup = Array.Empty<XmlNode>();
	}

	private static void BuildDocumentation_Source(XsdBuilder builder, string value)
	{
		builder._documentation.Source = ParseUriReference(value);
	}

	private static void BuildDocumentation_XmlLang(XsdBuilder builder, string value)
	{
		try
		{
			builder._documentation.Language = value;
		}
		catch (XmlSchemaException ex)
		{
			ex.SetSource(builder._reader.BaseURI, builder._positionInfo.LineNumber, builder._positionInfo.LinePosition);
			builder.SendValidationEvent(ex);
		}
	}

	private static void EndDocumentation(XsdBuilder builder)
	{
		builder._documentation.Markup = builder._markup;
	}

	private void AddAttribute(XmlSchemaObject value)
	{
		switch (ParentElement)
		{
		case SchemaNames.Token.XsdComplexType:
			if (_complexType.ContentModel != null)
			{
				SendValidationEvent(System.SR.Sch_AttributeMutuallyExclusive, "attribute");
			}
			if (_complexType.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_AnyAttributeLastChild, null);
			}
			_complexType.Attributes.Add(value);
			break;
		case SchemaNames.Token.XsdSimpleContentRestriction:
			if (_simpleContentRestriction.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_AnyAttributeLastChild, null);
			}
			_simpleContentRestriction.Attributes.Add(value);
			break;
		case SchemaNames.Token.XsdSimpleContentExtension:
			if (_simpleContentExtension.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_AnyAttributeLastChild, null);
			}
			_simpleContentExtension.Attributes.Add(value);
			break;
		case SchemaNames.Token.XsdComplexContentExtension:
			if (_complexContentExtension.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_AnyAttributeLastChild, null);
			}
			_complexContentExtension.Attributes.Add(value);
			break;
		case SchemaNames.Token.XsdComplexContentRestriction:
			if (_complexContentRestriction.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_AnyAttributeLastChild, null);
			}
			_complexContentRestriction.Attributes.Add(value);
			break;
		case SchemaNames.Token.xsdAttributeGroup:
			if (_attributeGroup.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_AnyAttributeLastChild, null);
			}
			_attributeGroup.Attributes.Add(value);
			break;
		}
	}

	private void AddParticle(XmlSchemaParticle particle)
	{
		switch (ParentElement)
		{
		case SchemaNames.Token.XsdComplexType:
			if (_complexType.ContentModel != null || _complexType.Attributes.Count != 0 || _complexType.AnyAttribute != null || _complexType.Particle != null)
			{
				SendValidationEvent(System.SR.Sch_ComplexTypeContentModel, "complexType");
			}
			_complexType.Particle = particle;
			break;
		case SchemaNames.Token.XsdComplexContentExtension:
			if (_complexContentExtension.Particle != null || _complexContentExtension.Attributes.Count != 0 || _complexContentExtension.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_ComplexContentContentModel, "ComplexContentExtension");
			}
			_complexContentExtension.Particle = particle;
			break;
		case SchemaNames.Token.XsdComplexContentRestriction:
			if (_complexContentRestriction.Particle != null || _complexContentRestriction.Attributes.Count != 0 || _complexContentRestriction.AnyAttribute != null)
			{
				SendValidationEvent(System.SR.Sch_ComplexContentContentModel, "ComplexContentExtension");
			}
			_complexContentRestriction.Particle = particle;
			break;
		case SchemaNames.Token.XsdGroup:
			if (_group.Particle != null)
			{
				SendValidationEvent(System.SR.Sch_DupGroupParticle, "particle");
			}
			_group.Particle = (XmlSchemaGroupBase)particle;
			break;
		case SchemaNames.Token.XsdChoice:
		case SchemaNames.Token.XsdSequence:
			((XmlSchemaGroupBase)ParentContainer).Items.Add(particle);
			break;
		}
	}

	private bool GetNextState(XmlQualifiedName qname)
	{
		if (_currentEntry.NextStates != null)
		{
			for (int i = 0; i < _currentEntry.NextStates.Length; i++)
			{
				int num = (int)_currentEntry.NextStates[i];
				if (_schemaNames.TokenToQName[(int)s_schemaEntries[num].Name].Equals(qname))
				{
					_nextEntry = s_schemaEntries[num];
					return true;
				}
			}
		}
		return false;
	}

	private bool IsSkipableElement(XmlQualifiedName qname)
	{
		if (CurrentElement != SchemaNames.Token.XsdDocumentation)
		{
			return CurrentElement == SchemaNames.Token.XsdAppInfo;
		}
		return true;
	}

	private void SetMinOccurs(XmlSchemaParticle particle, string value)
	{
		try
		{
			particle.MinOccursString = value;
		}
		catch (Exception)
		{
			SendValidationEvent(System.SR.Sch_MinOccursInvalidXsd, null);
		}
	}

	private void SetMaxOccurs(XmlSchemaParticle particle, string value)
	{
		try
		{
			particle.MaxOccursString = value;
		}
		catch (Exception)
		{
			SendValidationEvent(System.SR.Sch_MaxOccursInvalidXsd, null);
		}
	}

	private bool ParseBoolean(string value, string attributeName)
	{
		try
		{
			return XmlConvert.ToBoolean(value);
		}
		catch (Exception)
		{
			SendValidationEvent(System.SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
			return false;
		}
	}

	private int ParseEnum(string value, string attributeName, string[] values)
	{
		string text = value.Trim();
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] == text)
			{
				return i + 1;
			}
		}
		SendValidationEvent(System.SR.Sch_InvalidXsdAttributeValue, attributeName, text, null);
		return 0;
	}

	private XmlQualifiedName ParseQName(string value, string attributeName)
	{
		try
		{
			value = XmlComplianceUtil.NonCDataNormalize(value);
			string prefix;
			return XmlQualifiedName.Parse(value, _namespaceManager, out prefix);
		}
		catch (Exception)
		{
			SendValidationEvent(System.SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
			return XmlQualifiedName.Empty;
		}
	}

	private int ParseBlockFinalEnum(string value, string attributeName)
	{
		int num = 0;
		string[] array = XmlConvert.SplitString(value);
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = false;
			for (int j = 0; j < s_derivationMethodStrings.Length; j++)
			{
				if (array[i] == s_derivationMethodStrings[j])
				{
					if ((num & s_derivationMethodValues[j]) != 0 && (num & s_derivationMethodValues[j]) != s_derivationMethodValues[j])
					{
						SendValidationEvent(System.SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
						return 0;
					}
					num |= s_derivationMethodValues[j];
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				SendValidationEvent(System.SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
				return 0;
			}
			if (num == 255 && value.Length > 4)
			{
				SendValidationEvent(System.SR.Sch_InvalidXsdAttributeValue, attributeName, value, null);
				return 0;
			}
		}
		return num;
	}

	private static string ParseUriReference(string s)
	{
		return s;
	}

	private void SendValidationEvent(string code, string arg0, string arg1, string arg2)
	{
		SendValidationEvent(new XmlSchemaException(code, new string[3] { arg0, arg1, arg2 }, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
	}

	private void SendValidationEvent(string code, string msg)
	{
		SendValidationEvent(new XmlSchemaException(code, msg, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
	}

	private void SendValidationEvent(string code, string[] args, XmlSeverityType severity)
	{
		SendValidationEvent(new XmlSchemaException(code, args, _reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
	}

	private void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
	{
		_schema.ErrorCount++;
		e.SetSchemaObject(_schema);
		if (_validationEventHandler != null)
		{
			_validationEventHandler(null, new ValidationEventArgs(e, severity));
		}
		else if (severity == XmlSeverityType.Error)
		{
			throw e;
		}
	}

	private void SendValidationEvent(XmlSchemaException e)
	{
		SendValidationEvent(e, XmlSeverityType.Error);
	}

	private void RecordPosition()
	{
		_xso.SourceUri = _reader.BaseURI;
		_xso.LineNumber = _positionInfo.LineNumber;
		_xso.LinePosition = _positionInfo.LinePosition;
		if (_xso != _schema)
		{
			_xso.Parent = ParentContainer;
		}
	}
}
