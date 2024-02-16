namespace System.Xml.Serialization;

public class UnreferencedObjectEventArgs : EventArgs
{
	private readonly object _o;

	private readonly string _id;

	public object? UnreferencedObject => _o;

	public string? UnreferencedId => _id;

	public UnreferencedObjectEventArgs(object? o, string? id)
	{
		_o = o;
		_id = id;
	}
}
