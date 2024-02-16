using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal class TimeSpanDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteTimeSpan";

	internal override string ReadMethodName => "ReadElementContentAsTimeSpan";

	public TimeSpanDataContract()
		: this(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SerializationNamespace)
	{
	}

	internal TimeSpanDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(TimeSpan), name, ns)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteTimeSpan((TimeSpan)obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return HandleReadValue(reader.ReadElementContentAsTimeSpan(), context);
		}
		return reader.ReadElementContentAsTimeSpan();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		writer.WriteTimeSpan((TimeSpan)obj, name, ns);
	}
}
