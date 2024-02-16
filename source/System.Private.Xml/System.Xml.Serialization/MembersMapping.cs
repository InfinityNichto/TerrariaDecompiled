namespace System.Xml.Serialization;

internal sealed class MembersMapping : TypeMapping
{
	private MemberMapping[] _members;

	private bool _hasWrapperElement = true;

	private bool _validateRpcWrapperElement;

	private bool _writeAccessors = true;

	private MemberMapping _xmlnsMember;

	internal MemberMapping[] Members
	{
		get
		{
			return _members;
		}
		set
		{
			_members = value;
		}
	}

	internal MemberMapping XmlnsMember
	{
		get
		{
			return _xmlnsMember;
		}
		set
		{
			_xmlnsMember = value;
		}
	}

	internal bool HasWrapperElement
	{
		get
		{
			return _hasWrapperElement;
		}
		set
		{
			_hasWrapperElement = value;
		}
	}

	internal bool ValidateRpcWrapperElement
	{
		get
		{
			return _validateRpcWrapperElement;
		}
		set
		{
			_validateRpcWrapperElement = value;
		}
	}

	internal bool WriteAccessors
	{
		get
		{
			return _writeAccessors;
		}
		set
		{
			_writeAccessors = value;
		}
	}
}
