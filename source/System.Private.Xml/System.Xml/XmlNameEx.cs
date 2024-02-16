using System.Xml.Schema;

namespace System.Xml;

internal sealed class XmlNameEx : XmlName
{
	private byte _flags;

	private readonly XmlSchemaSimpleType _memberType;

	private readonly XmlSchemaType _schemaType;

	private readonly object _decl;

	public override XmlSchemaValidity Validity
	{
		get
		{
			if (!ownerDoc.CanReportValidity)
			{
				return XmlSchemaValidity.NotKnown;
			}
			return (XmlSchemaValidity)(_flags & 3);
		}
	}

	public override bool IsDefault => (_flags & 4) != 0;

	public override bool IsNil => (_flags & 8) != 0;

	public override XmlSchemaSimpleType MemberType => _memberType;

	public override XmlSchemaType SchemaType => _schemaType;

	public override XmlSchemaElement SchemaElement => _decl as XmlSchemaElement;

	public override XmlSchemaAttribute SchemaAttribute => _decl as XmlSchemaAttribute;

	internal XmlNameEx(string prefix, string localName, string ns, int hashCode, XmlDocument ownerDoc, XmlName next, IXmlSchemaInfo schemaInfo)
		: base(prefix, localName, ns, hashCode, ownerDoc, next)
	{
		SetValidity(schemaInfo.Validity);
		SetIsDefault(schemaInfo.IsDefault);
		SetIsNil(schemaInfo.IsNil);
		_memberType = schemaInfo.MemberType;
		_schemaType = schemaInfo.SchemaType;
		_decl = ((schemaInfo.SchemaElement != null) ? ((XmlSchemaAnnotated)schemaInfo.SchemaElement) : ((XmlSchemaAnnotated)schemaInfo.SchemaAttribute));
	}

	public void SetValidity(XmlSchemaValidity value)
	{
		_flags = (byte)((_flags & 0xFFFFFFFCu) | (byte)value);
	}

	public void SetIsDefault(bool value)
	{
		if (value)
		{
			_flags |= 4;
		}
		else
		{
			_flags = (byte)(_flags & 0xFFFFFFFBu);
		}
	}

	public void SetIsNil(bool value)
	{
		if (value)
		{
			_flags |= 8;
		}
		else
		{
			_flags = (byte)(_flags & 0xFFFFFFF7u);
		}
	}

	public override bool Equals(IXmlSchemaInfo schemaInfo)
	{
		if (schemaInfo != null && schemaInfo.Validity == (XmlSchemaValidity)(_flags & 3) && schemaInfo.IsDefault == ((_flags & 4) != 0) && schemaInfo.IsNil == ((_flags & 8) != 0) && schemaInfo.MemberType == _memberType && schemaInfo.SchemaType == _schemaType && schemaInfo.SchemaElement == _decl as XmlSchemaElement && schemaInfo.SchemaAttribute == _decl as XmlSchemaAttribute)
		{
			return true;
		}
		return false;
	}
}
