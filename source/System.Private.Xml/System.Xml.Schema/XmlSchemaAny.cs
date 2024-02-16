using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaAny : XmlSchemaParticle
{
	private string _ns;

	private XmlSchemaContentProcessing _processContents;

	private NamespaceList _namespaceList;

	[XmlAttribute("namespace")]
	public string? Namespace
	{
		get
		{
			return _ns;
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
	internal string ResolvedNamespace
	{
		get
		{
			if (_ns == null || _ns.Length == 0)
			{
				return "##any";
			}
			return _ns;
		}
	}

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

	internal override string NameString
	{
		get
		{
			switch (_namespaceList.Type)
			{
			case System.Xml.Schema.NamespaceList.ListType.Any:
				return "##any:*";
			case System.Xml.Schema.NamespaceList.ListType.Other:
				return "##other:*";
			case System.Xml.Schema.NamespaceList.ListType.Set:
			{
				StringBuilder stringBuilder = new StringBuilder();
				int num = 1;
				foreach (string item in _namespaceList.Enumerate)
				{
					stringBuilder.Append(item + ":*");
					if (num < _namespaceList.Enumerate.Count)
					{
						stringBuilder.Append(' ');
					}
					num++;
				}
				return stringBuilder.ToString();
			}
			default:
				return string.Empty;
			}
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
}
