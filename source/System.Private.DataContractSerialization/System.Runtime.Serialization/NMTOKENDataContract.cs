namespace System.Runtime.Serialization;

internal sealed class NMTOKENDataContract : StringDataContract
{
	internal NMTOKENDataContract()
		: base(DictionaryGlobals.NMTOKENLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
