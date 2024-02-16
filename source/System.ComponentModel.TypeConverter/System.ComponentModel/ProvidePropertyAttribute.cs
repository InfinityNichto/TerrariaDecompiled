using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ProvidePropertyAttribute : Attribute
{
	public string PropertyName { get; }

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public string ReceiverTypeName { get; }

	public override object TypeId => GetType().FullName + PropertyName;

	public ProvidePropertyAttribute(string propertyName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type receiverType)
	{
		if (receiverType == null)
		{
			throw new ArgumentNullException("receiverType");
		}
		PropertyName = propertyName;
		ReceiverTypeName = receiverType.AssemblyQualifiedName;
	}

	public ProvidePropertyAttribute(string propertyName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string receiverTypeName)
	{
		PropertyName = propertyName;
		ReceiverTypeName = receiverTypeName;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is ProvidePropertyAttribute providePropertyAttribute && providePropertyAttribute.PropertyName == PropertyName)
		{
			return providePropertyAttribute.ReceiverTypeName == ReceiverTypeName;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(PropertyName, ReceiverTypeName);
	}
}
