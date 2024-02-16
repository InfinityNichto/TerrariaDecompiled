using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class LocalizableAttribute : Attribute
{
	public static readonly LocalizableAttribute Yes = new LocalizableAttribute(isLocalizable: true);

	public static readonly LocalizableAttribute No = new LocalizableAttribute(isLocalizable: false);

	public static readonly LocalizableAttribute Default = No;

	public bool IsLocalizable { get; }

	public LocalizableAttribute(bool isLocalizable)
	{
		IsLocalizable = isLocalizable;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is LocalizableAttribute localizableAttribute)
		{
			return localizableAttribute.IsLocalizable == IsLocalizable;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return IsLocalizable == Default.IsLocalizable;
	}
}
