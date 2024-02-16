namespace System.Xml.Schema;

internal sealed class Datatype_monthDay : Datatype_dateTimeBase
{
	public override XmlTypeCode TypeCode => XmlTypeCode.GMonthDay;

	internal Datatype_monthDay()
		: base(XsdDateTimeFlags.GMonthDay)
	{
	}
}
