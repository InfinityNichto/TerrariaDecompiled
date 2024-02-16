using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class UriDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteUri";

	internal override string ReadMethodName => "ReadElementContentAsUri";

	public UriDataContract()
		: base(typeof(Uri), DictionaryGlobals.UriLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteUri((Uri)obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context == null)
		{
			if (!TryReadNullAtTopLevel(reader))
			{
				return reader.ReadElementContentAsUri();
			}
			return null;
		}
		return HandleReadValue(reader.ReadElementContentAsUri(), context);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		writer.WriteUri((Uri)obj, name, ns);
	}
}
