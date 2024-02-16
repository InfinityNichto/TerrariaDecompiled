namespace System.Runtime.Serialization;

internal sealed class GYearMonthDataContract : StringDataContract
{
	internal GYearMonthDataContract()
		: base(DictionaryGlobals.gYearMonthLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
