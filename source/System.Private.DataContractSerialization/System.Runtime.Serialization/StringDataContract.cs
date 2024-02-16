using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal class StringDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteString";

	internal override string ReadMethodName => "ReadElementContentAsString";

	public StringDataContract()
		: this(DictionaryGlobals.StringLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	internal StringDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(string), name, ns)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteString((string)obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context == null)
		{
			if (!TryReadNullAtTopLevel(reader))
			{
				return reader.ReadElementContentAsString();
			}
			return null;
		}
		return HandleReadValue(reader.ReadElementContentAsString(), context);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		context.WriteString(xmlWriter, (string)obj, name, ns);
	}
}
