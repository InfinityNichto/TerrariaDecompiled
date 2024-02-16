using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;

namespace System.Runtime.Serialization;

internal static class XmlFormatGeneratorStatics
{
	private static MethodInfo s_writeStartElementMethod2;

	private static MethodInfo s_writeStartElementMethod3;

	private static MethodInfo s_writeEndElementMethod;

	private static MethodInfo s_writeNamespaceDeclMethod;

	private static PropertyInfo s_extensionDataProperty;

	private static ConstructorInfo s_dictionaryEnumeratorCtor;

	private static MethodInfo s_ienumeratorMoveNextMethod;

	private static MethodInfo s_ienumeratorGetCurrentMethod;

	private static MethodInfo s_getItemContractMethod;

	private static MethodInfo s_isStartElementMethod2;

	private static MethodInfo s_isStartElementMethod0;

	private static MethodInfo s_getUninitializedObjectMethod;

	private static MethodInfo s_onDeserializationMethod;

	private static PropertyInfo s_nodeTypeProperty;

	private static ConstructorInfo s_extensionDataObjectCtor;

	private static ConstructorInfo s_hashtableCtor;

	private static MethodInfo s_getStreamingContextMethod;

	private static MethodInfo s_getCollectionMemberMethod;

	private static MethodInfo s_storeCollectionMemberInfoMethod;

	private static MethodInfo s_resetCollectionMemberInfoMethod;

	private static MethodInfo s_storeIsGetOnlyCollectionMethod;

	private static MethodInfo s_resetIsGetOnlyCollection;

	private static MethodInfo s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod;

	private static MethodInfo s_throwArrayExceededSizeExceptionMethod;

	private static MethodInfo s_incrementItemCountMethod;

	private static MethodInfo s_internalDeserializeMethod;

	private static MethodInfo s_moveToNextElementMethod;

	private static MethodInfo s_getMemberIndexMethod;

	private static MethodInfo s_getMemberIndexWithRequiredMembersMethod;

	private static MethodInfo s_throwRequiredMemberMissingExceptionMethod;

	private static MethodInfo s_skipUnknownElementMethod;

	private static MethodInfo s_readIfNullOrRefMethod;

	private static MethodInfo s_readAttributesMethod;

	private static MethodInfo s_resetAttributesMethod;

	private static MethodInfo s_getObjectIdMethod;

	private static MethodInfo s_getArraySizeMethod;

	private static MethodInfo s_addNewObjectMethod;

	private static MethodInfo s_addNewObjectWithIdMethod;

	private static MethodInfo s_getExistingObjectMethod;

	private static MethodInfo s_getRealObjectMethod;

	private static MethodInfo s_ensureArraySizeMethod;

	private static MethodInfo s_trimArraySizeMethod;

	private static MethodInfo s_checkEndOfArrayMethod;

	private static MethodInfo s_getArrayLengthMethod;

	private static MethodInfo s_createSerializationExceptionMethod;

	private static MethodInfo s_readSerializationInfoMethod;

	private static MethodInfo s_createUnexpectedStateExceptionMethod;

	private static MethodInfo s_internalSerializeReferenceMethod;

	private static MethodInfo s_internalSerializeMethod;

	private static MethodInfo s_writeNullMethod;

	private static MethodInfo s_incrementArrayCountMethod;

	private static MethodInfo s_incrementCollectionCountMethod;

	private static MethodInfo s_incrementCollectionCountGenericMethod;

	private static MethodInfo s_getDefaultValueMethod;

	private static MethodInfo s_getNullableValueMethod;

	private static MethodInfo s_throwRequiredMemberMustBeEmittedMethod;

	private static MethodInfo s_getHasValueMethod;

	private static MethodInfo s_writeISerializableMethod;

	private static MethodInfo s_isMemberTypeSameAsMemberValue;

	private static MethodInfo s_writeExtensionDataMethod;

	private static MethodInfo s_writeXmlValueMethod;

	private static MethodInfo s_readXmlValueMethod;

	private static PropertyInfo s_namespaceProperty;

	private static FieldInfo s_contractNamespacesField;

