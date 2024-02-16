using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class)]
public sealed class LookupBindingPropertiesAttribute : Attribute
{
	public static readonly LookupBindingPropertiesAttribute Default = new LookupBindingPropertiesAttribute();

	public string? DataSource { get; }

	public string? DisplayMember { get; }

	public string? ValueMember { get; }

	public string? LookupMember { get; }

	public LookupBindingPropertiesAttribute()
	{
		DataSource = null;
		DisplayMember = null;
		ValueMember = null;
		LookupMember = null;
	}

	public LookupBindingPropertiesAttribute(string dataSource, string displayMember, string valueMember, string lookupMember)
	{
		DataSource = dataSource;
		DisplayMember = displayMember;
		ValueMember = valueMember;
		LookupMember = lookupMember;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is LookupBindingPropertiesAttribute lookupBindingPropertiesAttribute && lookupBindingPropertiesAttribute.DataSource == DataSource && lookupBindingPropertiesAttribute.DisplayMember == DisplayMember && lookupBindingPropertiesAttribute.ValueMember == ValueMember)
		{
			return lookupBindingPropertiesAttribute.LookupMember == LookupMember;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
