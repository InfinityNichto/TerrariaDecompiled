using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization;

public abstract class XmlObjectSerializer
{
	private static IFormatterConverter s_formatterConverter;

	internal virtual Dictionary<XmlQualifiedName, DataContract>? KnownDataContracts
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return null;
		}
	}

	internal static IFormatterConverter FormatterConverter
	{
		get
		{
			if (s_formatterConverter == null)
			{
				s_formatterConverter = new FormatterConverter();
			}
			return s_formatterConverter;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract void WriteStartObject(XmlDictionaryWriter writer, object? graph);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract void WriteObjectContent(XmlDictionaryWriter writer, object? graph);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract void WriteEndObject(XmlDictionaryWriter writer);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObject(Stream stream, object? graph)
	{
		CheckNull(stream, "stream");
		XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, ownsStream: false);
		WriteObject(xmlDictionaryWriter, graph);
		xmlDictionaryWriter.Flush();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObject(XmlWriter writer, object? graph)
	{
		CheckNull(writer, "writer");
		WriteObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteStartObject(XmlWriter writer, object? graph)
	{
		CheckNull(writer, "writer");
		WriteStartObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObjectContent(XmlWriter writer, object? graph)
	{
		CheckNull(writer, "writer");
		WriteObjectContent(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteEndObject(XmlWriter writer)
	{
		CheckNull(writer, "writer");
		WriteEndObject(XmlDictionaryWriter.CreateDictionaryWriter(writer));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObject(XmlDictionaryWriter writer, object? graph)
	{
		WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object graph)
	{
		WriteObjectHandleExceptions(writer, graph, null);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
	{
		try
		{
			CheckNull(writer, "writer");
			InternalWriteObject(writer, graph, dataContractResolver);
		}
		catch (XmlException innerException)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException), innerException));
		}
		catch (FormatException innerException2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException2), innerException2));
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteObject(XmlWriterDelegator writer, object graph)
	{
		WriteStartObject(writer.Writer, graph);
		WriteObjectContent(writer.Writer, graph);
		WriteEndObject(writer.Writer);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteObject(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
	{
		InternalWriteObject(writer, graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteStartObject(XmlWriterDelegator writer, object graph)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteObjectContent(XmlWriterDelegator writer, object graph)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteEndObject(XmlWriterDelegator writer)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteStartObjectHandleExceptions(XmlWriterDelegator writer, object graph)
	{
		try
		{
			CheckNull(writer, "writer");
			InternalWriteStartObject(writer, graph);
		}
		catch (XmlException innerException)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteStartObject, GetSerializeType(graph), innerException), innerException));
		}
		catch (FormatException innerException2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteStartObject, GetSerializeType(graph), innerException2), innerException2));
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteObjectContentHandleExceptions(XmlWriterDelegator writer, object graph)
	{
		try
		{
			CheckNull(writer, "writer");
			if (writer.WriteState != WriteState.Element)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(System.SR.Format(System.SR.XmlWriterMustBeInElement, writer.WriteState)));
			}
			InternalWriteObjectContent(writer, graph);
		}
		catch (XmlException innerException)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException), innerException));
		}
		catch (FormatException innerException2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException2), innerException2));
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteEndObjectHandleExceptions(XmlWriterDelegator writer)
	{
		try
		{
			CheckNull(writer, "writer");
			InternalWriteEndObject(writer);
		}
		catch (XmlException innerException)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteEndObject, null, innerException), innerException));
		}
		catch (FormatException innerException2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteEndObject, null, innerException2), innerException2));
		}
	}

	internal void WriteRootElement(XmlWriterDelegator writer, DataContract contract, XmlDictionaryString name, XmlDictionaryString ns, bool needsContractNsAtRoot)
	{
		if (name == null)
		{
			if (contract.HasRoot)
			{
				contract.WriteRootElement(writer, contract.TopLevelElementName, contract.TopLevelElementNamespace);
			}
			return;
		}
		contract.WriteRootElement(writer, name, ns);
		if (needsContractNsAtRoot)
		{
			writer.WriteNamespaceDecl(contract.Namespace);
		}
	}

	internal bool CheckIfNeedsContractNsAtRoot(XmlDictionaryString name, XmlDictionaryString ns, DataContract contract)
	{
		if (name == null)
		{
			return false;
		}
		if (contract.IsBuiltInDataContract || !contract.CanContainReferences)
		{
			return false;
		}
		string @string = XmlDictionaryString.GetString(contract.Namespace);
		if (string.IsNullOrEmpty(@string) || @string == XmlDictionaryString.GetString(ns))
		{
			return false;
		}
		return true;
	}

	internal static void WriteNull(XmlWriterDelegator writer)
	{
		writer.WriteAttributeBool("i", DictionaryGlobals.XsiNilLocalName, DictionaryGlobals.SchemaInstanceNamespace, value: true);
	}

	internal static bool IsContractDeclared(DataContract contract, DataContract declaredContract)
	{
		if (contract.Name != declaredContract.Name || contract.Namespace != declaredContract.Namespace)
		{
			if (contract.Name.Value == declaredContract.Name.Value)
			{
				return contract.Namespace.Value == declaredContract.Namespace.Value;
			}
			return false;
		}
		return true;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(Stream stream)
	{
		CheckNull(stream, "stream");
		return ReadObject(XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(XmlReader reader)
	{
		CheckNull(reader, "reader");
		return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(XmlDictionaryReader reader)
	{
		return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName: true);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(XmlReader reader, bool verifyObjectName)
	{
		CheckNull(reader, "reader");
		return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader), verifyObjectName);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual bool IsStartObject(XmlReader reader)
	{
		CheckNull(reader, "reader");
		return IsStartObject(XmlDictionaryReader.CreateDictionaryReader(reader));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract bool IsStartObject(XmlDictionaryReader reader);

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName)
	{
		return ReadObject(reader.UnderlyingReader, verifyObjectName);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver dataContractResolver)
	{
		return InternalReadObject(reader, verifyObjectName);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual bool InternalIsStartObject(XmlReaderDelegator reader)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal object ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName)
	{
		return ReadObjectHandleExceptions(reader, verifyObjectName, null);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal object ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver dataContractResolver)
	{
		try
		{
			CheckNull(reader, "reader");
			return InternalReadObject(reader, verifyObjectName, dataContractResolver);
		}
		catch (XmlException innerException)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorDeserializing, GetDeserializeType(), innerException), innerException));
		}
		catch (FormatException innerException2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorDeserializing, GetDeserializeType(), innerException2), innerException2));
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool IsStartObjectHandleExceptions(XmlReaderDelegator reader)
	{
		try
		{
			CheckNull(reader, "reader");
			return InternalIsStartObject(reader);
		}
		catch (XmlException innerException)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorIsStartObject, GetDeserializeType(), innerException), innerException));
		}
		catch (FormatException innerException2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateSerializationException(GetTypeInfoError(System.SR.ErrorIsStartObject, GetDeserializeType(), innerException2), innerException2));
		}
	}

	internal bool IsRootXmlAny(XmlDictionaryString rootName, DataContract contract)
	{
		if (rootName == null)
		{
			return !contract.HasRoot;
		}
		return false;
	}

	internal bool IsStartElement(XmlReaderDelegator reader)
	{
		if (!reader.MoveToElement())
		{
			return reader.IsStartElement();
		}
		return true;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool IsRootElement(XmlReaderDelegator reader, DataContract contract, XmlDictionaryString name, XmlDictionaryString ns)
	{
		reader.MoveToElement();
		if (name != null)
		{
			return reader.IsStartElement(name, ns ?? XmlDictionaryString.Empty);
		}
		if (!contract.HasRoot)
		{
			return reader.IsStartElement();
		}
		if (reader.IsStartElement(contract.TopLevelElementName, contract.TopLevelElementNamespace))
		{
			return true;
		}
		ClassDataContract classDataContract = contract as ClassDataContract;
		if (classDataContract != null)
		{
			classDataContract = classDataContract.BaseContract;
		}
		while (classDataContract != null)
		{
			if (reader.IsStartElement(classDataContract.TopLevelElementName, classDataContract.TopLevelElementNamespace))
			{
				return true;
			}
			classDataContract = classDataContract.BaseContract;
		}
		if (classDataContract == null)
		{
			DataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(Globals.TypeOfObject);
			if (reader.IsStartElement(primitiveDataContract.TopLevelElementName, primitiveDataContract.TopLevelElementNamespace))
			{
				return true;
			}
		}
		return false;
	}

	internal static void CheckNull(object obj, string name)
	{
		if (obj == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(name));
		}
	}

	internal static string TryAddLineInfo(XmlReaderDelegator reader, string errorMessage)
	{
		if (reader.HasLineInfo())
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 2, invariantCulture);
			handler.AppendFormatted(System.SR.Format(System.SR.ErrorInLine, reader.LineNumber, reader.LinePosition));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(errorMessage);
			return string.Create(invariantCulture, ref handler);
		}
		return errorMessage;
	}

	internal static Exception CreateSerializationExceptionWithReaderDetails(string errorMessage, XmlReaderDelegator reader)
	{
		return CreateSerializationException(TryAddLineInfo(reader, System.SR.Format(System.SR.EncounteredWithNameNamespace, errorMessage, reader.NodeType, reader.LocalName, reader.NamespaceURI)));
	}

	internal static SerializationException CreateSerializationException(string errorMessage)
	{
		return CreateSerializationException(errorMessage, null);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static SerializationException CreateSerializationException(string errorMessage, Exception innerException)
	{
		return new SerializationException(errorMessage, innerException);
	}

	internal static string GetTypeInfoError(string errorMessage, Type type, Exception innerException)
	{
		string p = ((type == null) ? string.Empty : System.SR.Format(System.SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(type)));
		string p2 = ((innerException == null) ? string.Empty : innerException.Message);
		return System.SR.Format(errorMessage, p, p2);
	}

	internal virtual Type GetSerializeType(object graph)
	{
		return graph?.GetType();
	}

	internal virtual Type GetDeserializeType()
	{
		return null;
	}
}
