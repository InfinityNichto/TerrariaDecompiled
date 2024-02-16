using System.Buffers.Binary;
using System.IO;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

public sealed class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm
{
	private int _keySize;

	private readonly CspParameters _parameters;

	private readonly bool _randomKeyContainer;

	private SafeKeyHandle _safeKeyHandle;

	private SafeProvHandle _safeProvHandle;

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
						SafeKeyHandle keyPairHelper = CapiHelper.GetKeyPairHelper(CapiHelper.CspAlgorithmType.Rsa, _parameters, _keySize, SafeProvHandle);
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
		new KeySizes(384, 16384, 8)
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

	public override string? KeyExchangeAlgorithm
	{
		get
		{
			if (_parameters.KeyNumber == 1)
			{
				return "RSA-PKCS1-KeyEx";
			}
			return null;
		}
	}

	public override string SignatureAlgorithm => "http://www.w3.org/2000/09/xmldsig#rsa-sha1";

	public RSACryptoServiceProvider()
		: this(0, new CspParameters(24, null, null, s_useMachineKeyStore), useDefaultKeySize: true)
	{
	}

	public RSACryptoServiceProvider(int dwKeySize)
		: this(dwKeySize, new CspParameters(24, null, null, s_useMachineKeyStore), useDefaultKeySize: false)
	{
	}

	[SupportedOSPlatform("windows")]
	public RSACryptoServiceProvider(int dwKeySize, CspParameters? parameters)
		: this(dwKeySize, parameters, useDefaultKeySize: false)
	{
	}

	[SupportedOSPlatform("windows")]
	public RSACryptoServiceProvider(CspParameters? parameters)
		: this(0, parameters, useDefaultKeySize: true)
	{
	}

	private RSACryptoServiceProvider(int keySize, CspParameters parameters, bool useDefaultKeySize)
	{
		if (keySize < 0)
		{
			throw new ArgumentOutOfRangeException("dwKeySize", "ArgumentOutOfRange_NeedNonNegNum");
		}
		_parameters = CapiHelper.SaveCspParameters(CapiHelper.CspAlgorithmType.Rsa, parameters, s_useMachineKeyStore, out _randomKeyContainer);
		_keySize = (useDefaultKeySize ? 1024 : keySize);
		if (!_randomKeyContainer)
		{
			SafeKeyHandle safeKeyHandle = SafeKeyHandle;
		}
	}

	public byte[] Decrypt(byte[] rgb, bool fOAEP)
	{
		if (rgb == null)
		{
			throw new ArgumentNullException("rgb");
		}
		int keySize = KeySize;
		if (rgb.Length != keySize / 8)
		{
			throw new CryptographicException(System.SR.Cryptography_RSA_DecryptWrongSize);
		}
		CapiHelper.DecryptKey(SafeKeyHandle, rgb, rgb.Length, fOAEP, out var decryptedData);
		return decryptedData;
	}

	public override byte[] DecryptValue(byte[] rgb)
	{
		return base.DecryptValue(rgb);
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
	}

	public byte[] Encrypt(byte[] rgb, bool fOAEP)
	{
		if (rgb == null)
		{
			throw new ArgumentNullException("rgb");
		}
		if (fOAEP)
		{
			int num = (KeySize + 7) / 8;
			if (num - 42 < rgb.Length)
			{
				throw (-2146893820).ToCryptographicException();
			}
		}
		byte[] pbEncryptedKey = null;
		CapiHelper.EncryptKey(SafeKeyHandle, rgb, rgb.Length, fOAEP, ref pbEncryptedKey);
		return pbEncryptedKey;
	}

	public override byte[] EncryptValue(byte[] rgb)
	{
		return base.EncryptValue(rgb);
	}

	public byte[] ExportCspBlob(bool includePrivateParameters)
	{
		return CapiHelper.ExportKeyBlob(includePrivateParameters, SafeKeyHandle);
	}

	public override RSAParameters ExportParameters(bool includePrivateParameters)
	{
		byte[] cspBlob = ExportCspBlob(includePrivateParameters);
		return cspBlob.ToRSAParameters(includePrivateParameters);
	}

