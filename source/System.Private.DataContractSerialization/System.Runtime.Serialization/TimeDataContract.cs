namespace System.Runtime.Serialization;

internal sealed class TimeDataContract : StringDataContract
{
	internal TimeDataContract()
		: base(DictionaryGlobals.timeLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
