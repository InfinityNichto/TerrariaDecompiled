using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class SchemaObjectWriter
{
	private readonly StringBuilder _w = new StringBuilder();

	private int _indentLevel = -1;

	private void WriteIndent()
	{
		for (int i = 0; i < _indentLevel; i++)
		{
			_w.Append(' ');
		}
	}

	private void WriteAttribute(string localName, string ns, string value)
	{
		if (value != null && value.Length != 0)
		{
			_w.Append(',');
			_w.Append(ns);
			if (ns != null && ns.Length != 0)
			{
				_w.Append(':');
			}
			_w.Append(localName);
			_w.Append('=');
			_w.Append(value);
		}
	}

	private void WriteAttribute(string localName, string ns, XmlQualifiedName value)
	{
		if (!value.IsEmpty)
		{
			WriteAttribute(localName, ns, value.ToString());
		}
	}

	private void WriteStartElement(string name)
	{
		NewLine();
		_indentLevel++;
		_w.Append('[');
		_w.Append(name);
	}

	private void WriteEndElement()
	{
		_w.Append(']');
		_indentLevel--;
	}

	private void NewLine()
	{
		_w.Append(Environment.NewLine);
		WriteIndent();
	}

	private string GetString()
	{
		return _w.ToString();
	}

	private void WriteAttribute(XmlAttribute a)
	{
		if (a.Value != null)
		{
			WriteAttribute(a.Name, a.NamespaceURI, a.Value);
		}
	}

	private void WriteAttributes(XmlAttribute[] a, XmlSchemaObject o)
	{
		if (a != null)
		{
			ArrayList arrayList = new ArrayList();
			for (int i = 0; i < a.Length; i++)
			{
				arrayList.Add(a[i]);
			}
			arrayList.Sort(new XmlAttributeComparer());
			for (int j = 0; j < arrayList.Count; j++)
			{
				XmlAttribute a2 = (XmlAttribute)arrayList[j];
				WriteAttribute(a2);
			}
		}
	}

	[return: NotNullIfNotNull("list")]
	internal static string ToString(NamespaceList list)
	{
		if (list == null)
		{
			return null;
		}
		switch (list.Type)
		{
		case NamespaceList.ListType.Any:
			return "##any";
		case NamespaceList.ListType.Other:
			return "##other";
		case NamespaceList.ListType.Set:
		{
			ArrayList arrayList = new ArrayList();
			foreach (string item in list.Enumerate)
			{
				arrayList.Add(item);
			}
			arrayList.Sort();
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			foreach (string item2 in arrayList)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(' ');
				}
				if (item2.Length == 0)
				{
					stringBuilder.Append("##local");
				}
				else
				{
					stringBuilder.Append(item2);
				}
			}
			return stringBuilder.ToString();
		}
		default:
			return list.ToString();
		}
	}

	internal string WriteXmlSchemaObject(XmlSchemaObject o)
	{
		if (o == null)
		{
			return string.Empty;
		}
		Write3_XmlSchemaObject(o);
		return GetString();
	}

	private void WriteSortedItems(XmlSchemaObjectCollection items)
	{
		if (items != null)
		{
			ArrayList arrayList = new ArrayList();
			for (int i = 0; i < items.Count; i++)
			{
				arrayList.Add(items[i]);
			}
			arrayList.Sort(new XmlSchemaObjectComparer());
			for (int j = 0; j < arrayList.Count; j++)
			{
				Write3_XmlSchemaObject((XmlSchemaObject)arrayList[j]);
			}
		}
	}

	private void Write1_XmlSchemaAttribute(XmlSchemaAttribute o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("attribute");
		WriteAttribute("id", "", o.Id);
		WriteAttributes(o.UnhandledAttributes, o);
		WriteAttribute("default", "", o.DefaultValue);
		WriteAttribute("fixed", "", o.FixedValue);
		if (o.Parent != null && !(o.Parent is XmlSchema))
		{
			if (o.QualifiedName != null && !o.QualifiedName.IsEmpty && o.QualifiedName.Namespace != null && o.QualifiedName.Namespace.Length != 0)
			{
				WriteAttribute("form", "", "qualified");
			}
			else
			{
				WriteAttribute("form", "", "unqualified");
			}
		}
		WriteAttribute("name", "", o.Name);
		if (!o.RefName.IsEmpty)
		{
			WriteAttribute("ref", "", o.RefName);
		}
		else if (!o.SchemaTypeName.IsEmpty)
		{
			WriteAttribute("type", "", o.SchemaTypeName);
		}
		XmlSchemaUse v = ((o.Use == XmlSchemaUse.None) ? XmlSchemaUse.Optional : o.Use);
		WriteAttribute("use", "", Write30_XmlSchemaUse(v));
		Write5_XmlSchemaAnnotation(o.Annotation);
		Write9_XmlSchemaSimpleType(o.SchemaType);
		WriteEndElement();
	}

	private void Write3_XmlSchemaObject(XmlSchemaObject o)
	{
		if (o != null)
		{
			Type type = o.GetType();
			if (type == typeof(XmlSchemaComplexType))
			{
				Write35_XmlSchemaComplexType((XmlSchemaComplexType)o);
			}
			else if (type == typeof(XmlSchemaSimpleType))
			{
				Write9_XmlSchemaSimpleType((XmlSchemaSimpleType)o);
			}
			else if (type == typeof(XmlSchemaElement))
			{
				Write46_XmlSchemaElement((XmlSchemaElement)o);
			}
			else if (type == typeof(XmlSchemaAppInfo))
			{
				Write7_XmlSchemaAppInfo((XmlSchemaAppInfo)o);
			}
			else if (type == typeof(XmlSchemaDocumentation))
			{
				Write6_XmlSchemaDocumentation((XmlSchemaDocumentation)o);
			}
			else if (type == typeof(XmlSchemaAnnotation))
			{
				Write5_XmlSchemaAnnotation((XmlSchemaAnnotation)o);
			}
			else if (type == typeof(XmlSchemaGroup))
			{
				Write57_XmlSchemaGroup((XmlSchemaGroup)o);
			}
			else if (type == typeof(XmlSchemaXPath))
			{
				Write49_XmlSchemaXPath("xpath", "", (XmlSchemaXPath)o);
			}
			else if (type == typeof(XmlSchemaIdentityConstraint))
			{
				Write48_XmlSchemaIdentityConstraint((XmlSchemaIdentityConstraint)o);
			}
			else if (type == typeof(XmlSchemaUnique))
			{
				Write51_XmlSchemaUnique((XmlSchemaUnique)o);
			}
			else if (type == typeof(XmlSchemaKeyref))
			{
				Write50_XmlSchemaKeyref((XmlSchemaKeyref)o);
			}
			else if (type == typeof(XmlSchemaKey))
			{
				Write47_XmlSchemaKey((XmlSchemaKey)o);
			}
			else if (type == typeof(XmlSchemaGroupRef))
			{
				Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o);
			}
			else if (type == typeof(XmlSchemaAny))
			{
				Write53_XmlSchemaAny((XmlSchemaAny)o);
			}
			else if (type == typeof(XmlSchemaSequence))
			{
				Write54_XmlSchemaSequence((XmlSchemaSequence)o);
			}
			else if (type == typeof(XmlSchemaChoice))
			{
				Write52_XmlSchemaChoice((XmlSchemaChoice)o);
			}
			else if (type == typeof(XmlSchemaAll))
			{
				Write43_XmlSchemaAll((XmlSchemaAll)o);
			}
			else if (type == typeof(XmlSchemaComplexContentRestriction))
			{
				Write56_XmlSchemaComplexContentRestriction((XmlSchemaComplexContentRestriction)o);
			}
			else if (type == typeof(XmlSchemaComplexContentExtension))
			{
				Write42_XmlSchemaComplexContentExtension((XmlSchemaComplexContentExtension)o);
			}
			else if (type == typeof(XmlSchemaSimpleContentRestriction))
			{
				Write40_XmlSchemaSimpleContentRestriction((XmlSchemaSimpleContentRestriction)o);
			}
			else if (type == typeof(XmlSchemaSimpleContentExtension))
			{
				Write38_XmlSchemaSimpleContentExtension((XmlSchemaSimpleContentExtension)o);
			}
			else if (type == typeof(XmlSchemaComplexContent))
			{
				Write41_XmlSchemaComplexContent((XmlSchemaComplexContent)o);
			}
			else if (type == typeof(XmlSchemaSimpleContent))
			{
				Write36_XmlSchemaSimpleContent((XmlSchemaSimpleContent)o);
			}
			else if (type == typeof(XmlSchemaAnyAttribute))
			{
				Write33_XmlSchemaAnyAttribute((XmlSchemaAnyAttribute)o);
			}
			else if (type == typeof(XmlSchemaAttributeGroupRef))
			{
				Write32_XmlSchemaAttributeGroupRef((XmlSchemaAttributeGroupRef)o);
			}
			else if (type == typeof(XmlSchemaAttributeGroup))
			{
				Write31_XmlSchemaAttributeGroup((XmlSchemaAttributeGroup)o);
			}
			else if (type == typeof(XmlSchemaSimpleTypeRestriction))
			{
				Write15_XmlSchemaSimpleTypeRestriction((XmlSchemaSimpleTypeRestriction)o);
			}
			else if (type == typeof(XmlSchemaSimpleTypeList))
			{
				Write14_XmlSchemaSimpleTypeList((XmlSchemaSimpleTypeList)o);
			}
			else if (type == typeof(XmlSchemaSimpleTypeUnion))
			{
				Write12_XmlSchemaSimpleTypeUnion((XmlSchemaSimpleTypeUnion)o);
			}
			else if (type == typeof(XmlSchemaAttribute))
			{
				Write1_XmlSchemaAttribute((XmlSchemaAttribute)o);
			}
		}
	}

	private void Write5_XmlSchemaAnnotation(XmlSchemaAnnotation o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("annotation");
		WriteAttribute("id", "", o.Id);
		WriteAttributes(o.UnhandledAttributes, o);
		XmlSchemaObjectCollection items = o.Items;
		if (items != null)
		{
			for (int i = 0; i < items.Count; i++)
			{
				XmlSchemaObject xmlSchemaObject = items[i];
				if (xmlSchemaObject is XmlSchemaAppInfo)
				{
					Write7_XmlSchemaAppInfo((XmlSchemaAppInfo)xmlSchemaObject);
				}
				else if (xmlSchemaObject is XmlSchemaDocumentation)
				{
					Write6_XmlSchemaDocumentation((XmlSchemaDocumentation)xmlSchemaObject);
				}
			}
		}
		WriteEndElement();
	}

	private void Write6_XmlSchemaDocumentation(XmlSchemaDocumentation o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("documentation");
		WriteAttribute("source", "", o.Source);
		WriteAttribute("lang", "http://www.w3.org/XML/1998/namespace", o.Language);
		XmlNode[] markup = o.Markup;
		if (markup != null)
		{
			foreach (XmlNode xmlNode in markup)
			{
				WriteStartElement("node");
				WriteAttribute("xml", "", xmlNode.OuterXml);
			}
		}
		WriteEndElement();
	}

	private void Write7_XmlSchemaAppInfo(XmlSchemaAppInfo o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("appinfo");
		WriteAttribute("source", "", o.Source);
		XmlNode[] markup = o.Markup;
		if (markup != null)
		{
			foreach (XmlNode xmlNode in markup)
			{
				WriteStartElement("node");
				WriteAttribute("xml", "", xmlNode.OuterXml);
			}
		}
		WriteEndElement();
	}

	private void Write9_XmlSchemaSimpleType(XmlSchemaSimpleType o)
	{
		if (o != null)
		{
			WriteStartElement("simpleType");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			WriteAttribute("name", "", o.Name);
			WriteAttribute("final", "", Write11_XmlSchemaDerivationMethod(o.FinalResolved));
			Write5_XmlSchemaAnnotation(o.Annotation);
			if (o.Content is XmlSchemaSimpleTypeUnion)
			{
				Write12_XmlSchemaSimpleTypeUnion((XmlSchemaSimpleTypeUnion)o.Content);
			}
			else if (o.Content is XmlSchemaSimpleTypeRestriction)
			{
				Write15_XmlSchemaSimpleTypeRestriction((XmlSchemaSimpleTypeRestriction)o.Content);
			}
			else if (o.Content is XmlSchemaSimpleTypeList)
			{
				Write14_XmlSchemaSimpleTypeList((XmlSchemaSimpleTypeList)o.Content);
			}
			WriteEndElement();
		}
	}

	private string Write11_XmlSchemaDerivationMethod(XmlSchemaDerivationMethod v)
	{
		return v.ToString();
	}

	private void Write12_XmlSchemaSimpleTypeUnion(XmlSchemaSimpleTypeUnion o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("union");
		WriteAttribute("id", "", o.Id);
		WriteAttributes(o.UnhandledAttributes, o);
		if (o.MemberTypes != null)
		{
			ArrayList arrayList = new ArrayList();
			for (int i = 0; i < o.MemberTypes.Length; i++)
			{
				arrayList.Add(o.MemberTypes[i]);
			}
			arrayList.Sort(new QNameComparer());
			_w.Append(',');
			_w.Append("memberTypes=");
			for (int j = 0; j < arrayList.Count; j++)
			{
				XmlQualifiedName xmlQualifiedName = (XmlQualifiedName)arrayList[j];
				_w.Append(xmlQualifiedName.ToString());
				_w.Append(',');
			}
		}
		Write5_XmlSchemaAnnotation(o.Annotation);
		WriteSortedItems(o.BaseTypes);
		WriteEndElement();
	}

	private void Write14_XmlSchemaSimpleTypeList(XmlSchemaSimpleTypeList o)
	{
		if (o != null)
		{
			WriteStartElement("list");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			if (!o.ItemTypeName.IsEmpty)
			{
				WriteAttribute("itemType", "", o.ItemTypeName);
			}
			Write5_XmlSchemaAnnotation(o.Annotation);
			Write9_XmlSchemaSimpleType(o.ItemType);
			WriteEndElement();
		}
	}

	private void Write15_XmlSchemaSimpleTypeRestriction(XmlSchemaSimpleTypeRestriction o)
	{
		if (o != null)
		{
			WriteStartElement("restriction");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			if (!o.BaseTypeName.IsEmpty)
			{
				WriteAttribute("base", "", o.BaseTypeName);
			}
			Write5_XmlSchemaAnnotation(o.Annotation);
			Write9_XmlSchemaSimpleType(o.BaseType);
			WriteFacets(o.Facets);
			WriteEndElement();
		}
	}

	private void WriteFacets(XmlSchemaObjectCollection facets)
	{
		if (facets == null)
		{
			return;
		}
		ArrayList arrayList = new ArrayList();
		for (int i = 0; i < facets.Count; i++)
		{
			arrayList.Add(facets[i]);
		}
		arrayList.Sort(new XmlFacetComparer());
		for (int j = 0; j < arrayList.Count; j++)
		{
			XmlSchemaObject xmlSchemaObject = (XmlSchemaObject)arrayList[j];
			if (xmlSchemaObject is XmlSchemaMinExclusiveFacet)
			{
				Write_XmlSchemaFacet("minExclusive", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaMaxInclusiveFacet)
			{
				Write_XmlSchemaFacet("maxInclusive", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaMaxExclusiveFacet)
			{
				Write_XmlSchemaFacet("maxExclusive", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaMinInclusiveFacet)
			{
				Write_XmlSchemaFacet("minInclusive", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaLengthFacet)
			{
				Write_XmlSchemaFacet("length", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaEnumerationFacet)
			{
				Write_XmlSchemaFacet("enumeration", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaMinLengthFacet)
			{
				Write_XmlSchemaFacet("minLength", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaPatternFacet)
			{
				Write_XmlSchemaFacet("pattern", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaTotalDigitsFacet)
			{
				Write_XmlSchemaFacet("totalDigits", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaMaxLengthFacet)
			{
				Write_XmlSchemaFacet("maxLength", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaWhiteSpaceFacet)
			{
				Write_XmlSchemaFacet("whiteSpace", (XmlSchemaFacet)xmlSchemaObject);
			}
			else if (xmlSchemaObject is XmlSchemaFractionDigitsFacet)
			{
				Write_XmlSchemaFacet("fractionDigit", (XmlSchemaFacet)xmlSchemaObject);
			}
		}
	}

	private void Write_XmlSchemaFacet(string name, XmlSchemaFacet o)
	{
		if (o != null)
		{
			WriteStartElement(name);
			WriteAttribute("id", "", o.Id);
			WriteAttribute("value", "", o.Value);
			if (o.IsFixed)
			{
				WriteAttribute("fixed", "", XmlConvert.ToString(o.IsFixed));
			}
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteEndElement();
		}
	}

	private string Write30_XmlSchemaUse(XmlSchemaUse v)
	{
		string result = null;
		switch (v)
		{
		case XmlSchemaUse.Optional:
			result = "optional";
			break;
		case XmlSchemaUse.Prohibited:
			result = "prohibited";
			break;
		case XmlSchemaUse.Required:
			result = "required";
			break;
		}
		return result;
	}

	private void Write31_XmlSchemaAttributeGroup(XmlSchemaAttributeGroup o)
	{
		if (o != null)
		{
			WriteStartElement("attributeGroup");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("name", "", o.Name);
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteSortedItems(o.Attributes);
			Write33_XmlSchemaAnyAttribute(o.AnyAttribute);
			WriteEndElement();
		}
	}

	private void Write32_XmlSchemaAttributeGroupRef(XmlSchemaAttributeGroupRef o)
	{
		if (o != null)
		{
			WriteStartElement("attributeGroup");
			WriteAttribute("id", "", o.Id);
			if (!o.RefName.IsEmpty)
			{
				WriteAttribute("ref", "", o.RefName);
			}
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteEndElement();
		}
	}

	private void Write33_XmlSchemaAnyAttribute(XmlSchemaAnyAttribute o)
	{
		if (o != null)
		{
			WriteStartElement("anyAttribute");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("namespace", "", ToString(o.NamespaceList));
			XmlSchemaContentProcessing v = ((o.ProcessContents == XmlSchemaContentProcessing.None) ? XmlSchemaContentProcessing.Strict : o.ProcessContents);
			WriteAttribute("processContents", "", Write34_XmlSchemaContentProcessing(v));
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteEndElement();
		}
	}

	private string Write34_XmlSchemaContentProcessing(XmlSchemaContentProcessing v)
	{
		string result = null;
		switch (v)
		{
		case XmlSchemaContentProcessing.Skip:
			result = "skip";
			break;
		case XmlSchemaContentProcessing.Lax:
			result = "lax";
			break;
		case XmlSchemaContentProcessing.Strict:
			result = "strict";
			break;
		}
		return result;
	}

	private void Write35_XmlSchemaComplexType(XmlSchemaComplexType o)
	{
		if (o != null)
		{
			WriteStartElement("complexType");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("name", "", o.Name);
			WriteAttribute("final", "", Write11_XmlSchemaDerivationMethod(o.FinalResolved));
			if (o.IsAbstract)
			{
				WriteAttribute("abstract", "", XmlConvert.ToString(o.IsAbstract));
			}
			WriteAttribute("block", "", Write11_XmlSchemaDerivationMethod(o.BlockResolved));
			if (o.IsMixed)
			{
				WriteAttribute("mixed", "", XmlConvert.ToString(o.IsMixed));
			}
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			if (o.ContentModel is XmlSchemaComplexContent)
			{
				Write41_XmlSchemaComplexContent((XmlSchemaComplexContent)o.ContentModel);
			}
			else if (o.ContentModel is XmlSchemaSimpleContent)
			{
				Write36_XmlSchemaSimpleContent((XmlSchemaSimpleContent)o.ContentModel);
			}
			if (o.Particle is XmlSchemaSequence)
			{
				Write54_XmlSchemaSequence((XmlSchemaSequence)o.Particle);
			}
			else if (o.Particle is XmlSchemaGroupRef)
			{
				Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o.Particle);
			}
			else if (o.Particle is XmlSchemaChoice)
			{
				Write52_XmlSchemaChoice((XmlSchemaChoice)o.Particle);
			}
			else if (o.Particle is XmlSchemaAll)
			{
				Write43_XmlSchemaAll((XmlSchemaAll)o.Particle);
			}
			WriteSortedItems(o.Attributes);
			Write33_XmlSchemaAnyAttribute(o.AnyAttribute);
			WriteEndElement();
		}
	}

	private void Write36_XmlSchemaSimpleContent(XmlSchemaSimpleContent o)
	{
		if (o != null)
		{
			WriteStartElement("simpleContent");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			if (o.Content is XmlSchemaSimpleContentRestriction)
			{
				Write40_XmlSchemaSimpleContentRestriction((XmlSchemaSimpleContentRestriction)o.Content);
			}
			else if (o.Content is XmlSchemaSimpleContentExtension)
			{
				Write38_XmlSchemaSimpleContentExtension((XmlSchemaSimpleContentExtension)o.Content);
			}
			WriteEndElement();
		}
	}

	private void Write38_XmlSchemaSimpleContentExtension(XmlSchemaSimpleContentExtension o)
	{
		if (o != null)
		{
			WriteStartElement("extension");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			if (!o.BaseTypeName.IsEmpty)
			{
				WriteAttribute("base", "", o.BaseTypeName);
			}
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteSortedItems(o.Attributes);
			Write33_XmlSchemaAnyAttribute(o.AnyAttribute);
			WriteEndElement();
		}
	}

	private void Write40_XmlSchemaSimpleContentRestriction(XmlSchemaSimpleContentRestriction o)
	{
		if (o != null)
		{
			WriteStartElement("restriction");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			if (!o.BaseTypeName.IsEmpty)
			{
				WriteAttribute("base", "", o.BaseTypeName);
			}
			Write5_XmlSchemaAnnotation(o.Annotation);
			Write9_XmlSchemaSimpleType(o.BaseType);
			WriteFacets(o.Facets);
			WriteSortedItems(o.Attributes);
			Write33_XmlSchemaAnyAttribute(o.AnyAttribute);
			WriteEndElement();
		}
	}

	private void Write41_XmlSchemaComplexContent(XmlSchemaComplexContent o)
	{
		if (o != null)
		{
			WriteStartElement("complexContent");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("mixed", "", XmlConvert.ToString(o.IsMixed));
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			if (o.Content is XmlSchemaComplexContentRestriction)
			{
				Write56_XmlSchemaComplexContentRestriction((XmlSchemaComplexContentRestriction)o.Content);
			}
			else if (o.Content is XmlSchemaComplexContentExtension)
			{
				Write42_XmlSchemaComplexContentExtension((XmlSchemaComplexContentExtension)o.Content);
			}
			WriteEndElement();
		}
	}

	private void Write42_XmlSchemaComplexContentExtension(XmlSchemaComplexContentExtension o)
	{
		if (o != null)
		{
			WriteStartElement("extension");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			if (!o.BaseTypeName.IsEmpty)
			{
				WriteAttribute("base", "", o.BaseTypeName);
			}
			Write5_XmlSchemaAnnotation(o.Annotation);
			if (o.Particle is XmlSchemaSequence)
			{
				Write54_XmlSchemaSequence((XmlSchemaSequence)o.Particle);
			}
			else if (o.Particle is XmlSchemaGroupRef)
			{
				Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o.Particle);
			}
			else if (o.Particle is XmlSchemaChoice)
			{
				Write52_XmlSchemaChoice((XmlSchemaChoice)o.Particle);
			}
			else if (o.Particle is XmlSchemaAll)
			{
				Write43_XmlSchemaAll((XmlSchemaAll)o.Particle);
			}
			WriteSortedItems(o.Attributes);
			Write33_XmlSchemaAnyAttribute(o.AnyAttribute);
			WriteEndElement();
		}
	}

	private void Write43_XmlSchemaAll(XmlSchemaAll o)
	{
		if (o != null)
		{
			WriteStartElement("all");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
			WriteAttribute("maxOccurs", "", (o.MaxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteSortedItems(o.Items);
			WriteEndElement();
		}
	}

	private void Write46_XmlSchemaElement(XmlSchemaElement o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("element");
		WriteAttribute("id", "", o.Id);
		WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
		WriteAttribute("maxOccurs", "", (o.MaxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
		if (o.IsAbstract)
		{
			WriteAttribute("abstract", "", XmlConvert.ToString(o.IsAbstract));
		}
		WriteAttribute("block", "", Write11_XmlSchemaDerivationMethod(o.BlockResolved));
		WriteAttribute("default", "", o.DefaultValue);
		WriteAttribute("final", "", Write11_XmlSchemaDerivationMethod(o.FinalResolved));
		WriteAttribute("fixed", "", o.FixedValue);
		if (o.Parent != null && !(o.Parent is XmlSchema))
		{
			if (o.QualifiedName != null && !o.QualifiedName.IsEmpty && o.QualifiedName.Namespace != null && o.QualifiedName.Namespace.Length != 0)
			{
				WriteAttribute("form", "", "qualified");
			}
			else
			{
				WriteAttribute("form", "", "unqualified");
			}
		}
		if (o.Name != null && o.Name.Length != 0)
		{
			WriteAttribute("name", "", o.Name);
		}
		if (o.IsNillable)
		{
			WriteAttribute("nillable", "", XmlConvert.ToString(o.IsNillable));
		}
		if (!o.SubstitutionGroup.IsEmpty)
		{
			WriteAttribute("substitutionGroup", "", o.SubstitutionGroup);
		}
		if (!o.RefName.IsEmpty)
		{
			WriteAttribute("ref", "", o.RefName);
		}
		else if (!o.SchemaTypeName.IsEmpty)
		{
			WriteAttribute("type", "", o.SchemaTypeName);
		}
		WriteAttributes(o.UnhandledAttributes, o);
		Write5_XmlSchemaAnnotation(o.Annotation);
		if (o.SchemaType is XmlSchemaComplexType)
		{
			Write35_XmlSchemaComplexType((XmlSchemaComplexType)o.SchemaType);
		}
		else if (o.SchemaType is XmlSchemaSimpleType)
		{
			Write9_XmlSchemaSimpleType((XmlSchemaSimpleType)o.SchemaType);
		}
		WriteSortedItems(o.Constraints);
		WriteEndElement();
	}

	private void Write47_XmlSchemaKey(XmlSchemaKey o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("key");
		WriteAttribute("id", "", o.Id);
		WriteAttribute("name", "", o.Name);
		WriteAttributes(o.UnhandledAttributes, o);
		Write5_XmlSchemaAnnotation(o.Annotation);
		Write49_XmlSchemaXPath("selector", "", o.Selector);
		XmlSchemaObjectCollection fields = o.Fields;
		if (fields != null)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				Write49_XmlSchemaXPath("field", "", (XmlSchemaXPath)fields[i]);
			}
		}
		WriteEndElement();
	}

	private void Write48_XmlSchemaIdentityConstraint(XmlSchemaIdentityConstraint o)
	{
		if (o != null)
		{
			Type type = o.GetType();
			if (type == typeof(XmlSchemaUnique))
			{
				Write51_XmlSchemaUnique((XmlSchemaUnique)o);
			}
			else if (type == typeof(XmlSchemaKeyref))
			{
				Write50_XmlSchemaKeyref((XmlSchemaKeyref)o);
			}
			else if (type == typeof(XmlSchemaKey))
			{
				Write47_XmlSchemaKey((XmlSchemaKey)o);
			}
		}
	}

	private void Write49_XmlSchemaXPath(string name, string ns, XmlSchemaXPath o)
	{
		if (o != null)
		{
			WriteStartElement(name);
			WriteAttribute("id", "", o.Id);
			WriteAttribute("xpath", "", o.XPath);
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteEndElement();
		}
	}

	private void Write50_XmlSchemaKeyref(XmlSchemaKeyref o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("keyref");
		WriteAttribute("id", "", o.Id);
		WriteAttribute("name", "", o.Name);
		WriteAttributes(o.UnhandledAttributes, o);
		WriteAttribute("refer", "", o.Refer);
		Write5_XmlSchemaAnnotation(o.Annotation);
		Write49_XmlSchemaXPath("selector", "", o.Selector);
		XmlSchemaObjectCollection fields = o.Fields;
		if (fields != null)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				Write49_XmlSchemaXPath("field", "", (XmlSchemaXPath)fields[i]);
			}
		}
		WriteEndElement();
	}

	private void Write51_XmlSchemaUnique(XmlSchemaUnique o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("unique");
		WriteAttribute("id", "", o.Id);
		WriteAttribute("name", "", o.Name);
		WriteAttributes(o.UnhandledAttributes, o);
		Write5_XmlSchemaAnnotation(o.Annotation);
		Write49_XmlSchemaXPath("selector", "", o.Selector);
		XmlSchemaObjectCollection fields = o.Fields;
		if (fields != null)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				Write49_XmlSchemaXPath("field", "", (XmlSchemaXPath)fields[i]);
			}
		}
		WriteEndElement();
	}

	private void Write52_XmlSchemaChoice(XmlSchemaChoice o)
	{
		if (o != null)
		{
			WriteStartElement("choice");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
			WriteAttribute("maxOccurs", "", (o.MaxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteSortedItems(o.Items);
			WriteEndElement();
		}
	}

	private void Write53_XmlSchemaAny(XmlSchemaAny o)
	{
		if (o != null)
		{
			WriteStartElement("any");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
			WriteAttribute("maxOccurs", "", (o.MaxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
			WriteAttribute("namespace", "", ToString(o.NamespaceList));
			XmlSchemaContentProcessing v = ((o.ProcessContents == XmlSchemaContentProcessing.None) ? XmlSchemaContentProcessing.Strict : o.ProcessContents);
			WriteAttribute("processContents", "", Write34_XmlSchemaContentProcessing(v));
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteEndElement();
		}
	}

	private void Write54_XmlSchemaSequence(XmlSchemaSequence o)
	{
		if (o == null)
		{
			return;
		}
		WriteStartElement("sequence");
		WriteAttribute("id", "", o.Id);
		WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
		WriteAttribute("maxOccurs", "", (o.MaxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
		WriteAttributes(o.UnhandledAttributes, o);
		Write5_XmlSchemaAnnotation(o.Annotation);
		XmlSchemaObjectCollection items = o.Items;
		if (items != null)
		{
			for (int i = 0; i < items.Count; i++)
			{
				XmlSchemaObject xmlSchemaObject = items[i];
				if (xmlSchemaObject is XmlSchemaAny)
				{
					Write53_XmlSchemaAny((XmlSchemaAny)xmlSchemaObject);
				}
				else if (xmlSchemaObject is XmlSchemaSequence)
				{
					Write54_XmlSchemaSequence((XmlSchemaSequence)xmlSchemaObject);
				}
				else if (xmlSchemaObject is XmlSchemaChoice)
				{
					Write52_XmlSchemaChoice((XmlSchemaChoice)xmlSchemaObject);
				}
				else if (xmlSchemaObject is XmlSchemaElement)
				{
					Write46_XmlSchemaElement((XmlSchemaElement)xmlSchemaObject);
				}
				else if (xmlSchemaObject is XmlSchemaGroupRef)
				{
					Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)xmlSchemaObject);
				}
			}
		}
		WriteEndElement();
	}

	private void Write55_XmlSchemaGroupRef(XmlSchemaGroupRef o)
	{
		if (o != null)
		{
			WriteStartElement("group");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("minOccurs", "", XmlConvert.ToString(o.MinOccurs));
			WriteAttribute("maxOccurs", "", (o.MaxOccurs == decimal.MaxValue) ? "unbounded" : XmlConvert.ToString(o.MaxOccurs));
			if (!o.RefName.IsEmpty)
			{
				WriteAttribute("ref", "", o.RefName);
			}
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			WriteEndElement();
		}
	}

	private void Write56_XmlSchemaComplexContentRestriction(XmlSchemaComplexContentRestriction o)
	{
		if (o != null)
		{
			WriteStartElement("restriction");
			WriteAttribute("id", "", o.Id);
			WriteAttributes(o.UnhandledAttributes, o);
			if (!o.BaseTypeName.IsEmpty)
			{
				WriteAttribute("base", "", o.BaseTypeName);
			}
			Write5_XmlSchemaAnnotation(o.Annotation);
			if (o.Particle is XmlSchemaSequence)
			{
				Write54_XmlSchemaSequence((XmlSchemaSequence)o.Particle);
			}
			else if (o.Particle is XmlSchemaGroupRef)
			{
				Write55_XmlSchemaGroupRef((XmlSchemaGroupRef)o.Particle);
			}
			else if (o.Particle is XmlSchemaChoice)
			{
				Write52_XmlSchemaChoice((XmlSchemaChoice)o.Particle);
			}
			else if (o.Particle is XmlSchemaAll)
			{
				Write43_XmlSchemaAll((XmlSchemaAll)o.Particle);
			}
			WriteSortedItems(o.Attributes);
			Write33_XmlSchemaAnyAttribute(o.AnyAttribute);
			WriteEndElement();
		}
	}

	private void Write57_XmlSchemaGroup(XmlSchemaGroup o)
	{
		if (o != null)
		{
			WriteStartElement("group");
			WriteAttribute("id", "", o.Id);
			WriteAttribute("name", "", o.Name);
			WriteAttributes(o.UnhandledAttributes, o);
			Write5_XmlSchemaAnnotation(o.Annotation);
			if (o.Particle is XmlSchemaSequence)
			{
				Write54_XmlSchemaSequence((XmlSchemaSequence)o.Particle);
			}
			else if (o.Particle is XmlSchemaChoice)
			{
				Write52_XmlSchemaChoice((XmlSchemaChoice)o.Particle);
			}
			else if (o.Particle is XmlSchemaAll)
			{
				Write43_XmlSchemaAll((XmlSchemaAll)o.Particle);
			}
			WriteEndElement();
		}
	}
}
