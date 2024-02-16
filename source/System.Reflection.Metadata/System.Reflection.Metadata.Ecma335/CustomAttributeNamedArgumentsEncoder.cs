namespace System.Reflection.Metadata.Ecma335;

public readonly struct CustomAttributeNamedArgumentsEncoder
{
	public BlobBuilder Builder { get; }

	public CustomAttributeNamedArgumentsEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public NamedArgumentsEncoder Count(int count)
	{
		if ((uint)count > 65535u)
		{
			Throw.ArgumentOutOfRange("count");
		}
		Builder.WriteUInt16((ushort)count);
		return new NamedArgumentsEncoder(Builder);
	}
}
