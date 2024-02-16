namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class NameInfo
{
	internal string _fullName;

	internal long _objectId;

	internal long _assemId;

	internal InternalPrimitiveTypeE _primitiveTypeEnum;

	internal Type _type;

	internal bool _isSealed;

	internal bool _isArray;

	internal bool _isArrayItem;

	internal bool _transmitTypeOnObject;

	internal bool _transmitTypeOnMember;

	internal bool _isParentTypeOnObject;

	internal InternalArrayTypeE _arrayEnum;

	private bool _sealedStatusChecked;

	public bool IsSealed
	{
		get
		{
			if (!_sealedStatusChecked)
			{
				_isSealed = _type.IsSealed;
				_sealedStatusChecked = true;
			}
			return _isSealed;
		}
	}

	public string NIname
	{
		get
		{
			return _fullName ?? (_fullName = _type?.FullName);
		}
		set
		{
			_fullName = value;
		}
	}

	internal NameInfo()
	{
	}

	internal void Init()
	{
		_fullName = null;
		_objectId = 0L;
		_assemId = 0L;
		_primitiveTypeEnum = InternalPrimitiveTypeE.Invalid;
		_type = null;
		_isSealed = false;
		_transmitTypeOnObject = false;
		_transmitTypeOnMember = false;
		_isParentTypeOnObject = false;
		_isArray = false;
		_isArrayItem = false;
		_arrayEnum = InternalArrayTypeE.Empty;
		_sealedStatusChecked = false;
	}
}
