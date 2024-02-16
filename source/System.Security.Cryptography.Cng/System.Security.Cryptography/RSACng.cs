using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

public sealed class RSACng : RSA
{
	private CngAlgorithmCore _core = new CngAlgorithmCore("RSACng");

	private static readonly CngKeyBlobFormat s_rsaFullPrivateBlob = new CngKeyBlobFormat("RSAFULLPRIVATEBLOB");

	private static readonly CngKeyBlobFormat s_rsaPrivateBlob = new CngKeyBlobFormat("RSAPRIVATEBLOB");

	private static readonly CngKeyBlobFormat s_rsaPublicBlob = new CngKeyBlobFormat("RSAPUBLICBLOB");

	private static readonly ConcurrentDictionary<HashAlgorithmName, int> s_hashSizes = new ConcurrentDictionary<HashAlgorithmName, int>(new KeyValuePair<HashAlgorithmName, int>[3]
	{
		KeyValuePair.Create(HashAlgorithmName.SHA256, 32),
		KeyValuePair.Create(HashAlgorithmName.SHA384, 48),
		KeyValuePair.Create(HashAlgorithmName.SHA512, 64)
	});

	public CngKey Key
	{
		get
		{
			return _core.GetOrGenerateKey(KeySize, CngAlgorithm.Rsa);
		}
		private set
		{
			if (value.AlgorithmGroup != CngAlgorithmGroup.Rsa)
			{
				throw new ArgumentException(System.SR.Cryptography_ArgRSARequiresRSAKey, "value");
			}
			_core.SetKey(value);
			ForceSetKeySize(value.KeySize);
		}
	}

	public override KeySizes[] LegalKeySizes => new KeySizes[1]
	{
		new KeySizes(512, 16384, 64)
	};

	public RSACng(CngKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (key.AlgorithmGroup != CngAlgorithmGroup.Rsa)
		{
			throw new ArgumentException(System.SR.Cryptography_ArgRSARequiresRSAKey, "key");
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

	private void ImportKeyBlob(byte[] rsaBlob, bool includePrivate)
	{
		CngKeyBlobFormat format = (includePrivate ? s_rsaPrivateBlob : s_rsaPublicBlob);
		CngKey cngKey = CngKey.Import(rsaBlob, format);
		cngKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;
		Key = cngKey;
	}

	private void AcceptImport(System.Security.Cryptography.CngPkcs8.Pkcs8Response response)
	{
		Key = response.Key;
	}

	private byte[] ExportKeyBlob(bool includePrivateParameters)
	{
		return Key.Export(includePrivateParameters ? s_rsaFullPrivateBlob : s_rsaPublicBlob);
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

	private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
	{
		return Key.Handle;
	}

	public RSACng()
		: this(2048)
	{
	}

	public RSACng(int keySize)
	{
		KeySize = keySize;
	}

	protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		return Internal.Cryptography.CngCommon.HashData(data, offset, count, hashAlgorithm);
	}

	protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		return Internal.Cryptography.CngCommon.TryHashData(data, destination, hashAlgorithm, out bytesWritten);
	}

	protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		return Internal.Cryptography.CngCommon.HashData(data, hashAlgorithm);
	}

	private void ForceSetKeySize(int newKeySize)
	{
		KeySizeValue = newKeySize;
	}

	public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
	{
		return EncryptOrDecrypt(data, padding, encrypt: true);
	}

	public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
	{
		return EncryptOrDecrypt(data, padding, encrypt: false);
	}

