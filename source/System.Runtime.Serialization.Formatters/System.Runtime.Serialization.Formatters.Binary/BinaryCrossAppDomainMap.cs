namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryCrossAppDomainMap : IStreamable
{
	internal int _crossAppDomainArrayIndex;

	public void Read(BinaryParser input)
	{
		_crossAppDomainArrayIndex = input.ReadInt32();
	}
}
