namespace System.Runtime.Serialization;

internal sealed class ENTITYDataContract : StringDataContract
{
	internal ENTITYDataContract()
		: base(DictionaryGlobals.ENTITYLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
