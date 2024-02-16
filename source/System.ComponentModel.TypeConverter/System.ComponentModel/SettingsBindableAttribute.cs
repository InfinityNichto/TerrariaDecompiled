using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingsBindableAttribute : Attribute
{
	public static readonly SettingsBindableAttribute Yes = new SettingsBindableAttribute(bindable: true);

	public static readonly SettingsBindableAttribute No = new SettingsBindableAttribute(bindable: false);

	public bool Bindable { get; }

	public SettingsBindableAttribute(bool bindable)
	{
		Bindable = bindable;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is SettingsBindableAttribute settingsBindableAttribute)
		{
			return settingsBindableAttribute.Bindable == Bindable;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Bindable.GetHashCode();
	}
}
