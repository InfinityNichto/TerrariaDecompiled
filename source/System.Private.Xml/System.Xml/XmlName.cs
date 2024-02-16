using System.Xml.Schema;

namespace System.Xml;

internal class XmlName : IXmlSchemaInfo
{
	private readonly string _prefix;

	private readonly string _localName;

	private readonly string _ns;

	private string _name;

	private readonly int _hashCode;

	internal XmlDocument ownerDoc;

	internal XmlName next;

	public string LocalName => _localName;

	public string NamespaceURI => _ns;

	public string Prefix => _prefix;

	public int HashCode => _hashCode;

	public XmlDocument OwnerDocument => ownerDoc;

	public string Name
	{
		get
		{
			if (_name == null)
			{
				if (_prefix.Length > 0)
				{
					if (_localName.Length > 0)
					{
						string array = _prefix + ":" + _localName;
						lock (ownerDoc.NameTable)
						{
							if (_name == null)
							{
								_name = ownerDoc.NameTable.Add(array);
							}
						}
					}
					else
					{
						_name = _prefix;
					}
				}
				else
				{
					_name = _localName;
				}
			}
			return _name;
		}
	}

	public virtual XmlSchemaValidity Validity => XmlSchemaValidity.NotKnown;

	public virtual bool IsDefault => false;

	public virtual bool IsNil => false;

	public virtual XmlSchemaSimpleType MemberType => null;

	public virtual XmlSchemaType SchemaType => null;

	public virtual XmlSchemaElement SchemaElement => null;

	public virtual XmlSchemaAttribute SchemaAttribute => null;

	public static XmlName Create(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next, IXmlSchemaInfo schemaInfo)
	{
		if (schemaInfo == null)
		{
			return new XmlName(prefix, localName, ns, hashCode, ownerDoc, next);
		}
		return new XmlNameEx(prefix, localName, ns, hashCode, ownerDoc, next, schemaInfo);
	}

	internal XmlName(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next)
	{
		_prefix = prefix;
		_localName = localName;
		_ns = ns;
		_name = null;
		_hashCode = hashCode;
		this.ownerDoc = ownerDoc;
		this.next = next;
	}

	public virtual bool Equals(IXmlSchemaInfo schemaInfo)
	{
		return schemaInfo == null;
	}

	public static int GetHashCode(string name)
	{
		int result = 0;
		if (name != null)
		{
			return string.GetHashCode(name.AsSpan(name.LastIndexOf(':') + 1));
		}
		return result;
	}
}
