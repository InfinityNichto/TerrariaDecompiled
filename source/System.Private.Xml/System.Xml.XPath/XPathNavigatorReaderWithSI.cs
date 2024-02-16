using System.Xml.Schema;

namespace System.Xml.XPath;

internal sealed class XPathNavigatorReaderWithSI : XPathNavigatorReader, IXmlSchemaInfo
{
	public XmlSchemaValidity Validity
	{
		get
		{
			if (!base.IsReading)
			{
				return XmlSchemaValidity.NotKnown;
			}
			return schemaInfo.Validity;
		}
	}

	public override bool IsDefault
	{
		get
		{
			if (!base.IsReading)
			{
				return false;
			}
			return schemaInfo.IsDefault;
		}
	}

	public bool IsNil
	{
		get
		{
			if (!base.IsReading)
			{
				return false;
			}
			return schemaInfo.IsNil;
		}
	}

	public XmlSchemaSimpleType MemberType
	{
		get
		{
			if (!base.IsReading)
			{
				return null;
			}
			return schemaInfo.MemberType;
		}
	}

	public XmlSchemaType SchemaType
	{
		get
		{
			if (!base.IsReading)
			{
				return null;
			}
			return schemaInfo.SchemaType;
		}
	}

	public XmlSchemaElement SchemaElement
	{
		get
		{
			if (!base.IsReading)
			{
				return null;
			}
			return schemaInfo.SchemaElement;
		}
	}

	public XmlSchemaAttribute SchemaAttribute
	{
		get
		{
			if (!base.IsReading)
			{
				return null;
			}
			return schemaInfo.SchemaAttribute;
		}
	}

	internal XPathNavigatorReaderWithSI(XPathNavigator navToRead, IXmlLineInfo xli, IXmlSchemaInfo xsi)
		: base(navToRead, xli, xsi)
	{
		schemaInfo = xsi;
	}
}
