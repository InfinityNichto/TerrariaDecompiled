namespace System.Reflection.Metadata;

public readonly struct Document
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private DocumentHandle Handle => DocumentHandle.FromRowId(_rowId);

	public DocumentNameBlobHandle Name => _reader.DocumentTable.GetName(Handle);

	public GuidHandle Language => _reader.DocumentTable.GetLanguage(Handle);

	public GuidHandle HashAlgorithm => _reader.DocumentTable.GetHashAlgorithm(Handle);

	public BlobHandle Hash => _reader.DocumentTable.GetHash(Handle);

	internal Document(MetadataReader reader, DocumentHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}
}
