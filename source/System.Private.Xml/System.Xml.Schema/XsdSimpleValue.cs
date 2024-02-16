namespace System.Xml.Schema;

internal sealed class XsdSimpleValue
{
	private readonly XmlSchemaSimpleType _xmlType;

	private readonly object _typedValue;

	public XmlSchemaSimpleType XmlType => _xmlType;

	public object TypedValue => _typedValue;

	public XsdSimpleValue(XmlSchemaSimpleType st, object value)
	{
		_xmlType = st;
		_typedValue = value;
	}
}
