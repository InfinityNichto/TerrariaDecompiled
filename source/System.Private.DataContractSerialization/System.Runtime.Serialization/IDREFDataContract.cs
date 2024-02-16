namespace System.Runtime.Serialization;

internal sealed class IDREFDataContract : StringDataContract
{
	internal IDREFDataContract()
		: base(DictionaryGlobals.IDREFLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
