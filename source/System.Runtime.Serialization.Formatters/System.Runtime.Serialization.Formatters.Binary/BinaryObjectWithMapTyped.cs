namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryObjectWithMapTyped : IStreamable
{
	internal BinaryHeaderEnum _binaryHeaderEnum;

	internal int _objectId;

	internal string _name;

	internal int _numMembers;

	internal string[] _memberNames;

	internal BinaryTypeEnum[] _binaryTypeEnumA;

	internal object[] _typeInformationA;

	internal int[] _memberAssemIds;

	internal int _assemId;

	internal BinaryObjectWithMapTyped()
	{
	}

	internal BinaryObjectWithMapTyped(BinaryHeaderEnum binaryHeaderEnum)
	{
		_binaryHeaderEnum = binaryHeaderEnum;
	}

	internal void Set(int objectId, string name, int numMembers, string[] memberNames, BinaryTypeEnum[] binaryTypeEnumA, object[] typeInformationA, int[] memberAssemIds, int assemId)
	{
		_objectId = objectId;
		_assemId = assemId;
		_name = name;
		_numMembers = numMembers;
		_memberNames = memberNames;
		_binaryTypeEnumA = binaryTypeEnumA;
		_typeInformationA = typeInformationA;
		_memberAssemIds = memberAssemIds;
		_assemId = assemId;
		_binaryHeaderEnum = ((assemId > 0) ? BinaryHeaderEnum.ObjectWithMapTypedAssemId : BinaryHeaderEnum.ObjectWithMapTyped);
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte((byte)_binaryHeaderEnum);
		output.WriteInt32(_objectId);
		output.WriteString(_name);
		output.WriteInt32(_numMembers);
		for (int i = 0; i < _numMembers; i++)
		{
			output.WriteString(_memberNames[i]);
		}
		for (int j = 0; j < _numMembers; j++)
		{
			output.WriteByte((byte)_binaryTypeEnumA[j]);
		}
		for (int k = 0; k < _numMembers; k++)
		{
			BinaryTypeConverter.WriteTypeInfo(_binaryTypeEnumA[k], _typeInformationA[k], _memberAssemIds[k], output);
		}
		if (_assemId > 0)
		{
			output.WriteInt32(_assemId);
		}
	}

	public void Read(BinaryParser input)
	{
		_objectId = input.ReadInt32();
		_name = input.ReadString();
		_numMembers = input.ReadInt32();
		_memberNames = new string[_numMembers];
		_binaryTypeEnumA = new BinaryTypeEnum[_numMembers];
		_typeInformationA = new object[_numMembers];
		_memberAssemIds = new int[_numMembers];
		for (int i = 0; i < _numMembers; i++)
		{
			_memberNames[i] = input.ReadString();
		}
		for (int j = 0; j < _numMembers; j++)
		{
			_binaryTypeEnumA[j] = (BinaryTypeEnum)input.ReadByte();
		}
		for (int k = 0; k < _numMembers; k++)
		{
			if (_binaryTypeEnumA[k] != BinaryTypeEnum.ObjectUrt && _binaryTypeEnumA[k] != BinaryTypeEnum.ObjectUser)
			{
				_typeInformationA[k] = BinaryTypeConverter.ReadTypeInfo(_binaryTypeEnumA[k], input, out _memberAssemIds[k]);
			}
			else
			{
				BinaryTypeConverter.ReadTypeInfo(_binaryTypeEnumA[k], input, out _memberAssemIds[k]);
			}
		}
		if (_binaryHeaderEnum == BinaryHeaderEnum.ObjectWithMapTypedAssemId)
		{
			_assemId = input.ReadInt32();
		}
	}
}
