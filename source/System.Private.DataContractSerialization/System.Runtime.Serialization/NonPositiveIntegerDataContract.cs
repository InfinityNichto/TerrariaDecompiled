namespace System.Runtime.Serialization;

internal sealed class NonPositiveIntegerDataContract : LongDataContract
{
	internal NonPositiveIntegerDataContract()
		: base(DictionaryGlobals.nonPositiveIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
