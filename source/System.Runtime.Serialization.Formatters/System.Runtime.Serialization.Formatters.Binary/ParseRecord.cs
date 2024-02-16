namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ParseRecord
{
	internal InternalParseTypeE _parseTypeEnum;

	internal InternalObjectTypeE _objectTypeEnum;

	internal InternalArrayTypeE _arrayTypeEnum;

	internal InternalMemberTypeE _memberTypeEnum;

	internal InternalMemberValueE _memberValueEnum;

	internal InternalObjectPositionE _objectPositionEnum;

	internal string _name;

	internal string _value;

	internal object _varValue;

	internal string _keyDt;

	internal Type _dtType;

	internal InternalPrimitiveTypeE _dtTypeCode;

	internal long _objectId;

	internal long _idRef;

	internal string _arrayElementTypeString;

	internal Type _arrayElementType;

	internal bool _isArrayVariant;

	internal InternalPrimitiveTypeE _arrayElementTypeCode;

	internal int _rank;

	internal int[] _lengthA;

	internal int[] _lowerBoundA;

	internal int[] _indexMap;

	internal int _memberIndex;

	internal int _linearlength;

	internal int[] _rectangularMap;

	internal bool _isLowerBound;

	internal ReadObjectInfo _objectInfo;

	internal bool _isValueTypeFixup;

	internal object _newObj;

	internal object[] _objectA;

	internal PrimitiveArray _primitiveArray;

	internal bool _isRegistered;

	internal object[] _memberData;

	internal SerializationInfo _si;

	internal int _consecutiveNullArrayEntryCount;

	internal ParseRecord()
	{
	}

	internal void Init()
	{
		_parseTypeEnum = InternalParseTypeE.Empty;
		_objectTypeEnum = InternalObjectTypeE.Empty;
		_arrayTypeEnum = InternalArrayTypeE.Empty;
		_memberTypeEnum = InternalMemberTypeE.Empty;
		_memberValueEnum = InternalMemberValueE.Empty;
		_objectPositionEnum = InternalObjectPositionE.Empty;
		_name = null;
		_value = null;
		_keyDt = null;
		_dtType = null;
		_dtTypeCode = InternalPrimitiveTypeE.Invalid;
		_objectId = 0L;
		_idRef = 0L;
		_arrayElementTypeString = null;
		_arrayElementType = null;
		_isArrayVariant = false;
		_arrayElementTypeCode = InternalPrimitiveTypeE.Invalid;
		_rank = 0;
		_lengthA = null;
		_lowerBoundA = null;
		_indexMap = null;
		_memberIndex = 0;
		_linearlength = 0;
		_rectangularMap = null;
		_isLowerBound = false;
		_isValueTypeFixup = false;
		_newObj = null;
		_objectA = null;
		_primitiveArray = null;
		_objectInfo = null;
		_isRegistered = false;
		_memberData = null;
		_si = null;
		_consecutiveNullArrayEntryCount = 0;
	}
}
