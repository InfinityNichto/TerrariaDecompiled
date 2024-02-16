namespace System.Reflection.Metadata.Ecma335;

public readonly struct CustomModifiersEncoder
{
	public BlobBuilder Builder { get; }

	public CustomModifiersEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public CustomModifiersEncoder AddModifier(EntityHandle type, bool isOptional)
	{
		if (type.IsNil)
		{
			Throw.InvalidArgument_Handle("type");
		}
		if (isOptional)
		{
			Builder.WriteByte(32);
		}
		else
		{
			Builder.WriteByte(31);
		}
		Builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(type));
		return this;
	}
}
