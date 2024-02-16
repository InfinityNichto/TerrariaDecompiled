namespace System.Reflection.PortableExecutable;

public sealed class PEDirectoriesBuilder
{
	public int AddressOfEntryPoint { get; set; }

	public DirectoryEntry ExportTable { get; set; }

	public DirectoryEntry ImportTable { get; set; }

	public DirectoryEntry ResourceTable { get; set; }

	public DirectoryEntry ExceptionTable { get; set; }

	public DirectoryEntry BaseRelocationTable { get; set; }

	public DirectoryEntry DebugTable { get; set; }

	public DirectoryEntry CopyrightTable { get; set; }

	public DirectoryEntry GlobalPointerTable { get; set; }

	public DirectoryEntry ThreadLocalStorageTable { get; set; }

	public DirectoryEntry LoadConfigTable { get; set; }

	public DirectoryEntry BoundImportTable { get; set; }

	public DirectoryEntry ImportAddressTable { get; set; }

	public DirectoryEntry DelayImportTable { get; set; }

	public DirectoryEntry CorHeaderTable { get; set; }
}
