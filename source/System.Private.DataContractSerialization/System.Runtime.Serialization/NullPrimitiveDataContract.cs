using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class NullPrimitiveDataContract : PrimitiveDataContract
{
	internal override string ReadMethodName
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	internal override string WriteMethodName
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This warns because the call to Base has the type annotated with DynamicallyAccessedMembers so it warnswhen looking into the methods of NullPrimitiveDataContract which are annotated with RequiresUnreferencedCodeAttribute. Because this just represents null, we suppress.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers", Justification = "This warns because the call to Base has the type annotated with DynamicallyAccessedMembers so it warnswhen looking into the methods of NullPrimitiveDataContract which are annotated with DynamicallyAccessedMembersAttribute. Because this just represents null, we suppress.")]
	public NullPrimitiveDataContract()
		: base(typeof(NullPrimitiveDataContract), DictionaryGlobals.EmptyString, DictionaryGlobals.EmptyString)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		throw new NotImplementedException();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		throw new NotImplementedException();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		throw new NotImplementedException();
	}
}
