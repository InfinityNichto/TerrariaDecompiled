namespace System.Reflection.Metadata;

public readonly struct LocalVariable
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private LocalVariableHandle Handle => LocalVariableHandle.FromRowId(_rowId);

	public LocalVariableAttributes Attributes => _reader.LocalVariableTable.GetAttributes(Handle);

	public int Index => _reader.LocalVariableTable.GetIndex(Handle);

	public StringHandle Name => _reader.LocalVariableTable.GetName(Handle);

	internal LocalVariable(MetadataReader reader, LocalVariableHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}
}
