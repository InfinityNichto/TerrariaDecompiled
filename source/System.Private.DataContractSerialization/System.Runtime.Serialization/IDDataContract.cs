namespace System.Runtime.Serialization;

internal sealed class IDDataContract : StringDataContract
{
	internal IDDataContract()
		: base(DictionaryGlobals.XSDIDLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
