using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class HelpKeywordAttribute : Attribute
{
	public static readonly HelpKeywordAttribute Default = new HelpKeywordAttribute();

	public string? HelpKeyword { get; }

	public HelpKeywordAttribute()
	{
	}

	public HelpKeywordAttribute(string keyword)
	{
		HelpKeyword = keyword ?? throw new ArgumentNullException("keyword");
	}

	public HelpKeywordAttribute(Type t)
	{
		if (t == null)
		{
			throw new ArgumentNullException("t");
		}
		HelpKeyword = t.FullName;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj != null && obj is HelpKeywordAttribute)
		{
			return ((HelpKeywordAttribute)obj).HelpKeyword == HelpKeyword;
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
