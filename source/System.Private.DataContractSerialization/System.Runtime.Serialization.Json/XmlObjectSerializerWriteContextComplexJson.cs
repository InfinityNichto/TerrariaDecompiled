using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class XmlObjectSerializerWriteContextComplexJson : XmlObjectSerializerWriteContextComplex
{
	private readonly EmitTypeInformation _emitXsiType;

	private bool _perCallXsiTypeAlreadyEmitted;

	private readonly bool _useSimpleDictionaryFormat;

	internal IList<Type> SerializerKnownTypeList => serializerKnownTypeList;

	public bool UseSimpleDictionaryFormat => _useSimpleDictionaryFormat;

	internal XmlDictionaryString CollectionItemName => JsonGlobals.itemDictionaryString;

	internal static XmlObjectSerializerWriteContextComplexJson CreateContext(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
	{
		return new XmlObjectSerializerWriteContextComplexJson(serializer, rootTypeDataContract);
	}

	internal XmlObjectSerializerWriteContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
		: base(serializer, serializer.MaxItemsInObjectGraph, new StreamingContext(StreamingContextStates.All), serializer.IgnoreExtensionDataObject)
	{
		_emitXsiType = serializer.EmitTypeInformation;
		base.rootTypeDataContract = rootTypeDataContract;
		serializerKnownTypeList = serializer.knownTypeList;
		serializeReadOnlyTypes = serializer.SerializeReadOnlyTypes;
		_useSimpleDictionaryFormat = serializer.UseSimpleDictionaryFormat;
	}

	internal override void WriteArraySize(XmlWriterDelegator xmlWriter, int size)
	{
	}

	protected override void WriteTypeInfo(XmlWriterDelegator writer, string dataContractName, string dataContractNamespace)
	{
		if (_emitXsiType != EmitTypeInformation.Never)
		{
			if (string.IsNullOrEmpty(dataContractNamespace))
			{
				WriteTypeInfo(writer, dataContractName);
			}
			else
			{
				WriteTypeInfo(writer, dataContractName + ":" + TruncateDefaultDataContractNamespace(dataContractNamespace));
			}
		}
	}

	internal static string TruncateDefaultDataContractNamespace(string dataContractNamespace)
	{
		if (!string.IsNullOrEmpty(dataContractNamespace))
		{
			if (dataContractNamespace[0] == '#')
			{
				return "\\" + dataContractNamespace;
			}
			if (dataContractNamespace[0] == '\\')
			{
				return "\\" + dataContractNamespace;
			}
			if (dataContractNamespace.StartsWith("http://schemas.datacontract.org/2004/07/", StringComparison.Ordinal))
			{
				return "#" + dataContractNamespace.AsSpan(JsonGlobals.DataContractXsdBaseNamespaceLength);
			}
		}
		return dataContractNamespace;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override bool WriteTypeInfo(XmlWriterDelegator writer, DataContract contract, DataContract declaredContract)
	{
		if ((contract.Name != declaredContract.Name || contract.Namespace != declaredContract.Namespace) && (!(contract.Name.Value == declaredContract.Name.Value) || !(contract.Namespace.Value == declaredContract.Namespace.Value)) && contract.UnderlyingType != Globals.TypeOfObjectArray && _emitXsiType != EmitTypeInformation.Never)
		{
			if (RequiresJsonTypeInfo(contract))
			{
				_perCallXsiTypeAlreadyEmitted = true;
				WriteTypeInfo(writer, contract.Name.Value, contract.Namespace.Value);
			}
			else if (declaredContract.UnderlyingType == typeof(Enum))
			{
				throw new SerializationException(System.SR.Format(System.SR.EnumTypeNotSupportedByDataContractJsonSerializer, declaredContract.UnderlyingType));
			}
			return true;
		}
		return false;
	}

	private static bool RequiresJsonTypeInfo(DataContract contract)
	{
		return contract is ClassDataContract;
	}

	private void WriteTypeInfo(XmlWriterDelegator writer, string typeInformation)
	{
		writer.WriteAttributeString(null, "__type", null, typeInformation);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override void WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle declaredTypeHandle)
	{
		JsonDataContract jsonDataContract = JsonDataContract.GetJsonDataContract(dataContract);
		if (_emitXsiType == EmitTypeInformation.Always && !_perCallXsiTypeAlreadyEmitted && RequiresJsonTypeInfo(dataContract))
		{
			WriteTypeInfo(xmlWriter, jsonDataContract.TypeName);
		}
		_perCallXsiTypeAlreadyEmitted = false;
		DataContractJsonSerializer.WriteJsonValue(jsonDataContract, xmlWriter, obj, this, declaredTypeHandle);
	}

	protected override void WriteNull(XmlWriterDelegator xmlWriter)
	{
		DataContractJsonSerializer.WriteJsonNull(xmlWriter);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override void SerializeWithXsiType(XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle objectTypeHandle, Type objectType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
	{
		bool verifyKnownType = false;
		bool isInterface = declaredType.IsInterface;
		DataContract dataContract;
		if (isInterface && CollectionDataContract.IsCollectionInterface(declaredType))
		{
			dataContract = GetDataContract(declaredTypeHandle, declaredType);
		}
		else if (declaredType.IsArray)
		{
			dataContract = GetDataContract(declaredTypeHandle, declaredType);
		}
		else
		{
			dataContract = GetDataContract(objectTypeHandle, objectType);
			DataContract declaredContract = ((declaredTypeID >= 0) ? GetDataContract(declaredTypeID, declaredTypeHandle) : GetDataContract(declaredTypeHandle, declaredType));
			verifyKnownType = WriteTypeInfo(xmlWriter, dataContract, declaredContract);
			HandleCollectionAssignedToObject(declaredType, ref dataContract, ref obj, ref verifyKnownType);
		}
		if (isInterface)
		{
			VerifyObjectCompatibilityWithInterface(dataContract, obj, declaredType);
		}
		SerializeAndVerifyType(dataContract, xmlWriter, obj, verifyKnownType, declaredType.TypeHandle, declaredType);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void HandleCollectionAssignedToObject(Type declaredType, ref DataContract dataContract, ref object obj, ref bool verifyKnownType)
	{
		if (!(declaredType != dataContract.UnderlyingType) || !(dataContract is CollectionDataContract))
		{
			return;
		}
		if (verifyKnownType)
		{
			VerifyType(dataContract, declaredType);
			verifyKnownType = false;
		}
		if (((CollectionDataContract)dataContract).Kind == CollectionKind.Dictionary)
		{
			IDictionary dictionary = obj as IDictionary;
			Dictionary<object, object> dictionary2 = new Dictionary<object, object>(dictionary.Count);
			foreach (DictionaryEntry item in dictionary)
			{
				dictionary2.Add(item.Key, item.Value);
			}
			obj = dictionary2;
		}
		dataContract = GetDataContract(Globals.TypeOfIEnumerable);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void SerializeWithXsiTypeAtTopLevel(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle originalDeclaredTypeHandle, Type graphType)
	{
		bool verifyKnownType = false;
		Type underlyingType = rootTypeDataContract.UnderlyingType;
		bool isInterface = underlyingType.IsInterface;
		if ((!isInterface || !CollectionDataContract.IsCollectionInterface(underlyingType)) && !underlyingType.IsArray)
		{
			verifyKnownType = WriteTypeInfo(xmlWriter, dataContract, rootTypeDataContract);
			HandleCollectionAssignedToObject(underlyingType, ref dataContract, ref obj, ref verifyKnownType);
		}
		if (isInterface)
		{
			VerifyObjectCompatibilityWithInterface(dataContract, obj, underlyingType);
		}
		SerializeAndVerifyType(dataContract, xmlWriter, obj, verifyKnownType, underlyingType.TypeHandle, underlyingType);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void VerifyType(DataContract dataContract, Type declaredType)
	{
		bool flag = false;
		if (dataContract.KnownDataContracts != null)
		{
			scopedKnownTypes.Push(dataContract.KnownDataContracts);
			flag = true;
		}
		if (!IsKnownType(dataContract, declaredType))
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.DcTypeNotFoundOnSerialize, DataContract.GetClrTypeFullName(dataContract.UnderlyingType), dataContract.StableName.Name, dataContract.StableName.Namespace));
		}
		if (flag)
		{
			scopedKnownTypes.Pop();
		}
	}

	internal static void WriteJsonNameWithMapping(XmlWriterDelegator xmlWriter, XmlDictionaryString[] memberNames, int index)
	{
		xmlWriter.WriteStartElement("a", "item", "item");
		xmlWriter.WriteAttributeString(null, "item", null, memberNames[index].Value);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteExtensionDataTypeInfo(XmlWriterDelegator xmlWriter, IDataNode dataNode)
	{
		Type dataType = dataNode.DataType;
		if (dataType == Globals.TypeOfClassDataNode || dataType == Globals.TypeOfISerializableDataNode)
		{
			xmlWriter.WriteAttributeString(null, "type", null, "object");
			base.WriteExtensionDataTypeInfo(xmlWriter, dataNode);
		}
		else if (dataType == Globals.TypeOfCollectionDataNode)
		{
			xmlWriter.WriteAttributeString(null, "type", null, "array");
		}
		else if (!(dataType == Globals.TypeOfXmlDataNode) && dataType == Globals.TypeOfObject && dataNode.Value != null)
		{
			DataContract dataContract = GetDataContract(dataNode.Value.GetType());
			if (RequiresJsonTypeInfo(dataContract))
			{
				base.WriteExtensionDataTypeInfo(xmlWriter, dataNode);
			}
		}
	}

	internal static void VerifyObjectCompatibilityWithInterface(DataContract contract, object graph, Type declaredType)
	{
		Type type = contract.GetType();
		if (type == typeof(XmlDataContract) && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(declaredType))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.XmlObjectAssignedToIncompatibleInterface, graph.GetType(), declaredType)));
		}
		if (type == typeof(CollectionDataContract) && !CollectionDataContract.IsCollectionInterface(declaredType))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.CollectionAssignedToIncompatibleInterface, graph.GetType(), declaredType)));
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteJsonISerializable(XmlWriterDelegator xmlWriter, ISerializable obj)
	{
		Type type = obj.GetType();
		SerializationInfo serializationInfo = new SerializationInfo(type, XmlObjectSerializer.FormatterConverter);
		GetObjectData(obj, serializationInfo, GetStreamingContext());
		if (DataContract.GetClrTypeFullName(type) != serializationInfo.FullTypeName)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ChangingFullTypeNameNotSupported, serializationInfo.FullTypeName, DataContract.GetClrTypeFullName(type))));
		}
		WriteSerializationInfo(xmlWriter, type, serializationInfo);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	[return: NotNullIfNotNull("oldItemContract")]
	internal static DataContract GetRevisedItemContract(DataContract oldItemContract)
	{
		if (oldItemContract != null && oldItemContract.UnderlyingType.IsGenericType && oldItemContract.UnderlyingType.GetGenericTypeDefinition() == Globals.TypeOfKeyValue)
		{
			return DataContract.GetDataContract(oldItemContract.UnderlyingType);
		}
		return oldItemContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type)
	{
		DataContract dataContract = base.GetDataContract(typeHandle, type);
		DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
		return dataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type type)
	{
		DataContract dataContractSkipValidation = base.GetDataContractSkipValidation(typeId, typeHandle, type);
		DataContractJsonSerializer.CheckIfTypeIsReference(dataContractSkipValidation);
		return dataContractSkipValidation;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
	{
		DataContract dataContract = base.GetDataContract(id, typeHandle);
		DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
		return dataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override DataContract ResolveDataContractFromRootDataContract(XmlQualifiedName typeQName)
	{
		return ResolveJsonDataContractFromRootDataContract(this, typeQName, rootTypeDataContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract ResolveJsonDataContractFromRootDataContract(XmlObjectSerializerContext context, XmlQualifiedName typeQName, DataContract rootTypeDataContract)
	{
		if (rootTypeDataContract.StableName == typeQName)
		{
			return rootTypeDataContract;
		}
		CollectionDataContract collectionDataContract = rootTypeDataContract as CollectionDataContract;
		while (collectionDataContract != null)
		{
			DataContract dataContract = ((!collectionDataContract.ItemType.IsGenericType || !(collectionDataContract.ItemType.GetGenericTypeDefinition() == typeof(KeyValue<, >))) ? context.GetDataContract(context.GetSurrogatedType(collectionDataContract.ItemType)) : context.GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GetGenericArguments())));
			if (dataContract.StableName == typeQName)
			{
				return dataContract;
			}
			collectionDataContract = dataContract as CollectionDataContract;
		}
		return null;
	}
}
