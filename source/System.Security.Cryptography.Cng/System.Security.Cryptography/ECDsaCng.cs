using System.IO;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

public sealed class ECDsaCng : ECDsa
{
	private CngAlgorithmCore _core = new CngAlgorithmCore("ECDsaCng");

	private CngAlgorithm _hashAlgorithm = CngAlgorithm.Sha256;

	public CngAlgorithm HashAlgorithm
	{
		get
		{
			return _hashAlgorithm;
		}
		set
		{
			_hashAlgorithm = value ?? throw new ArgumentNullException("value");
		}
	}

	public CngKey Key
	{
		get
		{
			return GetKey();
		}
		private set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (!IsEccAlgorithmGroup(value.AlgorithmGroup))
			{
				throw new ArgumentException(System.SR.Cryptography_ArgECDsaRequiresECDsaKey, "value");
			}
			_core.SetKey(value);
			ForceSetKeySize(value.KeySize);
		}
	}

	public override int KeySize
	{
		get
		{
			return base.KeySize;
		}
		set
		{
			if (KeySize != value)
			{
				base.KeySize = value;
				DisposeKey();
			}
		}
	}

	public override KeySizes[] LegalKeySizes => new KeySizes[2]
	{
		new KeySizes(256, 384, 128),
		new KeySizes(521, 521, 0)
	};

	public ECDsaCng(CngKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (!IsEccAlgorithmGroup(key.AlgorithmGroup))
		{
			throw new ArgumentException(System.SR.Cryptography_ArgECDsaRequiresECDsaKey, "key");
		}
		Key = CngAlgorithmCore.Duplicate(key);
	}

	protected override void Dispose(bool disposing)
	{
		_core.Dispose();
	}

	private void ThrowIfDisposed()
	{
		_core.ThrowIfDisposed();
	}

	private void DisposeKey()
	{
		_core.DisposeKey();
	}

	private static bool IsEccAlgorithmGroup(CngAlgorithmGroup algorithmGroup)
	{
		if (!(algorithmGroup == CngAlgorithmGroup.ECDsa))
		{
			return algorithmGroup == CngAlgorithmGroup.ECDiffieHellman;
		}
		return true;
	}

	internal string GetCurveName(out string oidValue)
	{
		return Key.GetCurveName(out oidValue);
	}

	private void ImportFullKeyBlob(byte[] ecfullKeyBlob, bool includePrivateParameters)
	{
		Key = System.Security.Cryptography.ECCng.ImportFullKeyBlob(ecfullKeyBlob, includePrivateParameters);
	}

	private void ImportKeyBlob(byte[] ecfullKeyBlob, string curveName, bool includePrivateParameters)
	{
		Key = System.Security.Cryptography.ECCng.ImportKeyBlob(ecfullKeyBlob, curveName, includePrivateParameters);
	}

	private byte[] ExportKeyBlob(bool includePrivateParameters)
	{
		return System.Security.Cryptography.ECCng.ExportKeyBlob(Key, includePrivateParameters);
	}

	private byte[] ExportFullKeyBlob(bool includePrivateParameters)
	{
		return System.Security.Cryptography.ECCng.ExportFullKeyBlob(Key, includePrivateParameters);
	}

	private void AcceptImport(System.Security.Cryptography.CngPkcs8.Pkcs8Response response)
	{
		Key = response.Key;
	}

	public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
	{
		return Key.TryExportKeyBlob("PKCS8_PRIVATEKEY", destination, out bytesWritten);
	}

	private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
	{
		return Key.ExportPkcs8KeyBlob(pkcs8Password, kdfCount);
	}

	private bool TryExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount, Span<byte> destination, out int bytesWritten)
	{
		return Key.TryExportPkcs8KeyBlob(pkcs8Password, kdfCount, destination, out bytesWritten);
	}

	public void FromXmlString(string xml, ECKeyXmlFormat format)
	{
		throw new PlatformNotSupportedException();
	}

	public byte[] SignData(byte[] data)
	{
		return SignData(data, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public byte[] SignData(byte[] data, int offset, int count)
	{
		return SignData(data, offset, count, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public byte[] SignData(Stream data)
	{
		return SignData(data, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public string ToXmlString(ECKeyXmlFormat format)
	{
		throw new PlatformNotSupportedException();
	}

	public bool VerifyData(byte[] data, byte[] signature)
	{
		return VerifyData(data, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public bool VerifyData(byte[] data, int offset, int count, byte[] signature)
	{
		return VerifyData(data, offset, count, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public bool VerifyData(Stream data, byte[] signature)
	{
		return VerifyData(data, signature, new HashAlgorithmName(HashAlgorithm.Algorithm));
	}

	public override void GenerateKey(ECCurve curve)
	{
		curve.Validate();
		_core.DisposeKey();
		if (curve.IsNamed)
		{
			if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_InvalidCurveOid, curve.Oid.Value));
			}
			CngAlgorithm cngAlgorithm = CngKey.EcdsaCurveNameToAlgorithm(curve.Oid.FriendlyName);
			if (CngKey.IsECNamedCurve(cngAlgorithm.Algorithm))
			{
				CngKey orGenerateKey = _core.GetOrGenerateKey(curve);
				ForceSetKeySize(orGenerateKey.KeySize);
				return;
			}
			int num = 0;
			if (cngAlgorithm == CngAlgorithm.ECDsaP256)
			{
				num = 256;
			}
			else if (cngAlgorithm == CngAlgorithm.ECDsaP384)
			{
				num = 384;
			}
			else
			{
				if (!(cngAlgorithm == CngAlgorithm.ECDsaP521))
				{
					throw new ArgumentException(System.SR.Cryptography_InvalidKeySize);
				}
				num = 521;
			}
			_core.GetOrGenerateKey(num, cngAlgorithm);
			ForceSetKeySize(num);
		}
		else
		{
			if (!curve.IsExplicit)
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
			}
			CngKey orGenerateKey2 = _core.GetOrGenerateKey(curve);
			ForceSetKeySize(orGenerateKey2.KeySize);
		}
	}

	private CngKey GetKey()
	{
		CngKey cngKey = null;
		if (_core.IsKeyGeneratedNamedCurve())
		{
			return _core.GetOrGenerateKey(null);
		}
		CngAlgorithm cngAlgorithm = null;
		int num = 0;
		num = KeySize;
		cngAlgorithm = num switch
		{
			256 => CngAlgorithm.ECDsaP256, 
			384 => CngAlgorithm.ECDsaP384, 
			521 => CngAlgorithm.ECDsaP521, 
			_ => throw new ArgumentException(System.SR.Cryptography_InvalidKeySize), 
		};
		return _core.GetOrGenerateKey(num, cngAlgorithm);
	}

	private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
	{
		return Key.Handle;
	}

	public override void ImportParameters(ECParameters parameters)
	{
		parameters.Validate();
		ThrowIfDisposed();
		ECCurve curve = parameters.Curve;
		bool flag = parameters.D != null;
		bool flag2 = parameters.Q.X != null && parameters.Q.Y != null;
		if (curve.IsPrime)
		{
			if (!flag2 && flag)
			{
				byte[] array = new byte[parameters.D.Length];
				ECParameters parameters2 = parameters;
				parameters2.Q.X = array;
				parameters2.Q.Y = array;
				byte[] primeCurveBlob = System.Security.Cryptography.ECCng.GetPrimeCurveBlob(ref parameters2, ecdh: false);
				ImportFullKeyBlob(primeCurveBlob, includePrivateParameters: true);
			}
			else
			{
				byte[] primeCurveBlob2 = System.Security.Cryptography.ECCng.GetPrimeCurveBlob(ref parameters, ecdh: false);
				ImportFullKeyBlob(primeCurveBlob2, flag);
			}
			return;
		}
		if (curve.IsNamed)
		{
			if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
			{
				throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_InvalidCurveOid, curve.Oid.Value.ToString()));
			}
			if (!flag2 && flag)
			{
				byte[] array2 = new byte[parameters.D.Length];
				ECParameters parameters3 = parameters;
				parameters3.Q.X = array2;
				parameters3.Q.Y = array2;
				byte[] namedCurveBlob = System.Security.Cryptography.ECCng.GetNamedCurveBlob(ref parameters3, ecdh: false);
				ImportKeyBlob(namedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters: true);
			}
			else
			{
				byte[] namedCurveBlob2 = System.Security.Cryptography.ECCng.GetNamedCurveBlob(ref parameters, ecdh: false);
				ImportKeyBlob(namedCurveBlob2, curve.Oid.FriendlyName, flag);
			}
			return;
		}
		throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
	}

	public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
	{
		byte[] ecBlob = ExportFullKeyBlob(includePrivateParameters);
		ECParameters ecParams = default(ECParameters);
		System.Security.Cryptography.ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters);
		return ecParams;
	}

	public override ECParameters ExportParameters(bool includePrivateParameters)
	{
		ECParameters ecParams = default(ECParameters);
		string oidValue;
		string curveName = GetCurveName(out oidValue);
		if (string.IsNullOrEmpty(curveName))
		{
			byte[] ecBlob = ExportFullKeyBlob(includePrivateParameters);
			System.Security.Cryptography.ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters);
		}
		else
		{
			byte[] ecBlob2 = ExportKeyBlob(includePrivateParameters);
			System.Security.Cryptography.ECCng.ExportNamedCurveParameters(ref ecParams, ecBlob2, includePrivateParameters);
			ecParams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
		}
		return ecParams;
	}

	public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		int bytesRead2;
		System.Security.Cryptography.CngPkcs8.Pkcs8Response response = System.Security.Cryptography.CngPkcs8.ImportPkcs8PrivateKey(source, out bytesRead2);
		ProcessPkcs8Response(response);
		bytesRead = bytesRead2;
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		int bytesRead2;
		System.Security.Cryptography.CngPkcs8.Pkcs8Response response = System.Security.Cryptography.CngPkcs8.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead2);
		ProcessPkcs8Response(response);
		bytesRead = bytesRead2;
	}

	public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
	{
		ThrowIfDisposed();
		int bytesRead2;
		System.Security.Cryptography.CngPkcs8.Pkcs8Response response = System.Security.Cryptography.CngPkcs8.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead2);
		ProcessPkcs8Response(response);
		bytesRead = bytesRead2;
	}

	private void ProcessPkcs8Response(System.Security.Cryptography.CngPkcs8.Pkcs8Response response)
	{
		string algorithmGroup = response.GetAlgorithmGroup();
		if (algorithmGroup == "ECDSA" || algorithmGroup == "ECDH")
		{
			AcceptImport(response);
			return;
		}
		response.FreeKey();
		throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
	}

	public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		return System.Security.Cryptography.CngPkcs8.ExportEncryptedPkcs8PrivateKey(this, passwordBytes, pbeParameters);
	}

	public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		System.Security.Cryptography.PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		if (System.Security.Cryptography.CngPkcs8.IsPlatformScheme(pbeParameters))
		{
			return ExportEncryptedPkcs8(password, pbeParameters.IterationCount);
		}
		return System.Security.Cryptography.CngPkcs8.ExportEncryptedPkcs8PrivateKey(this, password, pbeParameters);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		System.Security.Cryptography.PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
		return System.Security.Cryptography.CngPkcs8.TryExportEncryptedPkcs8PrivateKey(this, passwordBytes, pbeParameters, destination, out bytesWritten);
	}

	public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
	{
		if (pbeParameters == null)
		{
			throw new ArgumentNullException("pbeParameters");
		}
		System.Security.Cryptography.PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
		if (System.Security.Cryptography.CngPkcs8.IsPlatformScheme(pbeParameters))
		{
			return TryExportEncryptedPkcs8(password, pbeParameters.IterationCount, destination, out bytesWritten);
		}
		return System.Security.Cryptography.CngPkcs8.TryExportEncryptedPkcs8PrivateKey(this, password, pbeParameters, destination, out bytesWritten);
	}

	public ECDsaCng(ECCurve curve)
	{
		GenerateKey(curve);
	}

	public ECDsaCng()
		: this(521)
	{
	}

	public ECDsaCng(int keySize)
	{
		KeySize = keySize;
	}

	private void ForceSetKeySize(int newKeySize)
	{
		KeySizeValue = newKeySize;
	}

	protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		return Internal.Cryptography.CngCommon.HashData(data, offset, count, hashAlgorithm);
	}

	protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		return Internal.Cryptography.CngCommon.HashData(data, hashAlgorithm);
	}

	protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		return Internal.Cryptography.CngCommon.TryHashData(source, destination, hashAlgorithm, out bytesWritten);
	}

	public unsafe override byte[] SignHash(byte[] hash)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		int estimatedSize = KeySize switch
		{
			256 => 64, 
			384 => 96, 
			521 => 132, 
			_ => KeySize / 4, 
		};
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		return keyHandle.SignHash(hash, global::Interop.NCrypt.AsymmetricPaddingMode.None, null, estimatedSize);
	}

	public unsafe override bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
		{
			if (!keyHandle.TrySignHash(source, destination, global::Interop.NCrypt.AsymmetricPaddingMode.None, null, out bytesWritten))
			{
				bytesWritten = 0;
				return false;
			}
		}
		return true;
	}

	public override bool VerifyHash(byte[] hash, byte[] signature)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
	}

	public unsafe override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
	{
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		return keyHandle.VerifyHash(hash, signature, global::Interop.NCrypt.AsymmetricPaddingMode.None, null);
	}
}
