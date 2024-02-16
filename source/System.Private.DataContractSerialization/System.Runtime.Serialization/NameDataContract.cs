namespace System.Runtime.Serialization;

internal sealed class NameDataContract : StringDataContract
{
	internal NameDataContract()
		: base(DictionaryGlobals.NameLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
