namespace System.Runtime.Serialization;

internal sealed class GDayDataContract : StringDataContract
{
	internal GDayDataContract()
		: base(DictionaryGlobals.gDayLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
