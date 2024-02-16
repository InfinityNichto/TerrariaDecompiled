namespace System.Runtime.Serialization;

internal sealed class NegativeIntegerDataContract : LongDataContract
{
	internal NegativeIntegerDataContract()
		: base(DictionaryGlobals.negativeIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
