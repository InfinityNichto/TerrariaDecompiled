using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Xml;

public sealed class XmlAttributeCollection : XmlNamedNodeMap, ICollection, IEnumerable
{
	[IndexerName("ItemOf")]
	public XmlAttribute this[int i]
	{
		get
		{
			try
			{
				return (XmlAttribute)nodes[i];
			}
			catch (ArgumentOutOfRangeException)
			{
				throw new IndexOutOfRangeException(System.SR.Xdom_IndexOutOfRange);
			}
		}
	}

	[IndexerName("ItemOf")]
	public XmlAttribute? this[string name]
	{
		get
		{
			int hashCode = XmlName.GetHashCode(name);
			for (int i = 0; i < nodes.Count; i++)
			{
				XmlAttribute xmlAttribute = (XmlAttribute)nodes[i];
				if (hashCode == xmlAttribute.LocalNameHash && name == xmlAttribute.Name)
				{
					return xmlAttribute;
				}
			}
			return null;
		}
	}

	[IndexerName("ItemOf")]
	public XmlAttribute? this[string localName, string? namespaceURI]
	{
		get
		{
			int hashCode = XmlName.GetHashCode(localName);
			for (int i = 0; i < nodes.Count; i++)
			{
				XmlAttribute xmlAttribute = (XmlAttribute)nodes[i];
				if (hashCode == xmlAttribute.LocalNameHash && localName == xmlAttribute.LocalName && namespaceURI == xmlAttribute.NamespaceURI)
				{
					return xmlAttribute;
				}
			}
			return null;
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	int ICollection.Count => base.Count;

	internal XmlAttributeCollection(XmlNode parent)
		: base(parent)
	{
	}

	internal int FindNodeOffsetNS(XmlAttribute node)
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			XmlAttribute xmlAttribute = (XmlAttribute)nodes[i];
			if (xmlAttribute.LocalNameHash == node.LocalNameHash && xmlAttribute.LocalName == node.LocalName && xmlAttribute.NamespaceURI == node.NamespaceURI)
			{
				return i;
			}
		}
		return -1;
	}

	[return: NotNullIfNotNull("node")]
	public override XmlNode? SetNamedItem(XmlNode? node)
	{
		if (node == null)
		{
			return null;
		}
		if (!(node is XmlAttribute))
		{
			throw new ArgumentException(System.SR.Xdom_AttrCol_Object);
		}
		int num = FindNodeOffset(node.LocalName, node.NamespaceURI);
		if (num == -1)
		{
			return InternalAppendAttribute((XmlAttribute)node);
		}
		XmlNode result = base.RemoveNodeAt(num);
		InsertNodeAt(num, node);
		return result;
	}

	public XmlAttribute Prepend(XmlAttribute node)
	{
		if (node.OwnerDocument != null && node.OwnerDocument != parent.OwnerDocument)
		{
			throw new ArgumentException(System.SR.Xdom_NamedNode_Context);
		}
		if (node.OwnerElement != null)
		{
			Detach(node);
		}
		RemoveDuplicateAttribute(node);
		InsertNodeAt(0, node);
		return node;
	}

	public XmlAttribute Append(XmlAttribute node)
	{
		XmlDocument ownerDocument = node.OwnerDocument;
		if (ownerDocument == null || !ownerDocument.IsLoading)
		{
			if (ownerDocument != null && ownerDocument != parent.OwnerDocument)
			{
				throw new ArgumentException(System.SR.Xdom_NamedNode_Context);
			}
			if (node.OwnerElement != null)
			{
				Detach(node);
			}
			AddNode(node);
		}
		else
		{
			base.AddNodeForLoad(node, ownerDocument);
			InsertParentIntoElementIdAttrMap(node);
		}
		return node;
	}

	public XmlAttribute InsertBefore(XmlAttribute newNode, XmlAttribute? refNode)
	{
		if (newNode == refNode)
		{
			return newNode;
		}
		if (refNode == null)
		{
			return Append(newNode);
		}
		if (refNode.OwnerElement != parent)
		{
			throw new ArgumentException(System.SR.Xdom_AttrCol_Insert);
		}
		if (newNode.OwnerDocument != null && newNode.OwnerDocument != parent.OwnerDocument)
		{
			throw new ArgumentException(System.SR.Xdom_NamedNode_Context);
		}
		if (newNode.OwnerElement != null)
		{
			Detach(newNode);
		}
		int num = FindNodeOffset(refNode.LocalName, refNode.NamespaceURI);
		int num2 = RemoveDuplicateAttribute(newNode);
		if (num2 >= 0 && num2 < num)
		{
			num--;
		}
		InsertNodeAt(num, newNode);
		return newNode;
	}

	public XmlAttribute InsertAfter(XmlAttribute newNode, XmlAttribute? refNode)
	{
		if (newNode == refNode)
		{
			return newNode;
		}
		if (refNode == null)
		{
			return Prepend(newNode);
		}
		if (refNode.OwnerElement != parent)
		{
			throw new ArgumentException(System.SR.Xdom_AttrCol_Insert);
		}
		if (newNode.OwnerDocument != null && newNode.OwnerDocument != parent.OwnerDocument)
		{
			throw new ArgumentException(System.SR.Xdom_NamedNode_Context);
		}
		if (newNode.OwnerElement != null)
		{
			Detach(newNode);
		}
		int num = FindNodeOffset(refNode.LocalName, refNode.NamespaceURI);
		int num2 = RemoveDuplicateAttribute(newNode);
		if (num2 >= 0 && num2 <= num)
		{
			num--;
		}
		InsertNodeAt(num + 1, newNode);
		return newNode;
	}

