using System.Buffers.Binary;
using System.IO;
using System.Runtime.Versioning;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

public sealed class DSACryptoServiceProvider : DSA, ICspAsymmetricAlgorithm
{
	private int _keySize;

	private readonly CspParameters _parameters;

	private readonly bool _randomKeyContainer;

	private SafeKeyHandle _safeKeyHandle;

	private SafeProvHandle _safeProvHandle;

	private readonly SHA1 _sha1;

	private static volatile CspProviderFlags s_useMachineKeyStore;

	private bool _disposed;

	private SafeProvHandle SafeProvHandle
	{
		get
		{
			if (_safeProvHandle == null)
			{
				lock (_parameters)
				{
					if (_safeProvHandle == null)
					{
						SafeProvHandle safeProvHandle = CapiHelper.CreateProvHandle(_parameters, _randomKeyContainer);
						_safeProvHandle = safeProvHandle;
					}
				}
				return _safeProvHandle;
			}
			return _safeProvHandle;
		}
		set
		{
			lock (_parameters)
			{
				SafeProvHandle safeProvHandle = _safeProvHandle;
				if (value != safeProvHandle)
				{
					if (safeProvHandle != null)
					{
						SafeKeyHandle safeKeyHandle = _safeKeyHandle;
						_safeKeyHandle = null;
						safeKeyHandle?.Dispose();
						safeProvHandle.Dispose();
					}
					_safeProvHandle = value;
				}
			}
		}
	}

	private SafeKeyHandle SafeKeyHandle
	{
		get
		{
			if (_safeKeyHandle == null)
			{
				lock (_parameters)
				{
					if (_safeKeyHandle == null)
					{
						SafeKeyHandle keyPairHelper = CapiHelper.GetKeyPairHelper(CapiHelper.CspAlgorithmType.Dss, _parameters, _keySize, SafeProvHandle);
						_safeKeyHandle = keyPairHelper;
					}
				}
			}
			return _safeKeyHandle;
		}
		set
		{
			lock (_parameters)
			{
				SafeKeyHandle safeKeyHandle = _safeKeyHandle;
				if (value != safeKeyHandle)
				{
					_safeKeyHandle = value;
					safeKeyHandle?.Dispose();
				}
			}
		}
	}

	[SupportedOSPlatform("windows")]
	public CspKeyContainerInfo CspKeyContainerInfo
	{
		get
		{
			SafeKeyHandle safeKeyHandle = SafeKeyHandle;
			return new CspKeyContainerInfo(_parameters, _randomKeyContainer);
		}
	}

	public override int KeySize
	{
		get
		{
			byte[] keyParameter = CapiHelper.GetKeyParameter(SafeKeyHandle, 1);
			_keySize = BinaryPrimitives.ReadInt32LittleEndian(keyParameter);
			return _keySize;
		}
	}

	public override KeySizes[] LegalKeySizes => new KeySizes[1]
	{
		new KeySizes(512, 1024, 64)
	};

	public bool PersistKeyInCsp
	{
		get
		{
			return CapiHelper.GetPersistKeyInCsp(SafeProvHandle);
		}
		set
		{
			bool persistKeyInCsp = PersistKeyInCsp;
			if (value != persistKeyInCsp)
			{
				CapiHelper.SetPersistKeyInCsp(SafeProvHandle, value);
			}
		}
	}

	public bool PublicOnly
	{
		get
		{
			byte[] keyParameter = CapiHelper.GetKeyParameter(SafeKeyHandle, 2);
			return keyParameter[0] == 1;
		}
	}

