using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ComplexBindingPropertiesAttribute : Attribute
{
	public static readonly ComplexBindingPropertiesAttribute Default = new ComplexBindingPropertiesAttribute();

	public string? DataSource { get; }

	public string? DataMember { get; }

	public ComplexBindingPropertiesAttribute()
	{
	}

	public ComplexBindingPropertiesAttribute(string? dataSource)
	{
		DataSource = dataSource;
	}

	public ComplexBindingPropertiesAttribute(string? dataSource, string? dataMember)
	{
		DataSource = dataSource;
		DataMember = dataMember;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ComplexBindingPropertiesAttribute complexBindingPropertiesAttribute && complexBindingPropertiesAttribute.DataSource == DataSource)
		{
			return complexBindingPropertiesAttribute.DataMember == DataMember;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
