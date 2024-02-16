namespace System.Runtime.Serialization;

internal sealed class AsmxGuidDataContract : GuidDataContract
{
	internal AsmxGuidDataContract()
		: base(DictionaryGlobals.GuidLocalName, DictionaryGlobals.AsmxTypesNamespace)
	{
	}
}
