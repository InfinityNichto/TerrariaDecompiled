using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotifyParentPropertyAttribute : Attribute
{
	public static readonly NotifyParentPropertyAttribute Yes = new NotifyParentPropertyAttribute(notifyParent: true);

	public static readonly NotifyParentPropertyAttribute No = new NotifyParentPropertyAttribute(notifyParent: false);

	public static readonly NotifyParentPropertyAttribute Default = No;

	public bool NotifyParent { get; }

	public NotifyParentPropertyAttribute(bool notifyParent)
	{
		NotifyParent = notifyParent;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is NotifyParentPropertyAttribute notifyParentPropertyAttribute)
		{
			return notifyParentPropertyAttribute.NotifyParent == NotifyParent;
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
