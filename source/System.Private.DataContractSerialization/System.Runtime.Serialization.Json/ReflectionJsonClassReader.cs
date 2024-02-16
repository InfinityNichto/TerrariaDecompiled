using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class ReflectionJsonClassReader
{
	private readonly ClassDataContract _classContract;

	private readonly ReflectionReader _reflectionReader;

	public ReflectionJsonClassReader(ClassDataContract classDataContract)
	{
		_classContract = classDataContract;
		_reflectionReader = new ReflectionJsonReader();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString[] memberNames)
	{
		return _reflectionReader.ReflectionReadClass(xmlReader, context, memberNames, null, _classContract);
	}
}
