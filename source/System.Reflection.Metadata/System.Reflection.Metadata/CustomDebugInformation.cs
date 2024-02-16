namespace System.Reflection.Metadata;

public readonly struct CustomDebugInformation
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private CustomDebugInformationHandle Handle => CustomDebugInformationHandle.FromRowId(_rowId);

	public EntityHandle Parent => _reader.CustomDebugInformationTable.GetParent(Handle);

	public GuidHandle Kind => _reader.CustomDebugInformationTable.GetKind(Handle);

	public BlobHandle Value => _reader.CustomDebugInformationTable.GetValue(Handle);

	internal CustomDebugInformation(MetadataReader reader, CustomDebugInformationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}
}
