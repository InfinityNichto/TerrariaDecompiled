using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ToolboxItemFilterAttribute : Attribute
{
	private string _typeId;

	public string FilterString { get; }

	public ToolboxItemFilterType FilterType { get; }

	public override object TypeId => _typeId ?? (_typeId = GetType().FullName + FilterString);

	public ToolboxItemFilterAttribute(string filterString)
		: this(filterString, ToolboxItemFilterType.Allow)
	{
	}

	public ToolboxItemFilterAttribute(string filterString, ToolboxItemFilterType filterType)
	{
		FilterString = filterString ?? string.Empty;
		FilterType = filterType;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is ToolboxItemFilterAttribute { FilterType: var filterType } toolboxItemFilterAttribute && filterType.Equals(FilterType))
		{
			return toolboxItemFilterAttribute.FilterString.Equals(FilterString);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return FilterString.GetHashCode();
	}

	public override bool Match([NotNullWhen(true)] object? obj)
	{
		if (obj is ToolboxItemFilterAttribute toolboxItemFilterAttribute)
		{
			return toolboxItemFilterAttribute.FilterString.Equals(FilterString);
		}
		return false;
	}

	public override string ToString()
	{
		return FilterString + "," + Enum.GetName(typeof(ToolboxItemFilterType), FilterType);
	}
}
