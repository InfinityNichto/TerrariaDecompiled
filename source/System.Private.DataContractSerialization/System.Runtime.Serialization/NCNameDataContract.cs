namespace System.Runtime.Serialization;

internal sealed class NCNameDataContract : StringDataContract
{
	internal NCNameDataContract()
		: base(DictionaryGlobals.NCNameLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
