using System.Xml.Schema;

namespace System.Xml;

internal sealed class XmlAsyncCheckReaderWithLineInfoNSSchema : XmlAsyncCheckReaderWithLineInfoNS, IXmlSchemaInfo
{
	private readonly IXmlSchemaInfo _readerAsIXmlSchemaInfo;

	XmlSchemaValidity IXmlSchemaInfo.Validity => _readerAsIXmlSchemaInfo.Validity;

	bool IXmlSchemaInfo.IsDefault => _readerAsIXmlSchemaInfo.IsDefault;

	bool IXmlSchemaInfo.IsNil => _readerAsIXmlSchemaInfo.IsNil;

	XmlSchemaSimpleType IXmlSchemaInfo.MemberType => _readerAsIXmlSchemaInfo.MemberType;

	XmlSchemaType IXmlSchemaInfo.SchemaType => _readerAsIXmlSchemaInfo.SchemaType;

	XmlSchemaElement IXmlSchemaInfo.SchemaElement => _readerAsIXmlSchemaInfo.SchemaElement;

	XmlSchemaAttribute IXmlSchemaInfo.SchemaAttribute => _readerAsIXmlSchemaInfo.SchemaAttribute;

	public XmlAsyncCheckReaderWithLineInfoNSSchema(XmlReader reader)
		: base(reader)
	{
		_readerAsIXmlSchemaInfo = (IXmlSchemaInfo)reader;
	}
}
