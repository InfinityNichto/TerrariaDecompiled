namespace System.Runtime.Remoting;

public class ObjectHandle : MarshalByRefObject
{
	private readonly object _wrappedObject;

	public ObjectHandle(object? o)
	{
		_wrappedObject = o;
	}

	public object? Unwrap()
	{
		return _wrappedObject;
	}
}
