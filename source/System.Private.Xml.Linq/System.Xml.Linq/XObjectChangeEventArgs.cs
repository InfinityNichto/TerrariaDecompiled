namespace System.Xml.Linq;

public class XObjectChangeEventArgs : EventArgs
{
	private readonly XObjectChange _objectChange;

	public static readonly XObjectChangeEventArgs Add = new XObjectChangeEventArgs(XObjectChange.Add);

	public static readonly XObjectChangeEventArgs Remove = new XObjectChangeEventArgs(XObjectChange.Remove);

	public static readonly XObjectChangeEventArgs Name = new XObjectChangeEventArgs(XObjectChange.Name);

	public static readonly XObjectChangeEventArgs Value = new XObjectChangeEventArgs(XObjectChange.Value);

	public XObjectChange ObjectChange => _objectChange;

	public XObjectChangeEventArgs(XObjectChange objectChange)
	{
		_objectChange = objectChange;
	}
}
