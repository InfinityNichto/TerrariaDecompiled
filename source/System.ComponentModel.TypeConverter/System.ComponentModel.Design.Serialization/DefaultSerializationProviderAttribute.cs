namespace System.ComponentModel.Design.Serialization;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DefaultSerializationProviderAttribute : Attribute
{
	public string ProviderTypeName { get; }

	public DefaultSerializationProviderAttribute(Type providerType)
	{
		if (providerType == null)
		{
			throw new ArgumentNullException("providerType");
		}
		ProviderTypeName = providerType.AssemblyQualifiedName;
	}

	public DefaultSerializationProviderAttribute(string providerTypeName)
	{
		if (providerTypeName == null)
		{
			throw new ArgumentNullException("providerTypeName");
		}
		ProviderTypeName = providerTypeName;
	}
}
