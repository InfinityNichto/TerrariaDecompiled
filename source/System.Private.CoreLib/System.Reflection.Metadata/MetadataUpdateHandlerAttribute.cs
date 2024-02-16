using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MetadataUpdateHandlerAttribute : Attribute
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public Type HandlerType { get; }

	public MetadataUpdateHandlerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type handlerType)
	{
		HandlerType = handlerType;
	}
}
