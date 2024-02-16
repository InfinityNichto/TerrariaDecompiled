using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SwitchLevelAttribute : Attribute
{
	private Type _type;

	public Type SwitchLevelType
	{
		get
		{
			return _type;
		}
		[MemberNotNull("_type")]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_type = value;
		}
	}

	public SwitchLevelAttribute(Type switchLevelType)
	{
		SwitchLevelType = switchLevelType;
	}
}
