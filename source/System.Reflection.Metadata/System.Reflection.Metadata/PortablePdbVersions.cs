namespace System.Reflection.Metadata;

internal static class PortablePdbVersions
{
	internal const string DefaultMetadataVersion = "PDB v1.0";

	internal const ushort DefaultFormatVersion = 256;

	internal const ushort MinFormatVersion = 256;

	internal const ushort MinEmbeddedVersion = 256;

	internal const ushort DefaultEmbeddedVersion = 256;

	internal const ushort MinUnsupportedEmbeddedVersion = 512;

	internal const uint DebugDirectoryEmbeddedSignature = 1111773261u;

	internal const ushort PortableCodeViewVersionMagic = 20557;

	internal static uint DebugDirectoryEntryVersion(ushort portablePdbVersion)
	{
		return 0x504D0000u | portablePdbVersion;
	}

	internal static uint DebugDirectoryEmbeddedVersion(ushort portablePdbVersion)
	{
		return 0x1000000u | portablePdbVersion;
	}

	internal static string Format(ushort version)
	{
		return (version >> 8) + "." + (version & 0xFF);
	}
}
