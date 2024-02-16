namespace System.Reflection;

public readonly struct CustomAttributeNamedArgument
{
	private readonly MemberInfo _memberInfo;

	private readonly CustomAttributeTypedArgument _value;

	internal Type ArgumentType
	{
		get
		{
			if (!(_memberInfo is FieldInfo fieldInfo))
			{
				return ((PropertyInfo)_memberInfo).PropertyType;
			}
			return fieldInfo.FieldType;
		}
	}

	public MemberInfo MemberInfo => _memberInfo;

	public CustomAttributeTypedArgument TypedValue => _value;

	public string MemberName => MemberInfo.Name;

	public bool IsField => MemberInfo is FieldInfo;

	public static bool operator ==(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(CustomAttributeNamedArgument left, CustomAttributeNamedArgument right)
	{
		return !left.Equals(right);
	}

	public CustomAttributeNamedArgument(MemberInfo memberInfo, object? value)
	{
		if ((object)memberInfo == null)
		{
			throw new ArgumentNullException("memberInfo");
		}
		Type type;
		if (!(memberInfo is FieldInfo fieldInfo))
		{
			if (!(memberInfo is PropertyInfo propertyInfo))
			{
				throw new ArgumentException(SR.Argument_InvalidMemberForNamedArgument);
			}
			type = propertyInfo.PropertyType;
		}
		else
		{
			type = fieldInfo.FieldType;
		}
		Type argumentType = type;
		_memberInfo = memberInfo;
		_value = new CustomAttributeTypedArgument(argumentType, value);
	}

	public CustomAttributeNamedArgument(MemberInfo memberInfo, CustomAttributeTypedArgument typedArgument)
	{
		_memberInfo = memberInfo ?? throw new ArgumentNullException("memberInfo");
		_value = typedArgument;
	}

	public override string ToString()
	{
		if ((object)_memberInfo == null)
		{
			return base.ToString();
		}
		return MemberInfo.Name + " = " + TypedValue.ToString(ArgumentType != typeof(object));
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object? obj)
	{
		return obj == (object)this;
	}
}
