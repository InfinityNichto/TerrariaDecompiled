using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class PasswordPropertyTextAttribute : Attribute
{
	public static readonly PasswordPropertyTextAttribute Yes = new PasswordPropertyTextAttribute(password: true);

	public static readonly PasswordPropertyTextAttribute No = new PasswordPropertyTextAttribute(password: false);

	public static readonly PasswordPropertyTextAttribute Default = No;

	public bool Password { get; }

	public PasswordPropertyTextAttribute()
		: this(password: false)
	{
	}

	public PasswordPropertyTextAttribute(bool password)
	{
		Password = password;
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (o is PasswordPropertyTextAttribute)
		{
			return ((PasswordPropertyTextAttribute)o).Password == Password;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return Equals(Default);
	}
}
