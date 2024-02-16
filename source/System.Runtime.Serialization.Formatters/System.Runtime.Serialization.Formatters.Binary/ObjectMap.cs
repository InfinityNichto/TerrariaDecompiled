using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ObjectMap
{
	internal string _objectName;

	internal Type _objectType;

	internal BinaryTypeEnum[] _binaryTypeEnumA;

	internal object[] _typeInformationA;

	internal Type[] _memberTypes;

	internal string[] _memberNames;

	internal ReadObjectInfo _objectInfo;

	internal bool _isInitObjectInfo = true;

	internal ObjectReader _objectReader;

	internal int _objectId;

	internal BinaryAssemblyInfo _assemblyInfo;

	internal ObjectMap(string objectName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, string[] memberNames, ObjectReader objectReader, int objectId, BinaryAssemblyInfo assemblyInfo)
	{
		_objectName = objectName;
		_objectType = objectType;
		_memberNames = memberNames;
		_objectReader = objectReader;
		_objectId = objectId;
		_assemblyInfo = assemblyInfo;
		_objectInfo = objectReader.CreateReadObjectInfo(objectType);
		_memberTypes = _objectInfo.GetMemberTypes(memberNames, objectType);
		_binaryTypeEnumA = new BinaryTypeEnum[_memberTypes.Length];
		_typeInformationA = new object[_memberTypes.Length];
		for (int i = 0; i < _memberTypes.Length; i++)
		{
			object typeInformation;
			BinaryTypeEnum parserBinaryTypeInfo = BinaryTypeConverter.GetParserBinaryTypeInfo(_memberTypes[i], out typeInformation);
			_binaryTypeEnumA[i] = parserBinaryTypeInfo;
			_typeInformationA[i] = typeInformation;
		}
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal ObjectMap(string objectName, string[] memberNames, BinaryTypeEnum[] binaryTypeEnumA, object[] typeInformationA, int[] memberAssemIds, ObjectReader objectReader, int objectId, BinaryAssemblyInfo assemblyInfo, SizedArray assemIdToAssemblyTable)
	{
		_objectName = objectName;
		_memberNames = memberNames;
		_binaryTypeEnumA = binaryTypeEnumA;
		_typeInformationA = typeInformationA;
		_objectReader = objectReader;
		_objectId = objectId;
		_assemblyInfo = assemblyInfo;
		if (assemblyInfo == null)
		{
			throw new SerializationException(System.SR.Format(System.SR.Serialization_Assembly, objectName));
		}
		_objectType = objectReader.GetType(assemblyInfo, objectName);
		_memberTypes = new Type[memberNames.Length];
		for (int i = 0; i < memberNames.Length; i++)
		{
			BinaryTypeConverter.TypeFromInfo(binaryTypeEnumA[i], typeInformationA[i], objectReader, (BinaryAssemblyInfo)assemIdToAssemblyTable[memberAssemIds[i]], out var _, out var _, out var type, out var _);
			_memberTypes[i] = type;
		}
		_objectInfo = objectReader.CreateReadObjectInfo(_objectType, memberNames, null);
		if (!_objectInfo._isSi)
		{
			_objectInfo.GetMemberTypes(memberNames, _objectInfo._objectType);
		}
	}

	internal ReadObjectInfo CreateObjectInfo(ref SerializationInfo si, ref object[] memberData)
	{
		if (_isInitObjectInfo)
		{
			_isInitObjectInfo = false;
			_objectInfo.InitDataStore(ref si, ref memberData);
			return _objectInfo;
		}
		_objectInfo.PrepareForReuse();
		_objectInfo.InitDataStore(ref si, ref memberData);
		return _objectInfo;
	}

	internal static ObjectMap Create(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, string[] memberNames, ObjectReader objectReader, int objectId, BinaryAssemblyInfo assemblyInfo)
	{
		return new ObjectMap(name, objectType, memberNames, objectReader, objectId, assemblyInfo);
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal static ObjectMap Create(string name, string[] memberNames, BinaryTypeEnum[] binaryTypeEnumA, object[] typeInformationA, int[] memberAssemIds, ObjectReader objectReader, int objectId, BinaryAssemblyInfo assemblyInfo, SizedArray assemIdToAssemblyTable)
	{
		return new ObjectMap(name, memberNames, binaryTypeEnumA, typeInformationA, memberAssemIds, objectReader, objectId, assemblyInfo, assemIdToAssemblyTable);
	}
}
