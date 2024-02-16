using System.Xml.Schema;

namespace System.Xml;

internal sealed class DomNameTable
{
	private XmlName[] _entries;

	private int _count;

	private int _mask;

	private readonly XmlDocument _ownerDocument;

	private readonly XmlNameTable _nameTable;

	public DomNameTable(XmlDocument document)
	{
		_ownerDocument = document;
		_nameTable = document.NameTable;
		_entries = new XmlName[64];
		_mask = 63;
	}

	public XmlName GetName(string prefix, string localName, string ns, IXmlSchemaInfo schemaInfo)
	{
		if (prefix == null)
		{
			prefix = string.Empty;
		}
		if (ns == null)
		{
			ns = string.Empty;
		}
		int hashCode = XmlName.GetHashCode(localName);
		for (XmlName xmlName = _entries[hashCode & _mask]; xmlName != null; xmlName = xmlName.next)
		{
			if (xmlName.HashCode == hashCode && ((object)xmlName.LocalName == localName || xmlName.LocalName.Equals(localName)) && ((object)xmlName.Prefix == prefix || xmlName.Prefix.Equals(prefix)) && ((object)xmlName.NamespaceURI == ns || xmlName.NamespaceURI.Equals(ns)) && xmlName.Equals(schemaInfo))
			{
				return xmlName;
			}
		}
		return null;
	}

	public XmlName AddName(string prefix, string localName, string ns, IXmlSchemaInfo schemaInfo)
	{
		if (prefix == null)
		{
			prefix = string.Empty;
		}
		if (ns == null)
		{
			ns = string.Empty;
		}
		int hashCode = XmlName.GetHashCode(localName);
		for (XmlName xmlName = _entries[hashCode & _mask]; xmlName != null; xmlName = xmlName.next)
		{
			if (xmlName.HashCode == hashCode && ((object)xmlName.LocalName == localName || xmlName.LocalName.Equals(localName)) && ((object)xmlName.Prefix == prefix || xmlName.Prefix.Equals(prefix)) && ((object)xmlName.NamespaceURI == ns || xmlName.NamespaceURI.Equals(ns)) && xmlName.Equals(schemaInfo))
			{
				return xmlName;
			}
		}
		prefix = _nameTable.Add(prefix);
		localName = _nameTable.Add(localName);
		ns = _nameTable.Add(ns);
		int num = hashCode & _mask;
		XmlName xmlName2 = XmlName.Create(prefix, localName, ns, hashCode, _ownerDocument, _entries[num], schemaInfo);
		_entries[num] = xmlName2;
		if (_count++ == _mask)
		{
			Grow();
		}
		return xmlName2;
	}

	private void Grow()
	{
		int num = _mask * 2 + 1;
		XmlName[] entries = _entries;
		XmlName[] array = new XmlName[num + 1];
		for (int i = 0; i < entries.Length; i++)
		{
			XmlName xmlName = entries[i];
			while (xmlName != null)
			{
				int num2 = xmlName.HashCode & num;
				XmlName next = xmlName.next;
				xmlName.next = array[num2];
				array[num2] = xmlName;
				xmlName = next;
			}
		}
		_entries = array;
		_mask = num;
	}
}
