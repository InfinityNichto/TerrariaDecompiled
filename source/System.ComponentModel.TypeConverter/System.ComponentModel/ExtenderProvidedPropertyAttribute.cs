using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class ExtenderProvidedPropertyAttribute : Attribute
{
	public PropertyDescriptor? ExtenderProperty { get; private set; }

	public IExtenderProvider? Provider { get; private set; }

	public Type? ReceiverType { get; private set; }

	internal static ExtenderProvidedPropertyAttribute Create(PropertyDescriptor extenderProperty, Type receiverType, IExtenderProvider provider)
	{
		return new ExtenderProvidedPropertyAttribute
		{
			ExtenderProperty = extenderProperty,
			ReceiverType = receiverType,
			Provider = provider
		};
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (!(obj is ExtenderProvidedPropertyAttribute extenderProvidedPropertyAttribute))
		{
			return false;
		}
		if (extenderProvidedPropertyAttribute.ExtenderProperty == null)
		{
			return ExtenderProperty == null;
		}
		if (extenderProvidedPropertyAttribute.ExtenderProperty.Equals(ExtenderProperty) && extenderProvidedPropertyAttribute.Provider.Equals(Provider))
		{
			return extenderProvidedPropertyAttribute.ReceiverType.Equals(ReceiverType);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return ReceiverType == null;
	}
}
