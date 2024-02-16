using System.Collections.Immutable;

namespace System.Reflection.PortableExecutable;

public readonly struct PdbChecksumDebugDirectoryData
{
	public string AlgorithmName { get; }

	public ImmutableArray<byte> Checksum { get; }

	internal PdbChecksumDebugDirectoryData(string algorithmName, ImmutableArray<byte> checksum)
	{
		AlgorithmName = algorithmName;
		Checksum = checksum;
	}
}
