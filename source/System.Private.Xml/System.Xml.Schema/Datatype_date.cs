namespace System.Xml.Schema;

internal sealed class Datatype_date : Datatype_dateTimeBase
{
	public override XmlTypeCode TypeCode => XmlTypeCode.Date;

	internal Datatype_date()
		: base(XsdDateTimeFlags.Date)
	{
	}
}
