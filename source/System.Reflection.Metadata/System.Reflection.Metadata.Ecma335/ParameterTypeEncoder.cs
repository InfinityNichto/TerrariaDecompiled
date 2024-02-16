namespace System.Reflection.Metadata.Ecma335;

public readonly struct ParameterTypeEncoder
{
	public BlobBuilder Builder { get; }

	public ParameterTypeEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public CustomModifiersEncoder CustomModifiers()
	{
		return new CustomModifiersEncoder(Builder);
	}

	public SignatureTypeEncoder Type(bool isByRef = false)
	{
		if (isByRef)
		{
			Builder.WriteByte(16);
		}
		return new SignatureTypeEncoder(Builder);
	}

	public void TypedReference()
	{
		Builder.WriteByte(22);
	}
}
