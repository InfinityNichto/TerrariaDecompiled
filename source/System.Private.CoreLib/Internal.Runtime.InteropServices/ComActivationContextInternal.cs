using System;

namespace Internal.Runtime.InteropServices;

[CLSCompliant(false)]
public struct ComActivationContextInternal
{
	public Guid ClassId;

	public Guid InterfaceId;

	public unsafe char* AssemblyPathBuffer;

	public unsafe char* AssemblyNameBuffer;

	public unsafe char* TypeNameBuffer;

	public IntPtr ClassFactoryDest;
}
