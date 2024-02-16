using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal class GuidDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteGuid";

	internal override string ReadMethodName => "ReadElementContentAsGuid";

	public GuidDataContract()
		: this(DictionaryGlobals.GuidLocalName, DictionaryGlobals.SerializationNamespace)
	{
	}

	internal GuidDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(Guid), name, ns)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteGuid((Guid)obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return HandleReadValue(reader.ReadElementContentAsGuid(), context);
		}
		return reader.ReadElementContentAsGuid();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteGuid((Guid)obj, name, ns);
	}
}
