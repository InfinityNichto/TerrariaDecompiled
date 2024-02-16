namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryObject : IStreamable
{
	internal int _objectId;

	internal int _mapId;

	internal BinaryObject()
	{
	}

	internal void Set(int objectId, int mapId)
	{
		_objectId = objectId;
		_mapId = mapId;
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte(1);
		output.WriteInt32(_objectId);
		output.WriteInt32(_mapId);
	}

	public void Read(BinaryParser input)
	{
		_objectId = input.ReadInt32();
		_mapId = input.ReadInt32();
	}
}
