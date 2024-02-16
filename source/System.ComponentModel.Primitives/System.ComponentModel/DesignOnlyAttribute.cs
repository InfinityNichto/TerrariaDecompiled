using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class DesignOnlyAttribute : Attribute
{
	public static readonly DesignOnlyAttribute Yes = new DesignOnlyAttribute(isDesignOnly: true);

	public static readonly DesignOnlyAttribute No = new DesignOnlyAttribute(isDesignOnly: false);

	public static readonly DesignOnlyAttribute Default = No;

	public bool IsDesignOnly { get; }

	public DesignOnlyAttribute(bool isDesignOnly)
	{
		IsDesignOnly = isDesignOnly;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is DesignOnlyAttribute designOnlyAttribute)
		{
			return designOnlyAttribute.IsDesignOnly == IsDesignOnly;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return IsDesignOnly.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return IsDesignOnly == Default.IsDesignOnly;
	}
}
