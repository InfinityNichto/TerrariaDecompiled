namespace System.Reflection.Metadata;

public readonly struct ImportScope
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private ImportScopeHandle Handle => ImportScopeHandle.FromRowId(_rowId);

	public ImportScopeHandle Parent => _reader.ImportScopeTable.GetParent(Handle);

	public BlobHandle ImportsBlob => _reader.ImportScopeTable.GetImports(Handle);

	internal ImportScope(MetadataReader reader, ImportScopeHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public ImportDefinitionCollection GetImports()
	{
		return new ImportDefinitionCollection(_reader.BlobHeap.GetMemoryBlock(ImportsBlob));
	}
}
