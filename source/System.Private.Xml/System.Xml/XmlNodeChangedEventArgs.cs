namespace System.Xml;

public class XmlNodeChangedEventArgs : EventArgs
{
	private readonly XmlNodeChangedAction _action;

	private readonly XmlNode _node;

	private readonly XmlNode _oldParent;

	private readonly XmlNode _newParent;

	private readonly string _oldValue;

	private readonly string _newValue;

	public XmlNodeChangedAction Action => _action;

	public XmlNode? Node => _node;

	public XmlNode? OldParent => _oldParent;

	public XmlNode? NewParent => _newParent;

	public string? OldValue => _oldValue;

	public string? NewValue => _newValue;

	public XmlNodeChangedEventArgs(XmlNode? node, XmlNode? oldParent, XmlNode? newParent, string? oldValue, string? newValue, XmlNodeChangedAction action)
	{
		_node = node;
		_oldParent = oldParent;
		_newParent = newParent;
		_action = action;
		_oldValue = oldValue;
		_newValue = newValue;
	}
}
