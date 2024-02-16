namespace System.Runtime.Serialization;

internal sealed class TokenDataContract : StringDataContract
{
	internal TokenDataContract()
		: base(DictionaryGlobals.tokenLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
