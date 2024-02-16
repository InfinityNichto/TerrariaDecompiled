namespace System.Reflection.Metadata;

public readonly struct MethodDebugInformation
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private MethodDebugInformationHandle Handle => MethodDebugInformationHandle.FromRowId(_rowId);

	public BlobHandle SequencePointsBlob => _reader.MethodDebugInformationTable.GetSequencePoints(Handle);

	public DocumentHandle Document => _reader.MethodDebugInformationTable.GetDocument(Handle);

	public StandaloneSignatureHandle LocalSignature
	{
		get
		{
			if (SequencePointsBlob.IsNil)
			{
				return default(StandaloneSignatureHandle);
			}
			return StandaloneSignatureHandle.FromRowId(_reader.GetBlobReader(SequencePointsBlob).ReadCompressedInteger());
		}
	}

	internal MethodDebugInformation(MetadataReader reader, MethodDebugInformationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public SequencePointCollection GetSequencePoints()
	{
		return new SequencePointCollection(_reader.BlobHeap.GetMemoryBlock(SequencePointsBlob), Document);
	}

	public MethodDefinitionHandle GetStateMachineKickoffMethod()
	{
		return _reader.StateMachineMethodTable.FindKickoffMethod(_rowId);
	}
}
