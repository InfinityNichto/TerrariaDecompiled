namespace System.Runtime.Serialization;

internal sealed class IntegerDataContract : LongDataContract
{
	internal IntegerDataContract()
		: base(DictionaryGlobals.integerLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
