namespace System.Reflection.Metadata;

public readonly struct AssemblyReference
{
	private readonly MetadataReader _reader;

	private readonly uint _treatmentAndRowId;

	private static readonly Version s_version_4_0_0_0 = new Version(4, 0, 0, 0);

	private int RowId => (int)(_treatmentAndRowId & 0xFFFFFF);

	private bool IsVirtual => (_treatmentAndRowId & 0x80000000u) != 0;

	public Version Version
	{
		get
		{
			if (IsVirtual)
			{
				return GetVirtualVersion();
			}
			if (RowId == _reader.WinMDMscorlibRef)
			{
				return s_version_4_0_0_0;
			}
			return _reader.AssemblyRefTable.GetVersion(RowId);
		}
	}

	public AssemblyFlags Flags
	{
		get
		{
			if (IsVirtual)
			{
				return GetVirtualFlags();
			}
			return _reader.AssemblyRefTable.GetFlags(RowId);
		}
	}

	public StringHandle Name
	{
		get
		{
			if (IsVirtual)
			{
				return GetVirtualName();
			}
			return _reader.AssemblyRefTable.GetName(RowId);
		}
	}

	public StringHandle Culture
	{
		get
		{
			if (IsVirtual)
			{
				return GetVirtualCulture();
			}
			return _reader.AssemblyRefTable.GetCulture(RowId);
		}
	}

	public BlobHandle PublicKeyOrToken
	{
		get
		{
			if (IsVirtual)
			{
				return GetVirtualPublicKeyOrToken();
			}
			return _reader.AssemblyRefTable.GetPublicKeyOrToken(RowId);
		}
	}

	public BlobHandle HashValue
	{
		get
		{
			if (IsVirtual)
			{
				return GetVirtualHashValue();
			}
			return _reader.AssemblyRefTable.GetHashValue(RowId);
		}
	}

	public AssemblyName GetAssemblyName()
	{
		return _reader.GetAssemblyName(Name, Version, Culture, PublicKeyOrToken, AssemblyHashAlgorithm.None, Flags);
	}

	internal AssemblyReference(MetadataReader reader, uint treatmentAndRowId)
	{
		_reader = reader;
		_treatmentAndRowId = treatmentAndRowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		if (IsVirtual)
		{
			return GetVirtualCustomAttributes();
		}
		return new CustomAttributeHandleCollection(_reader, AssemblyReferenceHandle.FromRowId(RowId));
	}

	private Version GetVirtualVersion()
	{
		return s_version_4_0_0_0;
	}

	private AssemblyFlags GetVirtualFlags()
	{
		return _reader.AssemblyRefTable.GetFlags(_reader.WinMDMscorlibRef);
	}

	private StringHandle GetVirtualName()
	{
		return StringHandle.FromVirtualIndex(GetVirtualNameIndex((AssemblyReferenceHandle.VirtualIndex)RowId));
	}

	private StringHandle.VirtualIndex GetVirtualNameIndex(AssemblyReferenceHandle.VirtualIndex index)
	{
		return index switch
		{
			AssemblyReferenceHandle.VirtualIndex.System_ObjectModel => StringHandle.VirtualIndex.System_ObjectModel, 
			AssemblyReferenceHandle.VirtualIndex.System_Runtime => StringHandle.VirtualIndex.System_Runtime, 
			AssemblyReferenceHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime => StringHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime, 
			AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime => StringHandle.VirtualIndex.System_Runtime_WindowsRuntime, 
			AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml => StringHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml, 
			AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors => StringHandle.VirtualIndex.System_Numerics_Vectors, 
			_ => StringHandle.VirtualIndex.System_Runtime_WindowsRuntime, 
		};
	}

	private StringHandle GetVirtualCulture()
	{
		return default(StringHandle);
	}

	private BlobHandle GetVirtualPublicKeyOrToken()
	{
		AssemblyReferenceHandle.VirtualIndex rowId = (AssemblyReferenceHandle.VirtualIndex)RowId;
		if ((uint)(rowId - 3) <= 1u)
		{
			return _reader.AssemblyRefTable.GetPublicKeyOrToken(_reader.WinMDMscorlibRef);
		}
		return BlobHandle.FromVirtualIndex(((_reader.AssemblyRefTable.GetFlags(_reader.WinMDMscorlibRef) & AssemblyFlags.PublicKey) == 0) ? BlobHandle.VirtualIndex.ContractPublicKeyToken : BlobHandle.VirtualIndex.ContractPublicKey, 0);
	}

	private BlobHandle GetVirtualHashValue()
	{
		return default(BlobHandle);
	}

	private CustomAttributeHandleCollection GetVirtualCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, AssemblyReferenceHandle.FromRowId(_reader.WinMDMscorlibRef));
	}
}
