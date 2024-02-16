namespace System.Xml.Schema;

internal sealed class Datatype_month : Datatype_dateTimeBase
{
	public override XmlTypeCode TypeCode => XmlTypeCode.GMonth;

	internal Datatype_month()
		: base(XsdDateTimeFlags.GMonth)
	{
	}
}
