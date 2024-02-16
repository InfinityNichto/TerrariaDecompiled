using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations;

public class AssociatedMetadataTypeTypeDescriptionProvider : TypeDescriptionProvider
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private readonly Type _associatedMetadataType;

	public AssociatedMetadataTypeTypeDescriptionProvider(Type type)
		: base(TypeDescriptor.GetProvider(type))
	{
	}

	public AssociatedMetadataTypeTypeDescriptionProvider(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type associatedMetadataType)
		: this(type)
	{
		if (associatedMetadataType == null)
		{
			throw new ArgumentNullException("associatedMetadataType");
		}
		_associatedMetadataType = associatedMetadataType;
	}

	public override ICustomTypeDescriptor GetTypeDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type objectType, object? instance)
	{
		ICustomTypeDescriptor typeDescriptor = base.GetTypeDescriptor(objectType, instance);
		return new AssociatedMetadataTypeTypeDescriptor(typeDescriptor, objectType, _associatedMetadataType);
	}
}
