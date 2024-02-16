using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization;

internal class XmlObjectSerializerReadContextComplex : XmlObjectSerializerReadContext
{
	private readonly bool _preserveObjectReferences;

	private readonly SerializationMode _mode;

	private readonly ISerializationSurrogateProvider _serializationSurrogateProvider;

	internal override SerializationMode Mode => _mode;

	internal XmlObjectSerializerReadContextComplex(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
		: base(serializer, rootTypeDataContract, dataContractResolver)
	{
		_mode = SerializationMode.SharedContract;
		_preserveObjectReferences = serializer.PreserveObjectReferences;
		_serializationSurrogateProvider = serializer.SerializationSurrogateProvider;
	}

	internal XmlObjectSerializerReadContextComplex(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
		: base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalDeserialize(XmlReaderDelegator xmlReader, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, string name, string ns)
	{
		if (_mode == SerializationMode.SharedContract)
		{
			if (_serializationSurrogateProvider == null)
			{
				return base.InternalDeserialize(xmlReader, declaredTypeID, declaredTypeHandle, name, ns);
			}
			return InternalDeserializeWithSurrogate(xmlReader, Type.GetTypeFromHandle(declaredTypeHandle), null, name, ns);
		}
		return InternalDeserializeInSharedTypeMode(xmlReader, declaredTypeID, Type.GetTypeFromHandle(declaredTypeHandle), name, ns);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, string name, string ns)
	{
		if (_mode == SerializationMode.SharedContract)
		{
			if (_serializationSurrogateProvider == null)
			{
				return base.InternalDeserialize(xmlReader, declaredType, name, ns);
			}
			return InternalDeserializeWithSurrogate(xmlReader, declaredType, null, name, ns);
		}
		return InternalDeserializeInSharedTypeMode(xmlReader, -1, declaredType, name, ns);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, DataContract dataContract, string name, string ns)
	{
		if (_mode == SerializationMode.SharedContract)
		{
			if (_serializationSurrogateProvider == null)
			{
				return base.InternalDeserialize(xmlReader, declaredType, dataContract, name, ns);
			}
			return InternalDeserializeWithSurrogate(xmlReader, declaredType, dataContract, name, ns);
		}
		return InternalDeserializeInSharedTypeMode(xmlReader, -1, declaredType, name, ns);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object InternalDeserializeInSharedTypeMode(XmlReaderDelegator xmlReader, int declaredTypeID, Type declaredType, string name, string ns)
	{
		object retObj = null;
		if (TryHandleNullOrRef(xmlReader, declaredType, name, ns, ref retObj))
		{
			return retObj;
		}
		string clrAssembly = attributes.ClrAssembly;
		string clrType = attributes.ClrType;
		DataContract dataContract2;
		if (clrAssembly != null && clrType != null)
		{
			Assembly assembly;
			Type type;
			DataContract dataContract = ResolveDataContractInSharedTypeMode(clrAssembly, clrType, out assembly, out type);
			if (dataContract == null)
			{
				if (assembly == null)
				{
					throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.AssemblyNotFound, clrAssembly));
				}
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ClrTypeNotFound, assembly.FullName, clrType));
			}
			dataContract2 = dataContract;
			if (declaredType != null && declaredType.IsArray)
			{
				dataContract2 = ((declaredTypeID < 0) ? GetDataContract(declaredType) : GetDataContract(declaredTypeID, declaredType.TypeHandle));
			}
		}
		else
		{
			if (clrAssembly != null)
			{
				throw XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, System.SR.Format(System.SR.AttributeNotFound, "http://schemas.microsoft.com/2003/10/Serialization/", "Type", xmlReader.NodeType, xmlReader.NamespaceURI, xmlReader.LocalName)));
			}
			if (clrType != null)
			{
				throw XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, System.SR.Format(System.SR.AttributeNotFound, "http://schemas.microsoft.com/2003/10/Serialization/", "Assembly", xmlReader.NodeType, xmlReader.NamespaceURI, xmlReader.LocalName)));
			}
			if (declaredType == null)
			{
				throw XmlObjectSerializer.CreateSerializationException(XmlObjectSerializer.TryAddLineInfo(xmlReader, System.SR.Format(System.SR.AttributeNotFound, "http://schemas.microsoft.com/2003/10/Serialization/", "Type", xmlReader.NodeType, xmlReader.NamespaceURI, xmlReader.LocalName)));
			}
			dataContract2 = ((declaredTypeID < 0) ? GetDataContract(declaredType) : GetDataContract(declaredTypeID, declaredType.TypeHandle));
		}
		return ReadDataContractValue(dataContract2, xmlReader);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object InternalDeserializeWithSurrogate(XmlReaderDelegator xmlReader, Type declaredType, DataContract surrogateDataContract, string name, string ns)
	{
		DataContract dataContract = surrogateDataContract ?? GetDataContract(DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, declaredType));
		if (IsGetOnlyCollection && dataContract.UnderlyingType != declaredType)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser, DataContract.GetClrTypeFullName(declaredType))));
		}
		ReadAttributes(xmlReader);
		string objectId = GetObjectId();
		object obj = InternalDeserialize(xmlReader, name, ns, declaredType, ref dataContract);
		object deserializedObject = DataContractSurrogateCaller.GetDeserializedObject(_serializationSurrogateProvider, obj, dataContract.UnderlyingType, declaredType);
		ReplaceDeserializedObject(objectId, obj, deserializedObject);
		return deserializedObject;
	}

	private Type ResolveDataContractTypeInSharedTypeMode(string assemblyName, string typeName, out Assembly assembly)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_NetDataContractSerializer);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ResolveDataContractInSharedTypeMode(string assemblyName, string typeName, out Assembly assembly, out Type type)
	{
		type = ResolveDataContractTypeInSharedTypeMode(assemblyName, typeName, out assembly);
		if (type != null)
		{
			return GetDataContract(type);
		}
		return null;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override DataContract ResolveDataContractFromTypeName()
	{
		if (_mode == SerializationMode.SharedContract)
		{
			return base.ResolveDataContractFromTypeName();
		}
		Assembly assembly;
		Type type;
		if (attributes.ClrAssembly != null && attributes.ClrType != null)
		{
			return ResolveDataContractInSharedTypeMode(attributes.ClrAssembly, attributes.ClrType, out assembly, out type);
		}
		return null;
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

	internal override int GetArraySize()
	{
		if (!_preserveObjectReferences)
		{
			return -1;
		}
		return attributes.ArraySZSize;
	}
}
