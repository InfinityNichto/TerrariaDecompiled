using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml;

namespace System.Runtime.Serialization;

public sealed class DataContractSerializer : XmlObjectSerializer
{
	private Type _rootType;

	private DataContract _rootContract;

	private bool _needsContractNsAtRoot;

	private XmlDictionaryString _rootName;

	private XmlDictionaryString _rootNamespace;

	private int _maxItemsInObjectGraph;

	private bool _ignoreExtensionDataObject;

	private bool _preserveObjectReferences;

	private ReadOnlyCollection<Type> _knownTypeCollection;

	internal IList<Type> knownTypeList;

	internal Dictionary<XmlQualifiedName, DataContract> knownDataContracts;

	private DataContractResolver _dataContractResolver;

	private ISerializationSurrogateProvider _serializationSurrogateProvider;

	private bool _serializeReadOnlyTypes;

	private static SerializationOption _option = (IsReflectionBackupAllowed() ? SerializationOption.ReflectionAsBackup : SerializationOption.CodeGenOnly);

	private static bool _optionAlreadySet;

	internal static SerializationOption Option
	{
		get
		{
			if (!RuntimeFeature.IsDynamicCodeSupported)
			{
				return SerializationOption.ReflectionOnly;
			}
			return _option;
		}
		set
		{
			if (_optionAlreadySet)
			{
				throw new InvalidOperationException(System.SR.CannotSetTwice);
			}
			_optionAlreadySet = true;
			_option = value;
		}
	}

	public ReadOnlyCollection<Type> KnownTypes
	{
		get
		{
			if (_knownTypeCollection == null)
			{
				if (knownTypeList != null)
				{
					_knownTypeCollection = new ReadOnlyCollection<Type>(knownTypeList);
				}
				else
				{
					_knownTypeCollection = new ReadOnlyCollection<Type>(Type.EmptyTypes);
				}
			}
			return _knownTypeCollection;
		}
	}

