using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

public sealed class CngKey : IDisposable
{
	private readonly SafeNCryptKeyHandle _keyHandle;

	private readonly SafeNCryptProviderHandle _providerHandle;

	private static readonly byte[] s_pkcs12TripleDesOidBytes = Encoding.ASCII.GetBytes("1.2.840.113549.1.12.1.3\0");

	public CngAlgorithm Algorithm
	{
		get
		{
			string propertyAsString = _keyHandle.GetPropertyAsString("Algorithm Name", CngPropertyOptions.None);
			return new CngAlgorithm(propertyAsString);
		}
	}

	public CngAlgorithmGroup? AlgorithmGroup
	{
		get
		{
			string propertyAsString = _keyHandle.GetPropertyAsString("Algorithm Group", CngPropertyOptions.None);
			if (propertyAsString == null)
			{
				return null;
			}
			return new CngAlgorithmGroup(propertyAsString);
		}
	}

	public CngExportPolicies ExportPolicy
	{
		get
		{
			return (CngExportPolicies)_keyHandle.GetPropertyAsDword("Export Policy", CngPropertyOptions.None);
		}
		internal set
		{
			_keyHandle.SetExportPolicy(value);
		}
	}

	public SafeNCryptKeyHandle Handle => _keyHandle.Duplicate();

	public unsafe bool IsEphemeral
	{
		get
		{
			Unsafe.SkipInit(out byte b);
			if (global::Interop.NCrypt.NCryptGetProperty(_keyHandle, "CLR IsEphemeral", &b, 1, out var pcbResult, CngPropertyOptions.CustomProperty) != 0)
			{
				return false;
			}
			if (pcbResult != 1)
			{
				return false;
			}
			if (b != 1)
			{
				return false;
			}
			return true;
		}
		private set
		{
			byte b = (byte)(value ? 1 : 0);
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(_keyHandle, "CLR IsEphemeral", &b, 1, CngPropertyOptions.CustomProperty);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
		}
	}

	public bool IsMachineKey
	{
		get
		{
			CngKeyOpenOptions propertyAsDword = (CngKeyOpenOptions)_keyHandle.GetPropertyAsDword("Key Type", CngPropertyOptions.None);
			return (propertyAsDword & CngKeyOpenOptions.MachineKey) == CngKeyOpenOptions.MachineKey;
		}
	}

	public string? KeyName
	{
		get
		{
			if (IsEphemeral)
			{
				return null;
			}
			return _keyHandle.GetPropertyAsString("Name", CngPropertyOptions.None);
		}
	}

	public int KeySize
	{
		get
		{
			int result = 0;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetIntProperty(_keyHandle, "PublicKeyLength", ref result);
			if (errorCode != 0)
			{
				errorCode = global::Interop.NCrypt.NCryptGetIntProperty(_keyHandle, "Length", ref result);
			}
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
			return result;
		}
	}

	public CngKeyUsages KeyUsage => (CngKeyUsages)_keyHandle.GetPropertyAsDword("Key Usage", CngPropertyOptions.None);

	public unsafe IntPtr ParentWindowHandle
	{
		get
		{
			return _keyHandle.GetPropertyAsIntPtr("HWND Handle", CngPropertyOptions.None);
		}
		set
		{
			global::Interop.NCrypt.NCryptSetProperty(_keyHandle, "HWND Handle", &value, IntPtr.Size, CngPropertyOptions.None);
		}
	}

	public CngProvider? Provider
	{
		get
		{
			string propertyAsString = _providerHandle.GetPropertyAsString("Name", CngPropertyOptions.None);
			if (propertyAsString == null)
			{
				return null;
			}
			return new CngProvider(propertyAsString);
		}
	}

	public SafeNCryptProviderHandle ProviderHandle => _providerHandle.Duplicate();

