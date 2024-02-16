using System.Reflection;

namespace System.Xml.Serialization;

internal sealed class MemberMapping : AccessorMapping
{
	private string _name;

	private bool _checkShouldPersist;

	private SpecifiedAccessor _checkSpecified;

	private bool _isReturnValue;

	private bool _readOnly;

	private int _sequenceId = -1;

	private MemberInfo _memberInfo;

	private MemberInfo _checkSpecifiedMemberInfo;

	private MethodInfo _checkShouldPersistMethodInfo;

	internal bool CheckShouldPersist
	{
		get
		{
			return _checkShouldPersist;
		}
		set
		{
			_checkShouldPersist = value;
		}
	}

	internal SpecifiedAccessor CheckSpecified
	{
		get
		{
			return _checkSpecified;
		}
		set
		{
			_checkSpecified = value;
		}
	}

	internal string Name
	{
		get
		{
			if (_name != null)
			{
				return _name;
			}
			return string.Empty;
		}
		set
		{
			_name = value;
		}
	}

	internal MemberInfo MemberInfo
	{
		get
		{
			return _memberInfo;
		}
		set
		{
			_memberInfo = value;
		}
	}

	internal MemberInfo CheckSpecifiedMemberInfo
	{
		get
		{
			return _checkSpecifiedMemberInfo;
		}
		set
		{
			_checkSpecifiedMemberInfo = value;
		}
	}

	internal MethodInfo CheckShouldPersistMethodInfo
	{
		get
		{
			return _checkShouldPersistMethodInfo;
		}
		set
		{
			_checkShouldPersistMethodInfo = value;
		}
	}

	internal bool IsReturnValue
	{
		get
		{
			return _isReturnValue;
		}
		set
		{
			_isReturnValue = value;
		}
	}

	internal bool ReadOnly
	{
		get
		{
			return _readOnly;
		}
		set
		{
			_readOnly = value;
		}
	}

	internal bool IsSequence => _sequenceId >= 0;

	internal int SequenceId
	{
		get
		{
			return _sequenceId;
		}
		set
		{
			_sequenceId = value;
		}
	}

	internal MemberMapping()
	{
	}

	private MemberMapping(MemberMapping mapping)
		: base(mapping)
	{
		_name = mapping._name;
		_checkShouldPersist = mapping._checkShouldPersist;
		_checkSpecified = mapping._checkSpecified;
		_isReturnValue = mapping._isReturnValue;
		_readOnly = mapping._readOnly;
		_sequenceId = mapping._sequenceId;
		_memberInfo = mapping._memberInfo;
		_checkSpecifiedMemberInfo = mapping._checkSpecifiedMemberInfo;
		_checkShouldPersistMethodInfo = mapping._checkShouldPersistMethodInfo;
	}

	internal MemberMapping Clone()
	{
		return new MemberMapping(this);
	}
}
