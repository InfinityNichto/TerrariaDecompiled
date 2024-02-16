namespace System.Runtime.Serialization;

internal sealed class XsDurationDataContract : TimeSpanDataContract
{
	public XsDurationDataContract()
		: base(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
