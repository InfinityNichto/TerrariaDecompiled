using System.Reflection.Internal;

namespace System.Reflection.PortableExecutable;

public sealed class PEHeaderBuilder
{
	public Machine Machine { get; }

	public Characteristics ImageCharacteristics { get; }

	public byte MajorLinkerVersion { get; }

	public byte MinorLinkerVersion { get; }

	public ulong ImageBase { get; }

	public int SectionAlignment { get; }

	public int FileAlignment { get; }

	public ushort MajorOperatingSystemVersion { get; }

	public ushort MinorOperatingSystemVersion { get; }

	public ushort MajorImageVersion { get; }

	public ushort MinorImageVersion { get; }

	public ushort MajorSubsystemVersion { get; }

	public ushort MinorSubsystemVersion { get; }

	public Subsystem Subsystem { get; }

	public DllCharacteristics DllCharacteristics { get; }

	public ulong SizeOfStackReserve { get; }

	public ulong SizeOfStackCommit { get; }

	public ulong SizeOfHeapReserve { get; }

	public ulong SizeOfHeapCommit { get; }

	internal bool Is32Bit
	{
		get
		{
			if (Machine != Machine.Amd64 && Machine != Machine.IA64)
			{
				return Machine != Machine.Arm64;
			}
			return false;
		}
	}

	public PEHeaderBuilder(Machine machine = Machine.Unknown, int sectionAlignment = 8192, int fileAlignment = 512, ulong imageBase = 4194304uL, byte majorLinkerVersion = 48, byte minorLinkerVersion = 0, ushort majorOperatingSystemVersion = 4, ushort minorOperatingSystemVersion = 0, ushort majorImageVersion = 0, ushort minorImageVersion = 0, ushort majorSubsystemVersion = 4, ushort minorSubsystemVersion = 0, Subsystem subsystem = Subsystem.WindowsCui, DllCharacteristics dllCharacteristics = DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware, Characteristics imageCharacteristics = Characteristics.Dll, ulong sizeOfStackReserve = 1048576uL, ulong sizeOfStackCommit = 4096uL, ulong sizeOfHeapReserve = 1048576uL, ulong sizeOfHeapCommit = 4096uL)
	{
		if (fileAlignment < 512 || fileAlignment > 65536 || BitArithmetic.CountBits(fileAlignment) != 1)
		{
			Throw.ArgumentOutOfRange("fileAlignment");
		}
		if (sectionAlignment < fileAlignment || BitArithmetic.CountBits(sectionAlignment) != 1)
		{
			Throw.ArgumentOutOfRange("sectionAlignment");
		}
		Machine = machine;
		SectionAlignment = sectionAlignment;
		FileAlignment = fileAlignment;
		ImageBase = imageBase;
		MajorLinkerVersion = majorLinkerVersion;
		MinorLinkerVersion = minorLinkerVersion;
		MajorOperatingSystemVersion = majorOperatingSystemVersion;
		MinorOperatingSystemVersion = minorOperatingSystemVersion;
		MajorImageVersion = majorImageVersion;
		MinorImageVersion = minorImageVersion;
		MajorSubsystemVersion = majorSubsystemVersion;
		MinorSubsystemVersion = minorSubsystemVersion;
		Subsystem = subsystem;
		DllCharacteristics = dllCharacteristics;
		ImageCharacteristics = imageCharacteristics;
		SizeOfStackReserve = sizeOfStackReserve;
		SizeOfStackCommit = sizeOfStackCommit;
		SizeOfHeapReserve = sizeOfHeapReserve;
		SizeOfHeapCommit = sizeOfHeapCommit;
	}

	public static PEHeaderBuilder CreateExecutableHeader()
	{
		return new PEHeaderBuilder(Machine.Unknown, 8192, 512, 4194304uL, 48, 0, 4, 0, 0, 0, 4, 0, Subsystem.WindowsCui, DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware, Characteristics.ExecutableImage, 1048576uL, 4096uL, 1048576uL, 4096uL);
	}

	public static PEHeaderBuilder CreateLibraryHeader()
	{
		return new PEHeaderBuilder(Machine.Unknown, 8192, 512, 4194304uL, 48, 0, 4, 0, 0, 0, 4, 0, Subsystem.WindowsCui, DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible | DllCharacteristics.NoSeh | DllCharacteristics.TerminalServerAware, Characteristics.ExecutableImage | Characteristics.Dll, 1048576uL, 4096uL, 1048576uL, 4096uL);
	}

	internal int ComputeSizeOfPEHeaders(int sectionCount)
	{
		return PEBuilder.DosHeaderSize + 4 + 20 + PEHeader.Size(Is32Bit) + 40 * sectionCount;
	}
}
