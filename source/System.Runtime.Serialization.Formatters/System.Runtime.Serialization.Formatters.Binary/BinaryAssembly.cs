namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryAssembly : IStreamable
{
	internal int _assemId;

	internal string _assemblyString;

	internal BinaryAssembly()
	{
	}

	internal void Set(int assemId, string assemblyString)
	{
		_assemId = assemId;
		_assemblyString = assemblyString;
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte(12);
		output.WriteInt32(_assemId);
		output.WriteString(_assemblyString);
	}

	public void Read(BinaryParser input)
	{
		_assemId = input.ReadInt32();
		_assemblyString = input.ReadString();
	}
}
