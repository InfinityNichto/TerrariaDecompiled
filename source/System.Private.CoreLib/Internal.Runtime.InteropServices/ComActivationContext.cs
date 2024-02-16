using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Internal.Runtime.InteropServices;

public struct ComActivationContext
{
	public Guid ClassId;

	public Guid InterfaceId;

	public string AssemblyPath;

	public string AssemblyName;

	public string TypeName;

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	[CLSCompliant(false)]
	public unsafe static ComActivationContext Create(ref ComActivationContextInternal cxtInt)
	{
		if (!Marshal.IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ComActivationContext result = default(ComActivationContext);
		result.ClassId = cxtInt.ClassId;
		result.InterfaceId = cxtInt.InterfaceId;
		result.AssemblyPath = Marshal.PtrToStringUni(new IntPtr(cxtInt.AssemblyPathBuffer));
		result.AssemblyName = Marshal.PtrToStringUni(new IntPtr(cxtInt.AssemblyNameBuffer));
		result.TypeName = Marshal.PtrToStringUni(new IntPtr(cxtInt.TypeNameBuffer));
		return result;
	}
}
