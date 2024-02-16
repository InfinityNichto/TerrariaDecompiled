using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class DataContractSet
{
	private Dictionary<XmlQualifiedName, DataContract> _contracts;

	private Dictionary<DataContract, object> _processedContracts;

	private readonly ICollection<Type> _referencedTypes;

	private readonly ICollection<Type> _referencedCollectionTypes;

	private Dictionary<XmlQualifiedName, DataContract> Contracts
	{
		get
		{
			if (_contracts == null)
			{
				_contracts = new Dictionary<XmlQualifiedName, DataContract>();
			}
			return _contracts;
		}
	}

	private Dictionary<DataContract, object> ProcessedContracts
	{
		get
		{
			if (_processedContracts == null)
			{
				_processedContracts = new Dictionary<DataContract, object>();
			}
			return _processedContracts;
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContractSet(DataContractSet dataContractSet)
	{
		if (dataContractSet == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("dataContractSet"));
		}
		_referencedTypes = dataContractSet._referencedTypes;
		_referencedCollectionTypes = dataContractSet._referencedCollectionTypes;
		foreach (KeyValuePair<XmlQualifiedName, DataContract> item in dataContractSet)
		{
			Add(item.Key, item.Value);
		}
		if (dataContractSet._processedContracts == null)
		{
			return;
		}
		foreach (KeyValuePair<DataContract, object> processedContract in dataContractSet._processedContracts)
		{
			ProcessedContracts.Add(processedContract.Key, processedContract.Value);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void Add(Type type)
	{
		DataContract dataContract = GetDataContract(type);
		EnsureTypeNotGeneric(dataContract.UnderlyingType);
		Add(dataContract);
	}

	internal static void EnsureTypeNotGeneric(Type type)
	{
		if (type.ContainsGenericParameters)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(System.SR.Format(System.SR.GenericTypeNotExportable, type)));
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void Add(DataContract dataContract)
	{
		Add(dataContract.StableName, dataContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void Add(XmlQualifiedName name, DataContract dataContract)
	{
		if (!dataContract.IsBuiltInDataContract)
		{
			InternalAdd(name, dataContract);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void InternalAdd(XmlQualifiedName name, DataContract dataContract)
	{
		DataContract value = null;
		if (Contracts.TryGetValue(name, out value))
		{
			if (!value.Equals(dataContract))
			{
				if (dataContract.UnderlyingType == null || value.UnderlyingType == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.DupContractInDataContractSet, dataContract.StableName.Name, dataContract.StableName.Namespace)));
				}
				bool flag = DataContract.GetClrTypeFullName(dataContract.UnderlyingType) == DataContract.GetClrTypeFullName(value.UnderlyingType);
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.DupTypeContractInDataContractSet, flag ? dataContract.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(dataContract.UnderlyingType), flag ? value.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(value.UnderlyingType), dataContract.StableName.Name, dataContract.StableName.Namespace)));
			}
		}
		else
		{
			Contracts.Add(name, dataContract);
			if (dataContract is ClassDataContract)
			{
				AddClassDataContract((ClassDataContract)dataContract);
			}
			else if (dataContract is CollectionDataContract)
			{
				AddCollectionDataContract((CollectionDataContract)dataContract);
			}
			else if (dataContract is XmlDataContract)
			{
				AddXmlDataContract((XmlDataContract)dataContract);
			}
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddClassDataContract(ClassDataContract classDataContract)
	{
		if (classDataContract.BaseContract != null)
		{
			Add(classDataContract.BaseContract.StableName, classDataContract.BaseContract);
		}
		if (!classDataContract.IsISerializable && classDataContract.Members != null)
		{
			for (int i = 0; i < classDataContract.Members.Count; i++)
			{
				DataMember dataMember = classDataContract.Members[i];
				DataContract memberTypeDataContract = GetMemberTypeDataContract(dataMember);
				Add(memberTypeDataContract.StableName, memberTypeDataContract);
			}
		}
		AddKnownDataContracts(classDataContract.KnownDataContracts);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddCollectionDataContract(CollectionDataContract collectionDataContract)
	{
		if (collectionDataContract.IsDictionary)
		{
			ClassDataContract classDataContract = collectionDataContract.ItemContract as ClassDataContract;
			AddClassDataContract(classDataContract);
		}
		else
		{
			DataContract itemTypeDataContract = GetItemTypeDataContract(collectionDataContract);
			if (itemTypeDataContract != null)
			{
				Add(itemTypeDataContract.StableName, itemTypeDataContract);
			}
		}
		AddKnownDataContracts(collectionDataContract.KnownDataContracts);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddXmlDataContract(XmlDataContract xmlDataContract)
	{
		AddKnownDataContracts(xmlDataContract.KnownDataContracts);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddKnownDataContracts(Dictionary<XmlQualifiedName, DataContract> knownDataContracts)
	{
		if (knownDataContracts == null)
		{
			return;
		}
		foreach (DataContract value in knownDataContracts.Values)
		{
			Add(value);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContract GetDataContract(Type clrType)
	{
		DataContract builtInDataContract = DataContract.GetBuiltInDataContract(clrType);
		if (builtInDataContract != null)
		{
			return builtInDataContract;
		}
		return DataContract.GetDataContract(clrType);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContract GetMemberTypeDataContract(DataMember dataMember)
	{
		Type memberType = dataMember.MemberType;
		if (dataMember.IsGetOnlyCollection)
		{
			return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle, memberType, SerializationMode.SharedContract);
		}
		return GetDataContract(memberType);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContract GetItemTypeDataContract(CollectionDataContract collectionContract)
	{
		if (collectionContract.ItemType != null)
		{
			return GetDataContract(collectionContract.ItemType);
		}
		return collectionContract.ItemContract;
	}

	public IEnumerator<KeyValuePair<XmlQualifiedName, DataContract>> GetEnumerator()
	{
		return Contracts.GetEnumerator();
	}

	internal bool IsContractProcessed(DataContract dataContract)
	{
		return ProcessedContracts.ContainsKey(dataContract);
	}

	internal void SetContractProcessed(DataContract dataContract)
	{
		ProcessedContracts.Add(dataContract, dataContract);
	}
}