	public unsafe CngUIPolicy UIPolicy
	{
		get
		{
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetProperty(_keyHandle, "UI Policy", null, 0, out var pcbResult, CngPropertyOptions.None);
			if (errorCode != 0 && errorCode != global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND)
			{
				throw errorCode.ToCryptographicException();
			}
			CngUIProtectionLevels protectionLevel;
			string friendlyName;
			string description;
			string creationTitle;
			if (errorCode != 0 || pcbResult == 0)
			{
				protectionLevel = CngUIProtectionLevels.None;
				friendlyName = null;
				description = null;
				creationTitle = null;
			}
			else
			{
				if (pcbResult < sizeof(global::Interop.NCrypt.NCRYPT_UI_POLICY))
				{
					throw global::Interop.NCrypt.ErrorCode.E_FAIL.ToCryptographicException();
				}
				byte[] array = new byte[pcbResult];
				fixed (byte* ptr = &array[0])
				{
					errorCode = global::Interop.NCrypt.NCryptGetProperty(_keyHandle, "UI Policy", ptr, array.Length, out pcbResult, CngPropertyOptions.None);
					if (errorCode != 0)
					{
						throw errorCode.ToCryptographicException();
					}
					global::Interop.NCrypt.NCRYPT_UI_POLICY* ptr2 = (global::Interop.NCrypt.NCRYPT_UI_POLICY*)ptr;
					protectionLevel = ptr2->dwFlags;
					friendlyName = Marshal.PtrToStringUni(ptr2->pszFriendlyName);
					description = Marshal.PtrToStringUni(ptr2->pszDescription);
					creationTitle = Marshal.PtrToStringUni(ptr2->pszCreationTitle);
				}
			}
			string propertyAsString = _keyHandle.GetPropertyAsString("Use Context", CngPropertyOptions.None);
			return new CngUIPolicy(protectionLevel, friendlyName, description, propertyAsString, creationTitle);
		}
	}

	public string? UniqueName
	{
		get
		{
			if (IsEphemeral)
			{
				return null;
			}
			return _keyHandle.GetPropertyAsString("Unique Name", CngPropertyOptions.None);
		}
	}

	private CngKey(SafeNCryptProviderHandle providerHandle, SafeNCryptKeyHandle keyHandle)
	{
		_providerHandle = providerHandle;
		_keyHandle = keyHandle;
	}

	public void Dispose()
	{
		if (_providerHandle != null)
		{
			_providerHandle.Dispose();
		}
		if (_keyHandle != null)
		{
			_keyHandle.Dispose();
		}
	}

