namespace System.Runtime.Serialization.Json;

internal enum JsonNodeType
{
	None,
	Object,
	Element,
	EndElement,
	QuotedText,
	StandaloneText,
	Collection
}
