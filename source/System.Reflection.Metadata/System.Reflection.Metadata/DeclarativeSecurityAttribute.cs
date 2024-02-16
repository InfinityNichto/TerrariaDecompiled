namespace System.Reflection.Metadata;

public readonly struct DeclarativeSecurityAttribute
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	public DeclarativeSecurityAction Action => _reader.DeclSecurityTable.GetAction(_rowId);

	public EntityHandle Parent => _reader.DeclSecurityTable.GetParent(_rowId);

	public BlobHandle PermissionSet => _reader.DeclSecurityTable.GetPermissionSet(_rowId);

	internal DeclarativeSecurityAttribute(MetadataReader reader, int rowId)
	{
		_reader = reader;
		_rowId = rowId;
	}
}
