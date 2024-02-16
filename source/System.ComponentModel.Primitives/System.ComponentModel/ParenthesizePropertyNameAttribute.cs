using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class ParenthesizePropertyNameAttribute : Attribute
{
	public static readonly ParenthesizePropertyNameAttribute Default = new ParenthesizePropertyNameAttribute();

	public bool NeedParenthesis { get; }

	public ParenthesizePropertyNameAttribute()
		: this(needParenthesis: false)
	{
	}

	public ParenthesizePropertyNameAttribute(bool needParenthesis)
	{
		NeedParenthesis = needParenthesis;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ParenthesizePropertyNameAttribute parenthesizePropertyNameAttribute)
		{
			return parenthesizePropertyNameAttribute.NeedParenthesis == NeedParenthesis;
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
