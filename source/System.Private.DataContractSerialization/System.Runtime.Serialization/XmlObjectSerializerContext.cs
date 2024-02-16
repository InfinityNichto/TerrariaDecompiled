using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal class XmlObjectSerializerContext
{
	protected XmlObjectSerializer serializer;

	protected DataContract rootTypeDataContract;

	internal ScopedKnownTypes scopedKnownTypes;

	protected Dictionary<XmlQualifiedName, DataContract> serializerKnownDataContracts;

	private bool _isSerializerKnownDataContractsSetExplicit;

	protected IList<Type> serializerKnownTypeList;

	private int _itemCount;

	private readonly int _maxItemsInObjectGraph;

	private readonly StreamingContext _streamingContext;

	private readonly bool _ignoreExtensionDataObject;

	private readonly DataContractResolver _dataContractResolver;

	private KnownTypeDataContractResolver _knownTypeResolver;

	internal virtual SerializationMode Mode => SerializationMode.SharedContract;

	internal virtual bool IsGetOnlyCollection
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	internal int RemainingItemCount => _maxItemsInObjectGraph - _itemCount;

	internal bool IgnoreExtensionDataObject => _ignoreExtensionDataObject;

	protected DataContractResolver DataContractResolver => _dataContractResolver;

	protected KnownTypeDataContractResolver KnownTypeResolver
	{
		get
		{
			if (_knownTypeResolver == null)
			{
				_knownTypeResolver = new KnownTypeDataContractResolver(this);
			}
			return _knownTypeResolver;
		}
	}

	internal virtual Dictionary<XmlQualifiedName, DataContract> SerializerKnownDataContracts
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (!_isSerializerKnownDataContractsSetExplicit)
			{
				serializerKnownDataContracts = serializer.KnownDataContracts;
				_isSerializerKnownDataContractsSetExplicit = true;
			}
			return serializerKnownDataContracts;
		}
	}

	internal XmlObjectSerializerContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject, DataContractResolver dataContractResolver)
	{
		this.serializer = serializer;
		_itemCount = 1;
		_maxItemsInObjectGraph = maxItemsInObjectGraph;
		_streamingContext = streamingContext;
		_ignoreExtensionDataObject = ignoreExtensionDataObject;
		_dataContractResolver = dataContractResolver;
	}

	internal XmlObjectSerializerContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
		: this(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject, null)
	{
	}

	internal XmlObjectSerializerContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
		: this(serializer, serializer.MaxItemsInObjectGraph, default(StreamingContext), serializer.IgnoreExtensionDataObject, dataContractResolver)
	{
		this.rootTypeDataContract = rootTypeDataContract;
		serializerKnownTypeList = serializer.knownTypeList;
	}

	internal StreamingContext GetStreamingContext()
	{
		return _streamingContext;
	}

	internal void IncrementItemCount(int count)
	{
		if (count > _maxItemsInObjectGraph - _itemCount)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ExceededMaxItemsQuota, _maxItemsInObjectGraph)));
		}
		_itemCount += count;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContract GetDataContract(Type type)
	{
		return GetDataContract(type.TypeHandle, type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type)
	{
		if (IsGetOnlyCollection)
		{
			return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(typeHandle), typeHandle, type, Mode);
		}
		return DataContract.GetDataContract(typeHandle, type, Mode);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type type)
	{
		if (IsGetOnlyCollection)
		{
			return DataContract.GetGetOnlyCollectionDataContractSkipValidation(typeId, typeHandle, type);
		}
		return DataContract.GetDataContractSkipValidation(typeId, typeHandle, type);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
	{
		if (IsGetOnlyCollection)
		{
			return DataContract.GetGetOnlyCollectionDataContract(id, typeHandle, null, Mode);
		}
		return DataContract.GetDataContract(id, typeHandle, Mode);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
	{
		if (!isMemberTypeSerializable)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, memberType)));
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual Type GetSurrogatedType(Type type)
	{
		return type;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract GetDataContractFromSerializerKnownTypes(XmlQualifiedName qname)
	{
		Dictionary<XmlQualifiedName, DataContract> dictionary = SerializerKnownDataContracts;
		if (dictionary == null)
		{
			return null;
		}
		if (!dictionary.TryGetValue(qname, out var value))
		{
			return null;
		}
		return value;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Dictionary<XmlQualifiedName, DataContract> GetDataContractsForKnownTypes(IList<Type> knownTypeList)
	{
		if (knownTypeList == null)
		{
			return null;
		}
		Dictionary<XmlQualifiedName, DataContract> nameToDataContractTable = new Dictionary<XmlQualifiedName, DataContract>();
		Dictionary<Type, Type> typesChecked = new Dictionary<Type, Type>();
		for (int i = 0; i < knownTypeList.Count; i++)
		{
			Type type = knownTypeList[i];
			if (type == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(System.SR.Format(System.SR.NullKnownType, "knownTypes")));
			}
			DataContract.CheckAndAdd(type, typesChecked, ref nameToDataContractTable);
		}
		return nameToDataContractTable;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool IsKnownType(DataContract dataContract, Dictionary<XmlQualifiedName, DataContract> knownDataContracts, Type declaredType)
	{
		bool flag = false;
		if (knownDataContracts != null)
		{
			scopedKnownTypes.Push(knownDataContracts);
			flag = true;
		}
		bool result = IsKnownType(dataContract, declaredType);
		if (flag)
		{
			scopedKnownTypes.Pop();
		}
		return result;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool IsKnownType(DataContract dataContract, Type declaredType)
	{
		DataContract dataContract2 = ResolveDataContractFromKnownTypes(dataContract.StableName.Name, dataContract.StableName.Namespace, null, declaredType);
		if (dataContract2 != null)
		{
			return dataContract2.UnderlyingType == dataContract.UnderlyingType;
		}
		return false;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal Type ResolveNameFromKnownTypes(XmlQualifiedName typeName)
	{
		return ResolveDataContractFromKnownTypes(typeName)?.UnderlyingType;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ResolveDataContractFromKnownTypes(XmlQualifiedName typeName)
	{
		DataContract dataContract = PrimitiveDataContract.GetPrimitiveDataContract(typeName.Name, typeName.Namespace);
		if (dataContract == null)
		{
			dataContract = scopedKnownTypes.GetDataContract(typeName);
			if (dataContract == null)
			{
				dataContract = GetDataContractFromSerializerKnownTypes(typeName);
			}
		}
		return dataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected DataContract ResolveDataContractFromKnownTypes(string typeName, string typeNs, DataContract memberTypeContract, Type declaredType)
	{
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(typeName, typeNs);
		DataContract dataContract;
		if (_dataContractResolver == null)
		{
			dataContract = ResolveDataContractFromKnownTypes(xmlQualifiedName);
		}
		else
		{
			Type type = _dataContractResolver.ResolveName(typeName, typeNs, declaredType, KnownTypeResolver);
			dataContract = ((type == null) ? null : GetDataContract(type));
		}
		if (dataContract == null)
		{
			if (memberTypeContract != null && !memberTypeContract.UnderlyingType.IsInterface && memberTypeContract.StableName == xmlQualifiedName)
			{
				dataContract = memberTypeContract;
			}
			if (dataContract == null && rootTypeDataContract != null)
			{
				dataContract = ((!(rootTypeDataContract.StableName == xmlQualifiedName)) ? ResolveDataContractFromRootDataContract(xmlQualifiedName) : rootTypeDataContract);
			}
		}
		return dataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual DataContract ResolveDataContractFromRootDataContract(XmlQualifiedName typeQName)
	{
		CollectionDataContract collectionDataContract = rootTypeDataContract as CollectionDataContract;
		while (collectionDataContract != null)
		{
			DataContract dataContract = GetDataContract(GetSurrogatedType(collectionDataContract.ItemType));
			if (dataContract.StableName == typeQName)
			{
				return dataContract;
			}
			collectionDataContract = dataContract as CollectionDataContract;
		}
		return null;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void PushKnownTypes(DataContract dc)
	{
		if (dc != null && dc.KnownDataContracts != null)
		{
			scopedKnownTypes.Push(dc.KnownDataContracts);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void PopKnownTypes(DataContract dc)
	{
		if (dc != null && dc.KnownDataContracts != null)
		{
			scopedKnownTypes.Pop();
		}
	}
}
