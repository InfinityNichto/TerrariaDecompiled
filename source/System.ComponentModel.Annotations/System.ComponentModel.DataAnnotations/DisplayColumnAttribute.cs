namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class DisplayColumnAttribute : Attribute
{
	public string DisplayColumn { get; }

	public string? SortColumn { get; }

	public bool SortDescending { get; }

	public DisplayColumnAttribute(string displayColumn)
		: this(displayColumn, null)
	{
	}

	public DisplayColumnAttribute(string displayColumn, string? sortColumn)
		: this(displayColumn, sortColumn, sortDescending: false)
	{
	}

	public DisplayColumnAttribute(string displayColumn, string? sortColumn, bool sortDescending)
	{
		DisplayColumn = displayColumn;
		SortColumn = sortColumn;
		SortDescending = sortDescending;
	}
}
