namespace System.Reflection.Metadata;

public readonly struct InterfaceImplementation
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private InterfaceImplementationHandle Handle => InterfaceImplementationHandle.FromRowId(_rowId);

	public EntityHandle Interface => _reader.InterfaceImplTable.GetInterface(_rowId);

	internal InterfaceImplementation(MetadataReader reader, InterfaceImplementationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
