using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml;

public class XmlElement : XmlLinkedNode
{
	private XmlName _name;

	private XmlAttributeCollection _attributes;

	private XmlLinkedNode _lastChild;

	internal XmlName XmlName
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public override string Name => _name.Name;

	public override string LocalName => _name.LocalName;

	public override string NamespaceURI => _name.NamespaceURI;

	public override string Prefix
	{
		get
		{
			return _name.Prefix;
		}
		set
		{
			_name = _name.OwnerDocument.AddXmlName(value, LocalName, NamespaceURI, SchemaInfo);
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.Element;

	public override XmlNode? ParentNode => parentNode;

	public override XmlDocument OwnerDocument => _name.OwnerDocument;

	internal override bool IsContainer => true;

	public bool IsEmpty
	{
		get
		{
			return _lastChild == this;
		}
		set
		{
			if (value)
			{
				if (_lastChild != this)
				{
					RemoveAllChildren();
					_lastChild = this;
				}
			}
			else if (_lastChild == this)
			{
				_lastChild = null;
			}
		}
	}

	internal override XmlLinkedNode? LastNode
	{
		get
		{
			if (_lastChild != this)
			{
				return _lastChild;
			}
			return null;
		}
		set
		{
			_lastChild = value;
		}
	}

	public override XmlAttributeCollection Attributes
	{
		get
		{
			if (_attributes == null)
			{
				lock (OwnerDocument.objLock)
				{
					if (_attributes == null)
					{
						_attributes = new XmlAttributeCollection(this);
					}
				}
			}
			return _attributes;
		}
	}

	public virtual bool HasAttributes
	{
		get
		{
			if (_attributes == null)
			{
				return false;
			}
			return _attributes.Count > 0;
		}
	}

	public override IXmlSchemaInfo SchemaInfo => _name;

	public override string InnerXml
	{
		get
		{
			return base.InnerXml;
		}
		set
		{
			RemoveAllChildren();
			XmlLoader xmlLoader = new XmlLoader();
			xmlLoader.LoadInnerXmlElement(this, value);
		}
	}

	public override string InnerText
	{
		get
		{
			return base.InnerText;
		}
		set
		{
			XmlLinkedNode lastNode = LastNode;
			if (lastNode != null && lastNode.NodeType == XmlNodeType.Text && lastNode.next == lastNode)
			{
				lastNode.Value = value;
				return;
			}
			RemoveAllChildren();
			AppendChild(OwnerDocument.CreateTextNode(value));
		}
	}

	public override XmlNode? NextSibling
	{
		get
		{
			if (parentNode != null && parentNode.LastNode != this)
			{
				return next;
			}
			return null;
		}
	}

	internal override XPathNodeType XPNodeType => XPathNodeType.Element;

	internal override string XPLocalName => LocalName;

	internal XmlElement(XmlName name, bool empty, XmlDocument doc)
		: base(doc)
	{
		parentNode = null;
		if (!doc.IsLoading)
		{
			XmlDocument.CheckName(name.Prefix);
			XmlDocument.CheckName(name.LocalName);
		}
		if (name.LocalName.Length == 0)
		{
			throw new ArgumentException(System.SR.Xdom_Empty_LocalName);
		}
		_name = name;
		if (empty)
		{
			_lastChild = this;
		}
	}

	protected internal XmlElement(string prefix, string localName, string? namespaceURI, XmlDocument doc)
		: this(doc.AddXmlName(prefix, localName, namespaceURI, null), empty: true, doc)
	{
	}

	public override XmlNode CloneNode(bool deep)
	{
		XmlDocument ownerDocument = OwnerDocument;
		bool isLoading = ownerDocument.IsLoading;
		ownerDocument.IsLoading = true;
		XmlElement xmlElement = ownerDocument.CreateElement(Prefix, LocalName, NamespaceURI);
		ownerDocument.IsLoading = isLoading;
		if (xmlElement.IsEmpty != IsEmpty)
		{
			xmlElement.IsEmpty = IsEmpty;
		}
		if (HasAttributes)
		{
			foreach (XmlAttribute attribute in Attributes)
			{
				XmlAttribute xmlAttribute2 = (XmlAttribute)attribute.CloneNode(deep: true);
				if (xmlAttribute2 is XmlUnspecifiedAttribute xmlUnspecifiedAttribute && !attribute.Specified)
				{
					xmlUnspecifiedAttribute.SetSpecified(f: false);
				}
				xmlElement.Attributes.InternalAppendAttribute(xmlAttribute2);
			}
		}
		if (deep)
		{
			xmlElement.CopyChildren(ownerDocument, this, deep);
		}
		return xmlElement;
	}

	internal override XmlNode AppendChildForLoad(XmlNode newChild, XmlDocument doc)
	{
		XmlNodeChangedEventArgs insertEventArgsForLoad = doc.GetInsertEventArgsForLoad(newChild, this);
		if (insertEventArgsForLoad != null)
		{
			doc.BeforeEvent(insertEventArgsForLoad);
		}
		XmlLinkedNode xmlLinkedNode = (XmlLinkedNode)newChild;
		if (_lastChild == null || _lastChild == this)
		{
			xmlLinkedNode.next = xmlLinkedNode;
			_lastChild = xmlLinkedNode;
			xmlLinkedNode.SetParentForLoad(this);
		}
		else
		{
			XmlLinkedNode lastChild = _lastChild;
			xmlLinkedNode.next = lastChild.next;
			lastChild.next = xmlLinkedNode;
			_lastChild = xmlLinkedNode;
			if (lastChild.IsText && xmlLinkedNode.IsText)
			{
				XmlNode.NestTextNodes(lastChild, xmlLinkedNode);
			}
			else
			{
				xmlLinkedNode.SetParentForLoad(this);
			}
		}
		if (insertEventArgsForLoad != null)
		{
			doc.AfterEvent(insertEventArgsForLoad);
		}
		return xmlLinkedNode;
	}

	internal override bool IsValidChildType(XmlNodeType type)
	{
		switch (type)
		{
		case XmlNodeType.Element:
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.EntityReference:
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.Comment:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			return true;
		default:
			return false;
		}
	}

	public virtual string GetAttribute(string name)
	{
		XmlAttribute attributeNode = GetAttributeNode(name);
		if (attributeNode != null)
		{
			return attributeNode.Value;
		}
		return string.Empty;
	}

	public virtual void SetAttribute(string name, string? value)
	{
		XmlAttribute attributeNode = GetAttributeNode(name);
		if (attributeNode == null)
		{
			attributeNode = OwnerDocument.CreateAttribute(name);
			attributeNode.Value = value;
			Attributes.InternalAppendAttribute(attributeNode);
		}
		else
		{
			attributeNode.Value = value;
		}
	}

	public virtual void RemoveAttribute(string name)
	{
		if (HasAttributes)
		{
			Attributes.RemoveNamedItem(name);
		}
	}

	public virtual XmlAttribute? GetAttributeNode(string name)
	{
		if (HasAttributes)
		{
			return Attributes[name];
		}
		return null;
	}

	public virtual XmlAttribute? SetAttributeNode(XmlAttribute newAttr)
	{
		if (newAttr.OwnerElement != null)
		{
			throw new InvalidOperationException(System.SR.Xdom_Attr_InUse);
		}
		return (XmlAttribute)Attributes.SetNamedItem(newAttr);
	}

	public virtual XmlAttribute? RemoveAttributeNode(XmlAttribute oldAttr)
	{
		if (HasAttributes)
		{
			return Attributes.Remove(oldAttr);
		}
		return null;
	}

	public virtual XmlNodeList GetElementsByTagName(string name)
	{
		return new XmlElementList(this, name);
	}

	public virtual string GetAttribute(string localName, string? namespaceURI)
	{
		XmlAttribute attributeNode = GetAttributeNode(localName, namespaceURI);
		if (attributeNode != null)
		{
			return attributeNode.Value;
		}
		return string.Empty;
	}

	[return: NotNullIfNotNull("value")]
	public virtual string? SetAttribute(string localName, string? namespaceURI, string? value)
	{
		XmlAttribute attributeNode = GetAttributeNode(localName, namespaceURI);
		if (attributeNode == null)
		{
			attributeNode = OwnerDocument.CreateAttribute(string.Empty, localName, namespaceURI);
			attributeNode.Value = value;
			Attributes.InternalAppendAttribute(attributeNode);
		}
		else
		{
			attributeNode.Value = value;
		}
		return value;
	}

	public virtual void RemoveAttribute(string localName, string? namespaceURI)
	{
		RemoveAttributeNode(localName, namespaceURI);
	}

	public virtual XmlAttribute? GetAttributeNode(string localName, string? namespaceURI)
	{
		if (HasAttributes)
		{
			return Attributes[localName, namespaceURI];
		}
		return null;
	}

	public virtual XmlAttribute SetAttributeNode(string localName, string? namespaceURI)
	{
		XmlAttribute xmlAttribute = GetAttributeNode(localName, namespaceURI);
		if (xmlAttribute == null)
		{
			xmlAttribute = OwnerDocument.CreateAttribute(string.Empty, localName, namespaceURI);
			Attributes.InternalAppendAttribute(xmlAttribute);
		}
		return xmlAttribute;
	}

	public virtual XmlAttribute? RemoveAttributeNode(string localName, string? namespaceURI)
	{
		if (HasAttributes)
		{
			XmlAttribute attributeNode = GetAttributeNode(localName, namespaceURI);
			Attributes.Remove(attributeNode);
			return attributeNode;
		}
		return null;
	}

	public virtual XmlNodeList GetElementsByTagName(string localName, string namespaceURI)
	{
		return new XmlElementList(this, localName, namespaceURI);
	}

	public virtual bool HasAttribute(string name)
	{
		return GetAttributeNode(name) != null;
	}

	public virtual bool HasAttribute(string localName, string? namespaceURI)
	{
		return GetAttributeNode(localName, namespaceURI) != null;
	}

	public override void WriteTo(XmlWriter w)
	{
		if (GetType() == typeof(XmlElement))
		{
			WriteElementTo(w, this);
			return;
		}
		WriteStartElement(w);
		if (IsEmpty)
		{
			w.WriteEndElement();
			return;
		}
		WriteContentTo(w);
		w.WriteFullEndElement();
	}

	private static void WriteElementTo(XmlWriter writer, XmlElement el)
	{
		XmlNode xmlNode = el;
		while (true)
		{
			if (xmlNode is XmlElement xmlElement && xmlElement.GetType() == typeof(XmlElement))
			{
				xmlElement.WriteStartElement(writer);
				if (xmlElement.IsEmpty)
				{
					writer.WriteEndElement();
				}
				else
				{
					if (xmlElement._lastChild != null)
					{
						xmlNode = xmlElement.FirstChild;
						continue;
					}
					writer.WriteFullEndElement();
				}
			}
			else
			{
				xmlNode.WriteTo(writer);
			}
			while (xmlNode != el && xmlNode == xmlNode.ParentNode.LastChild)
			{
				xmlNode = xmlNode.ParentNode;
				writer.WriteFullEndElement();
			}
			if (xmlNode != el)
			{
				xmlNode = xmlNode.NextSibling;
				continue;
			}
			break;
		}
	}

	private void WriteStartElement(XmlWriter w)
	{
		w.WriteStartElement(Prefix, LocalName, NamespaceURI);
		if (HasAttributes)
		{
			XmlAttributeCollection attributes = Attributes;
			for (int i = 0; i < attributes.Count; i++)
			{
				XmlAttribute xmlAttribute = attributes[i];
				xmlAttribute.WriteTo(w);
			}
		}
	}

	public override void WriteContentTo(XmlWriter w)
	{
		for (XmlNode xmlNode = FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			xmlNode.WriteTo(w);
		}
	}

	public virtual XmlNode? RemoveAttributeAt(int i)
	{
		if (HasAttributes)
		{
			return _attributes.RemoveAt(i);
		}
		return null;
	}

	public virtual void RemoveAllAttributes()
	{
		if (HasAttributes)
		{
			_attributes.RemoveAll();
		}
	}

	public override void RemoveAll()
	{
		base.RemoveAll();
		RemoveAllAttributes();
	}

	internal void RemoveAllChildren()
	{
		base.RemoveAll();
	}

	internal override void SetParent(XmlNode node)
	{
		parentNode = node;
	}

	internal override string GetXPAttribute(string localName, string ns)
	{
		if (ns == OwnerDocument.strReservedXmlns)
		{
			return string.Empty;
		}
		XmlAttribute attributeNode = GetAttributeNode(localName, ns);
		if (attributeNode != null)
		{
			return attributeNode.Value;
		}
		return string.Empty;
	}
}
