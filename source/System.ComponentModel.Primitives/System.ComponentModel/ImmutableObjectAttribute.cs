using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class ImmutableObjectAttribute : Attribute
{
	public static readonly ImmutableObjectAttribute Yes = new ImmutableObjectAttribute(immutable: true);

	public static readonly ImmutableObjectAttribute No = new ImmutableObjectAttribute(immutable: false);

	public static readonly ImmutableObjectAttribute Default = No;

	public bool Immutable { get; }

	public ImmutableObjectAttribute(bool immutable)
	{
		Immutable = immutable;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ImmutableObjectAttribute immutableObjectAttribute)
		{
			return immutableObjectAttribute.Immutable == Immutable;
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
