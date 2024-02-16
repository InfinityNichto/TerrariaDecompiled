using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class ListBindableAttribute : Attribute
{
	public static readonly ListBindableAttribute Yes = new ListBindableAttribute(listBindable: true);

	public static readonly ListBindableAttribute No = new ListBindableAttribute(listBindable: false);

	public static readonly ListBindableAttribute Default = Yes;

	private readonly bool _isDefault;

	public bool ListBindable { get; }

	public ListBindableAttribute(bool listBindable)
	{
		ListBindable = listBindable;
	}

	public ListBindableAttribute(BindableSupport flags)
	{
		ListBindable = flags != BindableSupport.No;
		_isDefault = flags == BindableSupport.Default;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is ListBindableAttribute listBindableAttribute)
		{
			return listBindableAttribute.ListBindable == ListBindable;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
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
