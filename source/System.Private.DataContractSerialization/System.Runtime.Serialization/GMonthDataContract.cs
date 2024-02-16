namespace System.Runtime.Serialization;

internal sealed class GMonthDataContract : StringDataContract
{
	internal GMonthDataContract()
		: base(DictionaryGlobals.gMonthLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
