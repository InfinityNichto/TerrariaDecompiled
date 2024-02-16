using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class DesignerCategoryAttribute : Attribute
{
	public static readonly DesignerCategoryAttribute Component = new DesignerCategoryAttribute("Component");

	public static readonly DesignerCategoryAttribute Default = new DesignerCategoryAttribute();

	public static readonly DesignerCategoryAttribute Form = new DesignerCategoryAttribute("Form");

	public static readonly DesignerCategoryAttribute Generic = new DesignerCategoryAttribute("Designer");

	public string Category { get; }

	public override object TypeId => GetType().FullName + Category;

	public DesignerCategoryAttribute()
		: this(string.Empty)
	{
	}

	public DesignerCategoryAttribute(string category)
	{
		Category = category;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is DesignerCategoryAttribute designerCategoryAttribute)
		{
			return designerCategoryAttribute.Category == Category;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Category?.GetHashCode() ?? 0;
	}

	public override bool IsDefaultAttribute()
	{
		return Category == Default.Category;
	}
}
