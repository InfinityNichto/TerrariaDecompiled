using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal class CharDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteChar";

	internal override string ReadMethodName => "ReadElementContentAsChar";

	public CharDataContract()
		: this(DictionaryGlobals.CharLocalName, DictionaryGlobals.SerializationNamespace)
	{
	}

	internal CharDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(char), name, ns)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteChar((char)obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return HandleReadValue(reader.ReadElementContentAsChar(), context);
		}
		return reader.ReadElementContentAsChar();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteChar((char)obj, name, ns);
	}
}
