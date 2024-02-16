namespace System.Reflection.Metadata;

public readonly struct AssemblyDefinition
{
	private readonly MetadataReader _reader;

	public AssemblyHashAlgorithm HashAlgorithm => _reader.AssemblyTable.GetHashAlgorithm();

	public Version Version => _reader.AssemblyTable.GetVersion();

	public AssemblyFlags Flags => _reader.AssemblyTable.GetFlags();

	public StringHandle Name => _reader.AssemblyTable.GetName();

	public StringHandle Culture => _reader.AssemblyTable.GetCulture();

	public BlobHandle PublicKey => _reader.AssemblyTable.GetPublicKey();

	public AssemblyName GetAssemblyName()
	{
		return _reader.GetAssemblyName(Name, Version, Culture, PublicKey, HashAlgorithm, Flags);
	}

	internal AssemblyDefinition(MetadataReader reader)
	{
		_reader = reader;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, EntityHandle.AssemblyDefinition);
	}

	public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
	{
		return new DeclarativeSecurityAttributeHandleCollection(_reader, EntityHandle.AssemblyDefinition);
	}
}
