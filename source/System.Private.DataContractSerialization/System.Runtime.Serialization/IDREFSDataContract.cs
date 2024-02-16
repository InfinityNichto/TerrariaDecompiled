namespace System.Runtime.Serialization;

internal sealed class IDREFSDataContract : StringDataContract
{
	internal IDREFSDataContract()
		: base(DictionaryGlobals.IDREFSLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
