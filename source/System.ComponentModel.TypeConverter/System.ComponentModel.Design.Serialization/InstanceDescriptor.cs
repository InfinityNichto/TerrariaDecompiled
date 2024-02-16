using System.Collections;
using System.Reflection;

namespace System.ComponentModel.Design.Serialization;

public sealed class InstanceDescriptor
{
	public ICollection Arguments { get; }

	public bool IsComplete { get; }

	public MemberInfo? MemberInfo { get; }

	public InstanceDescriptor(MemberInfo? member, ICollection? arguments)
		: this(member, arguments, isComplete: true)
	{
	}

	public InstanceDescriptor(MemberInfo? member, ICollection? arguments, bool isComplete)
	{
		MemberInfo = member;
		IsComplete = isComplete;
		if (arguments == null)
		{
			Arguments = Array.Empty<object>();
		}
		else
		{
			object[] array = new object[arguments.Count];
			arguments.CopyTo(array, 0);
			Arguments = array;
		}
		if (member is FieldInfo fieldInfo)
		{
			if (!fieldInfo.IsStatic)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorMustBeStatic);
			}
			if (Arguments.Count != 0)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorLengthMismatch);
			}
		}
		else if (member is ConstructorInfo constructorInfo)
		{
			if (constructorInfo.IsStatic)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorCannotBeStatic);
			}
			if (Arguments.Count != constructorInfo.GetParameters().Length)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorLengthMismatch);
			}
		}
		else if (member is MethodInfo methodInfo)
		{
			if (!methodInfo.IsStatic)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorMustBeStatic);
			}
			if (Arguments.Count != methodInfo.GetParameters().Length)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorLengthMismatch);
			}
		}
		else if (member is PropertyInfo propertyInfo)
		{
			if (!propertyInfo.CanRead)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorMustBeReadable);
			}
			MethodInfo getMethod = propertyInfo.GetGetMethod();
			if (getMethod != null && !getMethod.IsStatic)
			{
				throw new ArgumentException(System.SR.InstanceDescriptorMustBeStatic);
			}
		}
	}

	public object? Invoke()
	{
		object[] array = new object[Arguments.Count];
		Arguments.CopyTo(array, 0);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is InstanceDescriptor instanceDescriptor)
			{
				array[i] = instanceDescriptor.Invoke();
			}
		}
		if (MemberInfo is ConstructorInfo)
		{
			return ((ConstructorInfo)MemberInfo).Invoke(array);
		}
		if (MemberInfo is MethodInfo)
		{
			return ((MethodInfo)MemberInfo).Invoke(null, array);
		}
		if (MemberInfo is PropertyInfo)
		{
			return ((PropertyInfo)MemberInfo).GetValue(null, array);
		}
		if (MemberInfo is FieldInfo)
		{
			return ((FieldInfo)MemberInfo).GetValue(null);
		}
		return null;
	}
}
