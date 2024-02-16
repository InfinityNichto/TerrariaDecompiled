using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class MetadataTypeAttribute : Attribute
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private readonly Type _metadataClassType;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public Type MetadataClassType
	{
		get
		{
			if (_metadataClassType == null)
			{
				throw new InvalidOperationException(System.SR.MetadataTypeAttribute_TypeCannotBeNull);
			}
			return _metadataClassType;
		}
	}

	public MetadataTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type metadataClassType)
	{
		_metadataClassType = metadataClassType;
	}
}
