namespace System.Runtime.Serialization;

internal sealed class AsmxCharDataContract : CharDataContract
{
	internal AsmxCharDataContract()
		: base(DictionaryGlobals.CharLocalName, DictionaryGlobals.AsmxTypesNamespace)
	{
	}
}
