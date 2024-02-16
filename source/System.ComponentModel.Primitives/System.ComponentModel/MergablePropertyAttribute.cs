using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class MergablePropertyAttribute : Attribute
{
	public static readonly MergablePropertyAttribute Yes = new MergablePropertyAttribute(allowMerge: true);

	public static readonly MergablePropertyAttribute No = new MergablePropertyAttribute(allowMerge: false);

	public static readonly MergablePropertyAttribute Default = Yes;

	public bool AllowMerge { get; }

	public MergablePropertyAttribute(bool allowMerge)
	{
		AllowMerge = allowMerge;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is MergablePropertyAttribute mergablePropertyAttribute)
		{
			return mergablePropertyAttribute.AllowMerge == AllowMerge;
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
