using System.ComponentModel;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaAnyAttribute : XmlSchemaAnnotated
{
	private string _ns;

	private XmlSchemaContentProcessing _processContents;

	private NamespaceList _namespaceList;

	[XmlAttribute("namespace")]
	public string? Namespace
	{
		get
		{
			string text = _ns;
			if (text == null)
			{
				NamespaceList? namespaceList = NamespaceList;
				if (namespaceList == null)
				{
					return null;
				}
				text = namespaceList.ToString();
			}
			return text;
		}
		set
		{
			_ns = value;
		}
	}

	[XmlAttribute("processContents")]
	[DefaultValue(XmlSchemaContentProcessing.None)]
	public XmlSchemaContentProcessing ProcessContents
	{
		get
		{
			return _processContents;
		}
		set
		{
			_processContents = value;
		}
	}

	[XmlIgnore]
	internal NamespaceList? NamespaceList => _namespaceList;

	[XmlIgnore]
	internal XmlSchemaContentProcessing ProcessContentsCorrect
	{
		get
		{
			if (_processContents != 0)
			{
				return _processContents;
			}
			return XmlSchemaContentProcessing.Strict;
		}
	}

	internal void BuildNamespaceList(string targetNamespace)
	{
		if (_ns != null)
		{
			_namespaceList = new NamespaceList(_ns, targetNamespace);
		}
		else
		{
			_namespaceList = new NamespaceList();
		}
	}

	internal void BuildNamespaceListV1Compat(string targetNamespace)
	{
		if (_ns != null)
		{
			_namespaceList = new NamespaceListV1Compat(_ns, targetNamespace);
		}
		else
		{
			_namespaceList = new NamespaceList();
		}
	}

	internal bool Allows(XmlQualifiedName qname)
	{
		return _namespaceList.Allows(qname.Namespace);
	}

	internal static bool IsSubset(XmlSchemaAnyAttribute sub, XmlSchemaAnyAttribute super)
	{
		return System.Xml.Schema.NamespaceList.IsSubset(sub.NamespaceList, super.NamespaceList);
	}

	internal static XmlSchemaAnyAttribute Intersection(XmlSchemaAnyAttribute o1, XmlSchemaAnyAttribute o2, bool v1Compat)
	{
		NamespaceList namespaceList = System.Xml.Schema.NamespaceList.Intersection(o1.NamespaceList, o2.NamespaceList, v1Compat);
		if (namespaceList != null)
		{
			XmlSchemaAnyAttribute xmlSchemaAnyAttribute = new XmlSchemaAnyAttribute();
			xmlSchemaAnyAttribute._namespaceList = namespaceList;
			xmlSchemaAnyAttribute.ProcessContents = o1.ProcessContents;
			xmlSchemaAnyAttribute.Annotation = o1.Annotation;
			return xmlSchemaAnyAttribute;
		}
		return null;
	}

	internal static XmlSchemaAnyAttribute Union(XmlSchemaAnyAttribute o1, XmlSchemaAnyAttribute o2, bool v1Compat)
	{
		NamespaceList namespaceList = System.Xml.Schema.NamespaceList.Union(o1.NamespaceList, o2.NamespaceList, v1Compat);
		if (namespaceList != null)
		{
			XmlSchemaAnyAttribute xmlSchemaAnyAttribute = new XmlSchemaAnyAttribute();
			xmlSchemaAnyAttribute._namespaceList = namespaceList;
			xmlSchemaAnyAttribute._processContents = o1._processContents;
			xmlSchemaAnyAttribute.Annotation = o1.Annotation;
			return xmlSchemaAnyAttribute;
		}
		return null;
	}
}
