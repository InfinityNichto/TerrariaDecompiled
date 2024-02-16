using System.Text;

namespace System.Net.Http.HPack;

internal readonly struct HeaderField
{
	public byte[] Name { get; }

	public byte[] Value { get; }

	public int Length => GetLength(Name.Length, Value.Length);

	public HeaderField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
	{
		Name = name.ToArray();
		Value = value.ToArray();
	}

	public static int GetLength(int nameLength, int valueLength)
	{
		return nameLength + valueLength + 32;
	}

	public override string ToString()
	{
		if (Name != null)
		{
			return Encoding.Latin1.GetString(Name) + ": " + Encoding.Latin1.GetString(Value);
		}
		return "<empty>";
	}
}