	public CngProperty GetProperty(string name, CngPropertyOptions options)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		byte[] array = _keyHandle.GetProperty(name, options);
		if (array == null)
		{
			throw global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND.ToCryptographicException();
		}
		if (array.Length == 0)
		{
			array = null;
		}
		return new CngProperty(name, array, options);
	}

	public unsafe bool HasProperty(string name, CngPropertyOptions options)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		int pcbResult;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetProperty(_keyHandle, name, null, 0, out pcbResult, options);
		return errorCode switch
		{
			global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND => false, 
			global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS => true, 
			_ => throw errorCode.ToCryptographicException(), 
		};
	}

	public unsafe void SetProperty(CngProperty property)
	{
		byte[] valueWithoutCopying = property.GetValueWithoutCopying();
		if (valueWithoutCopying == null)
		{
			throw global::Interop.NCrypt.ErrorCode.NTE_INVALID_PARAMETER.ToCryptographicException();
		}
		fixed (byte* pbInput = valueWithoutCopying.MapZeroLengthArrayToNonNullPointer())
		{
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(_keyHandle, property.Name, pbInput, valueWithoutCopying.Length, property.Options);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
		}
	}

	public static CngKey Create(CngAlgorithm algorithm)
	{
		return Create(algorithm, null);
	}

	public static CngKey Create(CngAlgorithm algorithm, string? keyName)
	{
		return Create(algorithm, keyName, null);
	}

	public static CngKey Create(CngAlgorithm algorithm, string? keyName, CngKeyCreationParameters? creationParameters)
	{
		if (algorithm == null)
		{
			throw new ArgumentNullException("algorithm");
		}
		if (creationParameters == null)
		{
			creationParameters = new CngKeyCreationParameters();
		}
		SafeNCryptProviderHandle safeNCryptProviderHandle = creationParameters.Provider.OpenStorageProvider();
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptCreatePersistedKey(safeNCryptProviderHandle, out var phKey, algorithm.Algorithm, keyName, 0, creationParameters.KeyCreationOptions);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		InitializeKeyProperties(phKey, creationParameters);
		errorCode = global::Interop.NCrypt.NCryptFinalizeKey(phKey, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		CngKey cngKey = new CngKey(safeNCryptProviderHandle, phKey);
		if (keyName == null)
		{
			cngKey.IsEphemeral = true;
		}
		return cngKey;
	}

	private unsafe static void InitializeKeyProperties(SafeNCryptKeyHandle keyHandle, CngKeyCreationParameters creationParameters)
	{
		if (creationParameters.ExportPolicy.HasValue)
		{
			CngExportPolicies value = creationParameters.ExportPolicy.Value;
			keyHandle.SetExportPolicy(value);
		}
		if (creationParameters.KeyUsage.HasValue)
		{
			CngKeyUsages value2 = creationParameters.KeyUsage.Value;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "Key Usage", &value2, 4, CngPropertyOptions.Persist);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
		}
		if (creationParameters.ParentWindowHandle != IntPtr.Zero)
		{
			IntPtr parentWindowHandle = creationParameters.ParentWindowHandle;
			global::Interop.NCrypt.ErrorCode errorCode2 = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "HWND Handle", &parentWindowHandle, sizeof(IntPtr), CngPropertyOptions.None);
			if (errorCode2 != 0)
			{
				throw errorCode2.ToCryptographicException();
			}
		}
		CngUIPolicy uIPolicy = creationParameters.UIPolicy;
		if (uIPolicy != null)
		{
			InitializeKeyUiPolicyProperties(keyHandle, uIPolicy);
		}
		foreach (CngProperty parameter in creationParameters.Parameters)
		{
			byte[] valueWithoutCopying = parameter.GetValueWithoutCopying();
			int cbInput = ((valueWithoutCopying != null) ? valueWithoutCopying.Length : 0);
			fixed (byte* pbInput = valueWithoutCopying.MapZeroLengthArrayToNonNullPointer())
			{
				global::Interop.NCrypt.ErrorCode errorCode3 = global::Interop.NCrypt.NCryptSetProperty(keyHandle, parameter.Name, pbInput, cbInput, parameter.Options);
				if (errorCode3 != 0)
				{
					throw errorCode3.ToCryptographicException();
				}
			}
		}
	}

	private unsafe static void InitializeKeyUiPolicyProperties(SafeNCryptKeyHandle keyHandle, CngUIPolicy uiPolicy)
	{
		//The blocks IL_0031, IL_0039, IL_004a, IL_00b2 are reachable both inside and outside the pinned region starting at IL_002c. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		fixed (char* value3 = uiPolicy.CreationTitle)
		{
			string? friendlyName = uiPolicy.FriendlyName;
			char* intPtr;
			string? description;
			global::Interop.NCrypt.NCRYPT_UI_POLICY nCRYPT_UI_POLICY2;
			if (friendlyName == null)
			{
				char* value;
				intPtr = (value = null);
				description = uiPolicy.Description;
				fixed (char* ptr = description)
				{
					char* value2 = ptr;
					global::Interop.NCrypt.NCRYPT_UI_POLICY nCRYPT_UI_POLICY = default(global::Interop.NCrypt.NCRYPT_UI_POLICY);
					nCRYPT_UI_POLICY.dwVersion = 1;
					nCRYPT_UI_POLICY.dwFlags = uiPolicy.ProtectionLevel;
					nCRYPT_UI_POLICY.pszCreationTitle = new IntPtr(value3);
					nCRYPT_UI_POLICY.pszFriendlyName = new IntPtr(value);
					nCRYPT_UI_POLICY.pszDescription = new IntPtr(value2);
					nCRYPT_UI_POLICY2 = nCRYPT_UI_POLICY;
					global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "UI Policy", &nCRYPT_UI_POLICY2, sizeof(global::Interop.NCrypt.NCRYPT_UI_POLICY), CngPropertyOptions.Persist);
					if (errorCode != 0)
					{
						throw errorCode.ToCryptographicException();
					}
				}
			}
			else
			{
				fixed (char* ptr2 = &friendlyName.GetPinnableReference())
				{
					char* value;
					intPtr = (value = ptr2);
					description = uiPolicy.Description;
					fixed (char* ptr = description)
					{
						char* value2 = ptr;
						global::Interop.NCrypt.NCRYPT_UI_POLICY nCRYPT_UI_POLICY = default(global::Interop.NCrypt.NCRYPT_UI_POLICY);
						nCRYPT_UI_POLICY.dwVersion = 1;
						nCRYPT_UI_POLICY.dwFlags = uiPolicy.ProtectionLevel;
						nCRYPT_UI_POLICY.pszCreationTitle = new IntPtr(value3);
						nCRYPT_UI_POLICY.pszFriendlyName = new IntPtr(value);
						nCRYPT_UI_POLICY.pszDescription = new IntPtr(value2);
						nCRYPT_UI_POLICY2 = nCRYPT_UI_POLICY;
						global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "UI Policy", &nCRYPT_UI_POLICY2, sizeof(global::Interop.NCrypt.NCRYPT_UI_POLICY), CngPropertyOptions.Persist);
						if (errorCode != 0)
						{
							throw errorCode.ToCryptographicException();
						}
					}
				}
			}
		}
		string useContext = uiPolicy.UseContext;
		if (useContext == null)
		{
			return;
		}
		int cbInput = checked((useContext.Length + 1) * 2);
		fixed (char* pbInput = useContext)
		{
			global::Interop.NCrypt.ErrorCode errorCode2 = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "Use Context", pbInput, cbInput, CngPropertyOptions.Persist);
			if (errorCode2 != 0)
			{
				throw errorCode2.ToCryptographicException();
			}
		}
	}

	internal bool IsECNamedCurve()
	{
		return IsECNamedCurve(Algorithm.Algorithm);
	}

	internal static bool IsECNamedCurve(string algorithm)
	{
		if (!(algorithm == CngAlgorithm.ECDiffieHellman.Algorithm))
		{
			return algorithm == CngAlgorithm.ECDsa.Algorithm;
		}
		return true;
	}

	internal string GetCurveName(out string oidValue)
	{
		if (IsECNamedCurve())
		{
			oidValue = null;
			return _keyHandle.GetPropertyAsString("ECCCurveName", CngPropertyOptions.None);
		}
		return GetECSpecificCurveName(out oidValue);
	}

	private string GetECSpecificCurveName(out string oidValue)
	{
		string algorithm = Algorithm.Algorithm;
		if (algorithm == CngAlgorithm.ECDiffieHellmanP256.Algorithm || algorithm == CngAlgorithm.ECDsaP256.Algorithm)
		{
			oidValue = "1.2.840.10045.3.1.7";
			return "nistP256";
		}
		if (algorithm == CngAlgorithm.ECDiffieHellmanP384.Algorithm || algorithm == CngAlgorithm.ECDsaP384.Algorithm)
		{
			oidValue = "1.3.132.0.34";
			return "nistP384";
		}
		if (algorithm == CngAlgorithm.ECDiffieHellmanP521.Algorithm || algorithm == CngAlgorithm.ECDsaP521.Algorithm)
		{
			oidValue = "1.3.132.0.35";
			return "nistP521";
		}
		throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, algorithm));
	}

	internal static CngProperty GetPropertyFromNamedCurve(ECCurve curve)
	{
		string friendlyName = curve.Oid.FriendlyName;
		byte[] array = new byte[(friendlyName.Length + 1) * 2];
		Encoding.Unicode.GetBytes(friendlyName, 0, friendlyName.Length, array, 0);
		return new CngProperty("ECCCurveName", array, CngPropertyOptions.None);
	}

	internal static CngAlgorithm EcdsaCurveNameToAlgorithm(string name)
	{
		switch (name)
		{
		case "nistP256":
		case "ECDSA_P256":
			return CngAlgorithm.ECDsaP256;
		case "nistP384":
		case "ECDSA_P384":
			return CngAlgorithm.ECDsaP384;
		case "nistP521":
		case "ECDSA_P521":
			return CngAlgorithm.ECDsaP521;
		default:
			return CngAlgorithm.ECDsa;
		}
	}

	internal static CngAlgorithm EcdhCurveNameToAlgorithm(string name)
	{
		switch (name)
		{
		case "nistP256":
		case "ECDH_P256":
			return CngAlgorithm.ECDiffieHellmanP256;
		case "nistP384":
		case "ECDH_P384":
			return CngAlgorithm.ECDiffieHellmanP384;
		case "nistP521":
		case "ECDH_P521":
			return CngAlgorithm.ECDiffieHellmanP521;
		default:
			return CngAlgorithm.ECDiffieHellman;
		}
	}

	internal static CngKey Import(ReadOnlySpan<byte> keyBlob, CngKeyBlobFormat format)
	{
		return Import(keyBlob, null, format, CngProvider.MicrosoftSoftwareKeyStorageProvider);
	}

	public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format)
	{
		return Import(keyBlob, format, CngProvider.MicrosoftSoftwareKeyStorageProvider);
	}

	internal static CngKey Import(byte[] keyBlob, string curveName, CngKeyBlobFormat format)
	{
		return Import(keyBlob, curveName, format, CngProvider.MicrosoftSoftwareKeyStorageProvider);
	}

	public static CngKey Import(byte[] keyBlob, CngKeyBlobFormat format, CngProvider provider)
	{
		return Import(keyBlob, null, format, provider);
	}

	internal static CngKey ImportEncryptedPkcs8(ReadOnlySpan<byte> keyBlob, ReadOnlySpan<char> password)
	{
		return ImportEncryptedPkcs8(keyBlob, password, CngProvider.MicrosoftSoftwareKeyStorageProvider);
	}

	internal unsafe static CngKey ImportEncryptedPkcs8(ReadOnlySpan<byte> keyBlob, ReadOnlySpan<char> password, CngProvider provider)
	{
		SafeNCryptProviderHandle safeNCryptProviderHandle = provider.OpenStorageProvider();
		SafeNCryptKeyHandle phKey;
		using (Microsoft.Win32.SafeHandles.SafeUnicodeStringHandle safeUnicodeStringHandle = new Microsoft.Win32.SafeHandles.SafeUnicodeStringHandle(password))
		{
			global::Interop.NCrypt.NCryptBuffer* ptr = stackalloc global::Interop.NCrypt.NCryptBuffer[1];
			*ptr = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsSecret,
				cbBuffer = checked(2 * (password.Length + 1)),
				pvBuffer = safeUnicodeStringHandle.DangerousGetHandle()
			};
			if (ptr->pvBuffer == IntPtr.Zero)
			{
				ptr->cbBuffer = 0;
			}
			global::Interop.NCrypt.NCryptBufferDesc nCryptBufferDesc = default(global::Interop.NCrypt.NCryptBufferDesc);
			nCryptBufferDesc.cBuffers = 1;
			nCryptBufferDesc.pBuffers = (IntPtr)ptr;
			nCryptBufferDesc.ulVersion = 0;
			global::Interop.NCrypt.NCryptBufferDesc pParameterList = nCryptBufferDesc;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptImportKey(safeNCryptProviderHandle, IntPtr.Zero, "PKCS8_PRIVATEKEY", ref pParameterList, out phKey, ref MemoryMarshal.GetReference(keyBlob), keyBlob.Length, 0);
			if (errorCode != 0)
			{
				phKey.Dispose();
				throw errorCode.ToCryptographicException();
			}
		}
		CngKey cngKey = new CngKey(safeNCryptProviderHandle, phKey);
		cngKey.IsEphemeral = true;
		return cngKey;
	}

	internal static CngKey Import(byte[] keyBlob, string curveName, CngKeyBlobFormat format, CngProvider provider)
	{
		if (keyBlob == null)
		{
			throw new ArgumentNullException("keyBlob");
		}
		return Import(new ReadOnlySpan<byte>(keyBlob), curveName, format, provider);
	}

	internal static CngKey Import(ReadOnlySpan<byte> keyBlob, string curveName, CngKeyBlobFormat format, CngProvider provider)
	{
		if (format == null)
		{
			throw new ArgumentNullException("format");
		}
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		SafeNCryptProviderHandle safeNCryptProviderHandle = provider.OpenStorageProvider();
		SafeNCryptKeyHandle phKey = null;
		if (curveName == null)
		{
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptImportKey(safeNCryptProviderHandle, IntPtr.Zero, format.Format, IntPtr.Zero, out phKey, ref MemoryMarshal.GetReference(keyBlob), keyBlob.Length, 0);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
		}
		else
		{
			phKey = System.Security.Cryptography.ECCng.ImportKeyBlob(format.Format, keyBlob, curveName, safeNCryptProviderHandle);
		}
		CngKey cngKey = new CngKey(safeNCryptProviderHandle, phKey);
		cngKey.IsEphemeral = format != CngKeyBlobFormat.OpaqueTransportBlob;
		return cngKey;
	}

	public byte[] Export(CngKeyBlobFormat format)
	{
		if (format == null)
		{
			throw new ArgumentNullException("format");
		}
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, format.Format, IntPtr.Zero, null, 0, out var pcbResult, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		byte[] array = new byte[pcbResult];
		errorCode = global::Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, format.Format, IntPtr.Zero, array, array.Length, out pcbResult, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		Array.Resize(ref array, pcbResult);
		return array;
	}

	internal bool TryExportKeyBlob(string blobType, Span<byte> destination, out int bytesWritten)
	{
		Span<byte> span = default(Span<byte>);
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, blobType, IntPtr.Zero, ref MemoryMarshal.GetReference(span), span.Length, out var pcbResult, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		if (pcbResult > destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		errorCode = global::Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, blobType, IntPtr.Zero, ref MemoryMarshal.GetReference(destination), destination.Length, out pcbResult, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		bytesWritten = pcbResult;
		return true;
	}

	internal byte[] ExportPkcs8KeyBlob(ReadOnlySpan<char> password, int kdfCount)
	{
		int bytesWritten;
		byte[] allocated;
		bool flag = ExportPkcs8KeyBlob(allocate: true, _keyHandle, password, kdfCount, Span<byte>.Empty, out bytesWritten, out allocated);
		return allocated;
	}

	internal bool TryExportPkcs8KeyBlob(ReadOnlySpan<char> password, int kdfCount, Span<byte> destination, out int bytesWritten)
	{
		byte[] allocated;
		return ExportPkcs8KeyBlob(allocate: false, _keyHandle, password, kdfCount, destination, out bytesWritten, out allocated);
	}

	internal unsafe static bool ExportPkcs8KeyBlob(bool allocate, SafeNCryptKeyHandle keyHandle, ReadOnlySpan<char> password, int kdfCount, Span<byte> destination, out int bytesWritten, out byte[] allocated)
	{
		using Microsoft.Win32.SafeHandles.SafeUnicodeStringHandle safeUnicodeStringHandle = new Microsoft.Win32.SafeHandles.SafeUnicodeStringHandle(password);
		fixed (byte* ptr2 = s_pkcs12TripleDesOidBytes)
		{
			global::Interop.NCrypt.NCryptBuffer* ptr = stackalloc global::Interop.NCrypt.NCryptBuffer[3];
			global::Interop.NCrypt.PBE_PARAMS pBE_PARAMS = default(global::Interop.NCrypt.PBE_PARAMS);
			Span<byte> data = new Span<byte>(pBE_PARAMS.rgbSalt, 8);
			RandomNumberGenerator.Fill(data);
			pBE_PARAMS.Params.cbSalt = data.Length;
			pBE_PARAMS.Params.iIterations = kdfCount;
			*ptr = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsSecret,
				cbBuffer = checked(2 * (password.Length + 1)),
				pvBuffer = safeUnicodeStringHandle.DangerousGetHandle()
			};
			if (ptr->pvBuffer == IntPtr.Zero)
			{
				ptr->cbBuffer = 0;
			}
			ptr[1] = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsAlgOid,
				cbBuffer = s_pkcs12TripleDesOidBytes.Length,
				pvBuffer = (IntPtr)ptr2
			};
			ptr[2] = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsAlgParam,
				cbBuffer = sizeof(global::Interop.NCrypt.PBE_PARAMS),
				pvBuffer = (IntPtr)(&pBE_PARAMS)
			};
			global::Interop.NCrypt.NCryptBufferDesc nCryptBufferDesc = default(global::Interop.NCrypt.NCryptBufferDesc);
			nCryptBufferDesc.cBuffers = 3;
			nCryptBufferDesc.pBuffers = (IntPtr)ptr;
			nCryptBufferDesc.ulVersion = 0;
			global::Interop.NCrypt.NCryptBufferDesc pParameterList = nCryptBufferDesc;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptExportKey(keyHandle, IntPtr.Zero, "PKCS8_PRIVATEKEY", ref pParameterList, ref MemoryMarshal.GetReference(default(Span<byte>)), 0, out var pcbResult, 0);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
			allocated = null;
			if (allocate)
			{
				allocated = new byte[pcbResult];
				destination = allocated;
			}
			else if (pcbResult > destination.Length)
			{
				bytesWritten = 0;
				return false;
			}
			errorCode = global::Interop.NCrypt.NCryptExportKey(keyHandle, IntPtr.Zero, "PKCS8_PRIVATEKEY", ref pParameterList, ref MemoryMarshal.GetReference(destination), destination.Length, out pcbResult, 0);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
			if (allocate && pcbResult != destination.Length)
			{
				byte[] array = new byte[pcbResult];
				destination.Slice(0, pcbResult).CopyTo(array);
				Array.Clear(allocated, 0, pcbResult);
				allocated = array;
			}
			bytesWritten = pcbResult;
			return true;
		}
	}

	public void Delete()
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptDeleteKey(_keyHandle, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		_keyHandle.SetHandleAsInvalid();
		Dispose();
	}

	public static bool Exists(string keyName)
	{
		return Exists(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider);
	}

	public static bool Exists(string keyName, CngProvider provider)
	{
		return Exists(keyName, provider, CngKeyOpenOptions.None);
	}

	public static bool Exists(string keyName, CngProvider provider, CngKeyOpenOptions options)
	{
		if (keyName == null)
		{
			throw new ArgumentNullException("keyName");
		}
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		using SafeNCryptProviderHandle hProvider = provider.OpenStorageProvider();
		SafeNCryptKeyHandle phKey = null;
		try
		{
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptOpenKey(hProvider, out phKey, keyName, 0, options);
			return errorCode switch
			{
				global::Interop.NCrypt.ErrorCode.NTE_BAD_KEYSET => false, 
				global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS => true, 
				_ => throw errorCode.ToCryptographicException(), 
			};
		}
		finally
		{
			phKey?.Dispose();
		}
	}

	public static CngKey Open(string keyName)
	{
		return Open(keyName, CngProvider.MicrosoftSoftwareKeyStorageProvider);
	}

	public static CngKey Open(string keyName, CngProvider provider)
	{
		return Open(keyName, provider, CngKeyOpenOptions.None);
	}

	public static CngKey Open(string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
	{
		if (keyName == null)
		{
			throw new ArgumentNullException("keyName");
		}
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		SafeNCryptProviderHandle safeNCryptProviderHandle = provider.OpenStorageProvider();
		SafeNCryptKeyHandle phKey;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptOpenKey(safeNCryptProviderHandle, out phKey, keyName, 0, openOptions);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		return new CngKey(safeNCryptProviderHandle, phKey);
	}

	public static CngKey Open(SafeNCryptKeyHandle keyHandle, CngKeyHandleOpenOptions keyHandleOpenOptions)
	{
		if (keyHandle == null)
		{
			throw new ArgumentNullException("keyHandle");
		}
		if (keyHandle.IsClosed || keyHandle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Cryptography_OpenInvalidHandle, "keyHandle");
		}
		SafeNCryptKeyHandle keyHandle2 = keyHandle.Duplicate();
		SafeNCryptProviderHandle safeNCryptProviderHandle = new SafeNCryptProviderHandle();
		IntPtr propertyAsIntPtr = keyHandle.GetPropertyAsIntPtr("Provider Handle", CngPropertyOptions.None);
		safeNCryptProviderHandle.SetHandleValue(propertyAsIntPtr);
		CngKey cngKey = null;
		try
		{
			cngKey = new CngKey(safeNCryptProviderHandle, keyHandle2);
			bool flag = (keyHandleOpenOptions & CngKeyHandleOpenOptions.EphemeralKey) == CngKeyHandleOpenOptions.EphemeralKey;
			if (!cngKey.IsEphemeral && flag)
			{
				cngKey.IsEphemeral = true;
			}
			else if (cngKey.IsEphemeral && !flag)
			{
				throw new ArgumentException(System.SR.Cryptography_OpenEphemeralKeyHandleWithoutEphemeralFlag, "keyHandleOpenOptions");
			}
		}
		catch
		{
			cngKey?.Dispose();
			throw;
		}
		return cngKey;
	}
}
