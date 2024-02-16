using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal class XmlObjectSerializerWriteContextComplex : XmlObjectSerializerWriteContext
{
	private readonly ISerializationSurrogateProvider _serializationSurrogateProvider;

	private readonly SerializationMode _mode;

	internal override SerializationMode Mode => _mode;

	internal XmlObjectSerializerWriteContextComplex(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
		: base(serializer, rootTypeDataContract, dataContractResolver)
	{
		_mode = SerializationMode.SharedContract;
		preserveObjectReferences = serializer.PreserveObjectReferences;
		_serializationSurrogateProvider = serializer.SerializationSurrogateProvider;
	}

	internal XmlObjectSerializerWriteContextComplex(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
		: base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
	{
	}

	internal override bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, DataContract dataContract)
	{
		return false;
	}

	internal override bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, Type dataContractType, string clrTypeName, string clrAssemblyName)
	{
		return false;
	}

	internal override void WriteAnyType(XmlWriterDelegator xmlWriter, object value)
	{
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteAnyType(value);
		}
	}

	internal override void WriteString(XmlWriterDelegator xmlWriter, string value)
	{
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteString(value);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteString(XmlWriterDelegator xmlWriter, string value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(string), isMemberTypeSerializable: true, name, ns);
			return;
		}
		xmlWriter.WriteStartElementPrimitive(name, ns);
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteString(value);
		}
		xmlWriter.WriteEndElementPrimitive();
	}

	internal override void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value)
	{
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteBase64(value);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(byte[]), isMemberTypeSerializable: true, name, ns);
			return;
		}
		xmlWriter.WriteStartElementPrimitive(name, ns);
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteBase64(value);
		}
		xmlWriter.WriteEndElementPrimitive();
	}

	internal override void WriteUri(XmlWriterDelegator xmlWriter, Uri value)
	{
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteUri(value);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteUri(XmlWriterDelegator xmlWriter, Uri value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(Uri), isMemberTypeSerializable: true, name, ns);
			return;
		}
		xmlWriter.WriteStartElementPrimitive(name, ns);
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteUri(value);
		}
		xmlWriter.WriteEndElementPrimitive();
	}

	internal override void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value)
	{
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteQName(value);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(XmlQualifiedName), isMemberTypeSerializable: true, name, ns);
			return;
		}
		if (ns != null && ns.Value != null && ns.Value.Length > 0)
		{
			xmlWriter.WriteStartElement("q", name, ns);
		}
		else
		{
			xmlWriter.WriteStartElement(name, ns);
		}
		if (!OnHandleReference(xmlWriter, value, canContainCyclicReference: false))
		{
			xmlWriter.WriteQName(value);
		}
		xmlWriter.WriteEndElement();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void InternalSerialize(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
	{
		if (_serializationSurrogateProvider == null)
		{
			base.InternalSerialize(xmlWriter, obj, isDeclaredType, writeXsiType, declaredTypeID, declaredTypeHandle);
		}
		else
		{
			InternalSerializeWithSurrogate(xmlWriter, obj, isDeclaredType, writeXsiType, declaredTypeID, declaredTypeHandle);
		}
	}

	internal override bool OnHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
	{
		if (preserveObjectReferences && !IsGetOnlyCollection)
		{
			bool newId = true;
			int id = base.SerializedObjects.GetId(obj, ref newId);
			if (newId)
			{
				xmlWriter.WriteAttributeInt("z", DictionaryGlobals.IdLocalName, DictionaryGlobals.SerializationNamespace, id);
			}
			else
			{
				xmlWriter.WriteAttributeInt("z", DictionaryGlobals.RefLocalName, DictionaryGlobals.SerializationNamespace, id);
				xmlWriter.WriteAttributeBool("i", DictionaryGlobals.XsiNilLocalName, DictionaryGlobals.SchemaInstanceNamespace, value: true);
			}
			return !newId;
		}
		return base.OnHandleReference(xmlWriter, obj, canContainCyclicReference);
	}

	internal override void OnEndHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
	{
		if (!preserveObjectReferences || IsGetOnlyCollection)
		{
			base.OnEndHandleReference(xmlWriter, obj, canContainCyclicReference);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
	{
		if (_serializationSurrogateProvider != null)
		{
			while (memberType.IsArray)
			{
				memberType = memberType.GetElementType();
			}
			memberType = DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, memberType);
			if (!DataContract.IsTypeSerializable(memberType))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, memberType)));
			}
		}
		else
		{
			base.CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override Type GetSurrogatedType(Type type)
	{
		if (_serializationSurrogateProvider == null)
		{
			return base.GetSurrogatedType(type);
		}
		type = DataContract.UnwrapNullableType(type);
		Type surrogatedType = DataContractSerializer.GetSurrogatedType(_serializationSurrogateProvider, type);
		if (IsGetOnlyCollection && surrogatedType != type)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser, DataContract.GetClrTypeFullName(type))));
		}
		return surrogatedType;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void InternalSerializeWithSurrogate(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
	{
		RuntimeTypeHandle handle = (isDeclaredType ? declaredTypeHandle : obj.GetType().TypeHandle);
		object obj2 = obj;
		int oldObjId = 0;
		Type objType = Type.GetTypeFromHandle(handle);
		Type surrogatedType = GetSurrogatedType(Type.GetTypeFromHandle(declaredTypeHandle));
		declaredTypeHandle = surrogatedType.TypeHandle;
		obj = DataContractSerializer.SurrogateToDataContractType(_serializationSurrogateProvider, obj, surrogatedType, ref objType);
		handle = objType.TypeHandle;
		if (obj2 != obj)
		{
			oldObjId = base.SerializedObjects.ReassignId(0, obj2, obj);
		}
		if (writeXsiType)
		{
			surrogatedType = Globals.TypeOfObject;
			SerializeWithXsiType(xmlWriter, obj, handle, objType, -1, surrogatedType.TypeHandle, surrogatedType);
		}
		else if (declaredTypeHandle.Equals(handle))
		{
			DataContract dataContract = GetDataContract(handle, objType);
			SerializeWithoutXsiType(dataContract, xmlWriter, obj, declaredTypeHandle);
		}
		else
		{
			SerializeWithXsiType(xmlWriter, obj, handle, objType, -1, declaredTypeHandle, surrogatedType);
		}
		if (obj2 != obj)
		{
			base.SerializedObjects.ReassignId(oldObjId, obj, obj2);
		}
	}

	internal override void WriteArraySize(XmlWriterDelegator xmlWriter, int size)
	{
		if (preserveObjectReferences && size > -1)
		{
			xmlWriter.WriteAttributeInt("z", DictionaryGlobals.ArraySizeLocalName, DictionaryGlobals.SerializationNamespace, size);
		}
	}
}
