using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class RefreshPropertiesAttribute : Attribute
{
	public static readonly RefreshPropertiesAttribute All = new RefreshPropertiesAttribute(RefreshProperties.All);

	public static readonly RefreshPropertiesAttribute Repaint = new RefreshPropertiesAttribute(RefreshProperties.Repaint);

	public static readonly RefreshPropertiesAttribute Default = new RefreshPropertiesAttribute(RefreshProperties.None);

	public RefreshProperties RefreshProperties { get; }

	public RefreshPropertiesAttribute(RefreshProperties refresh)
	{
		RefreshProperties = refresh;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is RefreshPropertiesAttribute refreshPropertiesAttribute)
		{
			return refreshPropertiesAttribute.RefreshProperties == RefreshProperties;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return Equals(Default);
	}
}
