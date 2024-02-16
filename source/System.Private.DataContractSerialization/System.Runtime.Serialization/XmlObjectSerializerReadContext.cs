using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal class XmlObjectSerializerReadContext : XmlObjectSerializerContext
{
	internal Attributes attributes;

	private HybridObjectCache _deserializedObjects;

	private XmlSerializableReader _xmlSerializableReader;

	private XmlDocument _xmlDocument;

	private Attributes _attributesInXmlData;

	private object _getOnlyCollectionValue;

	private bool _isGetOnlyCollection;

	private HybridObjectCache DeserializedObjects
	{
		get
		{
			if (_deserializedObjects == null)
			{
				_deserializedObjects = new HybridObjectCache();
			}
			return _deserializedObjects;
		}
	}

	private XmlDocument Document => _xmlDocument ?? (_xmlDocument = new XmlDocument());

	internal override bool IsGetOnlyCollection
	{
		get
		{
			return _isGetOnlyCollection;
		}
		set
		{
			_isGetOnlyCollection = value;
		}
	}

	internal object GetCollectionMember()
	{
		return _getOnlyCollectionValue;
	}

	internal void StoreCollectionMemberInfo(object collectionMember)
	{
		_getOnlyCollectionValue = collectionMember;
		_isGetOnlyCollection = true;
	}

	internal void ResetCollectionMemberInfo()
	{
		_getOnlyCollectionValue = null;
		_isGetOnlyCollection = false;
	}

	[DoesNotReturn]
	internal static void ThrowNullValueReturnedForGetOnlyCollectionException(Type type)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.NullValueReturnedForGetOnlyCollection, DataContract.GetClrTypeFullName(type))));
	}

	[DoesNotReturn]
	internal static void ThrowArrayExceededSizeException(int arraySize, Type type)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ArrayExceededSize, arraySize, DataContract.GetClrTypeFullName(type))));
	}

	internal static XmlObjectSerializerReadContext CreateContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
	{
		if (!serializer.PreserveObjectReferences && serializer.SerializationSurrogateProvider == null)
		{
			return new XmlObjectSerializerReadContext(serializer, rootTypeDataContract, dataContractResolver);
		}
		return new XmlObjectSerializerReadContextComplex(serializer, rootTypeDataContract, dataContractResolver);
	}

	internal XmlObjectSerializerReadContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
		: base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
	{
	}

	internal XmlObjectSerializerReadContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
		: base(serializer, rootTypeDataContract, dataContractResolver)
	{
		attributes = new Attributes();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object InternalDeserialize(XmlReaderDelegator xmlReader, int id, RuntimeTypeHandle declaredTypeHandle, string name, string ns)
	{
		DataContract dataContract = GetDataContract(id, declaredTypeHandle);
		return InternalDeserialize(xmlReader, name, ns, Type.GetTypeFromHandle(declaredTypeHandle), ref dataContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, string name, string ns)
	{
		DataContract dataContract = GetDataContract(declaredType);
		return InternalDeserialize(xmlReader, name, ns, declaredType, ref dataContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, DataContract dataContract, string name, string ns)
	{
		if (dataContract == null)
		{
			dataContract = GetDataContract(declaredType);
		}
		return InternalDeserialize(xmlReader, name, ns, declaredType, ref dataContract);
	}

	protected bool TryHandleNullOrRef(XmlReaderDelegator reader, Type declaredType, string name, string ns, ref object retObj)
	{
		ReadAttributes(reader);
		if (attributes.Ref != Globals.NewObjectId)
		{
			if (_isGetOnlyCollection)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ErrorDeserializing, System.SR.Format(System.SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(declaredType)), System.SR.Format(System.SR.XmlStartElementExpected, "Ref"))));
			}
			retObj = GetExistingObject(attributes.Ref, declaredType, name, ns);
			reader.Skip();
			return true;
		}
		if (attributes.XsiNil)
		{
			reader.Skip();
			return true;
		}
		return false;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected object InternalDeserialize(XmlReaderDelegator reader, string name, string ns, Type declaredType, ref DataContract dataContract)
	{
		object retObj = null;
		if (TryHandleNullOrRef(reader, dataContract.UnderlyingType, name, ns, ref retObj))
		{
			return retObj;
		}
		bool flag = false;
		if (dataContract.KnownDataContracts != null)
		{
			scopedKnownTypes.Push(dataContract.KnownDataContracts);
			flag = true;
		}
		if (attributes.XsiTypeName != null)
		{
			DataContract dataContract2 = ResolveDataContractFromKnownTypes(attributes.XsiTypeName, attributes.XsiTypeNamespace, dataContract, declaredType);
			if (dataContract2 == null)
			{
				if (base.DataContractResolver == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(reader, System.SR.Format(System.SR.DcTypeNotFoundOnDeserialize, attributes.XsiTypeNamespace, attributes.XsiTypeName, reader.NamespaceURI, reader.LocalName))));
				}
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(reader, System.SR.Format(System.SR.DcTypeNotResolvedOnDeserialize, attributes.XsiTypeNamespace, attributes.XsiTypeName, reader.NamespaceURI, reader.LocalName))));
			}
			dataContract = dataContract2;
			flag = ReplaceScopedKnownTypesTop(dataContract.KnownDataContracts, flag);
		}
		if (dataContract.IsISerializable && attributes.FactoryTypeName != null)
		{
			DataContract dataContract3 = ResolveDataContractFromKnownTypes(attributes.FactoryTypeName, attributes.FactoryTypeNamespace, dataContract, declaredType);
			if (dataContract3 != null)
			{
				if (!dataContract3.IsISerializable)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.FactoryTypeNotISerializable, DataContract.GetClrTypeFullName(dataContract3.UnderlyingType), DataContract.GetClrTypeFullName(dataContract.UnderlyingType))));
				}
				dataContract = dataContract3;
				flag = ReplaceScopedKnownTypesTop(dataContract.KnownDataContracts, flag);
			}
		}
		if (flag)
		{
			object result = ReadDataContractValue(dataContract, reader);
			scopedKnownTypes.Pop();
			return result;
		}
		return ReadDataContractValue(dataContract, reader);
	}

	private bool ReplaceScopedKnownTypesTop(Dictionary<XmlQualifiedName, DataContract> knownDataContracts, bool knownTypesAddedInCurrentScope)
	{
		if (knownTypesAddedInCurrentScope)
		{
			scopedKnownTypes.Pop();
			knownTypesAddedInCurrentScope = false;
		}
		if (knownDataContracts != null)
		{
			scopedKnownTypes.Push(knownDataContracts);
			knownTypesAddedInCurrentScope = true;
		}
		return knownTypesAddedInCurrentScope;
	}

	internal static bool MoveToNextElement(XmlReaderDelegator xmlReader)
	{
		return xmlReader.MoveToContent() != XmlNodeType.EndElement;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal int GetMemberIndex(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, int memberIndex, ExtensionDataObject extensionData)
	{
		for (int i = memberIndex + 1; i < memberNames.Length; i++)
		{
			if (xmlReader.IsStartElement(memberNames[i], memberNamespaces[i]))
			{
				return i;
			}
		}
		HandleMemberNotFound(xmlReader, extensionData, memberIndex);
		return memberNames.Length;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal int GetMemberIndexWithRequiredMembers(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, int memberIndex, int requiredIndex, ExtensionDataObject extensionData)
	{
		for (int i = memberIndex + 1; i < memberNames.Length; i++)
		{
			if (xmlReader.IsStartElement(memberNames[i], memberNamespaces[i]))
			{
				if (requiredIndex < i)
				{
					ThrowRequiredMemberMissingException(xmlReader, memberIndex, requiredIndex, memberNames);
				}
				return i;
			}
		}
		HandleMemberNotFound(xmlReader, extensionData, memberIndex);
		return memberNames.Length;
	}

	[DoesNotReturn]
	internal static void ThrowRequiredMemberMissingException(XmlReaderDelegator xmlReader, int memberIndex, int requiredIndex, XmlDictionaryString[] memberNames)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (requiredIndex == memberNames.Length)
		{
			requiredIndex--;
		}
		for (int i = memberIndex + 1; i <= requiredIndex; i++)
		{
			if (stringBuilder.Length != 0)
			{
				stringBuilder.Append(" | ");
			}
			stringBuilder.Append(memberNames[i].Value);
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, System.SR.Format(System.SR.UnexpectedElementExpectingElements, xmlReader.NodeType, xmlReader.LocalName, xmlReader.NamespaceURI, stringBuilder.ToString()))));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected void HandleMemberNotFound(XmlReaderDelegator xmlReader, ExtensionDataObject extensionData, int memberIndex)
	{
		xmlReader.MoveToContent();
		if (xmlReader.NodeType != XmlNodeType.Element)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
		}
		if (base.IgnoreExtensionDataObject || extensionData == null)
		{
			SkipUnknownElement(xmlReader);
		}
		else
		{
			HandleUnknownElement(xmlReader, extensionData, memberIndex);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void HandleUnknownElement(XmlReaderDelegator xmlReader, ExtensionDataObject extensionData, int memberIndex)
	{
		if (extensionData.Members == null)
		{
			extensionData.Members = new List<ExtensionDataMember>();
		}
		extensionData.Members.Add(ReadExtensionDataMember(xmlReader, memberIndex));
	}

	internal void SkipUnknownElement(XmlReaderDelegator xmlReader)
	{
		ReadAttributes(xmlReader);
		xmlReader.Skip();
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal string ReadIfNullOrRef(XmlReaderDelegator xmlReader, Type memberType, bool isMemberTypeSerializable)
	{
		if (attributes.Ref != Globals.NewObjectId)
		{
			CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
			xmlReader.Skip();
			return attributes.Ref;
		}
		if (attributes.XsiNil)
		{
			CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
			xmlReader.Skip();
			return null;
		}
		return Globals.NewObjectId;
	}

	[MemberNotNull("attributes")]
	internal virtual void ReadAttributes(XmlReaderDelegator xmlReader)
	{
		if (attributes == null)
		{
			attributes = new Attributes();
		}
		attributes.Read(xmlReader);
	}

	internal void ResetAttributes()
	{
		if (attributes != null)
		{
			attributes.Reset();
		}
	}

	internal string GetObjectId()
	{
		return attributes.Id;
	}

	internal virtual int GetArraySize()
	{
		return -1;
	}

	internal void AddNewObject(object obj)
	{
		AddNewObjectWithId(attributes.Id, obj);
	}

	internal void AddNewObjectWithId(string id, object obj)
	{
		if (id != Globals.NewObjectId)
		{
			DeserializedObjects.Add(id, obj);
		}
	}

	public void ReplaceDeserializedObject(string id, object oldObj, object newObj)
	{
		if (oldObj != newObj && id != Globals.NewObjectId)
		{
			if (DeserializedObjects.IsObjectReferenced(id))
			{
				string p = ((oldObj != null) ? DataContract.GetClrTypeFullName(oldObj.GetType()) : System.SR.UnknownNullType);
				string p2 = ((newObj != null) ? DataContract.GetClrTypeFullName(newObj.GetType()) : System.SR.UnknownNullType);
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.FactoryObjectContainsSelfReference, p, p2, id)));
			}
			DeserializedObjects.Remove(id);
			DeserializedObjects.Add(id, newObj);
		}
	}

	internal object GetExistingObject(string id, Type type, string name, string ns)
	{
		object @object = DeserializedObjects.GetObject(id);
		if (@object == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.DeserializedObjectWithIdNotFound, id)));
		}
		return @object;
	}

	private object GetExistingObjectOrExtensionData(string id)
	{
		object @object = DeserializedObjects.GetObject(id);
		if (@object == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.DeserializedObjectWithIdNotFound, id)));
		}
		return @object;
	}

	public object GetRealObject(IObjectReference obj, string id)
	{
		object realObject = SurrogateDataContract.GetRealObject(obj, GetStreamingContext());
		if (realObject == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException("error"));
		}
		ReplaceDeserializedObject(id, obj, realObject);
		return realObject;
	}

	internal static void Read(XmlReaderDelegator xmlReader)
	{
		if (!xmlReader.Read())
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.UnexpectedEndOfFile));
		}
	}

	internal static void ParseQualifiedName(string qname, XmlReaderDelegator xmlReader, out string name, out string ns, out string prefix)
	{
		int num = qname.IndexOf(':');
		prefix = "";
		if (num >= 0)
		{
			prefix = qname.Substring(0, num);
		}
		name = qname.Substring(num + 1);
		ns = xmlReader.LookupNamespace(prefix);
	}

	internal static T[] EnsureArraySize<T>(T[] array, int index)
	{
		if (array.Length <= index)
		{
			if (index == int.MaxValue)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.MaxArrayLengthExceeded, int.MaxValue, DataContract.GetClrTypeFullName(typeof(T)))));
			}
			int num = ((index < 1073741823) ? (index * 2) : int.MaxValue);
			T[] array2 = new T[num];
			Array.Copy(array, array2, array.Length);
			array = array2;
		}
		return array;
	}

	internal static T[] TrimArraySize<T>(T[] array, int size)
	{
		if (size != array.Length)
		{
			T[] array2 = new T[size];
			Array.Copy(array, array2, size);
			array = array2;
		}
		return array;
	}

	internal void CheckEndOfArray(XmlReaderDelegator xmlReader, int arraySize, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		if (xmlReader.NodeType == XmlNodeType.EndElement)
		{
			return;
		}
		while (xmlReader.IsStartElement())
		{
			if (xmlReader.IsStartElement(itemName, itemNamespace))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ArrayExceededSizeAttribute, arraySize, itemName.Value, itemNamespace.Value)));
			}
			SkipUnknownElement(xmlReader);
		}
		if (xmlReader.NodeType == XmlNodeType.EndElement)
		{
			return;
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.EndElement, xmlReader));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal object ReadIXmlSerializable(XmlReaderDelegator xmlReader, XmlDataContract xmlDataContract, bool isMemberType)
	{
		if (_xmlSerializableReader == null)
		{
			_xmlSerializableReader = new XmlSerializableReader();
		}
		return ReadIXmlSerializable(_xmlSerializableReader, xmlReader, xmlDataContract, isMemberType);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static object ReadRootIXmlSerializable(XmlReaderDelegator xmlReader, XmlDataContract xmlDataContract, bool isMemberType)
	{
		return ReadIXmlSerializable(new XmlSerializableReader(), xmlReader, xmlDataContract, isMemberType);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static object ReadIXmlSerializable(XmlSerializableReader xmlSerializableReader, XmlReaderDelegator xmlReader, XmlDataContract xmlDataContract, bool isMemberType)
	{
		object obj = null;
		xmlSerializableReader.BeginRead(xmlReader);
		if (isMemberType && !xmlDataContract.HasRoot)
		{
			xmlReader.Read();
			xmlReader.MoveToContent();
		}
		if (xmlDataContract.UnderlyingType == Globals.TypeOfXmlElement)
		{
			if (!xmlReader.IsStartElement())
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
			}
			XmlDocument xmlDocument = new XmlDocument();
			obj = (XmlElement)xmlDocument.ReadNode(xmlSerializableReader);
		}
		else if (xmlDataContract.UnderlyingType == Globals.TypeOfXmlNodeArray)
		{
			obj = XmlSerializableServices.ReadNodes(xmlSerializableReader);
		}
		else
		{
			IXmlSerializable xmlSerializable = xmlDataContract.CreateXmlSerializableDelegate();
			xmlSerializable.ReadXml(xmlSerializableReader);
			obj = xmlSerializable;
		}
		xmlSerializableReader.EndRead();
		return obj;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public SerializationInfo ReadSerializationInfo(XmlReaderDelegator xmlReader, Type type)
	{
		SerializationInfo serializationInfo = new SerializationInfo(type, XmlObjectSerializer.FormatterConverter);
		XmlNodeType xmlNodeType;
		while ((xmlNodeType = xmlReader.MoveToContent()) != XmlNodeType.EndElement)
		{
			if (xmlNodeType != XmlNodeType.Element)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
			}
			if (xmlReader.NamespaceURI.Length != 0)
			{
				SkipUnknownElement(xmlReader);
				continue;
			}
			string name = XmlConvert.DecodeName(xmlReader.LocalName);
			IncrementItemCount(1);
			ReadAttributes(xmlReader);
			object value;
			if (attributes.Ref != Globals.NewObjectId)
			{
				xmlReader.Skip();
				value = GetExistingObject(attributes.Ref, null, name, string.Empty);
			}
			else if (attributes.XsiNil)
			{
				xmlReader.Skip();
				value = null;
			}
			else
			{
				value = InternalDeserialize(xmlReader, Globals.TypeOfObject, name, string.Empty);
			}
			serializationInfo.AddValue(name, value);
		}
		return serializationInfo;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual DataContract ResolveDataContractFromTypeName()
	{
		if (attributes.XsiTypeName != null)
		{
			return ResolveDataContractFromKnownTypes(attributes.XsiTypeName, attributes.XsiTypeNamespace, null, null);
		}
		return null;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private ExtensionDataMember ReadExtensionDataMember(XmlReaderDelegator xmlReader, int memberIndex)
	{
		ExtensionDataMember extensionDataMember = new ExtensionDataMember(xmlReader.LocalName, xmlReader.NamespaceURI)
		{
			MemberIndex = memberIndex
		};
		extensionDataMember.Value = ((xmlReader.UnderlyingExtensionDataReader != null) ? xmlReader.UnderlyingExtensionDataReader.GetCurrentNode() : ReadExtensionDataValue(xmlReader));
		return extensionDataMember;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public IDataNode ReadExtensionDataValue(XmlReaderDelegator xmlReader)
	{
		ReadAttributes(xmlReader);
		IncrementItemCount(1);
		IDataNode dataNode = null;
		if (attributes.Ref != Globals.NewObjectId)
		{
			xmlReader.Skip();
			object existingObjectOrExtensionData = GetExistingObjectOrExtensionData(attributes.Ref);
			object obj;
			if (!(existingObjectOrExtensionData is IDataNode))
			{
				IDataNode dataNode2 = new DataNode<object>(existingObjectOrExtensionData);
				obj = dataNode2;
			}
			else
			{
				obj = (IDataNode)existingObjectOrExtensionData;
			}
			dataNode = (IDataNode)obj;
			dataNode.Id = attributes.Ref;
		}
		else if (attributes.XsiNil)
		{
			xmlReader.Skip();
			dataNode = null;
		}
		else
		{
			string dataContractName = null;
			string dataContractNamespace = null;
			if (attributes.XsiTypeName != null)
			{
				dataContractName = attributes.XsiTypeName;
				dataContractNamespace = attributes.XsiTypeNamespace;
			}
			if (IsReadingCollectionExtensionData(xmlReader))
			{
				Read(xmlReader);
				dataNode = ReadUnknownCollectionData(xmlReader, dataContractName, dataContractNamespace);
			}
			else if (attributes.FactoryTypeName != null)
			{
				Read(xmlReader);
				dataNode = ReadUnknownISerializableData(xmlReader, dataContractName, dataContractNamespace);
			}
			else if (IsReadingClassExtensionData(xmlReader))
			{
				Read(xmlReader);
				dataNode = ReadUnknownClassData(xmlReader, dataContractName, dataContractNamespace);
			}
			else
			{
				DataContract dataContract = ResolveDataContractFromTypeName();
				if (dataContract == null)
				{
					dataNode = ReadExtensionDataValue(xmlReader, dataContractName, dataContractNamespace);
				}
				else if (dataContract is XmlDataContract)
				{
					dataNode = ReadUnknownXmlData(xmlReader, dataContractName, dataContractNamespace);
				}
				else if (dataContract.IsISerializable)
				{
					Read(xmlReader);
					dataNode = ReadUnknownISerializableData(xmlReader, dataContractName, dataContractNamespace);
				}
				else if (dataContract is PrimitiveDataContract)
				{
					if (attributes.Id == Globals.NewObjectId)
					{
						Read(xmlReader);
						xmlReader.MoveToContent();
						dataNode = ReadUnknownPrimitiveData(xmlReader, dataContract.UnderlyingType, dataContractName, dataContractNamespace);
						xmlReader.ReadEndElement();
					}
					else
					{
						dataNode = new DataNode<object>(xmlReader.ReadElementContentAsAnyType(dataContract.UnderlyingType));
						InitializeExtensionDataNode(dataNode, dataContractName, dataContractNamespace);
					}
				}
				else if (dataContract is EnumDataContract)
				{
					dataNode = new DataNode<object>(((EnumDataContract)dataContract).ReadEnumValue(xmlReader));
					InitializeExtensionDataNode(dataNode, dataContractName, dataContractNamespace);
				}
				else if (dataContract is ClassDataContract)
				{
					Read(xmlReader);
					dataNode = ReadUnknownClassData(xmlReader, dataContractName, dataContractNamespace);
				}
				else if (dataContract is CollectionDataContract)
				{
					Read(xmlReader);
					dataNode = ReadUnknownCollectionData(xmlReader, dataContractName, dataContractNamespace);
				}
			}
		}
		return dataNode;
	}

	protected virtual void StartReadExtensionDataValue(XmlReaderDelegator xmlReader)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private IDataNode ReadExtensionDataValue(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
	{
		StartReadExtensionDataValue(xmlReader);
		if (attributes.UnrecognizedAttributesFound)
		{
			return ReadUnknownXmlData(xmlReader, dataContractName, dataContractNamespace);
		}
		IDictionary<string, string> namespacesInScope = xmlReader.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);
		Read(xmlReader);
		xmlReader.MoveToContent();
		switch (xmlReader.NodeType)
		{
		case XmlNodeType.Text:
			return ReadPrimitiveExtensionDataValue(xmlReader, dataContractName, dataContractNamespace);
		case XmlNodeType.Element:
			if (xmlReader.NamespaceURI.StartsWith("http://schemas.datacontract.org/2004/07/", StringComparison.Ordinal))
			{
				return ReadUnknownClassData(xmlReader, dataContractName, dataContractNamespace);
			}
			return ReadAndResolveUnknownXmlData(xmlReader, namespacesInScope, dataContractName, dataContractNamespace);
		case XmlNodeType.EndElement:
		{
			IDataNode dataNode = ReadUnknownPrimitiveData(xmlReader, Globals.TypeOfObject, dataContractName, dataContractNamespace);
			xmlReader.ReadEndElement();
			dataNode.IsFinalValue = false;
			return dataNode;
		}
		default:
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
		}
	}

	protected virtual IDataNode ReadPrimitiveExtensionDataValue(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
	{
		Type valueType = xmlReader.ValueType;
		if (valueType == Globals.TypeOfString)
		{
			IDataNode dataNode = new DataNode<object>(xmlReader.ReadContentAsString());
			InitializeExtensionDataNode(dataNode, dataContractName, dataContractNamespace);
			dataNode.IsFinalValue = false;
			xmlReader.ReadEndElement();
			return dataNode;
		}
		IDataNode result = ReadUnknownPrimitiveData(xmlReader, valueType, dataContractName, dataContractNamespace);
		xmlReader.ReadEndElement();
		return result;
	}

	protected void InitializeExtensionDataNode(IDataNode dataNode, string dataContractName, string dataContractNamespace)
	{
		dataNode.DataContractName = dataContractName;
		dataNode.DataContractNamespace = dataContractNamespace;
		dataNode.ClrAssemblyName = attributes.ClrAssembly;
		dataNode.ClrTypeName = attributes.ClrType;
		AddNewObject(dataNode);
		dataNode.Id = attributes.Id;
	}

	private IDataNode ReadUnknownPrimitiveData(XmlReaderDelegator xmlReader, Type type, string dataContractName, string dataContractNamespace)
	{
		IDataNode dataNode = xmlReader.ReadExtensionData(type);
		InitializeExtensionDataNode(dataNode, dataContractName, dataContractNamespace);
		return dataNode;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private ClassDataNode ReadUnknownClassData(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
	{
		ClassDataNode classDataNode = new ClassDataNode();
		InitializeExtensionDataNode(classDataNode, dataContractName, dataContractNamespace);
		int num = 0;
		XmlNodeType xmlNodeType;
		while ((xmlNodeType = xmlReader.MoveToContent()) != XmlNodeType.EndElement)
		{
			if (xmlNodeType != XmlNodeType.Element)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
			}
			if (classDataNode.Members == null)
			{
				classDataNode.Members = new List<ExtensionDataMember>();
			}
			classDataNode.Members.Add(ReadExtensionDataMember(xmlReader, num++));
		}
		xmlReader.ReadEndElement();
		return classDataNode;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionDataNode ReadUnknownCollectionData(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
	{
		CollectionDataNode collectionDataNode = new CollectionDataNode();
		InitializeExtensionDataNode(collectionDataNode, dataContractName, dataContractNamespace);
		int arraySZSize = attributes.ArraySZSize;
		XmlNodeType xmlNodeType;
		while ((xmlNodeType = xmlReader.MoveToContent()) != XmlNodeType.EndElement)
		{
			if (xmlNodeType != XmlNodeType.Element)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
			}
			if (collectionDataNode.ItemName == null)
			{
				collectionDataNode.ItemName = xmlReader.LocalName;
				collectionDataNode.ItemNamespace = xmlReader.NamespaceURI;
			}
			if (xmlReader.IsStartElement(collectionDataNode.ItemName, collectionDataNode.ItemNamespace))
			{
				if (collectionDataNode.Items == null)
				{
					collectionDataNode.Items = new List<IDataNode>();
				}
				collectionDataNode.Items.Add(ReadExtensionDataValue(xmlReader));
			}
			else
			{
				SkipUnknownElement(xmlReader);
			}
		}
		xmlReader.ReadEndElement();
		if (arraySZSize != -1)
		{
			collectionDataNode.Size = arraySZSize;
			if (collectionDataNode.Items == null)
			{
				if (collectionDataNode.Size > 0)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ArraySizeAttributeIncorrect, arraySZSize, 0)));
				}
			}
			else if (collectionDataNode.Size != collectionDataNode.Items.Count)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ArraySizeAttributeIncorrect, arraySZSize, collectionDataNode.Items.Count)));
			}
		}
		else if (collectionDataNode.Items != null)
		{
			collectionDataNode.Size = collectionDataNode.Items.Count;
		}
		else
		{
			collectionDataNode.Size = 0;
		}
		return collectionDataNode;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private ISerializableDataNode ReadUnknownISerializableData(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
	{
		ISerializableDataNode serializableDataNode = new ISerializableDataNode();
		InitializeExtensionDataNode(serializableDataNode, dataContractName, dataContractNamespace);
		serializableDataNode.FactoryTypeName = attributes.FactoryTypeName;
		serializableDataNode.FactoryTypeNamespace = attributes.FactoryTypeNamespace;
		XmlNodeType xmlNodeType;
		while ((xmlNodeType = xmlReader.MoveToContent()) != XmlNodeType.EndElement)
		{
			if (xmlNodeType != XmlNodeType.Element)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateUnexpectedStateException(XmlNodeType.Element, xmlReader));
			}
			if (xmlReader.NamespaceURI.Length != 0)
			{
				SkipUnknownElement(xmlReader);
				continue;
			}
			ISerializableDataMember serializableDataMember = new ISerializableDataMember(xmlReader.LocalName);
			serializableDataMember.Value = ReadExtensionDataValue(xmlReader);
			if (serializableDataNode.Members == null)
			{
				serializableDataNode.Members = new List<ISerializableDataMember>();
			}
			serializableDataNode.Members.Add(serializableDataMember);
		}
		xmlReader.ReadEndElement();
		return serializableDataNode;
	}

	private IDataNode ReadUnknownXmlData(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
	{
		XmlDataNode xmlDataNode = new XmlDataNode();
		InitializeExtensionDataNode(xmlDataNode, dataContractName, dataContractNamespace);
		xmlDataNode.OwnerDocument = Document;
		if (xmlReader.NodeType == XmlNodeType.EndElement)
		{
			return xmlDataNode;
		}
		IList<XmlAttribute> list = null;
		IList<XmlNode> list2 = null;
		XmlNodeType xmlNodeType = xmlReader.MoveToContent();
		if (xmlNodeType != XmlNodeType.Text)
		{
			while (xmlReader.MoveToNextAttribute())
			{
				string namespaceURI = xmlReader.NamespaceURI;
				if (namespaceURI != "http://schemas.microsoft.com/2003/10/Serialization/" && namespaceURI != "http://www.w3.org/2001/XMLSchema-instance")
				{
					if (list == null)
					{
						list = new List<XmlAttribute>();
					}
					list.Add((XmlAttribute)Document.ReadNode(xmlReader.UnderlyingReader));
				}
			}
			Read(xmlReader);
		}
		while ((xmlNodeType = xmlReader.MoveToContent()) != XmlNodeType.EndElement)
		{
			if (xmlReader.EOF)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.UnexpectedEndOfFile));
			}
			if (list2 == null)
			{
				list2 = new List<XmlNode>();
			}
			list2.Add(Document.ReadNode(xmlReader.UnderlyingReader));
		}
		xmlReader.ReadEndElement();
		xmlDataNode.XmlAttributes = list;
		xmlDataNode.XmlChildNodes = list2;
		return xmlDataNode;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private IDataNode ReadAndResolveUnknownXmlData(XmlReaderDelegator xmlReader, IDictionary<string, string> namespaces, string dataContractName, string dataContractNamespace)
	{
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		string strA = null;
		string text = null;
		List<XmlNode> list = new List<XmlNode>();
		IList<XmlAttribute> list2 = null;
		if (namespaces != null)
		{
			list2 = new List<XmlAttribute>();
			foreach (KeyValuePair<string, string> @namespace in namespaces)
			{
				list2.Add(AddNamespaceDeclaration(@namespace.Key, @namespace.Value));
			}
		}
		XmlNodeType nodeType;
		while ((nodeType = xmlReader.NodeType) != XmlNodeType.EndElement)
		{
			if (nodeType == XmlNodeType.Element)
			{
				string namespaceURI = xmlReader.NamespaceURI;
				string localName = xmlReader.LocalName;
				if (flag)
				{
					flag = namespaceURI.Length == 0;
				}
				if (flag2)
				{
					if (text == null)
					{
						text = localName;
						strA = namespaceURI;
					}
					else
					{
						flag2 = string.CompareOrdinal(text, localName) == 0 && string.CompareOrdinal(strA, namespaceURI) == 0;
					}
				}
			}
			else
			{
				if (xmlReader.EOF)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.UnexpectedEndOfFile));
				}
				if (IsContentNode(xmlReader.NodeType))
				{
					flag3 = (flag = (flag2 = false));
				}
			}
			if (_attributesInXmlData == null)
			{
				_attributesInXmlData = new Attributes();
			}
			_attributesInXmlData.Read(xmlReader);
			XmlNode xmlNode = Document.ReadNode(xmlReader.UnderlyingReader);
			list.Add(xmlNode);
			if (namespaces == null)
			{
				if (_attributesInXmlData.XsiTypeName != null)
				{
					xmlNode.Attributes.Append(AddNamespaceDeclaration(_attributesInXmlData.XsiTypePrefix, _attributesInXmlData.XsiTypeNamespace));
				}
				if (_attributesInXmlData.FactoryTypeName != null)
				{
					xmlNode.Attributes.Append(AddNamespaceDeclaration(_attributesInXmlData.FactoryTypePrefix, _attributesInXmlData.FactoryTypeNamespace));
				}
			}
		}
		xmlReader.ReadEndElement();
		if (text != null && flag2)
		{
			return ReadUnknownCollectionData(CreateReaderOverChildNodes(list2, list), dataContractName, dataContractNamespace);
		}
		if (flag)
		{
			return ReadUnknownISerializableData(CreateReaderOverChildNodes(list2, list), dataContractName, dataContractNamespace);
		}
		if (flag3)
		{
			return ReadUnknownClassData(CreateReaderOverChildNodes(list2, list), dataContractName, dataContractNamespace);
		}
		XmlDataNode xmlDataNode = new XmlDataNode();
		InitializeExtensionDataNode(xmlDataNode, dataContractName, dataContractNamespace);
		xmlDataNode.OwnerDocument = Document;
		xmlDataNode.XmlChildNodes = list;
		xmlDataNode.XmlAttributes = list2;
		return xmlDataNode;
	}

	private bool IsContentNode(XmlNodeType nodeType)
	{
		switch (nodeType)
		{
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.Comment:
		case XmlNodeType.DocumentType:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			return false;
		default:
			return true;
		}
	}

	internal XmlReaderDelegator CreateReaderOverChildNodes(IList<XmlAttribute> xmlAttributes, IList<XmlNode> xmlChildNodes)
	{
		XmlNode node = CreateWrapperXmlElement(Document, xmlAttributes, xmlChildNodes, null, null, null);
		XmlReaderDelegator xmlReaderDelegator = CreateReaderDelegatorForReader(new XmlNodeReader(node));
		xmlReaderDelegator.MoveToContent();
		Read(xmlReaderDelegator);
		return xmlReaderDelegator;
	}

	internal static XmlNode CreateWrapperXmlElement(XmlDocument document, IList<XmlAttribute> xmlAttributes, IList<XmlNode> xmlChildNodes, string prefix, string localName, string ns)
	{
		localName = localName ?? "wrapper";
		ns = ns ?? string.Empty;
		XmlElement xmlElement = document.CreateElement(prefix, localName, ns);
		if (xmlAttributes != null)
		{
			for (int i = 0; i < xmlAttributes.Count; i++)
			{
				xmlElement.Attributes.Append(xmlAttributes[i]);
			}
		}
		if (xmlChildNodes != null)
		{
			for (int j = 0; j < xmlChildNodes.Count; j++)
			{
				xmlElement.AppendChild(xmlChildNodes[j]);
			}
		}
		return xmlElement;
	}

	private XmlAttribute AddNamespaceDeclaration(string prefix, string ns)
	{
		XmlAttribute xmlAttribute = ((prefix == null || prefix.Length == 0) ? Document.CreateAttribute(null, "xmlns", "http://www.w3.org/2000/xmlns/") : Document.CreateAttribute("xmlns", prefix, "http://www.w3.org/2000/xmlns/"));
		xmlAttribute.Value = ns;
		return xmlAttribute;
	}

	internal static Exception CreateUnexpectedStateException(XmlNodeType expectedState, XmlReaderDelegator xmlReader)
	{
		return XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(System.SR.Format(System.SR.ExpectingState, expectedState), xmlReader);
	}

	internal static Exception CreateSerializationException(string message)
	{
		return XmlObjectSerializer.CreateSerializationException(message);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual object ReadDataContractValue(DataContract dataContract, XmlReaderDelegator reader)
	{
		return dataContract.ReadXmlValue(reader, this);
	}

	protected virtual XmlReaderDelegator CreateReaderDelegatorForReader(XmlReader xmlReader)
	{
		return new XmlReaderDelegator(xmlReader);
	}

	protected virtual bool IsReadingCollectionExtensionData(XmlReaderDelegator xmlReader)
	{
		return attributes.ArraySZSize != -1;
	}

	protected virtual bool IsReadingClassExtensionData(XmlReaderDelegator xmlReader)
	{
		return false;
	}
}
