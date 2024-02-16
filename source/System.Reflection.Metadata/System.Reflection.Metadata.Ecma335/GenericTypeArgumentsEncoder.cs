namespace System.Reflection.Metadata.Ecma335;

public readonly struct GenericTypeArgumentsEncoder
{
	public BlobBuilder Builder { get; }

	public GenericTypeArgumentsEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public SignatureTypeEncoder AddArgument()
	{
		return new SignatureTypeEncoder(Builder);
	}
}
