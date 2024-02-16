namespace System.ComponentModel;

public class ListChangedEventArgs : EventArgs
{
	public ListChangedType ListChangedType { get; }

	public int NewIndex { get; }

	public int OldIndex { get; }

	public PropertyDescriptor? PropertyDescriptor { get; }

	public ListChangedEventArgs(ListChangedType listChangedType, int newIndex)
		: this(listChangedType, newIndex, -1)
	{
	}

	public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, PropertyDescriptor? propDesc)
		: this(listChangedType, newIndex)
	{
		PropertyDescriptor = propDesc;
		OldIndex = newIndex;
	}

	public ListChangedEventArgs(ListChangedType listChangedType, PropertyDescriptor? propDesc)
	{
		ListChangedType = listChangedType;
		PropertyDescriptor = propDesc;
	}

	public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, int oldIndex)
	{
		ListChangedType = listChangedType;
		NewIndex = newIndex;
		OldIndex = oldIndex;
	}
}
