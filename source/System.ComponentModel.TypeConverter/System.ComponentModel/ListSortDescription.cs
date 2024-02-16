namespace System.ComponentModel;

public class ListSortDescription
{
	public PropertyDescriptor? PropertyDescriptor { get; set; }

	public ListSortDirection SortDirection { get; set; }

	public ListSortDescription(PropertyDescriptor? property, ListSortDirection direction)
	{
		PropertyDescriptor = property;
		SortDirection = direction;
	}
}