	public static bool UseMachineKeyStore
	{
		get
		{
			return s_useMachineKeyStore == CspProviderFlags.UseMachineKeyStore;
		}
		set
		{
			s_useMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags);
		}
	}

	public override string? KeyExchangeAlgorithm => null;

	public override string SignatureAlgorithm => "http://www.w3.org/2000/09/xmldsig#dsa-sha1";

	public DSACryptoServiceProvider()
		: this(new CspParameters(13, null, null, s_useMachineKeyStore))
	{
	}

	public DSACryptoServiceProvider(int dwKeySize)
		: this(dwKeySize, new CspParameters(13, null, null, s_useMachineKeyStore))
	{
	}

	[SupportedOSPlatform("windows")]
	public DSACryptoServiceProvider(CspParameters? parameters)
		: this(0, parameters)
	{
	}

	[SupportedOSPlatform("windows")]
	public DSACryptoServiceProvider(int dwKeySize, CspParameters? parameters)
	{
		if (dwKeySize < 0)
		{
			throw new ArgumentOutOfRangeException("dwKeySize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		_parameters = CapiHelper.SaveCspParameters(CapiHelper.CspAlgorithmType.Dss, parameters, s_useMachineKeyStore, out _randomKeyContainer);
		_keySize = dwKeySize;
		_sha1 = SHA1.Create();
		if (!_randomKeyContainer)
		{
			SafeKeyHandle safeKeyHandle = SafeKeyHandle;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
			{
				_safeKeyHandle.Dispose();
			}
			if (_safeProvHandle != null && !_safeProvHandle.IsClosed)
			{
				_safeProvHandle.Dispose();
			}
			_disposed = true;
		}
		base.Dispose(disposing);
	}

	public byte[] ExportCspBlob(bool includePrivateParameters)
	{
		return CapiHelper.ExportKeyBlob(includePrivateParameters, SafeKeyHandle);
	}

	public override DSAParameters ExportParameters(bool includePrivateParameters)
	{
		byte[] cspBlob = ExportCspBlob(includePrivateParameters);
		byte[] cspPublicBlob = null;
		if (includePrivateParameters)
		{
			byte keyBlobHeaderVersion = CapiHelper.GetKeyBlobHeaderVersion(cspBlob);
			if (keyBlobHeaderVersion <= 2)
			{
				cspPublicBlob = ExportCspBlob(includePrivateParameters: false);
			}
		}
		return cspBlob.ToDSAParameters(includePrivateParameters, cspPublicBlob);
	}

	private SafeProvHandle AcquireSafeProviderHandle()
	{
		CapiHelper.AcquireCsp(new CspParameters(13), out var safeProvHandle);
		return safeProvHandle;
	}

	public void ImportCspBlob(byte[] keyBlob)
	{
		ThrowIfDisposed();
		SafeKeyHandle safeKeyHandle;
		if (IsPublic(keyBlob))
		{
			SafeProvHandle safeProvHandle = AcquireSafeProviderHandle();
			CapiHelper.ImportKeyBlob(safeProvHandle, CspProviderFlags.NoFlags, addNoSaltFlag: false, keyBlob, out safeKeyHandle);
			SafeProvHandle = safeProvHandle;
		}
		else
		{
			CapiHelper.ImportKeyBlob(SafeProvHandle, _parameters.Flags, addNoSaltFlag: false, keyBlob, out safeKeyHandle);
		}
		SafeKeyHandle = safeKeyHandle;
	}

	public override void ImportParameters(DSAParameters parameters)
	{
		byte[] keyBlob = parameters.ToKeyBlob();
		ImportCspBlob(keyBlob);
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		base.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
	}

	public byte[] SignData(Stream inputStream)
	{
		byte[] rgbHash = _sha1.ComputeHash(inputStream);
		return SignHash(rgbHash, null);
	}

	public byte[] SignData(byte[] buffer)
	{
		byte[] rgbHash = _sha1.ComputeHash(buffer);
		return SignHash(rgbHash, null);
	}

	public byte[] SignData(byte[] buffer, int offset, int count)
	{
		byte[] rgbHash = _sha1.ComputeHash(buffer, offset, count);
		return SignHash(rgbHash, null);
	}

	public bool VerifyData(byte[] rgbData, byte[] rgbSignature)
	{
		byte[] rgbHash = _sha1.ComputeHash(rgbData);
		return VerifyHash(rgbHash, null, rgbSignature);
	}

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		return SignHash(rgbHash, null);
	}

	public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
	{
		return VerifyHash(rgbHash, null, rgbSignature);
	}

	protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		if (hashAlgorithm != HashAlgorithmName.SHA1)
		{
			throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
		}
		return _sha1.ComputeHash(data, offset, count);
	}

	protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		if (hashAlgorithm != HashAlgorithmName.SHA1)
		{
			throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
		}
		return _sha1.ComputeHash(data);
	}

	public byte[] SignHash(byte[] rgbHash, string? str)
	{
		if (rgbHash == null)
		{
			throw new ArgumentNullException("rgbHash");
		}
		if (PublicOnly)
		{
			throw new CryptographicException(System.SR.Cryptography_CSP_NoPrivateKey);
		}
		int calgHash = CapiHelper.NameOrOidToHashAlgId(str, OidGroup.HashAlgorithm);
		if (rgbHash.Length != _sha1.HashSize / 8)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidHashSize, "SHA1", _sha1.HashSize / 8));
		}
		return CapiHelper.SignValue(SafeProvHandle, SafeKeyHandle, _parameters.KeyNumber, 8704, calgHash, rgbHash);
	}

	public bool VerifyHash(byte[] rgbHash, string? str, byte[] rgbSignature)
	{
		if (rgbHash == null)
		{
			throw new ArgumentNullException("rgbHash");
		}
		if (rgbSignature == null)
		{
			throw new ArgumentNullException("rgbSignature");
		}
		int calgHash = CapiHelper.NameOrOidToHashAlgId(str, OidGroup.HashAlgorithm);
		return CapiHelper.VerifySign(SafeProvHandle, SafeKeyHandle, 8704, calgHash, rgbHash, rgbSignature);
	}

	private static bool IsPublic(byte[] keyBlob)
	{
		if (keyBlob == null)
		{
			throw new ArgumentNullException("keyBlob");
		}
		if (keyBlob[0] != 6)
		{
			return false;
		}
		if ((keyBlob[11] != 49 && keyBlob[11] != 51) || keyBlob[10] != 83 || keyBlob[9] != 83 || keyBlob[8] != 68)
		{
			return false;
		}
		return true;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("DSACryptoServiceProvider");
		}
	}
}
