using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace System.Xml.Schema;

internal sealed class SchemaCollectionPreprocessor : BaseProcessor
{
	private enum Compositor
	{
		Root,
		Include,
		Import
	}

	private XmlSchema _schema;

	private string _targetNamespace;

	private bool _buildinIncluded;

	private XmlSchemaForm _elementFormDefault;

	private XmlSchemaForm _attributeFormDefault;

	private XmlSchemaDerivationMethod _blockDefault;

	private XmlSchemaDerivationMethod _finalDefault;

	private Hashtable _schemaLocations;

	private Hashtable _referenceNamespaces;

	private string _xmlns;

	private XmlResolver _xmlResolver;

	internal XmlResolver XmlResolver
	{
		set
		{
			_xmlResolver = value;
		}
	}

	public SchemaCollectionPreprocessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventHandler)
		: base(nameTable, schemaNames, eventHandler)
	{
	}

	public bool Execute(XmlSchema schema, string targetNamespace, bool loadExternals, XmlSchemaCollection xsc)
	{
		_schema = schema;
		_xmlns = base.NameTable.Add("xmlns");
		Cleanup(schema);
		if (loadExternals && _xmlResolver != null)
		{
			_schemaLocations = new Hashtable();
			if (schema.BaseUri != null)
			{
				_schemaLocations.Add(schema.BaseUri, schema.BaseUri);
			}
			LoadExternals(schema, xsc);
		}
		ValidateIdAttribute(schema);
		Preprocess(schema, targetNamespace, Compositor.Root);
		if (!base.HasErrors)
		{
			schema.IsPreprocessed = true;
			for (int i = 0; i < schema.Includes.Count; i++)
			{
				XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
				if (xmlSchemaExternal.Schema != null)
				{
					xmlSchemaExternal.Schema.IsPreprocessed = true;
				}
			}
		}
		return !base.HasErrors;
	}

	private void Cleanup(XmlSchema schema)
	{
		if (schema.IsProcessing)
		{
			return;
		}
		schema.IsProcessing = true;
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			if (xmlSchemaExternal.Schema != null)
			{
				Cleanup(xmlSchemaExternal.Schema);
			}
			if (xmlSchemaExternal is XmlSchemaRedefine)
			{
				XmlSchemaRedefine xmlSchemaRedefine = xmlSchemaExternal as XmlSchemaRedefine;
				xmlSchemaRedefine.AttributeGroups.Clear();
				xmlSchemaRedefine.Groups.Clear();
				xmlSchemaRedefine.SchemaTypes.Clear();
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
		schema.IsProcessing = false;
	}

	private void LoadExternals(XmlSchema schema, XmlSchemaCollection xsc)
	{
		if (schema.IsProcessing)
		{
			return;
		}
		schema.IsProcessing = true;
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			Uri uri = null;
			if (xmlSchemaExternal.Schema != null)
			{
				if (xmlSchemaExternal is XmlSchemaImport && ((XmlSchemaImport)xmlSchemaExternal).Namespace == "http://www.w3.org/XML/1998/namespace")
				{
					_buildinIncluded = true;
					continue;
				}
				uri = xmlSchemaExternal.BaseUri;
				if (uri != null && _schemaLocations[uri] == null)
				{
					_schemaLocations.Add(uri, uri);
				}
				LoadExternals(xmlSchemaExternal.Schema, xsc);
				continue;
			}
			if (xsc != null && xmlSchemaExternal is XmlSchemaImport)
			{
				XmlSchemaImport xmlSchemaImport = (XmlSchemaImport)xmlSchemaExternal;
				string ns = ((xmlSchemaImport.Namespace != null) ? xmlSchemaImport.Namespace : string.Empty);
				xmlSchemaExternal.Schema = xsc[ns];
				if (xmlSchemaExternal.Schema != null)
				{
					xmlSchemaExternal.Schema = xmlSchemaExternal.Schema.Clone();
					if (xmlSchemaExternal.Schema.BaseUri != null && _schemaLocations[xmlSchemaExternal.Schema.BaseUri] == null)
					{
						_schemaLocations.Add(xmlSchemaExternal.Schema.BaseUri, xmlSchemaExternal.Schema.BaseUri);
					}
					Uri uri2 = null;
					for (int j = 0; j < xmlSchemaExternal.Schema.Includes.Count; j++)
					{
						XmlSchemaExternal xmlSchemaExternal2 = (XmlSchemaExternal)xmlSchemaExternal.Schema.Includes[j];
						if (!(xmlSchemaExternal2 is XmlSchemaImport))
						{
							continue;
						}
						XmlSchemaImport xmlSchemaImport2 = (XmlSchemaImport)xmlSchemaExternal2;
						uri2 = ((xmlSchemaImport2.BaseUri != null) ? xmlSchemaImport2.BaseUri : ((xmlSchemaImport2.Schema != null && xmlSchemaImport2.Schema.BaseUri != null) ? xmlSchemaImport2.Schema.BaseUri : null));
						if (uri2 != null)
						{
							if (_schemaLocations[uri2] != null)
							{
								xmlSchemaImport2.Schema = null;
							}
							else
							{
								_schemaLocations.Add(uri2, uri2);
							}
						}
					}
					continue;
				}
			}
			if (xmlSchemaExternal is XmlSchemaImport && ((XmlSchemaImport)xmlSchemaExternal).Namespace == "http://www.w3.org/XML/1998/namespace")
			{
				if (!_buildinIncluded)
				{
					_buildinIncluded = true;
					xmlSchemaExternal.Schema = Preprocessor.GetBuildInSchema();
				}
				continue;
			}
			string schemaLocation = xmlSchemaExternal.SchemaLocation;
			if (schemaLocation == null)
			{
				continue;
			}
			Uri uri3 = ResolveSchemaLocationUri(schema, schemaLocation);
			if (!(uri3 != null) || _schemaLocations[uri3] != null)
			{
				continue;
			}
			Stream schemaEntity = GetSchemaEntity(uri3);
			if (schemaEntity != null)
			{
				xmlSchemaExternal.BaseUri = uri3;
				_schemaLocations.Add(uri3, uri3);
				XmlTextReader xmlTextReader = new XmlTextReader(uri3.ToString(), schemaEntity, base.NameTable);
				xmlTextReader.XmlResolver = _xmlResolver;
				try
				{
					Parser parser = new Parser(SchemaType.XSD, base.NameTable, base.SchemaNames, base.EventHandler);
					parser.Parse(xmlTextReader, null);
					while (xmlTextReader.Read())
					{
					}
					xmlSchemaExternal.Schema = parser.XmlSchema;
					LoadExternals(xmlSchemaExternal.Schema, xsc);
				}
				catch (XmlSchemaException ex)
				{
					SendValidationEventNoThrow(new XmlSchemaException(System.SR.Sch_CannotLoadSchema, new string[2] { schemaLocation, ex.Message }, ex.SourceUri, ex.LineNumber, ex.LinePosition), XmlSeverityType.Error);
				}
				catch (Exception)
				{
					SendValidationEvent(System.SR.Sch_InvalidIncludeLocation, xmlSchemaExternal, XmlSeverityType.Warning);
				}
				finally
				{
					xmlTextReader.Close();
				}
			}
			else
			{
				SendValidationEvent(System.SR.Sch_InvalidIncludeLocation, xmlSchemaExternal, XmlSeverityType.Warning);
			}
		}
		schema.IsProcessing = false;
	}

	private void BuildRefNamespaces(XmlSchema schema)
	{
		_referenceNamespaces = new Hashtable();
		_referenceNamespaces.Add("http://www.w3.org/2001/XMLSchema", "http://www.w3.org/2001/XMLSchema");
		_referenceNamespaces.Add(string.Empty, string.Empty);
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			if (schema.Includes[i] is XmlSchemaImport xmlSchemaImport)
			{
				string @namespace = xmlSchemaImport.Namespace;
				if (@namespace != null && _referenceNamespaces[@namespace] == null)
				{
					_referenceNamespaces.Add(@namespace, @namespace);
				}
			}
		}
		if (schema.TargetNamespace != null && _referenceNamespaces[schema.TargetNamespace] == null)
		{
			_referenceNamespaces.Add(schema.TargetNamespace, schema.TargetNamespace);
		}
	}

	private void Preprocess(XmlSchema schema, string targetNamespace, Compositor compositor)
	{
		if (schema.IsProcessing)
		{
			return;
		}
		schema.IsProcessing = true;
		string targetNamespace2 = schema.TargetNamespace;
		if (targetNamespace2 != null)
		{
			targetNamespace2 = (schema.TargetNamespace = base.NameTable.Add(targetNamespace2));
			if (targetNamespace2.Length == 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidTargetNamespaceAttribute, schema);
			}
			else
			{
				try
				{
					XmlConvert.ToUri(targetNamespace2);
				}
				catch
				{
					SendValidationEvent(System.SR.Sch_InvalidNamespace, schema.TargetNamespace, schema);
				}
			}
		}
		if (schema.Version != null)
		{
			try
			{
				XmlConvert.VerifyTOKEN(schema.Version);
			}
			catch (Exception)
			{
				SendValidationEvent(System.SR.Sch_AttributeValueDataType, "version", schema);
			}
		}
		switch (compositor)
		{
		case Compositor.Root:
			if (targetNamespace == null && schema.TargetNamespace != null)
			{
				targetNamespace = schema.TargetNamespace;
			}
			else if (schema.TargetNamespace == null && targetNamespace != null && targetNamespace.Length == 0)
			{
				targetNamespace = null;
			}
			if (targetNamespace != schema.TargetNamespace)
			{
				SendValidationEvent(System.SR.Sch_MismatchTargetNamespaceEx, targetNamespace, schema.TargetNamespace, schema);
			}
			break;
		case Compositor.Import:
			if (targetNamespace != schema.TargetNamespace)
			{
				SendValidationEvent(System.SR.Sch_MismatchTargetNamespaceImport, targetNamespace, schema.TargetNamespace, schema);
			}
			break;
		case Compositor.Include:
			if (schema.TargetNamespace != null && targetNamespace != schema.TargetNamespace)
			{
				SendValidationEvent(System.SR.Sch_MismatchTargetNamespaceInclude, targetNamespace, schema.TargetNamespace, schema);
			}
			break;
		}
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			SetParent(xmlSchemaExternal, schema);
			PreprocessAnnotation(xmlSchemaExternal);
			string schemaLocation = xmlSchemaExternal.SchemaLocation;
			if (schemaLocation != null)
			{
				try
				{
					XmlConvert.ToUri(schemaLocation);
				}
				catch
				{
					SendValidationEvent(System.SR.Sch_InvalidSchemaLocation, schemaLocation, xmlSchemaExternal);
				}
			}
			else if ((xmlSchemaExternal is XmlSchemaRedefine || xmlSchemaExternal is XmlSchemaInclude) && xmlSchemaExternal.Schema == null)
			{
				SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "schemaLocation", xmlSchemaExternal);
			}
			if (xmlSchemaExternal.Schema != null)
			{
				if (xmlSchemaExternal is XmlSchemaRedefine)
				{
					Preprocess(xmlSchemaExternal.Schema, schema.TargetNamespace, Compositor.Include);
				}
				else if (xmlSchemaExternal is XmlSchemaImport)
				{
					if (((XmlSchemaImport)xmlSchemaExternal).Namespace == null && schema.TargetNamespace == null)
					{
						SendValidationEvent(System.SR.Sch_ImportTargetNamespaceNull, xmlSchemaExternal);
					}
					else if (((XmlSchemaImport)xmlSchemaExternal).Namespace == schema.TargetNamespace)
					{
						SendValidationEvent(System.SR.Sch_ImportTargetNamespace, xmlSchemaExternal);
					}
					Preprocess(xmlSchemaExternal.Schema, ((XmlSchemaImport)xmlSchemaExternal).Namespace, Compositor.Import);
				}
				else
				{
					Preprocess(xmlSchemaExternal.Schema, schema.TargetNamespace, Compositor.Include);
				}
			}
			else
			{
				if (!(xmlSchemaExternal is XmlSchemaImport))
				{
					continue;
				}
				string @namespace = ((XmlSchemaImport)xmlSchemaExternal).Namespace;
				if (@namespace == null)
				{
					continue;
				}
				if (@namespace.Length == 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidNamespaceAttribute, @namespace, xmlSchemaExternal);
					continue;
				}
				try
				{
					XmlConvert.ToUri(@namespace);
				}
				catch (FormatException)
				{
					SendValidationEvent(System.SR.Sch_InvalidNamespace, @namespace, xmlSchemaExternal);
				}
			}
		}
		BuildRefNamespaces(schema);
		_targetNamespace = ((targetNamespace == null) ? string.Empty : targetNamespace);
		if (schema.BlockDefault == XmlSchemaDerivationMethod.All)
		{
			_blockDefault = XmlSchemaDerivationMethod.All;
		}
		else if (schema.BlockDefault == XmlSchemaDerivationMethod.None)
		{
			_blockDefault = XmlSchemaDerivationMethod.Empty;
		}
		else
		{
			if (((uint)schema.BlockDefault & 0xFFFFFFF8u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidBlockDefaultValue, schema);
			}
			_blockDefault = schema.BlockDefault & (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction);
		}
		if (schema.FinalDefault == XmlSchemaDerivationMethod.All)
		{
			_finalDefault = XmlSchemaDerivationMethod.All;
		}
		else if (schema.FinalDefault == XmlSchemaDerivationMethod.None)
		{
			_finalDefault = XmlSchemaDerivationMethod.Empty;
		}
		else
		{
			if (((uint)schema.FinalDefault & 0xFFFFFFE1u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidFinalDefaultValue, schema);
			}
			_finalDefault = schema.FinalDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union);
		}
		_elementFormDefault = schema.ElementFormDefault;
		if (_elementFormDefault == XmlSchemaForm.None)
		{
			_elementFormDefault = XmlSchemaForm.Unqualified;
		}
		_attributeFormDefault = schema.AttributeFormDefault;
		if (_attributeFormDefault == XmlSchemaForm.None)
		{
			_attributeFormDefault = XmlSchemaForm.Unqualified;
		}
		for (int j = 0; j < schema.Includes.Count; j++)
		{
			XmlSchemaExternal xmlSchemaExternal2 = (XmlSchemaExternal)schema.Includes[j];
			if (xmlSchemaExternal2 is XmlSchemaRedefine)
			{
				XmlSchemaRedefine xmlSchemaRedefine = (XmlSchemaRedefine)xmlSchemaExternal2;
				if (xmlSchemaExternal2.Schema != null)
				{
					PreprocessRedefine(xmlSchemaRedefine);
				}
				else
				{
					for (int k = 0; k < xmlSchemaRedefine.Items.Count; k++)
					{
						if (!(xmlSchemaRedefine.Items[k] is XmlSchemaAnnotation))
						{
							SendValidationEvent(System.SR.Sch_RedefineNoSchema, xmlSchemaRedefine);
							break;
						}
					}
				}
			}
			XmlSchema schema2 = xmlSchemaExternal2.Schema;
			if (schema2 != null)
			{
				foreach (XmlSchemaElement value in schema2.Elements.Values)
				{
					AddToTable(schema.Elements, value.QualifiedName, value);
				}
				foreach (XmlSchemaAttribute value2 in schema2.Attributes.Values)
				{
					AddToTable(schema.Attributes, value2.QualifiedName, value2);
				}
				foreach (XmlSchemaGroup value3 in schema2.Groups.Values)
				{
					AddToTable(schema.Groups, value3.QualifiedName, value3);
				}
				foreach (XmlSchemaAttributeGroup value4 in schema2.AttributeGroups.Values)
				{
					AddToTable(schema.AttributeGroups, value4.QualifiedName, value4);
				}
				foreach (XmlSchemaType value5 in schema2.SchemaTypes.Values)
				{
					AddToTable(schema.SchemaTypes, value5.QualifiedName, value5);
				}
				foreach (XmlSchemaNotation value6 in schema2.Notations.Values)
				{
					AddToTable(schema.Notations, value6.QualifiedName, value6);
				}
			}
			ValidateIdAttribute(xmlSchemaExternal2);
		}
		List<XmlSchemaObject> list = new List<XmlSchemaObject>();
		for (int l = 0; l < schema.Items.Count; l++)
		{
			SetParent(schema.Items[l], schema);
			if (schema.Items[l] is XmlSchemaAttribute xmlSchemaAttribute2)
			{
				PreprocessAttribute(xmlSchemaAttribute2);
				AddToTable(schema.Attributes, xmlSchemaAttribute2.QualifiedName, xmlSchemaAttribute2);
			}
			else if (schema.Items[l] is XmlSchemaAttributeGroup)
			{
				XmlSchemaAttributeGroup xmlSchemaAttributeGroup2 = (XmlSchemaAttributeGroup)schema.Items[l];
				PreprocessAttributeGroup(xmlSchemaAttributeGroup2);
				AddToTable(schema.AttributeGroups, xmlSchemaAttributeGroup2.QualifiedName, xmlSchemaAttributeGroup2);
			}
			else if (schema.Items[l] is XmlSchemaComplexType)
			{
				XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)schema.Items[l];
				PreprocessComplexType(xmlSchemaComplexType, local: false);
				AddToTable(schema.SchemaTypes, xmlSchemaComplexType.QualifiedName, xmlSchemaComplexType);
			}
			else if (schema.Items[l] is XmlSchemaSimpleType)
			{
				XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)schema.Items[l];
				PreprocessSimpleType(xmlSchemaSimpleType, local: false);
				AddToTable(schema.SchemaTypes, xmlSchemaSimpleType.QualifiedName, xmlSchemaSimpleType);
			}
			else if (schema.Items[l] is XmlSchemaElement)
			{
				XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)schema.Items[l];
				PreprocessElement(xmlSchemaElement2);
				AddToTable(schema.Elements, xmlSchemaElement2.QualifiedName, xmlSchemaElement2);
			}
			else if (schema.Items[l] is XmlSchemaGroup)
			{
				XmlSchemaGroup xmlSchemaGroup2 = (XmlSchemaGroup)schema.Items[l];
				PreprocessGroup(xmlSchemaGroup2);
				AddToTable(schema.Groups, xmlSchemaGroup2.QualifiedName, xmlSchemaGroup2);
			}
			else if (schema.Items[l] is XmlSchemaNotation)
			{
				XmlSchemaNotation xmlSchemaNotation2 = (XmlSchemaNotation)schema.Items[l];
				PreprocessNotation(xmlSchemaNotation2);
				AddToTable(schema.Notations, xmlSchemaNotation2.QualifiedName, xmlSchemaNotation2);
			}
			else if (!(schema.Items[l] is XmlSchemaAnnotation))
			{
				SendValidationEvent(System.SR.Sch_InvalidCollection, schema.Items[l]);
				list.Add(schema.Items[l]);
			}
		}
		for (int m = 0; m < list.Count; m++)
		{
			schema.Items.Remove(list[m]);
		}
		schema.IsProcessing = false;
	}

	private void PreprocessRedefine(XmlSchemaRedefine redefine)
	{
		for (int i = 0; i < redefine.Items.Count; i++)
		{
			SetParent(redefine.Items[i], redefine);
			if (redefine.Items[i] is XmlSchemaGroup xmlSchemaGroup)
			{
				PreprocessGroup(xmlSchemaGroup);
				if (redefine.Groups[xmlSchemaGroup.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_GroupDoubleRedefine, xmlSchemaGroup);
					continue;
				}
				AddToTable(redefine.Groups, xmlSchemaGroup.QualifiedName, xmlSchemaGroup);
				xmlSchemaGroup.Redefined = (XmlSchemaGroup)redefine.Schema.Groups[xmlSchemaGroup.QualifiedName];
				if (xmlSchemaGroup.Redefined != null)
				{
					CheckRefinedGroup(xmlSchemaGroup);
				}
				else
				{
					SendValidationEvent(System.SR.Sch_GroupRedefineNotFound, xmlSchemaGroup);
				}
			}
			else if (redefine.Items[i] is XmlSchemaAttributeGroup)
			{
				XmlSchemaAttributeGroup xmlSchemaAttributeGroup = (XmlSchemaAttributeGroup)redefine.Items[i];
				PreprocessAttributeGroup(xmlSchemaAttributeGroup);
				if (redefine.AttributeGroups[xmlSchemaAttributeGroup.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_AttrGroupDoubleRedefine, xmlSchemaAttributeGroup);
					continue;
				}
				AddToTable(redefine.AttributeGroups, xmlSchemaAttributeGroup.QualifiedName, xmlSchemaAttributeGroup);
				xmlSchemaAttributeGroup.Redefined = (XmlSchemaAttributeGroup)redefine.Schema.AttributeGroups[xmlSchemaAttributeGroup.QualifiedName];
				if (xmlSchemaAttributeGroup.Redefined != null)
				{
					CheckRefinedAttributeGroup(xmlSchemaAttributeGroup);
				}
				else
				{
					SendValidationEvent(System.SR.Sch_AttrGroupRedefineNotFound, xmlSchemaAttributeGroup);
				}
			}
			else if (redefine.Items[i] is XmlSchemaComplexType)
			{
				XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)redefine.Items[i];
				PreprocessComplexType(xmlSchemaComplexType, local: false);
				if (redefine.SchemaTypes[xmlSchemaComplexType.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_ComplexTypeDoubleRedefine, xmlSchemaComplexType);
					continue;
				}
				AddToTable(redefine.SchemaTypes, xmlSchemaComplexType.QualifiedName, xmlSchemaComplexType);
				XmlSchemaType xmlSchemaType = (XmlSchemaType)redefine.Schema.SchemaTypes[xmlSchemaComplexType.QualifiedName];
				if (xmlSchemaType != null)
				{
					if (xmlSchemaType is XmlSchemaComplexType)
					{
						xmlSchemaComplexType.Redefined = xmlSchemaType;
						CheckRefinedComplexType(xmlSchemaComplexType);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_SimpleToComplexTypeRedefine, xmlSchemaComplexType);
					}
				}
				else
				{
					SendValidationEvent(System.SR.Sch_ComplexTypeRedefineNotFound, xmlSchemaComplexType);
				}
			}
			else
			{
				if (!(redefine.Items[i] is XmlSchemaSimpleType))
				{
					continue;
				}
				XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)redefine.Items[i];
				PreprocessSimpleType(xmlSchemaSimpleType, local: false);
				if (redefine.SchemaTypes[xmlSchemaSimpleType.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_SimpleTypeDoubleRedefine, xmlSchemaSimpleType);
					continue;
				}
				AddToTable(redefine.SchemaTypes, xmlSchemaSimpleType.QualifiedName, xmlSchemaSimpleType);
				XmlSchemaType xmlSchemaType2 = (XmlSchemaType)redefine.Schema.SchemaTypes[xmlSchemaSimpleType.QualifiedName];
				if (xmlSchemaType2 != null)
				{
					if (xmlSchemaType2 is XmlSchemaSimpleType)
					{
						xmlSchemaSimpleType.Redefined = xmlSchemaType2;
						CheckRefinedSimpleType(xmlSchemaSimpleType);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_ComplexToSimpleTypeRedefine, xmlSchemaSimpleType);
					}
				}
				else
				{
					SendValidationEvent(System.SR.Sch_SimpleTypeRedefineNotFound, xmlSchemaSimpleType);
				}
			}
		}
		foreach (DictionaryEntry group in redefine.Groups)
		{
			redefine.Schema.Groups.Insert((XmlQualifiedName)group.Key, (XmlSchemaObject)group.Value);
		}
		foreach (DictionaryEntry attributeGroup in redefine.AttributeGroups)
		{
			redefine.Schema.AttributeGroups.Insert((XmlQualifiedName)attributeGroup.Key, (XmlSchemaObject)attributeGroup.Value);
		}
		foreach (DictionaryEntry schemaType in redefine.SchemaTypes)
		{
			redefine.Schema.SchemaTypes.Insert((XmlQualifiedName)schemaType.Key, (XmlSchemaObject)schemaType.Value);
		}
	}

	private int CountGroupSelfReference(XmlSchemaObjectCollection items, XmlQualifiedName name)
	{
		int num = 0;
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] is XmlSchemaGroupRef xmlSchemaGroupRef)
			{
				if (xmlSchemaGroupRef.RefName == name)
				{
					if (xmlSchemaGroupRef.MinOccurs != 1m || xmlSchemaGroupRef.MaxOccurs != 1m)
					{
						SendValidationEvent(System.SR.Sch_MinMaxGroupRedefine, xmlSchemaGroupRef);
					}
					num++;
				}
			}
			else if (items[i] is XmlSchemaGroupBase)
			{
				num += CountGroupSelfReference(((XmlSchemaGroupBase)items[i]).Items, name);
			}
			if (num > 1)
			{
				break;
			}
		}
		return num;
	}

	private void CheckRefinedGroup(XmlSchemaGroup group)
	{
		int num = 0;
		if (group.Particle != null)
		{
			num = CountGroupSelfReference(group.Particle.Items, group.QualifiedName);
		}
		if (num > 1)
		{
			SendValidationEvent(System.SR.Sch_MultipleGroupSelfRef, group);
		}
	}

	private void CheckRefinedAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
	{
		int num = 0;
		for (int i = 0; i < attributeGroup.Attributes.Count; i++)
		{
			if (attributeGroup.Attributes[i] is XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef && xmlSchemaAttributeGroupRef.RefName == attributeGroup.QualifiedName)
			{
				num++;
			}
		}
		if (num > 1)
		{
			SendValidationEvent(System.SR.Sch_MultipleAttrGroupSelfRef, attributeGroup);
		}
	}

	private void CheckRefinedSimpleType(XmlSchemaSimpleType stype)
	{
		if (stype.Content != null && stype.Content is XmlSchemaSimpleTypeRestriction)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)stype.Content;
			if (xmlSchemaSimpleTypeRestriction.BaseTypeName == stype.QualifiedName)
			{
				return;
			}
		}
		SendValidationEvent(System.SR.Sch_InvalidTypeRedefine, stype);
	}

	private void CheckRefinedComplexType(XmlSchemaComplexType ctype)
	{
		if (ctype.ContentModel != null)
		{
			XmlQualifiedName xmlQualifiedName;
			if (ctype.ContentModel is XmlSchemaComplexContent)
			{
				XmlSchemaComplexContent xmlSchemaComplexContent = (XmlSchemaComplexContent)ctype.ContentModel;
				xmlQualifiedName = ((!(xmlSchemaComplexContent.Content is XmlSchemaComplexContentRestriction)) ? ((XmlSchemaComplexContentExtension)xmlSchemaComplexContent.Content).BaseTypeName : ((XmlSchemaComplexContentRestriction)xmlSchemaComplexContent.Content).BaseTypeName);
			}
			else
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)ctype.ContentModel;
				xmlQualifiedName = ((!(xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentRestriction)) ? ((XmlSchemaSimpleContentExtension)xmlSchemaSimpleContent.Content).BaseTypeName : ((XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContent.Content).BaseTypeName);
			}
			if (xmlQualifiedName == ctype.QualifiedName)
			{
				return;
			}
		}
		SendValidationEvent(System.SR.Sch_InvalidTypeRedefine, ctype);
	}

	private void PreprocessAttribute(XmlSchemaAttribute attribute)
	{
		if (attribute.Name != null)
		{
			ValidateNameAttribute(attribute);
			attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", attribute);
		}
		if (attribute.Use != 0)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "use", attribute);
		}
		if (attribute.Form != 0)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "form", attribute);
		}
		PreprocessAttributeContent(attribute);
		ValidateIdAttribute(attribute);
	}

	private void PreprocessLocalAttribute(XmlSchemaAttribute attribute)
	{
		if (attribute.Name != null)
		{
			ValidateNameAttribute(attribute);
			PreprocessAttributeContent(attribute);
			attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, (attribute.Form == XmlSchemaForm.Qualified || (attribute.Form == XmlSchemaForm.None && _attributeFormDefault == XmlSchemaForm.Qualified)) ? _targetNamespace : null));
		}
		else
		{
			PreprocessAnnotation(attribute);
			if (attribute.RefName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_AttributeNameRef, "???", attribute);
			}
			else
			{
				ValidateQNameAttribute(attribute, "ref", attribute.RefName);
			}
			if (!attribute.SchemaTypeName.IsEmpty || attribute.SchemaType != null || attribute.Form != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidAttributeRef, attribute);
			}
			attribute.SetQualifiedName(attribute.RefName);
		}
		ValidateIdAttribute(attribute);
	}

	private void PreprocessAttributeContent(XmlSchemaAttribute attribute)
	{
		PreprocessAnnotation(attribute);
		if (_schema.TargetNamespace == "http://www.w3.org/2001/XMLSchema-instance")
		{
			SendValidationEvent(System.SR.Sch_TargetNamespaceXsi, attribute);
		}
		if (!attribute.RefName.IsEmpty)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "ref", attribute);
		}
		if (attribute.DefaultValue != null && attribute.FixedValue != null)
		{
			SendValidationEvent(System.SR.Sch_DefaultFixedAttributes, attribute);
		}
		if (attribute.DefaultValue != null && attribute.Use != XmlSchemaUse.Optional && attribute.Use != 0)
		{
			SendValidationEvent(System.SR.Sch_OptionalDefaultAttribute, attribute);
		}
		if (attribute.Name == _xmlns)
		{
			SendValidationEvent(System.SR.Sch_XmlNsAttribute, attribute);
		}
		if (attribute.SchemaType != null)
		{
			SetParent(attribute.SchemaType, attribute);
			if (!attribute.SchemaTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_TypeMutualExclusive, attribute);
			}
			PreprocessSimpleType(attribute.SchemaType, local: true);
		}
		if (!attribute.SchemaTypeName.IsEmpty)
		{
			ValidateQNameAttribute(attribute, "type", attribute.SchemaTypeName);
		}
	}

	private void PreprocessAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
	{
		if (attributeGroup.Name != null)
		{
			ValidateNameAttribute(attributeGroup);
			attributeGroup.SetQualifiedName(new XmlQualifiedName(attributeGroup.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", attributeGroup);
		}
		PreprocessAttributes(attributeGroup.Attributes, attributeGroup.AnyAttribute, attributeGroup);
		PreprocessAnnotation(attributeGroup);
		ValidateIdAttribute(attributeGroup);
	}

	private void PreprocessElement(XmlSchemaElement element)
	{
		if (element.Name != null)
		{
			ValidateNameAttribute(element);
			element.SetQualifiedName(new XmlQualifiedName(element.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", element);
		}
		PreprocessElementContent(element);
		if (element.Final == XmlSchemaDerivationMethod.All)
		{
			element.SetFinalResolved(XmlSchemaDerivationMethod.All);
		}
		else if (element.Final == XmlSchemaDerivationMethod.None)
		{
			if (_finalDefault == XmlSchemaDerivationMethod.All)
			{
				element.SetFinalResolved(XmlSchemaDerivationMethod.All);
			}
			else
			{
				element.SetFinalResolved(_finalDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
		}
		else
		{
			if (((uint)element.Final & 0xFFFFFFF9u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidElementFinalValue, element);
			}
			element.SetFinalResolved(element.Final & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
		}
		if (element.Form != 0)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "form", element);
		}
		if (element.MinOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "minOccurs", element);
		}
		if (element.MaxOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "maxOccurs", element);
		}
		if (!element.SubstitutionGroup.IsEmpty)
		{
			ValidateQNameAttribute(element, "type", element.SubstitutionGroup);
		}
		ValidateIdAttribute(element);
	}

	private void PreprocessLocalElement(XmlSchemaElement element)
	{
		if (element.Name != null)
		{
			ValidateNameAttribute(element);
			PreprocessElementContent(element);
			element.SetQualifiedName(new XmlQualifiedName(element.Name, (element.Form == XmlSchemaForm.Qualified || (element.Form == XmlSchemaForm.None && _elementFormDefault == XmlSchemaForm.Qualified)) ? _targetNamespace : null));
		}
		else
		{
			PreprocessAnnotation(element);
			if (element.RefName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_ElementNameRef, element);
			}
			else
			{
				ValidateQNameAttribute(element, "ref", element.RefName);
			}
			if (!element.SchemaTypeName.IsEmpty || element.IsAbstract || element.Block != XmlSchemaDerivationMethod.None || element.SchemaType != null || element.HasConstraints || element.DefaultValue != null || element.Form != 0 || element.FixedValue != null || element.HasNillableAttribute)
			{
				SendValidationEvent(System.SR.Sch_InvalidElementRef, element);
			}
			if (element.DefaultValue != null && element.FixedValue != null)
			{
				SendValidationEvent(System.SR.Sch_DefaultFixedAttributes, element);
			}
			element.SetQualifiedName(element.RefName);
		}
		if (element.MinOccurs > element.MaxOccurs)
		{
			element.MinOccurs = 0m;
			SendValidationEvent(System.SR.Sch_MinGtMax, element);
		}
		if (element.IsAbstract)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "abstract", element);
		}
		if (element.Final != XmlSchemaDerivationMethod.None)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "final", element);
		}
		if (!element.SubstitutionGroup.IsEmpty)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "substitutionGroup", element);
		}
		ValidateIdAttribute(element);
	}

	private void PreprocessElementContent(XmlSchemaElement element)
	{
		PreprocessAnnotation(element);
		if (!element.RefName.IsEmpty)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "ref", element);
		}
		if (element.Block == XmlSchemaDerivationMethod.All)
		{
			element.SetBlockResolved(XmlSchemaDerivationMethod.All);
		}
		else if (element.Block == XmlSchemaDerivationMethod.None)
		{
			if (_blockDefault == XmlSchemaDerivationMethod.All)
			{
				element.SetBlockResolved(XmlSchemaDerivationMethod.All);
			}
			else
			{
				element.SetBlockResolved(_blockDefault & (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
		}
		else
		{
			if (((uint)element.Block & 0xFFFFFFF8u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidElementBlockValue, element);
			}
			element.SetBlockResolved(element.Block & (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
		}
		if (element.SchemaType != null)
		{
			SetParent(element.SchemaType, element);
			if (!element.SchemaTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_TypeMutualExclusive, element);
			}
			if (element.SchemaType is XmlSchemaComplexType)
			{
				PreprocessComplexType((XmlSchemaComplexType)element.SchemaType, local: true);
			}
			else
			{
				PreprocessSimpleType((XmlSchemaSimpleType)element.SchemaType, local: true);
			}
		}
		if (!element.SchemaTypeName.IsEmpty)
		{
			ValidateQNameAttribute(element, "type", element.SchemaTypeName);
		}
		if (element.DefaultValue != null && element.FixedValue != null)
		{
			SendValidationEvent(System.SR.Sch_DefaultFixedAttributes, element);
		}
		for (int i = 0; i < element.Constraints.Count; i++)
		{
			SetParent(element.Constraints[i], element);
			PreprocessIdentityConstraint((XmlSchemaIdentityConstraint)element.Constraints[i]);
		}
	}

	private void PreprocessIdentityConstraint(XmlSchemaIdentityConstraint constraint)
	{
		bool flag = true;
		PreprocessAnnotation(constraint);
		if (constraint.Name != null)
		{
			ValidateNameAttribute(constraint);
			constraint.SetQualifiedName(new XmlQualifiedName(constraint.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", constraint);
			flag = false;
		}
		if (_schema.IdentityConstraints[constraint.QualifiedName] != null)
		{
			SendValidationEvent(System.SR.Sch_DupIdentityConstraint, constraint.QualifiedName.ToString(), constraint);
			flag = false;
		}
		else
		{
			_schema.IdentityConstraints.Add(constraint.QualifiedName, constraint);
		}
		if (constraint.Selector == null)
		{
			SendValidationEvent(System.SR.Sch_IdConstraintNoSelector, constraint);
			flag = false;
		}
		if (constraint.Fields.Count == 0)
		{
			SendValidationEvent(System.SR.Sch_IdConstraintNoFields, constraint);
			flag = false;
		}
		if (constraint is XmlSchemaKeyref)
		{
			XmlSchemaKeyref xmlSchemaKeyref = (XmlSchemaKeyref)constraint;
			if (xmlSchemaKeyref.Refer.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_IdConstraintNoRefer, constraint);
				flag = false;
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaKeyref, "refer", xmlSchemaKeyref.Refer);
			}
		}
		if (flag)
		{
			ValidateIdAttribute(constraint);
			ValidateIdAttribute(constraint.Selector);
			SetParent(constraint.Selector, constraint);
			for (int i = 0; i < constraint.Fields.Count; i++)
			{
				SetParent(constraint.Fields[i], constraint);
				ValidateIdAttribute(constraint.Fields[i]);
			}
		}
	}

	private void PreprocessSimpleType(XmlSchemaSimpleType simpleType, bool local)
	{
		if (local)
		{
			if (simpleType.Name != null)
			{
				SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "name", simpleType);
			}
		}
		else
		{
			if (simpleType.Name != null)
			{
				ValidateNameAttribute(simpleType);
				simpleType.SetQualifiedName(new XmlQualifiedName(simpleType.Name, _targetNamespace));
			}
			else
			{
				SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", simpleType);
			}
			if (simpleType.Final == XmlSchemaDerivationMethod.All)
			{
				simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
			}
			else if (simpleType.Final == XmlSchemaDerivationMethod.None)
			{
				if (_finalDefault == XmlSchemaDerivationMethod.All)
				{
					simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
				}
				else
				{
					simpleType.SetFinalResolved(_finalDefault & (XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union));
				}
			}
			else
			{
				if (((uint)simpleType.Final & 0xFFFFFFE3u) != 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidSimpleTypeFinalValue, simpleType);
				}
				simpleType.SetFinalResolved(simpleType.Final & (XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union));
			}
		}
		if (simpleType.Content == null)
		{
			SendValidationEvent(System.SR.Sch_NoSimpleTypeContent, simpleType);
		}
		else if (simpleType.Content is XmlSchemaSimpleTypeRestriction)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
			SetParent(xmlSchemaSimpleTypeRestriction, simpleType);
			for (int i = 0; i < xmlSchemaSimpleTypeRestriction.Facets.Count; i++)
			{
				SetParent(xmlSchemaSimpleTypeRestriction.Facets[i], xmlSchemaSimpleTypeRestriction);
			}
			if (xmlSchemaSimpleTypeRestriction.BaseType != null)
			{
				if (!xmlSchemaSimpleTypeRestriction.BaseTypeName.IsEmpty)
				{
					SendValidationEvent(System.SR.Sch_SimpleTypeRestRefBase, xmlSchemaSimpleTypeRestriction);
				}
				PreprocessSimpleType(xmlSchemaSimpleTypeRestriction.BaseType, local: true);
			}
			else if (xmlSchemaSimpleTypeRestriction.BaseTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_SimpleTypeRestRefBaseNone, xmlSchemaSimpleTypeRestriction);
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaSimpleTypeRestriction, "base", xmlSchemaSimpleTypeRestriction.BaseTypeName);
			}
			PreprocessAnnotation(xmlSchemaSimpleTypeRestriction);
			ValidateIdAttribute(xmlSchemaSimpleTypeRestriction);
		}
		else if (simpleType.Content is XmlSchemaSimpleTypeList)
		{
			XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = (XmlSchemaSimpleTypeList)simpleType.Content;
			SetParent(xmlSchemaSimpleTypeList, simpleType);
			if (xmlSchemaSimpleTypeList.ItemType != null)
			{
				if (!xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
				{
					SendValidationEvent(System.SR.Sch_SimpleTypeListRefBase, xmlSchemaSimpleTypeList);
				}
				SetParent(xmlSchemaSimpleTypeList.ItemType, xmlSchemaSimpleTypeList);
				PreprocessSimpleType(xmlSchemaSimpleTypeList.ItemType, local: true);
			}
			else if (xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_SimpleTypeListRefBaseNone, xmlSchemaSimpleTypeList);
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaSimpleTypeList, "itemType", xmlSchemaSimpleTypeList.ItemTypeName);
			}
			PreprocessAnnotation(xmlSchemaSimpleTypeList);
			ValidateIdAttribute(xmlSchemaSimpleTypeList);
		}
		else
		{
			XmlSchemaSimpleTypeUnion xmlSchemaSimpleTypeUnion = (XmlSchemaSimpleTypeUnion)simpleType.Content;
			SetParent(xmlSchemaSimpleTypeUnion, simpleType);
			int num = xmlSchemaSimpleTypeUnion.BaseTypes.Count;
			if (xmlSchemaSimpleTypeUnion.MemberTypes != null)
			{
				num += xmlSchemaSimpleTypeUnion.MemberTypes.Length;
				for (int j = 0; j < xmlSchemaSimpleTypeUnion.MemberTypes.Length; j++)
				{
					ValidateQNameAttribute(xmlSchemaSimpleTypeUnion, "memberTypes", xmlSchemaSimpleTypeUnion.MemberTypes[j]);
				}
			}
			if (num == 0)
			{
				SendValidationEvent(System.SR.Sch_SimpleTypeUnionNoBase, xmlSchemaSimpleTypeUnion);
			}
			for (int k = 0; k < xmlSchemaSimpleTypeUnion.BaseTypes.Count; k++)
			{
				SetParent(xmlSchemaSimpleTypeUnion.BaseTypes[k], xmlSchemaSimpleTypeUnion);
				PreprocessSimpleType((XmlSchemaSimpleType)xmlSchemaSimpleTypeUnion.BaseTypes[k], local: true);
			}
			PreprocessAnnotation(xmlSchemaSimpleTypeUnion);
			ValidateIdAttribute(xmlSchemaSimpleTypeUnion);
		}
		ValidateIdAttribute(simpleType);
	}

	private void PreprocessComplexType(XmlSchemaComplexType complexType, bool local)
	{
		if (local)
		{
			if (complexType.Name != null)
			{
				SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "name", complexType);
			}
		}
		else
		{
			if (complexType.Name != null)
			{
				ValidateNameAttribute(complexType);
				complexType.SetQualifiedName(new XmlQualifiedName(complexType.Name, _targetNamespace));
			}
			else
			{
				SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", complexType);
			}
			if (complexType.Block == XmlSchemaDerivationMethod.All)
			{
				complexType.SetBlockResolved(XmlSchemaDerivationMethod.All);
			}
			else if (complexType.Block == XmlSchemaDerivationMethod.None)
			{
				complexType.SetBlockResolved(_blockDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
			else
			{
				if (((uint)complexType.Block & 0xFFFFFFF9u) != 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidComplexTypeBlockValue, complexType);
				}
				complexType.SetBlockResolved(complexType.Block & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
			if (complexType.Final == XmlSchemaDerivationMethod.All)
			{
				complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
			}
			else if (complexType.Final == XmlSchemaDerivationMethod.None)
			{
				if (_finalDefault == XmlSchemaDerivationMethod.All)
				{
					complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
				}
				else
				{
					complexType.SetFinalResolved(_finalDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
				}
			}
			else
			{
				if (((uint)complexType.Final & 0xFFFFFFF9u) != 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidComplexTypeFinalValue, complexType);
				}
				complexType.SetFinalResolved(complexType.Final & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
		}
		if (complexType.ContentModel != null)
		{
			SetParent(complexType.ContentModel, complexType);
			PreprocessAnnotation(complexType.ContentModel);
			if (complexType.Particle == null)
			{
				_ = complexType.Attributes;
			}
			if (complexType.ContentModel is XmlSchemaSimpleContent)
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;
				if (xmlSchemaSimpleContent.Content == null)
				{
					if (complexType.QualifiedName == XmlQualifiedName.Empty)
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExt, complexType);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
					}
				}
				else
				{
					SetParent(xmlSchemaSimpleContent.Content, xmlSchemaSimpleContent);
					PreprocessAnnotation(xmlSchemaSimpleContent.Content);
					if (xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentExtension)
					{
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)xmlSchemaSimpleContent.Content;
						if (xmlSchemaSimpleContentExtension.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaSimpleContentExtension);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaSimpleContentExtension, "base", xmlSchemaSimpleContentExtension.BaseTypeName);
						}
						PreprocessAttributes(xmlSchemaSimpleContentExtension.Attributes, xmlSchemaSimpleContentExtension.AnyAttribute, xmlSchemaSimpleContentExtension);
						ValidateIdAttribute(xmlSchemaSimpleContentExtension);
					}
					else
					{
						XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction = (XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContent.Content;
						if (xmlSchemaSimpleContentRestriction.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaSimpleContentRestriction);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaSimpleContentRestriction, "base", xmlSchemaSimpleContentRestriction.BaseTypeName);
						}
						if (xmlSchemaSimpleContentRestriction.BaseType != null)
						{
							SetParent(xmlSchemaSimpleContentRestriction.BaseType, xmlSchemaSimpleContentRestriction);
							PreprocessSimpleType(xmlSchemaSimpleContentRestriction.BaseType, local: true);
						}
						PreprocessAttributes(xmlSchemaSimpleContentRestriction.Attributes, xmlSchemaSimpleContentRestriction.AnyAttribute, xmlSchemaSimpleContentRestriction);
						ValidateIdAttribute(xmlSchemaSimpleContentRestriction);
					}
				}
				ValidateIdAttribute(xmlSchemaSimpleContent);
			}
			else
			{
				XmlSchemaComplexContent xmlSchemaComplexContent = (XmlSchemaComplexContent)complexType.ContentModel;
				if (xmlSchemaComplexContent.Content == null)
				{
					if (complexType.QualifiedName == XmlQualifiedName.Empty)
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExt, complexType);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
					}
				}
				else
				{
					if (!xmlSchemaComplexContent.HasMixedAttribute && complexType.IsMixed)
					{
						xmlSchemaComplexContent.IsMixed = true;
					}
					SetParent(xmlSchemaComplexContent.Content, xmlSchemaComplexContent);
					PreprocessAnnotation(xmlSchemaComplexContent.Content);
					if (xmlSchemaComplexContent.Content is XmlSchemaComplexContentExtension)
					{
						XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = (XmlSchemaComplexContentExtension)xmlSchemaComplexContent.Content;
						if (xmlSchemaComplexContentExtension.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaComplexContentExtension);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaComplexContentExtension, "base", xmlSchemaComplexContentExtension.BaseTypeName);
						}
						if (xmlSchemaComplexContentExtension.Particle != null)
						{
							SetParent(xmlSchemaComplexContentExtension.Particle, xmlSchemaComplexContentExtension);
							PreprocessParticle(xmlSchemaComplexContentExtension.Particle);
						}
						PreprocessAttributes(xmlSchemaComplexContentExtension.Attributes, xmlSchemaComplexContentExtension.AnyAttribute, xmlSchemaComplexContentExtension);
						ValidateIdAttribute(xmlSchemaComplexContentExtension);
					}
					else
					{
						XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction = (XmlSchemaComplexContentRestriction)xmlSchemaComplexContent.Content;
						if (xmlSchemaComplexContentRestriction.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaComplexContentRestriction);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaComplexContentRestriction, "base", xmlSchemaComplexContentRestriction.BaseTypeName);
						}
						if (xmlSchemaComplexContentRestriction.Particle != null)
						{
							SetParent(xmlSchemaComplexContentRestriction.Particle, xmlSchemaComplexContentRestriction);
							PreprocessParticle(xmlSchemaComplexContentRestriction.Particle);
						}
						PreprocessAttributes(xmlSchemaComplexContentRestriction.Attributes, xmlSchemaComplexContentRestriction.AnyAttribute, xmlSchemaComplexContentRestriction);
						ValidateIdAttribute(xmlSchemaComplexContentRestriction);
					}
					ValidateIdAttribute(xmlSchemaComplexContent);
				}
			}
		}
		else
		{
			if (complexType.Particle != null)
			{
				SetParent(complexType.Particle, complexType);
				PreprocessParticle(complexType.Particle);
			}
			PreprocessAttributes(complexType.Attributes, complexType.AnyAttribute, complexType);
		}
		ValidateIdAttribute(complexType);
	}

	private void PreprocessGroup(XmlSchemaGroup group)
	{
		if (group.Name != null)
		{
			ValidateNameAttribute(group);
			group.SetQualifiedName(new XmlQualifiedName(group.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", group);
		}
		if (group.Particle == null)
		{
			SendValidationEvent(System.SR.Sch_NoGroupParticle, group);
			return;
		}
		if (group.Particle.MinOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "minOccurs", group.Particle);
		}
		if (group.Particle.MaxOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "maxOccurs", group.Particle);
		}
		PreprocessParticle(group.Particle);
		PreprocessAnnotation(group);
		ValidateIdAttribute(group);
	}

	private void PreprocessNotation(XmlSchemaNotation notation)
	{
		if (notation.Name != null)
		{
			ValidateNameAttribute(notation);
			notation.QualifiedName = new XmlQualifiedName(notation.Name, _targetNamespace);
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", notation);
		}
		if (notation.Public != null)
		{
			try
			{
				XmlConvert.ToUri(notation.Public);
			}
			catch
			{
				SendValidationEvent(System.SR.Sch_InvalidPublicAttribute, notation.Public, notation);
			}
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "public", notation);
		}
		if (notation.System != null)
		{
			try
			{
				XmlConvert.ToUri(notation.System);
			}
			catch
			{
				SendValidationEvent(System.SR.Sch_InvalidSystemAttribute, notation.System, notation);
			}
		}
		PreprocessAnnotation(notation);
		ValidateIdAttribute(notation);
	}

	private void PreprocessParticle(XmlSchemaParticle particle)
	{
		if (particle is XmlSchemaAll xmlSchemaAll)
		{
			if (particle.MinOccurs != 0m && particle.MinOccurs != 1m)
			{
				particle.MinOccurs = 1m;
				SendValidationEvent(System.SR.Sch_InvalidAllMin, particle);
			}
			if (particle.MaxOccurs != 1m)
			{
				particle.MaxOccurs = 1m;
				SendValidationEvent(System.SR.Sch_InvalidAllMax, particle);
			}
			for (int i = 0; i < xmlSchemaAll.Items.Count; i++)
			{
				XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)xmlSchemaAll.Items[i];
				if (xmlSchemaElement.MaxOccurs != 0m && xmlSchemaElement.MaxOccurs != 1m)
				{
					xmlSchemaElement.MaxOccurs = 1m;
					SendValidationEvent(System.SR.Sch_InvalidAllElementMax, xmlSchemaElement);
				}
				SetParent(xmlSchemaElement, particle);
				PreprocessLocalElement(xmlSchemaElement);
			}
		}
		else
		{
			if (particle.MinOccurs > particle.MaxOccurs)
			{
				particle.MinOccurs = particle.MaxOccurs;
				SendValidationEvent(System.SR.Sch_MinGtMax, particle);
			}
			if (particle is XmlSchemaChoice xmlSchemaChoice)
			{
				XmlSchemaObjectCollection items = xmlSchemaChoice.Items;
				for (int j = 0; j < items.Count; j++)
				{
					SetParent(items[j], particle);
					if (items[j] is XmlSchemaElement element)
					{
						PreprocessLocalElement(element);
					}
					else
					{
						PreprocessParticle((XmlSchemaParticle)items[j]);
					}
				}
			}
			else if (particle is XmlSchemaSequence)
			{
				XmlSchemaObjectCollection items2 = ((XmlSchemaSequence)particle).Items;
				for (int k = 0; k < items2.Count; k++)
				{
					SetParent(items2[k], particle);
					if (items2[k] is XmlSchemaElement element2)
					{
						PreprocessLocalElement(element2);
					}
					else
					{
						PreprocessParticle((XmlSchemaParticle)items2[k]);
					}
				}
			}
			else if (particle is XmlSchemaGroupRef)
			{
				XmlSchemaGroupRef xmlSchemaGroupRef = (XmlSchemaGroupRef)particle;
				if (xmlSchemaGroupRef.RefName.IsEmpty)
				{
					SendValidationEvent(System.SR.Sch_MissAttribute, "ref", xmlSchemaGroupRef);
				}
				else
				{
					ValidateQNameAttribute(xmlSchemaGroupRef, "ref", xmlSchemaGroupRef.RefName);
				}
			}
			else if (particle is XmlSchemaAny)
			{
				try
				{
					((XmlSchemaAny)particle).BuildNamespaceListV1Compat(_targetNamespace);
				}
				catch
				{
					SendValidationEvent(System.SR.Sch_InvalidAny, particle);
				}
			}
		}
		PreprocessAnnotation(particle);
		ValidateIdAttribute(particle);
	}

	private void PreprocessAttributes(XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute anyAttribute, XmlSchemaObject parent)
	{
		for (int i = 0; i < attributes.Count; i++)
		{
			SetParent(attributes[i], parent);
			if (attributes[i] is XmlSchemaAttribute attribute)
			{
				PreprocessLocalAttribute(attribute);
				continue;
			}
			XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef = (XmlSchemaAttributeGroupRef)attributes[i];
			if (xmlSchemaAttributeGroupRef.RefName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_MissAttribute, "ref", xmlSchemaAttributeGroupRef);
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaAttributeGroupRef, "ref", xmlSchemaAttributeGroupRef.RefName);
			}
			PreprocessAnnotation(attributes[i]);
			ValidateIdAttribute(attributes[i]);
		}
		if (anyAttribute != null)
		{
			try
			{
				SetParent(anyAttribute, parent);
				PreprocessAnnotation(anyAttribute);
				anyAttribute.BuildNamespaceListV1Compat(_targetNamespace);
			}
			catch
			{
				SendValidationEvent(System.SR.Sch_InvalidAnyAttribute, anyAttribute);
			}
			ValidateIdAttribute(anyAttribute);
		}
	}

	private void ValidateIdAttribute(XmlSchemaObject xso)
	{
		if (xso.IdAttribute == null)
		{
			return;
		}
		try
		{
			xso.IdAttribute = base.NameTable.Add(XmlConvert.VerifyNCName(xso.IdAttribute));
			if (_schema.Ids[xso.IdAttribute] != null)
			{
				SendValidationEvent(System.SR.Sch_DupIdAttribute, xso);
			}
			else
			{
				_schema.Ids.Add(xso.IdAttribute, xso);
			}
		}
		catch (Exception ex)
		{
			SendValidationEvent(System.SR.Sch_InvalidIdAttribute, ex.Message, xso);
		}
	}

	private void ValidateNameAttribute(XmlSchemaObject xso)
	{
		string nameAttribute = xso.NameAttribute;
		if (nameAttribute == null || nameAttribute.Length == 0)
		{
			SendValidationEvent(System.SR.Sch_InvalidNameAttributeEx, null, System.SR.Sch_NullValue, xso);
		}
		nameAttribute = XmlComplianceUtil.NonCDataNormalize(nameAttribute);
		int num = ValidateNames.ParseNCName(nameAttribute, 0);
		if (num != nameAttribute.Length)
		{
			string[] array = XmlException.BuildCharExceptionArgs(nameAttribute, num);
			string msg = System.SR.Format(System.SR.Xml_BadNameCharWithPos, array[0], array[1], num);
			SendValidationEvent(System.SR.Sch_InvalidNameAttributeEx, nameAttribute, msg, xso);
		}
		else
		{
			xso.NameAttribute = base.NameTable.Add(nameAttribute);
		}
	}

	private void ValidateQNameAttribute(XmlSchemaObject xso, string attributeName, XmlQualifiedName value)
	{
		try
		{
			value.Verify();
			value.Atomize(base.NameTable);
			if (_referenceNamespaces[value.Namespace] == null)
			{
				SendValidationEvent(System.SR.Sch_UnrefNS, value.Namespace, xso, XmlSeverityType.Warning);
			}
		}
		catch (Exception ex)
		{
			SendValidationEvent(System.SR.Sch_InvalidAttribute, attributeName, ex.Message, xso);
		}
	}

	private void SetParent(XmlSchemaObject child, XmlSchemaObject parent)
	{
		child.Parent = parent;
	}

	private void PreprocessAnnotation(XmlSchemaObject schemaObject)
	{
		if (schemaObject is XmlSchemaAnnotated { Annotation: not null } xmlSchemaAnnotated)
		{
			xmlSchemaAnnotated.Annotation.Parent = schemaObject;
			for (int i = 0; i < xmlSchemaAnnotated.Annotation.Items.Count; i++)
			{
				xmlSchemaAnnotated.Annotation.Items[i].Parent = xmlSchemaAnnotated.Annotation;
			}
		}
	}

	private Uri ResolveSchemaLocationUri(XmlSchema enclosingSchema, string location)
	{
		try
		{
			return _xmlResolver.ResolveUri(enclosingSchema.BaseUri, location);
		}
		catch
		{
			return null;
		}
	}

	private Stream GetSchemaEntity(Uri ruri)
	{
		try
		{
			return (Stream)_xmlResolver.GetEntity(ruri, null, null);
		}
		catch
		{
			return null;
		}
	}
}
