using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
public class XmlChoiceIdentifierAttribute : Attribute
{
	private string _name;

	private MemberInfo _memberInfo;

	public string MemberName
	{
		get
		{
			if (_name != null)
			{
				return _name;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			_name = value;
		}
	}

	internal MemberInfo? MemberInfo
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

	public XmlChoiceIdentifierAttribute()
	{
	}

	public XmlChoiceIdentifierAttribute(string? name)
	{
		_name = name;
	}

	internal MemberInfo GetMemberInfo()
	{
		return MemberInfo;
	}

	internal void SetMemberInfo(MemberInfo memberInfo)
	{
		MemberInfo = memberInfo;
	}
}
