namespace System.Reflection.Metadata;

public readonly struct Constant
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private ConstantHandle Handle => ConstantHandle.FromRowId(_rowId);

	public ConstantTypeCode TypeCode => _reader.ConstantTable.GetType(Handle);

	public BlobHandle Value => _reader.ConstantTable.GetValue(Handle);

	public EntityHandle Parent => _reader.ConstantTable.GetParent(Handle);

	internal Constant(MetadataReader reader, int rowId)
	{
		_reader = reader;
		_rowId = rowId;
	}
}
