namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class MessageEnd : IStreamable
{
	internal MessageEnd()
	{
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte(11);
	}

	public void Read(BinaryParser input)
	{
	}
}
