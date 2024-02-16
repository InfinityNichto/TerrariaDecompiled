using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class ReflectionJsonCollectionReader
{
	private readonly ReflectionReader _reflectionReader = new ReflectionJsonReader();

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public object ReflectionReadCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
	{
		return _reflectionReader.ReflectionReadCollection(xmlReader, context, itemName, emptyDictionaryString, collectionContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionReadGetOnlyCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract)
	{
		_reflectionReader.ReflectionReadGetOnlyCollection(xmlReader, context, itemName, emptyDictionaryString, collectionContract);
	}
}
