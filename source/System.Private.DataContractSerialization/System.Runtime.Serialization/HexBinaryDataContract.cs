namespace System.Runtime.Serialization;

internal sealed class HexBinaryDataContract : StringDataContract
{
	internal HexBinaryDataContract()
		: base(DictionaryGlobals.hexBinaryLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
