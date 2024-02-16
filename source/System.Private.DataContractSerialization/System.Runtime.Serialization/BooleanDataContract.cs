using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class BooleanDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteBoolean";

	internal override string ReadMethodName => "ReadElementContentAsBoolean";

	public BooleanDataContract()
		: base(typeof(bool), DictionaryGlobals.BooleanLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteBoolean((bool)obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return HandleReadValue(reader.ReadElementContentAsBoolean(), context);
		}
		return reader.ReadElementContentAsBoolean();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteBoolean((bool)obj, name, ns);
	}
}
