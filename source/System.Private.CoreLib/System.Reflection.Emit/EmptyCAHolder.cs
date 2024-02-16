namespace System.Reflection.Emit;

internal sealed class EmptyCAHolder : ICustomAttributeProvider
{
	internal EmptyCAHolder()
	{
	}

	object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit)
	{
		return Array.Empty<object>();
	}

	object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit)
	{
		return Array.Empty<object>();
	}

	bool ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit)
	{
		return false;
	}
}
