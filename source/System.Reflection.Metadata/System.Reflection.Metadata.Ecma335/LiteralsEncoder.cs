namespace System.Reflection.Metadata.Ecma335;

public readonly struct LiteralsEncoder
{
	public BlobBuilder Builder { get; }

	public LiteralsEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public LiteralEncoder AddLiteral()
	{
		return new LiteralEncoder(Builder);
	}
}
