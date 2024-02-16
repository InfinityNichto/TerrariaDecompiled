namespace System.Reflection.Metadata;

public readonly struct MethodImplementation
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private MethodImplementationHandle Handle => MethodImplementationHandle.FromRowId(_rowId);

	public TypeDefinitionHandle Type => _reader.MethodImplTable.GetClass(Handle);

	public EntityHandle MethodBody => _reader.MethodImplTable.GetMethodBody(Handle);

	public EntityHandle MethodDeclaration => _reader.MethodImplTable.GetMethodDeclaration(Handle);

	internal MethodImplementation(MetadataReader reader, MethodImplementationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
