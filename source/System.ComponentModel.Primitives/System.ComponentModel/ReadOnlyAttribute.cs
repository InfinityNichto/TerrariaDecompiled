using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class ReadOnlyAttribute : Attribute
{
	public static readonly ReadOnlyAttribute Yes = new ReadOnlyAttribute(isReadOnly: true);

	public static readonly ReadOnlyAttribute No = new ReadOnlyAttribute(isReadOnly: false);

	public static readonly ReadOnlyAttribute Default = No;

	public bool IsReadOnly { get; }

	public ReadOnlyAttribute(bool isReadOnly)
	{
		IsReadOnly = isReadOnly;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is ReadOnlyAttribute readOnlyAttribute)
		{
			return readOnlyAttribute.IsReadOnly == IsReadOnly;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return IsReadOnly == Default.IsReadOnly;
	}
}
