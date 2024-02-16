using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class ReflectionXmlClassReader
{
	private readonly ClassDataContract _classContract;

	private readonly ReflectionReader _reflectionReader;

	public ReflectionXmlClassReader(ClassDataContract classDataContract)
	{
		_classContract = classDataContract;
		_reflectionReader = new ReflectionXmlReader();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public object ReflectionReadClass(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces)
	{
		return _reflectionReader.ReflectionReadClass(xmlReader, context, memberNames, memberNamespaces, _classContract);
	}
}
