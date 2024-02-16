using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DefaultBindingPropertyAttribute : Attribute
{
	public static readonly DefaultBindingPropertyAttribute Default = new DefaultBindingPropertyAttribute();

	public string? Name { get; }

	public DefaultBindingPropertyAttribute()
	{
	}

	public DefaultBindingPropertyAttribute(string? name)
	{
		Name = name;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is DefaultBindingPropertyAttribute defaultBindingPropertyAttribute)
		{
			return defaultBindingPropertyAttribute.Name == Name;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
