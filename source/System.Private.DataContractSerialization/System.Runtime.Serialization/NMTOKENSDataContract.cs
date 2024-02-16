namespace System.Runtime.Serialization;

internal sealed class NMTOKENSDataContract : StringDataContract
{
	internal NMTOKENSDataContract()
		: base(DictionaryGlobals.NMTOKENSLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
