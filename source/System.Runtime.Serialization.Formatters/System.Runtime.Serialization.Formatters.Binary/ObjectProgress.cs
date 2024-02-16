namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ObjectProgress
{
	internal bool _isInitial;

	internal int _count;

	internal BinaryTypeEnum _expectedType = BinaryTypeEnum.ObjectUrt;

	internal object _expectedTypeInformation;

	internal string _name;

	internal InternalObjectTypeE _objectTypeEnum;

	internal InternalMemberTypeE _memberTypeEnum;

	internal InternalMemberValueE _memberValueEnum;

	internal Type _dtType;

	internal int _numItems;

	internal BinaryTypeEnum _binaryTypeEnum;

	internal object _typeInformation;

	internal int _memberLength;

	internal BinaryTypeEnum[] _binaryTypeEnumA;

	internal object[] _typeInformationA;

	internal string[] _memberNames;

	internal Type[] _memberTypes;

	internal ParseRecord _pr = new ParseRecord();

	internal ObjectProgress()
	{
	}

	internal void Init()
	{
		_isInitial = false;
		_count = 0;
		_expectedType = BinaryTypeEnum.ObjectUrt;
		_expectedTypeInformation = null;
		_name = null;
		_objectTypeEnum = InternalObjectTypeE.Empty;
		_memberTypeEnum = InternalMemberTypeE.Empty;
		_memberValueEnum = InternalMemberValueE.Empty;
		_dtType = null;
		_numItems = 0;
		_typeInformation = null;
		_memberLength = 0;
		_binaryTypeEnumA = null;
		_typeInformationA = null;
		_memberNames = null;
		_memberTypes = null;
		_pr.Init();
	}

	internal void ArrayCountIncrement(int value)
	{
		_count += value;
	}

	internal bool GetNext(out BinaryTypeEnum outBinaryTypeEnum, out object outTypeInformation)
	{
		outBinaryTypeEnum = BinaryTypeEnum.Primitive;
		outTypeInformation = null;
		if (_objectTypeEnum == InternalObjectTypeE.Array)
		{
			if (_count == _numItems)
			{
				return false;
			}
			outBinaryTypeEnum = _binaryTypeEnum;
			outTypeInformation = _typeInformation;
			if (_count == 0)
			{
				_isInitial = false;
			}
			_count++;
			return true;
		}
		if (_count == _memberLength && !_isInitial)
		{
			return false;
		}
		outBinaryTypeEnum = _binaryTypeEnumA[_count];
		outTypeInformation = _typeInformationA[_count];
		if (_count == 0)
		{
			_isInitial = false;
		}
		_name = _memberNames[_count];
		_dtType = _memberTypes[_count];
		_count++;
		return true;
	}
}