	internal override Dictionary<XmlQualifiedName, DataContract>? KnownDataContracts
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (knownDataContracts == null && knownTypeList != null)
			{
				knownDataContracts = XmlObjectSerializerContext.GetDataContractsForKnownTypes(knownTypeList);
			}
			return knownDataContracts;
		}
	}

	public int MaxItemsInObjectGraph => _maxItemsInObjectGraph;

	internal ISerializationSurrogateProvider? SerializationSurrogateProvider
	{
		get
		{
			return _serializationSurrogateProvider;
		}
		set
		{
			_serializationSurrogateProvider = value;
		}
	}

	public bool PreserveObjectReferences => _preserveObjectReferences;

	public bool IgnoreExtensionDataObject => _ignoreExtensionDataObject;

	public DataContractResolver? DataContractResolver => _dataContractResolver;

	public bool SerializeReadOnlyTypes => _serializeReadOnlyTypes;

	private DataContract RootContract
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_rootContract == null)
			{
				_rootContract = DataContract.GetDataContract((_serializationSurrogateProvider == null) ? _rootType : GetSurrogatedType(_serializationSurrogateProvider, _rootType));
				_needsContractNsAtRoot = CheckIfNeedsContractNsAtRoot(_rootName, _rootNamespace, _rootContract);
			}
			return _rootContract;
		}
	}

	private static bool IsReflectionBackupAllowed()
	{
		return true;
	}

	public DataContractSerializer(Type type)
		: this(type, (IEnumerable<Type>?)null)
	{
	}

	public DataContractSerializer(Type type, IEnumerable<Type>? knownTypes)
	{
		Initialize(type, knownTypes, int.MaxValue, ignoreExtensionDataObject: false, preserveObjectReferences: false, null, serializeReadOnlyTypes: false);
	}

	public DataContractSerializer(Type type, string rootName, string rootNamespace)
		: this(type, rootName, rootNamespace, null)
	{
	}

	public DataContractSerializer(Type type, string rootName, string rootNamespace, IEnumerable<Type>? knownTypes)
	{
		XmlDictionary xmlDictionary = new XmlDictionary(2);
		Initialize(type, xmlDictionary.Add(rootName), xmlDictionary.Add(DataContract.GetNamespace(rootNamespace)), knownTypes, int.MaxValue, ignoreExtensionDataObject: false, preserveObjectReferences: false, null, serializeReadOnlyTypes: false);
	}

	public DataContractSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace)
		: this(type, rootName, rootNamespace, null)
	{
	}

	public DataContractSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace, IEnumerable<Type>? knownTypes)
	{
		Initialize(type, rootName, rootNamespace, knownTypes, int.MaxValue, ignoreExtensionDataObject: false, preserveObjectReferences: false, null, serializeReadOnlyTypes: false);
	}

	internal DataContractSerializer(Type type, IEnumerable<Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences)
	{
		Initialize(type, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, preserveObjectReferences, null, serializeReadOnlyTypes: false);
	}

	public DataContractSerializer(Type type, DataContractSerializerSettings? settings)
	{
		if (settings == null)
		{
			settings = new DataContractSerializerSettings();
		}
		Initialize(type, settings.RootName, settings.RootNamespace, settings.KnownTypes, settings.MaxItemsInObjectGraph, ignoreExtensionDataObject: false, settings.PreserveObjectReferences, settings.DataContractResolver, settings.SerializeReadOnlyTypes);
	}

	[MemberNotNull("_rootType")]
	private void Initialize(Type type, IEnumerable<Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, DataContractResolver dataContractResolver, bool serializeReadOnlyTypes)
	{
		XmlObjectSerializer.CheckNull(type, "type");
		_rootType = type;
		if (knownTypes != null)
		{
			knownTypeList = new List<Type>();
			foreach (Type knownType in knownTypes)
			{
				knownTypeList.Add(knownType);
			}
		}
		if (maxItemsInObjectGraph < 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("maxItemsInObjectGraph", System.SR.ValueMustBeNonNegative));
		}
		_maxItemsInObjectGraph = maxItemsInObjectGraph;
		_ignoreExtensionDataObject = ignoreExtensionDataObject;
		_preserveObjectReferences = preserveObjectReferences;
		_dataContractResolver = dataContractResolver;
		_serializeReadOnlyTypes = serializeReadOnlyTypes;
	}

	[MemberNotNull("_rootType")]
	private void Initialize(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace, IEnumerable<Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, DataContractResolver dataContractResolver, bool serializeReadOnlyTypes)
	{
		Initialize(type, knownTypes, maxItemsInObjectGraph, ignoreExtensionDataObject, preserveObjectReferences, dataContractResolver, serializeReadOnlyTypes);
		_rootName = rootName;
		_rootNamespace = rootNamespace;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void InternalWriteObject(XmlWriterDelegator writer, object graph)
	{
		InternalWriteObject(writer, graph, null);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void InternalWriteObject(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
	{
		InternalWriteStartObject(writer, graph);
		InternalWriteObjectContent(writer, graph, dataContractResolver);
		InternalWriteEndObject(writer);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteObject(XmlWriter writer, object? graph)
	{
		WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteStartObject(XmlWriter writer, object? graph)
	{
		WriteStartObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteObjectContent(XmlWriter writer, object? graph)
	{
		WriteObjectContentHandleExceptions(new XmlWriterDelegator(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteEndObject(XmlWriter writer)
	{
		WriteEndObjectHandleExceptions(new XmlWriterDelegator(writer));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteStartObject(XmlDictionaryWriter writer, object? graph)
	{
		WriteStartObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteObjectContent(XmlDictionaryWriter writer, object? graph)
	{
		WriteObjectContentHandleExceptions(new XmlWriterDelegator(writer), graph);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteEndObject(XmlDictionaryWriter writer)
	{
		WriteEndObjectHandleExceptions(new XmlWriterDelegator(writer));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void WriteObject(XmlDictionaryWriter writer, object? graph, DataContractResolver? dataContractResolver)
	{
		WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph, dataContractResolver);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object? ReadObject(XmlReader reader)
	{
		return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName: true);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object? ReadObject(XmlReader reader, bool verifyObjectName)
	{
		return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override bool IsStartObject(XmlReader reader)
	{
		return IsStartObjectHandleExceptions(new XmlReaderDelegator(reader));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
	{
		return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override bool IsStartObject(XmlDictionaryReader reader)
	{
		return IsStartObjectHandleExceptions(new XmlReaderDelegator(reader));
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName, DataContractResolver? dataContractResolver)
	{
		return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName, dataContractResolver);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void InternalWriteStartObject(XmlWriterDelegator writer, object graph)
	{
		WriteRootElement(writer, RootContract, _rootName, _rootNamespace, _needsContractNsAtRoot);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void InternalWriteObjectContent(XmlWriterDelegator writer, object graph)
	{
		InternalWriteObjectContent(writer, graph, null);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void InternalWriteObjectContent(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
	{
		if (MaxItemsInObjectGraph == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph)));
		}
		DataContract rootContract = RootContract;
		Type underlyingType = rootContract.UnderlyingType;
		Type objType = ((graph == null) ? underlyingType : graph.GetType());
		if (_serializationSurrogateProvider != null)
		{
			graph = SurrogateToDataContractType(_serializationSurrogateProvider, graph, underlyingType, ref objType);
		}
		if (dataContractResolver == null)
		{
			dataContractResolver = DataContractResolver;
		}
		if (graph == null)
		{
			if (IsRootXmlAny(_rootName, rootContract))
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.IsAnyCannotBeNull, underlyingType)));
			}
			XmlObjectSerializer.WriteNull(writer);
			return;
		}
		if (underlyingType == objType)
		{
			if (rootContract.CanContainReferences)
			{
				XmlObjectSerializerWriteContext xmlObjectSerializerWriteContext = XmlObjectSerializerWriteContext.CreateContext(this, rootContract, dataContractResolver);
				xmlObjectSerializerWriteContext.HandleGraphAtTopLevel(writer, graph, rootContract);
				xmlObjectSerializerWriteContext.SerializeWithoutXsiType(rootContract, writer, graph, underlyingType.TypeHandle);
			}
			else
			{
				rootContract.WriteXmlValue(writer, graph, null);
			}
			return;
		}
		XmlObjectSerializerWriteContext xmlObjectSerializerWriteContext2 = null;
		if (IsRootXmlAny(_rootName, rootContract))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.IsAnyCannotBeSerializedAsDerivedType, objType, rootContract.UnderlyingType)));
		}
		rootContract = GetDataContract(rootContract, underlyingType, objType);
		xmlObjectSerializerWriteContext2 = XmlObjectSerializerWriteContext.CreateContext(this, RootContract, dataContractResolver);
		if (rootContract.CanContainReferences)
		{
			xmlObjectSerializerWriteContext2.HandleGraphAtTopLevel(writer, graph, rootContract);
		}
		xmlObjectSerializerWriteContext2.OnHandleIsReference(writer, rootContract, graph);
		xmlObjectSerializerWriteContext2.SerializeWithXsiTypeAtTopLevel(rootContract, writer, graph, underlyingType.TypeHandle, objType);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static DataContract GetDataContract(DataContract declaredTypeContract, Type declaredType, Type objectType)
	{
		if (declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
		{
			return declaredTypeContract;
		}
		if (declaredType.IsArray)
		{
			return declaredTypeContract;
		}
		return DataContract.GetDataContract(objectType.TypeHandle, objectType, SerializationMode.SharedContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void InternalWriteEndObject(XmlWriterDelegator writer)
	{
		if (!IsRootXmlAny(_rootName, RootContract))
		{
			writer.WriteEndElement();
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalReadObject(XmlReaderDelegator xmlReader, bool verifyObjectName)
	{
		return InternalReadObject(xmlReader, verifyObjectName, null);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalReadObject(XmlReaderDelegator xmlReader, bool verifyObjectName, DataContractResolver dataContractResolver)
	{
		if (MaxItemsInObjectGraph == 0)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ExceededMaxItemsQuota, MaxItemsInObjectGraph)));
		}
		if (dataContractResolver == null)
		{
			dataContractResolver = DataContractResolver;
		}
		if (verifyObjectName)
		{
			if (!InternalIsStartObject(xmlReader))
			{
				XmlDictionaryString p;
				XmlDictionaryString p2;
				if (_rootName == null)
				{
					p = RootContract.TopLevelElementName;
					p2 = RootContract.TopLevelElementNamespace;
				}
				else
				{
					p = _rootName;
					p2 = _rootNamespace;
				}
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(System.SR.Format(System.SR.ExpectingElement, p2, p), xmlReader));
			}
		}
		else if (!IsStartElement(xmlReader))
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationExceptionWithReaderDetails(System.SR.Format(System.SR.ExpectingElementAtDeserialize, XmlNodeType.Element), xmlReader));
		}
		DataContract rootContract = RootContract;
		if (rootContract.IsPrimitive && (object)rootContract.UnderlyingType == _rootType)
		{
			return rootContract.ReadXmlValue(xmlReader, null);
		}
		if (IsRootXmlAny(_rootName, rootContract))
		{
			return XmlObjectSerializerReadContext.ReadRootIXmlSerializable(xmlReader, rootContract as XmlDataContract, isMemberType: false);
		}
		XmlObjectSerializerReadContext xmlObjectSerializerReadContext = XmlObjectSerializerReadContext.CreateContext(this, rootContract, dataContractResolver);
		return xmlObjectSerializerReadContext.InternalDeserialize(xmlReader, _rootType, rootContract, null, null);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override bool InternalIsStartObject(XmlReaderDelegator reader)
	{
		return IsRootElement(reader, RootContract, _rootName, _rootNamespace);
	}

	internal override Type GetSerializeType(object graph)
	{
		if (graph != null)
		{
			return graph.GetType();
		}
		return _rootType;
	}

	internal override Type GetDeserializeType()
	{
		return _rootType;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	[return: NotNullIfNotNull("oldObj")]
	internal static object SurrogateToDataContractType(ISerializationSurrogateProvider serializationSurrogateProvider, object oldObj, Type surrogatedDeclaredType, ref Type objType)
	{
		object objectToSerialize = DataContractSurrogateCaller.GetObjectToSerialize(serializationSurrogateProvider, oldObj, objType, surrogatedDeclaredType);
		if (objectToSerialize != oldObj)
		{
			objType = ((objectToSerialize != null) ? objectToSerialize.GetType() : Globals.TypeOfObject);
		}
		return objectToSerialize;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Type GetSurrogatedType(ISerializationSurrogateProvider serializationSurrogateProvider, Type type)
	{
		return DataContractSurrogateCaller.GetDataContractType(serializationSurrogateProvider, DataContract.UnwrapNullableType(type));
	}
}
