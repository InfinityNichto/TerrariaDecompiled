namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryObjectString : IStreamable
{
	internal int _objectId;

	internal string _value;

	internal BinaryObjectString()
	{
	}

	internal void Set(int objectId, string value)
	{
		_objectId = objectId;
		_value = value;
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte(6);
		output.WriteInt32(_objectId);
		output.WriteString(_value);
	}

	public void Read(BinaryParser input)
	{
		_objectId = input.ReadInt32();
		_value = input.ReadString();
	}
}
