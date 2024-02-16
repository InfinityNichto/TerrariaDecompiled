using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class BindableAttribute : Attribute
{
	public static readonly BindableAttribute Yes = new BindableAttribute(bindable: true);

	public static readonly BindableAttribute No = new BindableAttribute(bindable: false);

	public static readonly BindableAttribute Default = No;

	private readonly bool _isDefault;

	public bool Bindable { get; }

	public BindingDirection Direction { get; }

	public BindableAttribute(bool bindable)
		: this(bindable, BindingDirection.OneWay)
	{
	}

	public BindableAttribute(bool bindable, BindingDirection direction)
	{
		Bindable = bindable;
		Direction = direction;
	}

	public BindableAttribute(BindableSupport flags)
		: this(flags, BindingDirection.OneWay)
	{
	}

	public BindableAttribute(BindableSupport flags, BindingDirection direction)
	{
		Bindable = flags != BindableSupport.No;
		_isDefault = flags == BindableSupport.Default;
		Direction = direction;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is BindableAttribute bindableAttribute)
		{
			return bindableAttribute.Bindable == Bindable;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Bindable.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		if (!Equals(Default))
		{
			return _isDefault;
		}
		return true;
	}
}
