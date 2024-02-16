using System.Diagnostics;

namespace System.Runtime.Versioning;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
[Conditional("RESOURCE_ANNOTATION_WORK")]
public sealed class ResourceConsumptionAttribute : Attribute
{
	public ResourceScope ResourceScope { get; }

	public ResourceScope ConsumptionScope { get; }

	public ResourceConsumptionAttribute(ResourceScope resourceScope)
	{
		ResourceScope = resourceScope;
		ConsumptionScope = resourceScope;
	}

	public ResourceConsumptionAttribute(ResourceScope resourceScope, ResourceScope consumptionScope)
	{
		ResourceScope = resourceScope;
		ConsumptionScope = consumptionScope;
	}
}
