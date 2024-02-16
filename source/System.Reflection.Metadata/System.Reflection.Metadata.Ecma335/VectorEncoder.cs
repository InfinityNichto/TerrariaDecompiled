namespace System.Reflection.Metadata.Ecma335;

public readonly struct VectorEncoder
{
	public BlobBuilder Builder { get; }

	public VectorEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public LiteralsEncoder Count(int count)
	{
		if (count < 0)
		{
			Throw.ArgumentOutOfRange("count");
		}
		Builder.WriteUInt32((uint)count);
		return new LiteralsEncoder(Builder);
	}
}
