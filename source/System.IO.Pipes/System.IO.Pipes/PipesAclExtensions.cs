using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes;

public static class PipesAclExtensions
{
	public static PipeSecurity GetAccessControl(this PipeStream stream)
	{
		SafePipeHandle safePipeHandle = stream.SafePipeHandle;
		return new PipeSecurity(safePipeHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
	}

	public static void SetAccessControl(this PipeStream stream, PipeSecurity pipeSecurity)
	{
		if (pipeSecurity == null)
		{
			throw new ArgumentNullException("pipeSecurity");
		}
		SafePipeHandle safePipeHandle = stream.SafePipeHandle;
		if (stream is NamedPipeClientStream && !stream.IsConnected)
		{
			throw new IOException(System.SR.IO_IO_PipeBroken);
		}
		pipeSecurity.Persist(safePipeHandle);
	}
}
