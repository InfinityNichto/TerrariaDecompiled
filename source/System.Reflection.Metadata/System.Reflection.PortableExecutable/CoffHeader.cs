namespace System.Reflection.PortableExecutable;

public sealed class CoffHeader
{
	internal const int Size = 20;

	public Machine Machine { get; }

	public short NumberOfSections { get; }

	public int TimeDateStamp { get; }

	public int PointerToSymbolTable { get; }

	public int NumberOfSymbols { get; }

	public short SizeOfOptionalHeader { get; }

	public Characteristics Characteristics { get; }

	internal CoffHeader(ref PEBinaryReader reader)
	{
		Machine = (Machine)reader.ReadUInt16();
		NumberOfSections = reader.ReadInt16();
		TimeDateStamp = reader.ReadInt32();
		PointerToSymbolTable = reader.ReadInt32();
		NumberOfSymbols = reader.ReadInt32();
		SizeOfOptionalHeader = reader.ReadInt16();
		Characteristics = (Characteristics)reader.ReadUInt16();
	}
}
