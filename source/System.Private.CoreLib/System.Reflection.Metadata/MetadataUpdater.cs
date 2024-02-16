using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata;

public static class MetadataUpdater
{
	public static bool IsSupported { get; } = IsApplyUpdateSupported();


	[DllImport("QCall")]
	private unsafe static extern void ApplyUpdate(QCallAssembly assembly, byte* metadataDelta, int metadataDeltaLength, byte* ilDelta, int ilDeltaLength, byte* pdbDelta, int pdbDeltaLength);

	[DllImport("QCall")]
	private static extern bool IsApplyUpdateSupported();

	public unsafe static void ApplyUpdate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta)
	{
		if (!(assembly is RuntimeAssembly runtimeAssembly))
		{
			if ((object)assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		RuntimeAssembly assembly2 = runtimeAssembly;
		fixed (byte* metadataDelta2 = metadataDelta)
		{
			fixed (byte* ilDelta2 = ilDelta)
			{
				fixed (byte* pdbDelta2 = pdbDelta)
				{
					ApplyUpdate(new QCallAssembly(ref assembly2), metadataDelta2, metadataDelta.Length, ilDelta2, ilDelta.Length, pdbDelta2, pdbDelta.Length);
				}
			}
		}
	}

	internal static string GetCapabilities()
	{
		return "Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters";
	}
}
