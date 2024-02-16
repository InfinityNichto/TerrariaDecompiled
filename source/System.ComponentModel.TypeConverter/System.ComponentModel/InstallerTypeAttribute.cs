using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class)]
public class InstallerTypeAttribute : Attribute
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private readonly string _typeName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public virtual Type? InstallerType => Type.GetType(_typeName);

	public InstallerTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type installerType)
	{
		if (installerType == null)
		{
			throw new ArgumentNullException("installerType");
		}
		_typeName = installerType.AssemblyQualifiedName;
	}

	public InstallerTypeAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string? typeName)
	{
		_typeName = typeName;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is InstallerTypeAttribute installerTypeAttribute)
		{
			return installerTypeAttribute._typeName == _typeName;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
