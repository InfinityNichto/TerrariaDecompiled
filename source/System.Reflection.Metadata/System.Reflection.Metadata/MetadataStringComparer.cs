namespace System.Reflection.Metadata;

public readonly struct MetadataStringComparer
{
	private readonly MetadataReader _reader;

	internal MetadataStringComparer(MetadataReader reader)
	{
		_reader = reader;
	}

	public bool Equals(StringHandle handle, string value)
	{
		return Equals(handle, value, ignoreCase: false);
	}

	public bool Equals(StringHandle handle, string value, bool ignoreCase)
	{
		if (value == null)
		{
			Throw.ValueArgumentNull();
		}
		return _reader.StringHeap.Equals(handle, value, _reader.UTF8Decoder, ignoreCase);
	}

	public bool Equals(NamespaceDefinitionHandle handle, string value)
	{
		return Equals(handle, value, ignoreCase: false);
	}

	public bool Equals(NamespaceDefinitionHandle handle, string value, bool ignoreCase)
	{
		if (value == null)
		{
			Throw.ValueArgumentNull();
		}
		if (handle.HasFullName)
		{
			return _reader.StringHeap.Equals(handle.GetFullName(), value, _reader.UTF8Decoder, ignoreCase);
		}
		return value == _reader.NamespaceCache.GetFullName(handle);
	}

	public bool Equals(DocumentNameBlobHandle handle, string value)
	{
		return Equals(handle, value, ignoreCase: false);
	}

	public bool Equals(DocumentNameBlobHandle handle, string value, bool ignoreCase)
	{
		if (value == null)
		{
			Throw.ValueArgumentNull();
		}
		return _reader.BlobHeap.DocumentNameEquals(handle, value, ignoreCase);
	}

	public bool StartsWith(StringHandle handle, string value)
	{
		return StartsWith(handle, value, ignoreCase: false);
	}

	public bool StartsWith(StringHandle handle, string value, bool ignoreCase)
	{
		if (value == null)
		{
			Throw.ValueArgumentNull();
		}
		return _reader.StringHeap.StartsWith(handle, value, _reader.UTF8Decoder, ignoreCase);
	}
}