	private SafeProvHandle AcquireSafeProviderHandle()
	{
		CapiHelper.AcquireCsp(new CspParameters(24), out var safeProvHandle);
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

	public override void ImportParameters(RSAParameters parameters)
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

	public byte[] SignData(byte[] buffer, int offset, int count, object halg)
	{
		int calgHash = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithm hashAlgorithm = CapiHelper.ObjToHashAlgorithm(halg);
		byte[] rgbHash = hashAlgorithm.ComputeHash(buffer, offset, count);
		return SignHash(rgbHash, calgHash);
	}

	public byte[] SignData(byte[] buffer, object halg)
	{
		int calgHash = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithm hashAlgorithm = CapiHelper.ObjToHashAlgorithm(halg);
		byte[] rgbHash = hashAlgorithm.ComputeHash(buffer);
		return SignHash(rgbHash, calgHash);
	}

	public byte[] SignData(Stream inputStream, object halg)
	{
		int calgHash = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithm hashAlgorithm = CapiHelper.ObjToHashAlgorithm(halg);
		byte[] rgbHash = hashAlgorithm.ComputeHash(inputStream);
		return SignHash(rgbHash, calgHash);
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
		return SignHash(rgbHash, calgHash);
	}

	private byte[] SignHash(byte[] rgbHash, int calgHash)
	{
		return CapiHelper.SignValue(SafeProvHandle, SafeKeyHandle, _parameters.KeyNumber, 9216, calgHash, rgbHash);
	}

	public bool VerifyData(byte[] buffer, object halg, byte[] signature)
	{
		int calgHash = CapiHelper.ObjToHashAlgId(halg);
		HashAlgorithm hashAlgorithm = CapiHelper.ObjToHashAlgorithm(halg);
		byte[] rgbHash = hashAlgorithm.ComputeHash(buffer);
		return VerifyHash(rgbHash, calgHash, signature);
	}

	public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
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
		return VerifyHash(rgbHash, calgHash, rgbSignature);
	}

	private bool VerifyHash(byte[] rgbHash, int calgHash, byte[] rgbSignature)
	{
		return CapiHelper.VerifySign(SafeProvHandle, SafeKeyHandle, 9216, calgHash, rgbHash, rgbSignature);
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
		if (keyBlob[11] != 49 || keyBlob[10] != 65 || keyBlob[9] != 83 || keyBlob[8] != 82)
		{
			return false;
		}
		return true;
	}

	protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		using HashAlgorithm hashAlgorithm2 = GetHashAlgorithm(hashAlgorithm);
		return hashAlgorithm2.ComputeHash(data, offset, count);
	}

	protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		using HashAlgorithm hashAlgorithm2 = GetHashAlgorithm(hashAlgorithm);
		return hashAlgorithm2.ComputeHash(data);
	}

	private static HashAlgorithm GetHashAlgorithm(HashAlgorithmName hashAlgorithm)
	{
		return hashAlgorithm.Name switch
		{
			"MD5" => MD5.Create(), 
			"SHA1" => SHA1.Create(), 
			"SHA256" => SHA256.Create(), 
			"SHA384" => SHA384.Create(), 
			"SHA512" => SHA512.Create(), 
			_ => throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name), 
		};
	}

	private static int GetAlgorithmId(HashAlgorithmName hashAlgorithm)
	{
		return hashAlgorithm.Name switch
		{
			"MD5" => 32771, 
			"SHA1" => 32772, 
			"SHA256" => 32780, 
			"SHA384" => 32781, 
			"SHA512" => 32782, 
			_ => throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name), 
		};
	}

	public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		if (padding == RSAEncryptionPadding.Pkcs1)
		{
			return Encrypt(data, fOAEP: false);
		}
		if (padding == RSAEncryptionPadding.OaepSHA1)
		{
			return Encrypt(data, fOAEP: true);
		}
		throw PaddingModeNotSupported();
	}

	public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		if (padding == RSAEncryptionPadding.Pkcs1)
		{
			return Decrypt(data, fOAEP: false);
		}
		if (padding == RSAEncryptionPadding.OaepSHA1)
		{
			return Decrypt(data, fOAEP: true);
		}
		throw PaddingModeNotSupported();
	}

	public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		if (padding != RSASignaturePadding.Pkcs1)
		{
			throw PaddingModeNotSupported();
		}
		return SignHash(hash, GetAlgorithmId(hashAlgorithm));
	}

	public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw HashAlgorithmNameNullOrEmpty();
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		if (padding != RSASignaturePadding.Pkcs1)
		{
			throw PaddingModeNotSupported();
		}
		return VerifyHash(hash, GetAlgorithmId(hashAlgorithm), signature);
	}

	private static Exception PaddingModeNotSupported()
	{
		return new CryptographicException(System.SR.Cryptography_InvalidPaddingMode);
	}

	private static Exception HashAlgorithmNameNullOrEmpty()
	{
		return new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("DSACryptoServiceProvider");
		}
	}
}
