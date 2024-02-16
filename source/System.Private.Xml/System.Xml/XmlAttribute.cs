using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml;

public class XmlAttribute : XmlNode
{
	private XmlName _name;

	private XmlLinkedNode _lastChild;

	internal int LocalNameHash => _name.HashCode;

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

	public override XmlNode? ParentNode => null;

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
			_name = _name.OwnerDocument.AddAttrXmlName(value, LocalName, NamespaceURI, SchemaInfo);
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.Attribute;

	public override XmlDocument OwnerDocument => _name.OwnerDocument;

	public override string Value
	{
		get
		{
			return InnerText;
		}
		[param: AllowNull]
		set
		{
			InnerText = value;
		}
	}

	public override IXmlSchemaInfo SchemaInfo => _name;

	public override string InnerText
	{
		set
		{
			if (PrepareOwnerElementInElementIdAttrMap())
			{
				string innerText = base.InnerText;
				base.InnerText = value;
				ResetOwnerElementInElementIdAttrMap(innerText);
			}
			else
			{
				base.InnerText = value;
			}
		}
	}

	internal override bool IsContainer => true;

	internal override XmlLinkedNode? LastNode
	{
		get
		{
			return _lastChild;
		}
		set
		{
			_lastChild = value;
		}
	}

	public virtual bool Specified => true;

	public virtual XmlElement? OwnerElement => parentNode as XmlElement;

	public override string InnerXml
	{
		set
		{
			RemoveAll();
			XmlLoader xmlLoader = new XmlLoader();
			xmlLoader.LoadInnerXmlAttribute(this, value);
		}
	}

	public override string BaseURI
	{
		get
		{
			if (OwnerElement != null)
			{
				return OwnerElement.BaseURI;
			}
			return string.Empty;
		}
	}

	internal override XmlSpace XmlSpace
	{
		get
		{
			if (OwnerElement != null)
			{
				return OwnerElement.XmlSpace;
			}
			return XmlSpace.None;
		}
	}

	internal override string XmlLang
	{
		get
		{
			if (OwnerElement != null)
			{
				return OwnerElement.XmlLang;
			}
			return string.Empty;
		}
	}

	internal override XPathNodeType XPNodeType
	{
		get
		{
			if (IsNamespace)
			{
				return XPathNodeType.Namespace;
			}
			return XPathNodeType.Attribute;
		}
	}

	internal override string XPLocalName
	{
		get
		{
			if (_name.Prefix.Length == 0 && _name.LocalName == "xmlns")
			{
				return string.Empty;
			}
			return _name.LocalName;
		}
	}

	internal bool IsNamespace => Ref.Equal(_name.NamespaceURI, _name.OwnerDocument.strReservedXmlns);

	internal XmlAttribute(XmlName name, XmlDocument doc)
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
			throw new ArgumentException(System.SR.Xdom_Attr_Name);
		}
		_name = name;
	}

	protected internal XmlAttribute(string? prefix, string localName, string? namespaceURI, XmlDocument doc)
		: this(doc.AddAttrXmlName(prefix, localName, namespaceURI, null), doc)
	{
	}

	public override XmlNode CloneNode(bool deep)
	{
		XmlDocument ownerDocument = OwnerDocument;
		XmlAttribute xmlAttribute = ownerDocument.CreateAttribute(Prefix, LocalName, NamespaceURI);
		xmlAttribute.CopyChildren(ownerDocument, this, deep: true);
		return xmlAttribute;
	}

	internal bool PrepareOwnerElementInElementIdAttrMap()
	{
		XmlDocument ownerDocument = OwnerDocument;
		if (ownerDocument.DtdSchemaInfo != null)
		{
			XmlElement ownerElement = OwnerElement;
			if (ownerElement != null)
			{
				return ownerElement.Attributes.PrepareParentInElementIdAttrMap(Prefix, LocalName);
			}
		}
		return false;
	}

	internal void ResetOwnerElementInElementIdAttrMap(string oldInnerText)
	{
		OwnerElement?.Attributes.ResetParentInElementIdAttrMap(oldInnerText, InnerText);
	}

	internal override XmlNode AppendChildForLoad(XmlNode newChild, XmlDocument doc)
	{
		XmlNodeChangedEventArgs insertEventArgsForLoad = doc.GetInsertEventArgsForLoad(newChild, this);
		if (insertEventArgsForLoad != null)
		{
			doc.BeforeEvent(insertEventArgsForLoad);
		}
		XmlLinkedNode xmlLinkedNode = (XmlLinkedNode)newChild;
		if (_lastChild == null)
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
		if (type != XmlNodeType.Text)
		{
			return type == XmlNodeType.EntityReference;
		}
		return true;
	}

	public override XmlNode? InsertBefore(XmlNode newChild, XmlNode? refChild)
	{
		XmlNode result;
		if (PrepareOwnerElementInElementIdAttrMap())
		{
			string innerText = InnerText;
			result = base.InsertBefore(newChild, refChild);
			ResetOwnerElementInElementIdAttrMap(innerText);
		}
		else
		{
			result = base.InsertBefore(newChild, refChild);
		}
		return result;
	}

	public override XmlNode? InsertAfter(XmlNode newChild, XmlNode? refChild)
	{
		XmlNode result;
		if (PrepareOwnerElementInElementIdAttrMap())
		{
			string innerText = InnerText;
			result = base.InsertAfter(newChild, refChild);
			ResetOwnerElementInElementIdAttrMap(innerText);
		}
		else
		{
			result = base.InsertAfter(newChild, refChild);
		}
		return result;
	}

	public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
	{
		XmlNode result;
		if (PrepareOwnerElementInElementIdAttrMap())
		{
			string innerText = InnerText;
			result = base.ReplaceChild(newChild, oldChild);
			ResetOwnerElementInElementIdAttrMap(innerText);
		}
		else
		{
			result = base.ReplaceChild(newChild, oldChild);
		}
		return result;
	}

	public override XmlNode RemoveChild(XmlNode oldChild)
	{
		XmlNode result;
		if (PrepareOwnerElementInElementIdAttrMap())
		{
			string innerText = InnerText;
			result = base.RemoveChild(oldChild);
			ResetOwnerElementInElementIdAttrMap(innerText);
		}
		else
		{
			result = base.RemoveChild(oldChild);
		}
		return result;
	}

	public override XmlNode? PrependChild(XmlNode newChild)
	{
		XmlNode result;
		if (PrepareOwnerElementInElementIdAttrMap())
		{
			string innerText = InnerText;
			result = base.PrependChild(newChild);
			ResetOwnerElementInElementIdAttrMap(innerText);
		}
		else
		{
			result = base.PrependChild(newChild);
		}
		return result;
	}

	public override XmlNode? AppendChild(XmlNode newChild)
	{
		XmlNode result;
		if (PrepareOwnerElementInElementIdAttrMap())
		{
			string innerText = InnerText;
			result = base.AppendChild(newChild);
			ResetOwnerElementInElementIdAttrMap(innerText);
		}
		else
		{
			result = base.AppendChild(newChild);
		}
		return result;
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteStartAttribute(Prefix, LocalName, NamespaceURI);
		WriteContentTo(w);
		w.WriteEndAttribute();
	}

	public override void WriteContentTo(XmlWriter w)
	{
		for (XmlNode xmlNode = FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			xmlNode.WriteTo(w);
		}
	}

	internal override void SetParent(XmlNode node)
	{
		parentNode = node;
	}
}
