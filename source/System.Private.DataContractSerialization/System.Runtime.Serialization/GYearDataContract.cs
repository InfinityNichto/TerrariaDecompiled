namespace System.Runtime.Serialization;

internal sealed class GYearDataContract : StringDataContract
{
	internal GYearDataContract()
		: base(DictionaryGlobals.gYearLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
