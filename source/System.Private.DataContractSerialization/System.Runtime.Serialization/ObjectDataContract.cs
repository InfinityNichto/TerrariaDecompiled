using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class ObjectDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteAnyType";

	internal override string ReadMethodName => "ReadElementContentAsAnyType";

	internal override bool CanContainReferences => true;

	internal override bool IsPrimitive => false;

	public ObjectDataContract()
		: base(typeof(object), DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		object obj;
		if (reader.IsEmptyElement)
		{
			reader.Skip();
			obj = new object();
		}
		else
		{
			string localName = reader.LocalName;
			string namespaceURI = reader.NamespaceURI;
			reader.Read();
			try
			{
				reader.ReadEndElement();
				obj = new object();
			}
			catch (XmlException innerException)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.XmlForObjectCannotHaveContent, localName, namespaceURI), innerException));
			}
		}
		if (context != null)
		{
			return HandleReadValue(obj, context);
		}
		return obj;
	}
}
