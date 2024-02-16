using System.Runtime.Versioning;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

[SupportedOSPlatform("windows")]
public sealed class CspKeyContainerInfo
{
	private readonly CspParameters _parameters;

	private readonly bool _randomKeyContainer;

	public bool Accessible
	{
		get
		{
			object obj = ReadKeyParameterSilent(6, throwOnNotFound: false);
			if (obj == null)
			{
				return false;
			}
			return (bool)obj;
		}
	}

	public bool Exportable
	{
		get
		{
			if (HardwareDevice)
			{
				return false;
			}
			return (bool)ReadKeyParameterSilent(3);
		}
	}

	public bool HardwareDevice => (bool)ReadDeviceParameterVerifyContext(5);

	public string? KeyContainerName => _parameters.KeyContainerName;

	public KeyNumber KeyNumber => (KeyNumber)_parameters.KeyNumber;

	public bool MachineKeyStore => CapiHelper.IsFlagBitSet((uint)_parameters.Flags, 1u);

	public bool Protected
	{
		get
		{
			if (HardwareDevice)
			{
				return true;
			}
			return (bool)ReadKeyParameterSilent(7);
		}
	}

	public string? ProviderName => _parameters.ProviderName;

	public int ProviderType => _parameters.ProviderType;

	public bool RandomlyGenerated => _randomKeyContainer;

	public bool Removable => (bool)ReadDeviceParameterVerifyContext(4);

	public string UniqueKeyContainerName => (string)ReadKeyParameterSilent(8);

	public CspKeyContainerInfo(CspParameters parameters)
		: this(parameters, randomKeyContainer: false)
	{
	}

	internal CspKeyContainerInfo(CspParameters parameters, bool randomKeyContainer)
	{
		_parameters = new CspParameters(parameters);
		if (_parameters.KeyNumber == -1)
		{
			if (_parameters.ProviderType == 1 || _parameters.ProviderType == 24)
			{
				_parameters.KeyNumber = 1;
			}
			else if (_parameters.ProviderType == 13)
			{
				_parameters.KeyNumber = 2;
			}
		}
		_randomKeyContainer = randomKeyContainer;
	}

	private object ReadKeyParameterSilent(int keyParam, bool throwOnNotFound = true)
	{
		SafeProvHandle safeProvHandle;
		int num = CapiHelper.OpenCSP(_parameters, 64u, out safeProvHandle);
		using (safeProvHandle)
		{
			if (num != 0)
			{
				if (throwOnNotFound)
				{
					throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CSP_NotFound, "Error"));
				}
				return null;
			}
			return CapiHelper.GetProviderParameter(safeProvHandle, _parameters.KeyNumber, keyParam);
		}
	}

	private object ReadDeviceParameterVerifyContext(int keyParam)
	{
		CspParameters cspParameters = new CspParameters(_parameters);
		cspParameters.Flags &= CspProviderFlags.UseMachineKeyStore;
		cspParameters.KeyContainerName = null;
		SafeProvHandle safeProvHandle;
		int num = CapiHelper.OpenCSP(cspParameters, 4026531840u, out safeProvHandle);
		using (safeProvHandle)
		{
			if (num != 0)
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CSP_NotFound, "Error"));
			}
			return CapiHelper.GetProviderParameter(safeProvHandle, cspParameters.KeyNumber, keyParam);
		}
	}
}
