using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
public sealed class DesignerSerializationVisibilityAttribute : Attribute
{
	public static readonly DesignerSerializationVisibilityAttribute Content = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content);

	public static readonly DesignerSerializationVisibilityAttribute Hidden = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden);

	public static readonly DesignerSerializationVisibilityAttribute Visible = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible);

	public static readonly DesignerSerializationVisibilityAttribute Default = Visible;

	public DesignerSerializationVisibility Visibility { get; }

	public DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility visibility)
	{
		Visibility = visibility;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is DesignerSerializationVisibilityAttribute designerSerializationVisibilityAttribute)
		{
			return designerSerializationVisibilityAttribute.Visibility == Visibility;
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
