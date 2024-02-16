namespace System.Runtime.Serialization;

internal sealed class NormalizedStringDataContract : StringDataContract
{
	internal NormalizedStringDataContract()
		: base(DictionaryGlobals.normalizedStringLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
