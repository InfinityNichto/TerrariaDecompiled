namespace System.Runtime.Serialization;

internal sealed class ENTITIESDataContract : StringDataContract
{
	internal ENTITIESDataContract()
		: base(DictionaryGlobals.ENTITIESLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