	public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		return TryEncryptOrDecrypt(data, destination, padding, encrypt: true, out bytesWritten);
	}

	public override bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
	{
		return TryEncryptOrDecrypt(data, destination, padding, encrypt: false, out bytesWritten);
	}

	private unsafe byte[] EncryptOrDecrypt(byte[] data, RSAEncryptionPadding padding, bool encrypt)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		int num = System.Security.Cryptography.RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
		if (!encrypt && data.Length != num)
		{
			throw new CryptographicException(System.SR.Cryptography_RSA_DecryptWrongSize);
		}
		if (encrypt && padding.Mode == RSAEncryptionPaddingMode.Pkcs1 && data.Length > num - 11)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Encryption_MessageTooLong, num - 11));
		}
		using SafeNCryptKeyHandle key = GetDuplicatedKeyHandle();
		if (encrypt && data.Length == 0)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
			Span<byte> span = new Span<byte>(array, 0, num);
			try
			{
				if (padding == RSAEncryptionPadding.Pkcs1)
				{
					System.Security.Cryptography.RsaPaddingProcessor.PadPkcs1Encryption(data, span);
				}
				else
				{
					if (padding.Mode != RSAEncryptionPaddingMode.Oaep)
					{
						throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
					}
					System.Security.Cryptography.RsaPaddingProcessor rsaPaddingProcessor = System.Security.Cryptography.RsaPaddingProcessor.OpenProcessor(padding.OaepHashAlgorithm);
					rsaPaddingProcessor.PadOaep(data, span);
				}
				return EncryptOrDecrypt(key, span, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_NO_PADDING_FLAG, null, encrypt);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
		switch (padding.Mode)
		{
		case RSAEncryptionPaddingMode.Pkcs1:
			return EncryptOrDecrypt(key, data, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, null, encrypt);
		case RSAEncryptionPaddingMode.Oaep:
		{
			IntPtr intPtr = Marshal.StringToHGlobalUni(padding.OaepHashAlgorithm.Name);
			try
			{
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO);
				bCRYPT_OAEP_PADDING_INFO.pszAlgId = intPtr;
				bCRYPT_OAEP_PADDING_INFO.pbLabel = IntPtr.Zero;
				bCRYPT_OAEP_PADDING_INFO.cbLabel = 0;
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO2 = bCRYPT_OAEP_PADDING_INFO;
				return EncryptOrDecrypt(key, data, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_OAEP_FLAG, &bCRYPT_OAEP_PADDING_INFO2, encrypt);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
		default:
			throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
		}
	}

	private unsafe bool TryEncryptOrDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, bool encrypt, out int bytesWritten)
	{
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		int num = System.Security.Cryptography.RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
		if (!encrypt && data.Length != num)
		{
			throw new CryptographicException(System.SR.Cryptography_RSA_DecryptWrongSize);
		}
		if (encrypt && padding.Mode == RSAEncryptionPaddingMode.Pkcs1 && data.Length > num - 11)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Encryption_MessageTooLong, num - 11));
		}
		using SafeNCryptKeyHandle key = GetDuplicatedKeyHandle();
		if (encrypt && data.Length == 0)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(num);
			Span<byte> span = new Span<byte>(array, 0, num);
			try
			{
				if (padding == RSAEncryptionPadding.Pkcs1)
				{
					System.Security.Cryptography.RsaPaddingProcessor.PadPkcs1Encryption(data, span);
				}
				else
				{
					if (padding.Mode != RSAEncryptionPaddingMode.Oaep)
					{
						throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
					}
					System.Security.Cryptography.RsaPaddingProcessor rsaPaddingProcessor = System.Security.Cryptography.RsaPaddingProcessor.OpenProcessor(padding.OaepHashAlgorithm);
					rsaPaddingProcessor.PadOaep(data, span);
				}
				return TryEncryptOrDecrypt(key, span, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_NO_PADDING_FLAG, null, encrypt, out bytesWritten);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
		switch (padding.Mode)
		{
		case RSAEncryptionPaddingMode.Pkcs1:
			return TryEncryptOrDecrypt(key, data, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, null, encrypt, out bytesWritten);
		case RSAEncryptionPaddingMode.Oaep:
		{
			IntPtr intPtr = Marshal.StringToHGlobalUni(padding.OaepHashAlgorithm.Name);
			try
			{
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO);
				bCRYPT_OAEP_PADDING_INFO.pszAlgId = intPtr;
				bCRYPT_OAEP_PADDING_INFO.pbLabel = IntPtr.Zero;
				bCRYPT_OAEP_PADDING_INFO.cbLabel = 0;
				global::Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO bCRYPT_OAEP_PADDING_INFO2 = bCRYPT_OAEP_PADDING_INFO;
				return TryEncryptOrDecrypt(key, data, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_OAEP_FLAG, &bCRYPT_OAEP_PADDING_INFO2, encrypt, out bytesWritten);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
		default:
			throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
		}
	}

	private unsafe byte[] EncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt)
	{
		int num = KeySize / 8;
		byte[] array = new byte[num];
		int bytesNeeded = 0;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS;
		for (int i = 0; i <= 1; i++)
		{
			errorCode = EncryptOrDecrypt(key, input, array, paddingMode, paddingInfo, encrypt, out bytesNeeded);
			if (errorCode != global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
			{
				break;
			}
		}
		if (errorCode == global::Interop.NCrypt.ErrorCode.NTE_BUFFER_TOO_SMALL)
		{
			CryptographicOperations.ZeroMemory(array);
			array = new byte[bytesNeeded];
			for (int j = 0; j <= 1; j++)
			{
				errorCode = EncryptOrDecrypt(key, input, array, paddingMode, paddingInfo, encrypt, out bytesNeeded);
				if (errorCode != global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
				{
					break;
				}
			}
		}
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		if (bytesNeeded != array.Length)
		{
			byte[] array2 = array.AsSpan(0, bytesNeeded).ToArray();
			CryptographicOperations.ZeroMemory(array);
			array = array2;
		}
		return array;
	}

	private unsafe bool TryEncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, Span<byte> output, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt, out int bytesWritten)
	{
		for (int i = 0; i <= 1; i++)
		{
			int bytesNeeded;
			global::Interop.NCrypt.ErrorCode errorCode = EncryptOrDecrypt(key, input, output, paddingMode, paddingInfo, encrypt, out bytesNeeded);
			switch (errorCode)
			{
			case global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS:
				bytesWritten = bytesNeeded;
				return true;
			case global::Interop.NCrypt.ErrorCode.NTE_BUFFER_TOO_SMALL:
				bytesWritten = 0;
				return false;
			default:
				throw errorCode.ToCryptographicException();
			case global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL:
				break;
			}
		}
		throw global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL.ToCryptographicException();
	}

	private unsafe static global::Interop.NCrypt.ErrorCode EncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, Span<byte> output, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt, out int bytesNeeded)
	{
		global::Interop.NCrypt.ErrorCode errorCode = (encrypt ? global::Interop.NCrypt.NCryptEncrypt(key, input, input.Length, paddingInfo, output, output.Length, out bytesNeeded, paddingMode) : global::Interop.NCrypt.NCryptDecrypt(key, input, input.Length, paddingInfo, output, output.Length, out bytesNeeded, paddingMode));
		if (errorCode == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS && bytesNeeded > output.Length)
		{
			errorCode = global::Interop.NCrypt.ErrorCode.NTE_BUFFER_TOO_SMALL;
		}
		return errorCode;
	}

	public unsafe override void ImportParameters(RSAParameters parameters)
	{
		if (parameters.Exponent == null || parameters.Modulus == null)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
		}
		bool flag;
		if (parameters.D == null)
		{
			flag = false;
			if (parameters.P != null || parameters.DP != null || parameters.Q != null || parameters.DQ != null || parameters.InverseQ != null)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
			}
		}
		else
		{
			flag = true;
			if (parameters.P == null || parameters.DP == null || parameters.Q == null || parameters.DQ == null || parameters.InverseQ == null)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
			}
			int num = (parameters.Modulus.Length + 1) / 2;
			if (parameters.D.Length != parameters.Modulus.Length || parameters.P.Length != num || parameters.Q.Length != num || parameters.DP.Length != num || parameters.DQ.Length != num || parameters.InverseQ.Length != num)
			{
				throw new CryptographicException(System.SR.Cryptography_InvalidRsaParameters);
			}
		}
		int num2 = sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB) + parameters.Exponent.Length + parameters.Modulus.Length;
		if (flag)
		{
			num2 += parameters.P.Length + parameters.Q.Length;
		}
		byte[] array = new byte[num2];
		fixed (byte* ptr = &array[0])
		{
			global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB* ptr2 = (global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB*)ptr;
			ptr2->Magic = (flag ? global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC : global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPUBLIC_MAGIC);
			ptr2->BitLength = parameters.Modulus.Length * 8;
			ptr2->cbPublicExp = parameters.Exponent.Length;
			ptr2->cbModulus = parameters.Modulus.Length;
			if (flag)
			{
				ptr2->cbPrime1 = parameters.P.Length;
				ptr2->cbPrime2 = parameters.Q.Length;
			}
			int offset = sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB);
			global::Interop.BCrypt.Emit(array, ref offset, parameters.Exponent);
			global::Interop.BCrypt.Emit(array, ref offset, parameters.Modulus);
			if (flag)
			{
				global::Interop.BCrypt.Emit(array, ref offset, parameters.P);
				global::Interop.BCrypt.Emit(array, ref offset, parameters.Q);
			}
		}
		ImportKeyBlob(array, flag);
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
		if (response.GetAlgorithmGroup() != "RSA")
		{
			response.FreeKey();
			throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
		}
		AcceptImport(response);
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

	public override RSAParameters ExportParameters(bool includePrivateParameters)
	{
		byte[] rsaBlob = ExportKeyBlob(includePrivateParameters);
		RSAParameters rsaParams = default(RSAParameters);
		ExportParameters(ref rsaParams, rsaBlob, includePrivateParameters);
		return rsaParams;
	}

	private unsafe static void ExportParameters(ref RSAParameters rsaParams, byte[] rsaBlob, bool includePrivateParameters)
	{
		global::Interop.BCrypt.KeyBlobMagicNumber magic = (global::Interop.BCrypt.KeyBlobMagicNumber)BitConverter.ToInt32(rsaBlob, 0);
		CheckMagicValueOfKey(magic, includePrivateParameters);
		if (rsaBlob.Length < sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB))
		{
			throw global::Interop.NCrypt.ErrorCode.E_FAIL.ToCryptographicException();
		}
		fixed (byte* ptr = &rsaBlob[0])
		{
			global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB* ptr2 = (global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB*)ptr;
			int offset = sizeof(global::Interop.BCrypt.BCRYPT_RSAKEY_BLOB);
			rsaParams.Exponent = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPublicExp);
			rsaParams.Modulus = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbModulus);
			if (includePrivateParameters)
			{
				rsaParams.P = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime1);
				rsaParams.Q = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime2);
				rsaParams.DP = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime1);
				rsaParams.DQ = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime2);
				rsaParams.InverseQ = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbPrime1);
				rsaParams.D = global::Interop.BCrypt.Consume(rsaBlob, ref offset, ptr2->cbModulus);
			}
		}
	}

	private static void CheckMagicValueOfKey(global::Interop.BCrypt.KeyBlobMagicNumber magic, bool includePrivateParameters)
	{
		if (includePrivateParameters)
		{
			if (magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAFULLPRIVATE_MAGIC)
			{
				throw new CryptographicException(System.SR.Cryptography_NotValidPrivateKey);
			}
		}
		else if (magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPUBLIC_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_RSAFULLPRIVATE_MAGIC)
		{
			throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
		}
	}

	private static int GetHashSizeInBytes(HashAlgorithmName hashAlgorithm)
	{
		return s_hashSizes.GetOrAdd(hashAlgorithm, delegate(HashAlgorithmName hashAlgorithm)
		{
			using Internal.Cryptography.HashProviderCng hashProviderCng = new Internal.Cryptography.HashProviderCng(hashAlgorithm.Name, null);
			return hashProviderCng.HashSizeInBytes;
		});
	}

	public unsafe override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		if (hash == null)
		{
			throw new ArgumentNullException("hash");
		}
		string name = hashAlgorithm.Name;
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
		{
			throw new CryptographicException(System.SR.Cryptography_SignHash_WrongSize);
		}
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		IntPtr intPtr = Marshal.StringToHGlobalUni(name);
		try
		{
			int estimatedSize = KeySize / 8;
			switch (padding.Mode)
			{
			case RSASignaturePaddingMode.Pkcs1:
			{
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO);
				bCRYPT_PKCS1_PADDING_INFO.pszAlgId = intPtr;
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO2 = bCRYPT_PKCS1_PADDING_INFO;
				return keyHandle.SignHash(hash, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &bCRYPT_PKCS1_PADDING_INFO2, estimatedSize);
			}
			case RSASignaturePaddingMode.Pss:
			{
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO);
				bCRYPT_PSS_PADDING_INFO.pszAlgId = intPtr;
				bCRYPT_PSS_PADDING_INFO.cbSalt = hash.Length;
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO2 = bCRYPT_PSS_PADDING_INFO;
				return keyHandle.SignHash(hash, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &bCRYPT_PSS_PADDING_INFO2, estimatedSize);
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}

	public unsafe override bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
	{
		string name = hashAlgorithm.Name;
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
		{
			throw new CryptographicException(System.SR.Cryptography_SignHash_WrongSize);
		}
		IntPtr intPtr = Marshal.StringToHGlobalUni(name);
		try
		{
			switch (padding.Mode)
			{
			case RSASignaturePaddingMode.Pkcs1:
			{
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO);
				bCRYPT_PKCS1_PADDING_INFO.pszAlgId = intPtr;
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO2 = bCRYPT_PKCS1_PADDING_INFO;
				return keyHandle.TrySignHash(hash, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &bCRYPT_PKCS1_PADDING_INFO2, out bytesWritten);
			}
			case RSASignaturePaddingMode.Pss:
			{
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO);
				bCRYPT_PSS_PADDING_INFO.pszAlgId = intPtr;
				bCRYPT_PSS_PADDING_INFO.cbSalt = hash.Length;
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO2 = bCRYPT_PSS_PADDING_INFO;
				return keyHandle.TrySignHash(hash, destination, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &bCRYPT_PSS_PADDING_INFO2, out bytesWritten);
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
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
		return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);
	}

	public unsafe override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
	{
		string name = hashAlgorithm.Name;
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		if (padding == null)
		{
			throw new ArgumentNullException("padding");
		}
		using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
		if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
		{
			return false;
		}
		IntPtr intPtr = Marshal.StringToHGlobalUni(name);
		try
		{
			switch (padding.Mode)
			{
			case RSASignaturePaddingMode.Pkcs1:
			{
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO);
				bCRYPT_PKCS1_PADDING_INFO.pszAlgId = intPtr;
				global::Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO2 = bCRYPT_PKCS1_PADDING_INFO;
				return keyHandle.VerifyHash(hash, signature, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &bCRYPT_PKCS1_PADDING_INFO2);
			}
			case RSASignaturePaddingMode.Pss:
			{
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO = default(global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO);
				bCRYPT_PSS_PADDING_INFO.pszAlgId = intPtr;
				bCRYPT_PSS_PADDING_INFO.cbSalt = hash.Length;
				global::Interop.BCrypt.BCRYPT_PSS_PADDING_INFO bCRYPT_PSS_PADDING_INFO2 = bCRYPT_PSS_PADDING_INFO;
				return keyHandle.VerifyHash(hash, signature, global::Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &bCRYPT_PSS_PADDING_INFO2);
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_UnsupportedPaddingMode);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}
}
