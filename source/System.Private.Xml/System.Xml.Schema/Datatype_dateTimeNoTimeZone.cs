namespace System.Xml.Schema;

internal sealed class Datatype_dateTimeNoTimeZone : Datatype_dateTimeBase
{
	internal Datatype_dateTimeNoTimeZone()
		: base(XsdDateTimeFlags.XdrDateTimeNoTz)
	{
	}
}
