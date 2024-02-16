using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LicenseProviderAttribute : Attribute
{
	public static readonly LicenseProviderAttribute Default = new LicenseProviderAttribute();

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	private Type _licenseProviderType;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	private readonly string _licenseProviderName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type? LicenseProvider
	{
		get
		{
			if (_licenseProviderType == null && _licenseProviderName != null)
			{
				_licenseProviderType = Type.GetType(_licenseProviderName);
			}
			return _licenseProviderType;
		}
	}

	public override object TypeId
	{
		get
		{
			string text = _licenseProviderName;
			if (text == null && _licenseProviderType != null)
			{
				text = _licenseProviderType.FullName;
			}
			return GetType().FullName + text;
		}
	}

	public LicenseProviderAttribute()
		: this((string?)null)
	{
	}

	public LicenseProviderAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string? typeName)
	{
		_licenseProviderName = typeName;
	}

	public LicenseProviderAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
	{
		_licenseProviderType = type;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is LicenseProviderAttribute && value != null)
		{
			Type licenseProvider = ((LicenseProviderAttribute)value).LicenseProvider;
			if (licenseProvider == LicenseProvider)
			{
				return true;
			}
			if (licenseProvider != null && licenseProvider.Equals(LicenseProvider))
			{
				return true;
			}
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
