namespace System.Reflection.Metadata.Ecma335;

public readonly struct NameEncoder
{
	public BlobBuilder Builder { get; }

	public NameEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public void Name(string name)
	{
		if (name == null)
		{
			Throw.ArgumentNull("name");
		}
		if (name.Length == 0)
		{
			Throw.ArgumentEmptyString("name");
		}
		Builder.WriteSerializedString(name);
	}
}
