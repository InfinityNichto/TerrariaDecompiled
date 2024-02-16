using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class ValueFixup
{
	internal ValueFixupEnum _valueFixupEnum;

	internal Array _arrayObj;

	internal int[] _indexMap;

	internal object _memberObject;

	internal ReadObjectInfo _objectInfo;

	internal string _memberName;

	internal ValueFixup(Array arrayObj, int[] indexMap)
	{
		_valueFixupEnum = ValueFixupEnum.Array;
		_arrayObj = arrayObj;
		_indexMap = indexMap;
	}

	internal ValueFixup(object memberObject, string memberName, ReadObjectInfo objectInfo)
	{
		_valueFixupEnum = ValueFixupEnum.Member;
		_memberObject = memberObject;
		_memberName = memberName;
		_objectInfo = objectInfo;
	}

	internal void Fixup(ParseRecord record, ParseRecord parent)
	{
		object newObj = record._newObj;
		switch (_valueFixupEnum)
		{
		case ValueFixupEnum.Array:
			_arrayObj.SetValue(newObj, _indexMap);
			break;
		case ValueFixupEnum.Header:
			throw new PlatformNotSupportedException();
		case ValueFixupEnum.Member:
		{
			if (_objectInfo._isSi)
			{
				_objectInfo._objectManager.RecordDelayedFixup(parent._objectId, _memberName, record._objectId);
				break;
			}
			MemberInfo memberInfo = _objectInfo.GetMemberInfo(_memberName);
			if (memberInfo != null)
			{
				_objectInfo._objectManager.RecordFixup(parent._objectId, memberInfo, record._objectId);
			}
			break;
		}
		}
	}
}
