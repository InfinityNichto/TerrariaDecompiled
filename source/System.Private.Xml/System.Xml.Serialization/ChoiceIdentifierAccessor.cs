using System.Reflection;

namespace System.Xml.Serialization;

internal sealed class ChoiceIdentifierAccessor : Accessor
{
	private string _memberName;

	private string[] _memberIds;

	private MemberInfo _memberInfo;

	internal string MemberName
	{
		get
		{
			return _memberName;
		}
		set
		{
			_memberName = value;
		}
	}

	internal string[] MemberIds
	{
		get
		{
			return _memberIds;
		}
		set
		{
			_memberIds = value;
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
}
