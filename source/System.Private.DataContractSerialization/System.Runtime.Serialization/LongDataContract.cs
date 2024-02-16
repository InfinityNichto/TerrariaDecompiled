using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal class LongDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteLong";

	internal override string ReadMethodName => "ReadElementContentAsLong";

	public LongDataContract()
		: this(DictionaryGlobals.LongLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	internal LongDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(long), name, ns)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteLong((long)obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return HandleReadValue(reader.ReadElementContentAsLong(), context);
		}
		return reader.ReadElementContentAsLong();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteLong((long)obj, name, ns);
	}
}
