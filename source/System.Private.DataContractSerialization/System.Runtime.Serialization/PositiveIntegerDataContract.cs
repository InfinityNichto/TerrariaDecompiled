namespace System.Runtime.Serialization;

internal sealed class PositiveIntegerDataContract : LongDataContract
{
	internal PositiveIntegerDataContract()
		: base(DictionaryGlobals.positiveIntegerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
