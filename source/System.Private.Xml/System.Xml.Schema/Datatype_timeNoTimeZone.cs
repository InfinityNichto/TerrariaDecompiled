namespace System.Xml.Schema;

internal sealed class Datatype_timeNoTimeZone : Datatype_dateTimeBase
{
	internal Datatype_timeNoTimeZone()
		: base(XsdDateTimeFlags.XdrTimeNoTz)
	{
	}
}
