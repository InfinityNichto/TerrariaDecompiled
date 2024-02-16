namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryCrossAppDomainAssembly : IStreamable
{
	internal int _assemId;

	internal int _assemblyIndex;

	internal BinaryCrossAppDomainAssembly()
	{
	}

	public void Read(BinaryParser input)
	{
		_assemId = input.ReadInt32();
		_assemblyIndex = input.ReadInt32();
	}
}
