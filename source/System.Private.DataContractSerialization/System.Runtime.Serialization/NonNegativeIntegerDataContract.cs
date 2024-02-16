namespace System.Runtime.Serialization;

internal sealed class NonNegativeIntegerDataContract : LongDataContract
{
	internal NonNegativeIntegerDataContract()
		: base(DictionaryGlobals.nonNegativeIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
