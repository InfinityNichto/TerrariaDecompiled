namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class MemberReference : IStreamable
{
	internal int _idRef;

	internal MemberReference()
	{
	}

	internal void Set(int idRef)
	{
		_idRef = idRef;
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte(9);
		output.WriteInt32(_idRef);
	}

	public void Read(BinaryParser input)
	{
		_idRef = input.ReadInt32();
	}
}
