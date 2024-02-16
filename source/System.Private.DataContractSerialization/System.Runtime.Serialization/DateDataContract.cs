namespace System.Runtime.Serialization;

internal sealed class DateDataContract : StringDataContract
{
	internal DateDataContract()
		: base(DictionaryGlobals.dateLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
