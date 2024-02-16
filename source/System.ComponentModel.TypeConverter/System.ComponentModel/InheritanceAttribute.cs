using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event)]
public sealed class InheritanceAttribute : Attribute
{
	public static readonly InheritanceAttribute Inherited = new InheritanceAttribute(InheritanceLevel.Inherited);

	public static readonly InheritanceAttribute InheritedReadOnly = new InheritanceAttribute(InheritanceLevel.InheritedReadOnly);

	public static readonly InheritanceAttribute NotInherited = new InheritanceAttribute(InheritanceLevel.NotInherited);

	public static readonly InheritanceAttribute Default = NotInherited;

	public InheritanceLevel InheritanceLevel { get; }

	public InheritanceAttribute()
	{
		InheritanceLevel = Default.InheritanceLevel;
	}

	public InheritanceAttribute(InheritanceLevel inheritanceLevel)
	{
		InheritanceLevel = inheritanceLevel;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value == this)
		{
			return true;
		}
		if (!(value is InheritanceAttribute))
		{
			return false;
		}
		InheritanceLevel inheritanceLevel = ((InheritanceAttribute)value).InheritanceLevel;
		return inheritanceLevel == InheritanceLevel;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return Equals(Default);
	}

	public override string ToString()
	{
		return TypeDescriptor.GetConverterTrimUnsafe(typeof(InheritanceLevel)).ConvertToString(InheritanceLevel);
	}
}
