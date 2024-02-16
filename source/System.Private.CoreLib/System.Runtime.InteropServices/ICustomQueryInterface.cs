namespace System.Runtime.InteropServices;

public interface ICustomQueryInterface
{
	CustomQueryInterfaceResult GetInterface([In] ref Guid iid, out IntPtr ppv);
}
