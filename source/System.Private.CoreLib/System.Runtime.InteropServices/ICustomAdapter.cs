namespace System.Runtime.InteropServices;

public interface ICustomAdapter
{
	[return: MarshalAs(UnmanagedType.IUnknown)]
	object GetUnderlyingObject();
}
