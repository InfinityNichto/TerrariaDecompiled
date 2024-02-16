namespace System.ComponentModel.Design;

public class ComponentRenameEventArgs : EventArgs
{
	public object? Component { get; }

	public virtual string? OldName { get; }

	public virtual string? NewName { get; }

	public ComponentRenameEventArgs(object? component, string? oldName, string? newName)
	{
		OldName = oldName;
		NewName = newName;
		Component = component;
	}
}
