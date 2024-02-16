namespace System.Runtime.Serialization;

internal sealed class GMonthDayDataContract : StringDataContract
{
	internal GMonthDayDataContract()
		: base(DictionaryGlobals.gMonthDayLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
