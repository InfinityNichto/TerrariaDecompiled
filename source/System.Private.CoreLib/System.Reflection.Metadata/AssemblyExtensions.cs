using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata;

public static class AssemblyExtensions
{
	[DllImport("QCall")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private unsafe static extern bool InternalTryGetRawMetadata(QCallAssembly assembly, ref byte* blob, ref int length);

	[CLSCompliant(false)]
	public unsafe static bool TryGetRawMetadata(this Assembly assembly, out byte* blob, out int length)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		blob = null;
		length = 0;
		RuntimeAssembly runtimeAssembly = assembly as RuntimeAssembly;
		if (runtimeAssembly == null)
		{
			return false;
		}
		RuntimeAssembly assembly2 = runtimeAssembly;
		return InternalTryGetRawMetadata(new QCallAssembly(ref assembly2), ref blob, ref length);
	}
}
