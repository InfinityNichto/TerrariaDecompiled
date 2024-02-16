namespace System.Reflection.Metadata.Ecma335;

public readonly struct LocalVariablesEncoder
{
	public BlobBuilder Builder { get; }

	public LocalVariablesEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public LocalVariableTypeEncoder AddVariable()
	{
		return new LocalVariableTypeEncoder(Builder);
	}
}
