using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[SupportedOSPlatform("windows")]
public sealed class CspParameters
{
	public int ProviderType;

	public string? ProviderName;

	public string? KeyContainerName;

	public int KeyNumber;

	private int _flags;

	private IntPtr _parentWindowHandle;

	public CspProviderFlags Flags
	{
		get
		{
			return (CspProviderFlags)_flags;
		}
		set
		{
			int num = 255;
			if (((uint)value & (uint)(~num)) != 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "value"));
			}
			_flags = (int)value;
		}
	}

	[CLSCompliant(false)]
	public SecureString? KeyPassword { get; set; }

	public IntPtr ParentWindowHandle
	{
		get
		{
			return _parentWindowHandle;
		}
		set
		{
			_parentWindowHandle = value;
		}
	}

	public CspParameters()
		: this(24, null, null)
	{
	}

	public CspParameters(int dwTypeIn)
		: this(dwTypeIn, null, null)
	{
	}

	public CspParameters(int dwTypeIn, string? strProviderNameIn)
		: this(dwTypeIn, strProviderNameIn, null)
	{
	}

	public CspParameters(int dwTypeIn, string? strProviderNameIn, string? strContainerNameIn)
		: this(dwTypeIn, strProviderNameIn, strContainerNameIn, CspProviderFlags.NoFlags)
	{
	}

	internal CspParameters(int providerType, string providerName, string keyContainerName, CspProviderFlags flags)
	{
		ProviderType = providerType;
		ProviderName = providerName;
		KeyContainerName = keyContainerName;
		KeyNumber = -1;
		Flags = flags;
	}

	internal CspParameters(CspParameters parameters)
	{
		ProviderType = parameters.ProviderType;
		ProviderName = parameters.ProviderName;
		KeyContainerName = parameters.KeyContainerName;
		KeyNumber = parameters.KeyNumber;
		KeyPassword = parameters.KeyPassword;
		Flags = parameters.Flags;
		_parentWindowHandle = parameters._parentWindowHandle;
	}
}
