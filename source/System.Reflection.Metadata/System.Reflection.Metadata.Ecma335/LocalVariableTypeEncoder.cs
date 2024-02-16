namespace System.Reflection.Metadata.Ecma335;

public readonly struct LocalVariableTypeEncoder
{
	public BlobBuilder Builder { get; }

	public LocalVariableTypeEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public CustomModifiersEncoder CustomModifiers()
	{
		return new CustomModifiersEncoder(Builder);
	}

	public SignatureTypeEncoder Type(bool isByRef = false, bool isPinned = false)
	{
		if (isPinned)
		{
			Builder.WriteByte(69);
		}
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
