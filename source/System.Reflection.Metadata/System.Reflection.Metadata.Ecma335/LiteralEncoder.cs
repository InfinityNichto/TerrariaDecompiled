namespace System.Reflection.Metadata.Ecma335;

public readonly struct LiteralEncoder
{
	public BlobBuilder Builder { get; }

	public LiteralEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public VectorEncoder Vector()
	{
		return new VectorEncoder(Builder);
	}

	public void TaggedVector(out CustomAttributeArrayTypeEncoder arrayType, out VectorEncoder vector)
	{
		arrayType = new CustomAttributeArrayTypeEncoder(Builder);
		vector = new VectorEncoder(Builder);
	}

	public void TaggedVector(Action<CustomAttributeArrayTypeEncoder> arrayType, Action<VectorEncoder> vector)
	{
		if (arrayType == null)
		{
			Throw.ArgumentNull("arrayType");
		}
		if (vector == null)
		{
			Throw.ArgumentNull("vector");
		}
		TaggedVector(out var arrayType2, out var vector2);
		arrayType(arrayType2);
		vector(vector2);
	}

	public ScalarEncoder Scalar()
	{
		return new ScalarEncoder(Builder);
	}

	public void TaggedScalar(out CustomAttributeElementTypeEncoder type, out ScalarEncoder scalar)
	{
		type = new CustomAttributeElementTypeEncoder(Builder);
		scalar = new ScalarEncoder(Builder);
	}

	public void TaggedScalar(Action<CustomAttributeElementTypeEncoder> type, Action<ScalarEncoder> scalar)
	{
		if (type == null)
		{
			Throw.ArgumentNull("type");
		}
		if (scalar == null)
		{
			Throw.ArgumentNull("scalar");
		}
		TaggedScalar(out var type2, out var scalar2);
		type(type2);
		scalar(scalar2);
	}
}