	public XmlAttribute? Remove(XmlAttribute? node)
	{
		int count = nodes.Count;
		for (int i = 0; i < count; i++)
		{
			if (nodes[i] == node)
			{
				RemoveNodeAt(i);
				return node;
			}
		}
		return null;
	}

	public XmlAttribute? RemoveAt(int i)
	{
		if (i < 0 || i >= Count)
		{
			return null;
		}
		return (XmlAttribute)RemoveNodeAt(i);
	}

	public void RemoveAll()
	{
		int num = Count;
		while (num > 0)
		{
			num--;
			RemoveAt(num);
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		int num = 0;
		int count = Count;
		while (num < count)
		{
			array.SetValue(nodes[num], index);
			num++;
			index++;
		}
	}

	public void CopyTo(XmlAttribute[] array, int index)
	{
		int num = 0;
		int count = Count;
		while (num < count)
		{
			array[index] = (XmlAttribute)((XmlNode)nodes[num]).CloneNode(deep: true);
			num++;
			index++;
		}
	}

	internal override XmlNode AddNode(XmlNode node)
	{
		RemoveDuplicateAttribute((XmlAttribute)node);
		XmlNode result = base.AddNode(node);
		InsertParentIntoElementIdAttrMap((XmlAttribute)node);
		return result;
	}

	internal override XmlNode InsertNodeAt(int i, XmlNode node)
	{
		XmlNode result = base.InsertNodeAt(i, node);
		InsertParentIntoElementIdAttrMap((XmlAttribute)node);
		return result;
	}

	internal override XmlNode RemoveNodeAt(int i)
	{
		XmlNode xmlNode = base.RemoveNodeAt(i);
		RemoveParentFromElementIdAttrMap((XmlAttribute)xmlNode);
		XmlAttribute defaultAttribute = parent.OwnerDocument.GetDefaultAttribute((XmlElement)parent, xmlNode.Prefix, xmlNode.LocalName, xmlNode.NamespaceURI);
		if (defaultAttribute != null)
		{
			InsertNodeAt(i, defaultAttribute);
		}
		return xmlNode;
	}

	internal void Detach(XmlAttribute attr)
	{
		attr.OwnerElement.Attributes.Remove(attr);
	}

	internal void InsertParentIntoElementIdAttrMap(XmlAttribute attr)
	{
		if (parent is XmlElement xmlElement && parent.OwnerDocument != null)
		{
			XmlName iDInfoByElement = parent.OwnerDocument.GetIDInfoByElement(xmlElement.XmlName);
			if (iDInfoByElement != null && iDInfoByElement.Prefix == attr.XmlName.Prefix && iDInfoByElement.LocalName == attr.XmlName.LocalName)
			{
				parent.OwnerDocument.AddElementWithId(attr.Value, xmlElement);
			}
		}
	}

	internal void RemoveParentFromElementIdAttrMap(XmlAttribute attr)
	{
		if (parent is XmlElement xmlElement && parent.OwnerDocument != null)
		{
			XmlName iDInfoByElement = parent.OwnerDocument.GetIDInfoByElement(xmlElement.XmlName);
			if (iDInfoByElement != null && iDInfoByElement.Prefix == attr.XmlName.Prefix && iDInfoByElement.LocalName == attr.XmlName.LocalName)
			{
				parent.OwnerDocument.RemoveElementWithId(attr.Value, xmlElement);
			}
		}
	}

	internal int RemoveDuplicateAttribute(XmlAttribute attr)
	{
		int num = FindNodeOffset(attr.LocalName, attr.NamespaceURI);
		if (num != -1)
		{
			XmlAttribute attr2 = (XmlAttribute)nodes[num];
			base.RemoveNodeAt(num);
			RemoveParentFromElementIdAttrMap(attr2);
		}
		return num;
	}

	internal bool PrepareParentInElementIdAttrMap(string attrPrefix, string attrLocalName)
	{
		XmlElement xmlElement = parent as XmlElement;
		XmlDocument ownerDocument = parent.OwnerDocument;
		XmlName iDInfoByElement = ownerDocument.GetIDInfoByElement(xmlElement.XmlName);
		if (iDInfoByElement != null && iDInfoByElement.Prefix == attrPrefix && iDInfoByElement.LocalName == attrLocalName)
		{
			return true;
		}
		return false;
	}

	internal void ResetParentInElementIdAttrMap(string oldVal, string newVal)
	{
		XmlElement elem = parent as XmlElement;
		XmlDocument ownerDocument = parent.OwnerDocument;
		ownerDocument.RemoveElementWithId(oldVal, elem);
		ownerDocument.AddElementWithId(newVal, elem);
	}

	internal XmlAttribute InternalAppendAttribute(XmlAttribute node)
	{
		XmlNode xmlNode = base.AddNode(node);
		InsertParentIntoElementIdAttrMap(node);
		return (XmlAttribute)xmlNode;
	}
}