	private static FieldInfo s_memberNamesField;

	private static MethodInfo s_extensionDataSetExplicitMethodInfo;

	private static PropertyInfo s_childElementNamespacesProperty;

	private static PropertyInfo s_collectionItemNameProperty;

	private static PropertyInfo s_childElementNamespaceProperty;

	private static MethodInfo s_getDateTimeOffsetMethod;

	private static MethodInfo s_getDateTimeOffsetAdapterMethod;

	private static MethodInfo s_getMemoryStreamMethod;

	private static MethodInfo s_getMemoryStreamAdapterMethod;

	private static MethodInfo s_getTypeHandleMethod;

	private static MethodInfo s_getTypeMethod;

	private static MethodInfo s_throwInvalidDataContractExceptionMethod;

	private static PropertyInfo s_serializeReadOnlyTypesProperty;

	private static PropertyInfo s_classSerializationExceptionMessageProperty;

	private static PropertyInfo s_collectionSerializationExceptionMessageProperty;

	internal static MethodInfo WriteStartElementMethod2
	{
		get
		{
			if (s_writeStartElementMethod2 == null)
			{
				s_writeStartElementMethod2 = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
				{
					typeof(XmlDictionaryString),
					typeof(XmlDictionaryString)
				});
			}
			return s_writeStartElementMethod2;
		}
	}

	internal static MethodInfo WriteStartElementMethod3
	{
		get
		{
			if (s_writeStartElementMethod3 == null)
			{
				s_writeStartElementMethod3 = typeof(XmlWriterDelegator).GetMethod("WriteStartElement", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
				{
					typeof(string),
					typeof(XmlDictionaryString),
					typeof(XmlDictionaryString)
				});
			}
			return s_writeStartElementMethod3;
		}
	}

	internal static MethodInfo WriteEndElementMethod
	{
		get
		{
			if (s_writeEndElementMethod == null)
			{
				s_writeEndElementMethod = typeof(XmlWriterDelegator).GetMethod("WriteEndElement", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			}
			return s_writeEndElementMethod;
		}
	}

	internal static MethodInfo WriteNamespaceDeclMethod
	{
		get
		{
			if (s_writeNamespaceDeclMethod == null)
			{
				s_writeNamespaceDeclMethod = typeof(XmlWriterDelegator).GetMethod("WriteNamespaceDecl", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(XmlDictionaryString) });
			}
			return s_writeNamespaceDeclMethod;
		}
	}

	internal static PropertyInfo ExtensionDataProperty => s_extensionDataProperty ?? (s_extensionDataProperty = typeof(IExtensibleDataObject).GetProperty("ExtensionData"));

	internal static ConstructorInfo DictionaryEnumeratorCtor
	{
		get
		{
			if (s_dictionaryEnumeratorCtor == null)
			{
				s_dictionaryEnumeratorCtor = typeof(CollectionDataContract.DictionaryEnumerator).GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { Globals.TypeOfIDictionaryEnumerator });
			}
			return s_dictionaryEnumeratorCtor;
		}
	}

	internal static MethodInfo MoveNextMethod
	{
		get
		{
			if (s_ienumeratorMoveNextMethod == null)
			{
				s_ienumeratorMoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext");
			}
			return s_ienumeratorMoveNextMethod;
		}
	}

	internal static MethodInfo GetCurrentMethod
	{
		get
		{
			if (s_ienumeratorGetCurrentMethod == null)
			{
				s_ienumeratorGetCurrentMethod = typeof(IEnumerator).GetProperty("Current").GetGetMethod();
			}
			return s_ienumeratorGetCurrentMethod;
		}
	}

	internal static MethodInfo GetItemContractMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_getItemContractMethod == null)
			{
				s_getItemContractMethod = typeof(CollectionDataContract).GetProperty("ItemContract", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetMethod;
			}
			return s_getItemContractMethod;
		}
	}

	internal static MethodInfo IsStartElementMethod2
	{
		get
		{
			if (s_isStartElementMethod2 == null)
			{
				s_isStartElementMethod2 = typeof(XmlReaderDelegator).GetMethod("IsStartElement", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
				{
					typeof(XmlDictionaryString),
					typeof(XmlDictionaryString)
				});
			}
			return s_isStartElementMethod2;
		}
	}

	internal static MethodInfo IsStartElementMethod0
	{
		get
		{
			if (s_isStartElementMethod0 == null)
			{
				s_isStartElementMethod0 = typeof(XmlReaderDelegator).GetMethod("IsStartElement", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			}
			return s_isStartElementMethod0;
		}
	}

	internal static MethodInfo GetUninitializedObjectMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_getUninitializedObjectMethod == null)
			{
				s_getUninitializedObjectMethod = typeof(XmlFormatReaderGenerator).GetMethod("UnsafeGetUninitializedObject", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(int) });
			}
			return s_getUninitializedObjectMethod;
		}
	}

	internal static MethodInfo OnDeserializationMethod
	{
		get
		{
			if (s_onDeserializationMethod == null)
			{
				s_onDeserializationMethod = typeof(IDeserializationCallback).GetMethod("OnDeserialization");
			}
			return s_onDeserializationMethod;
		}
	}

	internal static PropertyInfo NodeTypeProperty
	{
		get
		{
			if (s_nodeTypeProperty == null)
			{
				s_nodeTypeProperty = typeof(XmlReaderDelegator).GetProperty("NodeType", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_nodeTypeProperty;
		}
	}

	internal static ConstructorInfo ExtensionDataObjectCtor => s_extensionDataObjectCtor ?? (s_extensionDataObjectCtor = typeof(ExtensionDataObject).GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes));

	internal static ConstructorInfo HashtableCtor
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_hashtableCtor == null)
			{
				s_hashtableCtor = Globals.TypeOfHashtable.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			}
			return s_hashtableCtor;
		}
	}

	internal static MethodInfo GetStreamingContextMethod
	{
		get
		{
			if (s_getStreamingContextMethod == null)
			{
				s_getStreamingContextMethod = typeof(XmlObjectSerializerContext).GetMethod("GetStreamingContext", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getStreamingContextMethod;
		}
	}

	internal static MethodInfo GetCollectionMemberMethod
	{
		get
		{
			if (s_getCollectionMemberMethod == null)
			{
				s_getCollectionMemberMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetCollectionMember", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getCollectionMemberMethod;
		}
	}

	internal static MethodInfo StoreCollectionMemberInfoMethod
	{
		get
		{
			if (s_storeCollectionMemberInfoMethod == null)
			{
				s_storeCollectionMemberInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("StoreCollectionMemberInfo", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(object) });
			}
			return s_storeCollectionMemberInfoMethod;
		}
	}

	internal static MethodInfo ResetCollectionMemberInfoMethod
	{
		get
		{
			if (s_resetCollectionMemberInfoMethod == null)
			{
				s_resetCollectionMemberInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ResetCollectionMemberInfo", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			}
			return s_resetCollectionMemberInfoMethod;
		}
	}

	internal static MethodInfo StoreIsGetOnlyCollectionMethod
	{
		get
		{
			if (s_storeIsGetOnlyCollectionMethod == null)
			{
				s_storeIsGetOnlyCollectionMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("StoreIsGetOnlyCollection", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_storeIsGetOnlyCollectionMethod;
		}
	}

	internal static MethodInfo ResetIsGetOnlyCollectionMethod
	{
		get
		{
			if (s_resetIsGetOnlyCollection == null)
			{
				s_resetIsGetOnlyCollection = typeof(XmlObjectSerializerWriteContext).GetMethod("ResetIsGetOnlyCollection", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_resetIsGetOnlyCollection;
		}
	}

	internal static MethodInfo ThrowNullValueReturnedForGetOnlyCollectionExceptionMethod
	{
		get
		{
			if (s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod == null)
			{
				s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowNullValueReturnedForGetOnlyCollectionException", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_throwNullValueReturnedForGetOnlyCollectionExceptionMethod;
		}
	}

	internal static MethodInfo ThrowArrayExceededSizeExceptionMethod
	{
		get
		{
			if (s_throwArrayExceededSizeExceptionMethod == null)
			{
				s_throwArrayExceededSizeExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowArrayExceededSizeException", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_throwArrayExceededSizeExceptionMethod;
		}
	}

	internal static MethodInfo IncrementItemCountMethod
	{
		get
		{
			if (s_incrementItemCountMethod == null)
			{
				s_incrementItemCountMethod = typeof(XmlObjectSerializerContext).GetMethod("IncrementItemCount", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_incrementItemCountMethod;
		}
	}

	internal static MethodInfo InternalDeserializeMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_internalDeserializeMethod == null)
			{
				s_internalDeserializeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("InternalDeserialize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[5]
				{
					typeof(XmlReaderDelegator),
					typeof(int),
					typeof(RuntimeTypeHandle),
					typeof(string),
					typeof(string)
				});
			}
			return s_internalDeserializeMethod;
		}
	}

	internal static MethodInfo MoveToNextElementMethod
	{
		get
		{
			if (s_moveToNextElementMethod == null)
			{
				s_moveToNextElementMethod = typeof(XmlObjectSerializerReadContext).GetMethod("MoveToNextElement", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_moveToNextElementMethod;
		}
	}

	internal static MethodInfo GetMemberIndexMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_getMemberIndexMethod == null)
			{
				s_getMemberIndexMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetMemberIndex", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getMemberIndexMethod;
		}
	}

	internal static MethodInfo GetMemberIndexWithRequiredMembersMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_getMemberIndexWithRequiredMembersMethod == null)
			{
				s_getMemberIndexWithRequiredMembersMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetMemberIndexWithRequiredMembers", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getMemberIndexWithRequiredMembersMethod;
		}
	}

	internal static MethodInfo ThrowRequiredMemberMissingExceptionMethod
	{
		get
		{
			if (s_throwRequiredMemberMissingExceptionMethod == null)
			{
				s_throwRequiredMemberMissingExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ThrowRequiredMemberMissingException", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_throwRequiredMemberMissingExceptionMethod;
		}
	}

	internal static MethodInfo SkipUnknownElementMethod
	{
		get
		{
			if (s_skipUnknownElementMethod == null)
			{
				s_skipUnknownElementMethod = typeof(XmlObjectSerializerReadContext).GetMethod("SkipUnknownElement", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_skipUnknownElementMethod;
		}
	}

	internal static MethodInfo ReadIfNullOrRefMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_readIfNullOrRefMethod == null)
			{
				s_readIfNullOrRefMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadIfNullOrRef", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
				{
					typeof(XmlReaderDelegator),
					typeof(Type),
					typeof(bool)
				});
			}
			return s_readIfNullOrRefMethod;
		}
	}

	internal static MethodInfo ReadAttributesMethod
	{
		get
		{
			if (s_readAttributesMethod == null)
			{
				s_readAttributesMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadAttributes", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_readAttributesMethod;
		}
	}

	internal static MethodInfo ResetAttributesMethod
	{
		get
		{
			if (s_resetAttributesMethod == null)
			{
				s_resetAttributesMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ResetAttributes", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_resetAttributesMethod;
		}
	}

	internal static MethodInfo GetObjectIdMethod
	{
		get
		{
			if (s_getObjectIdMethod == null)
			{
				s_getObjectIdMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetObjectId", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getObjectIdMethod;
		}
	}

	internal static MethodInfo GetArraySizeMethod
	{
		get
		{
			if (s_getArraySizeMethod == null)
			{
				s_getArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetArraySize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getArraySizeMethod;
		}
	}

	internal static MethodInfo AddNewObjectMethod
	{
		get
		{
			if (s_addNewObjectMethod == null)
			{
				s_addNewObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("AddNewObject", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_addNewObjectMethod;
		}
	}

	internal static MethodInfo AddNewObjectWithIdMethod
	{
		get
		{
			if (s_addNewObjectWithIdMethod == null)
			{
				s_addNewObjectWithIdMethod = typeof(XmlObjectSerializerReadContext).GetMethod("AddNewObjectWithId", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_addNewObjectWithIdMethod;
		}
	}

	internal static MethodInfo GetExistingObjectMethod
	{
		get
		{
			if (s_getExistingObjectMethod == null)
			{
				s_getExistingObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetExistingObject", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getExistingObjectMethod;
		}
	}

	internal static MethodInfo GetRealObjectMethod
	{
		get
		{
			if (s_getRealObjectMethod == null)
			{
				s_getRealObjectMethod = typeof(XmlObjectSerializerReadContext).GetMethod("GetRealObject", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getRealObjectMethod;
		}
	}

	internal static MethodInfo EnsureArraySizeMethod
	{
		get
		{
			if (s_ensureArraySizeMethod == null)
			{
				s_ensureArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("EnsureArraySize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_ensureArraySizeMethod;
		}
	}

	internal static MethodInfo TrimArraySizeMethod
	{
		get
		{
			if (s_trimArraySizeMethod == null)
			{
				s_trimArraySizeMethod = typeof(XmlObjectSerializerReadContext).GetMethod("TrimArraySize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_trimArraySizeMethod;
		}
	}

	internal static MethodInfo CheckEndOfArrayMethod
	{
		get
		{
			if (s_checkEndOfArrayMethod == null)
			{
				s_checkEndOfArrayMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CheckEndOfArray", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_checkEndOfArrayMethod;
		}
	}

	internal static MethodInfo GetArrayLengthMethod
	{
		get
		{
			if (s_getArrayLengthMethod == null)
			{
				s_getArrayLengthMethod = typeof(Array).GetProperty("Length").GetMethod;
			}
			return s_getArrayLengthMethod;
		}
	}

	internal static MethodInfo CreateSerializationExceptionMethod
	{
		get
		{
			if (s_createSerializationExceptionMethod == null)
			{
				s_createSerializationExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CreateSerializationException", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
			}
			return s_createSerializationExceptionMethod;
		}
	}

	internal static MethodInfo ReadSerializationInfoMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_readSerializationInfoMethod == null)
			{
				s_readSerializationInfoMethod = typeof(XmlObjectSerializerReadContext).GetMethod("ReadSerializationInfo", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_readSerializationInfoMethod;
		}
	}

	internal static MethodInfo CreateUnexpectedStateExceptionMethod
	{
		get
		{
			if (s_createUnexpectedStateExceptionMethod == null)
			{
				s_createUnexpectedStateExceptionMethod = typeof(XmlObjectSerializerReadContext).GetMethod("CreateUnexpectedStateException", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
				{
					typeof(XmlNodeType),
					typeof(XmlReaderDelegator)
				});
			}
			return s_createUnexpectedStateExceptionMethod;
		}
	}

	internal static MethodInfo InternalSerializeReferenceMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_internalSerializeReferenceMethod == null)
			{
				s_internalSerializeReferenceMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("InternalSerializeReference", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_internalSerializeReferenceMethod;
		}
	}

	internal static MethodInfo InternalSerializeMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_internalSerializeMethod == null)
			{
				s_internalSerializeMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("InternalSerialize", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_internalSerializeMethod;
		}
	}

	internal static MethodInfo WriteNullMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_writeNullMethod == null)
			{
				s_writeNullMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteNull", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
				{
					typeof(XmlWriterDelegator),
					typeof(Type),
					typeof(bool)
				});
			}
			return s_writeNullMethod;
		}
	}

	internal static MethodInfo IncrementArrayCountMethod
	{
		get
		{
			if (s_incrementArrayCountMethod == null)
			{
				s_incrementArrayCountMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementArrayCount", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_incrementArrayCountMethod;
		}
	}

	internal static MethodInfo IncrementCollectionCountMethod
	{
		get
		{
			if (s_incrementCollectionCountMethod == null)
			{
				s_incrementCollectionCountMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementCollectionCount", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
				{
					typeof(XmlWriterDelegator),
					typeof(ICollection)
				});
			}
			return s_incrementCollectionCountMethod;
		}
	}

	internal static MethodInfo IncrementCollectionCountGenericMethod
	{
		get
		{
			if (s_incrementCollectionCountGenericMethod == null)
			{
				s_incrementCollectionCountGenericMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("IncrementCollectionCountGeneric", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_incrementCollectionCountGenericMethod;
		}
	}

	internal static MethodInfo GetDefaultValueMethod
	{
		get
		{
			if (s_getDefaultValueMethod == null)
			{
				s_getDefaultValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("GetDefaultValue", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getDefaultValueMethod;
		}
	}

	internal static MethodInfo GetNullableValueMethod
	{
		get
		{
			if (s_getNullableValueMethod == null)
			{
				s_getNullableValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("GetNullableValue", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getNullableValueMethod;
		}
	}

	internal static MethodInfo ThrowRequiredMemberMustBeEmittedMethod
	{
		get
		{
			if (s_throwRequiredMemberMustBeEmittedMethod == null)
			{
				s_throwRequiredMemberMustBeEmittedMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("ThrowRequiredMemberMustBeEmitted", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_throwRequiredMemberMustBeEmittedMethod;
		}
	}

	internal static MethodInfo GetHasValueMethod
	{
		get
		{
			if (s_getHasValueMethod == null)
			{
				s_getHasValueMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("GetHasValue", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getHasValueMethod;
		}
	}

	internal static MethodInfo WriteISerializableMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_writeISerializableMethod == null)
			{
				s_writeISerializableMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteISerializable", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_writeISerializableMethod;
		}
	}

	internal static MethodInfo IsMemberTypeSameAsMemberValue
	{
		get
		{
			if (s_isMemberTypeSameAsMemberValue == null)
			{
				s_isMemberTypeSameAsMemberValue = typeof(XmlObjectSerializerWriteContext).GetMethod("IsMemberTypeSameAsMemberValue", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
				{
					typeof(object),
					typeof(Type)
				});
			}
			return s_isMemberTypeSameAsMemberValue;
		}
	}

	internal static MethodInfo WriteExtensionDataMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_writeExtensionDataMethod == null)
			{
				s_writeExtensionDataMethod = typeof(XmlObjectSerializerWriteContext).GetMethod("WriteExtensionData", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_writeExtensionDataMethod;
		}
	}

	internal static MethodInfo WriteXmlValueMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_writeXmlValueMethod == null)
			{
				s_writeXmlValueMethod = typeof(DataContract).GetMethod("WriteXmlValue", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_writeXmlValueMethod;
		}
	}

	internal static MethodInfo ReadXmlValueMethod
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_readXmlValueMethod == null)
			{
				s_readXmlValueMethod = typeof(DataContract).GetMethod("ReadXmlValue", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_readXmlValueMethod;
		}
	}

	internal static PropertyInfo NamespaceProperty
	{
		get
		{
			if (s_namespaceProperty == null)
			{
				s_namespaceProperty = typeof(DataContract).GetProperty("Namespace", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_namespaceProperty;
		}
	}

	internal static FieldInfo ContractNamespacesField
	{
		get
		{
			if (s_contractNamespacesField == null)
			{
				s_contractNamespacesField = typeof(ClassDataContract).GetField("ContractNamespaces", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_contractNamespacesField;
		}
	}

	internal static FieldInfo MemberNamesField
	{
		get
		{
			if (s_memberNamesField == null)
			{
				s_memberNamesField = typeof(ClassDataContract).GetField("MemberNames", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_memberNamesField;
		}
	}

	internal static MethodInfo ExtensionDataSetExplicitMethodInfo => s_extensionDataSetExplicitMethodInfo ?? (s_extensionDataSetExplicitMethodInfo = typeof(IExtensibleDataObject).GetMethod("set_ExtensionData"));

	internal static PropertyInfo ChildElementNamespacesProperty
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_childElementNamespacesProperty == null)
			{
				s_childElementNamespacesProperty = typeof(ClassDataContract).GetProperty("ChildElementNamespaces", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_childElementNamespacesProperty;
		}
	}

	internal static PropertyInfo CollectionItemNameProperty
	{
		get
		{
			if (s_collectionItemNameProperty == null)
			{
				s_collectionItemNameProperty = typeof(CollectionDataContract).GetProperty("CollectionItemName", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_collectionItemNameProperty;
		}
	}

	internal static PropertyInfo ChildElementNamespaceProperty
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (s_childElementNamespaceProperty == null)
			{
				s_childElementNamespaceProperty = typeof(CollectionDataContract).GetProperty("ChildElementNamespace", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_childElementNamespaceProperty;
		}
	}

	internal static MethodInfo GetDateTimeOffsetMethod
	{
		get
		{
			if (s_getDateTimeOffsetMethod == null)
			{
				s_getDateTimeOffsetMethod = typeof(DateTimeOffsetAdapter).GetMethod("GetDateTimeOffset", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getDateTimeOffsetMethod;
		}
	}

	internal static MethodInfo GetDateTimeOffsetAdapterMethod
	{
		get
		{
			if (s_getDateTimeOffsetAdapterMethod == null)
			{
				s_getDateTimeOffsetAdapterMethod = typeof(DateTimeOffsetAdapter).GetMethod("GetDateTimeOffsetAdapter", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getDateTimeOffsetAdapterMethod;
		}
	}

	internal static MethodInfo GetMemoryStreamMethod
	{
		get
		{
			if (s_getMemoryStreamMethod == null)
			{
				s_getMemoryStreamMethod = typeof(MemoryStreamAdapter).GetMethod("GetMemoryStream", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getMemoryStreamMethod;
		}
	}

	internal static MethodInfo GetMemoryStreamAdapterMethod
	{
		get
		{
			if (s_getMemoryStreamAdapterMethod == null)
			{
				s_getMemoryStreamAdapterMethod = typeof(MemoryStreamAdapter).GetMethod("GetMemoryStreamAdapter", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_getMemoryStreamAdapterMethod;
		}
	}

	internal static MethodInfo GetTypeHandleMethod
	{
		get
		{
			if (s_getTypeHandleMethod == null)
			{
				s_getTypeHandleMethod = typeof(Type).GetMethod("get_TypeHandle");
			}
			return s_getTypeHandleMethod;
		}
	}

	internal static MethodInfo GetTypeMethod
	{
		get
		{
			if (s_getTypeMethod == null)
			{
				s_getTypeMethod = typeof(object).GetMethod("GetType");
			}
			return s_getTypeMethod;
		}
	}

	internal static MethodInfo ThrowInvalidDataContractExceptionMethod
	{
		get
		{
			if (s_throwInvalidDataContractExceptionMethod == null)
			{
				s_throwInvalidDataContractExceptionMethod = typeof(DataContract).GetMethod("ThrowInvalidDataContractException", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
				{
					typeof(string),
					typeof(Type)
				});
			}
			return s_throwInvalidDataContractExceptionMethod;
		}
	}

	internal static PropertyInfo SerializeReadOnlyTypesProperty
	{
		get
		{
			if (s_serializeReadOnlyTypesProperty == null)
			{
				s_serializeReadOnlyTypesProperty = typeof(XmlObjectSerializerWriteContext).GetProperty("SerializeReadOnlyTypes", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_serializeReadOnlyTypesProperty;
		}
	}

	internal static PropertyInfo ClassSerializationExceptionMessageProperty
	{
		get
		{
			if (s_classSerializationExceptionMessageProperty == null)
			{
				s_classSerializationExceptionMessageProperty = typeof(ClassDataContract).GetProperty("SerializationExceptionMessage", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_classSerializationExceptionMessageProperty;
		}
	}

	internal static PropertyInfo CollectionSerializationExceptionMessageProperty
	{
		get
		{
			if (s_collectionSerializationExceptionMessageProperty == null)
			{
				s_collectionSerializationExceptionMessageProperty = typeof(CollectionDataContract).GetProperty("SerializationExceptionMessage", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return s_collectionSerializationExceptionMessageProperty;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The call to MakeGenericMethod is safe due to the fact that XmlObjectSerializerWriteContext.GetDefaultValue is not annotated.")]
	internal static object GetDefaultValue(Type type)
	{
		return GetDefaultValueMethod.MakeGenericMethod(type).Invoke(null, Array.Empty<object>());
	}
}
