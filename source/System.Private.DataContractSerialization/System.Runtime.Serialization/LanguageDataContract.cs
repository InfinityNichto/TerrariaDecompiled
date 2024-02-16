namespace System.Runtime.Serialization;

internal sealed class LanguageDataContract : StringDataContract
{
	internal LanguageDataContract()
		: base(DictionaryGlobals.languageLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
