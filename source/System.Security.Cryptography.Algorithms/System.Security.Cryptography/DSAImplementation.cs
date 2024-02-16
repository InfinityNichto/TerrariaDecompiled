using System.Buffers.Binary;
using System.IO;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class DSAImplementation
{
	public sealed class DSACng : DSA
	{
		private SafeNCryptKeyHandle _keyHandle;

		private int _lastKeySize;

		private bool _disposed;

		private static readonly KeySizes[] s_legalKeySizes = new KeySizes[1]
		{
			new KeySizes(512, 3072, 64)
		};

		private static readonly int s_defaultKeySize = (Supports2048KeySize() ? 2048 : 1024);

		public override KeySizes[] LegalKeySizes => base.LegalKeySizes;

		public override string SignatureAlgorithm => "DSA";

		public override string KeyExchangeAlgorithm => null;

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("RSA");
			}
		}

		private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
		{
			ThrowIfDisposed();
			int keySize = KeySize;
			if (_lastKeySize != keySize)
			{
				if (_keyHandle != null)
				{
					_keyHandle.Dispose();
				}
				_keyHandle = CngKeyLite.GenerateNewExportableKey("DSA", keySize);
				_lastKeySize = keySize;
			}
			return new DuplicateSafeNCryptKeyHandle(_keyHandle);
		}

		private byte[] ExportKeyBlob(bool includePrivateParameters)
		{
			string blobType = (includePrivateParameters ? "PRIVATEBLOB" : "PUBLICBLOB");
			using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
			return CngKeyLite.ExportKeyBlob(keyHandle, blobType);
		}

		private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
		{
			using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
			return CngKeyLite.ExportPkcs8KeyBlob(keyHandle, pkcs8Password, kdfCount);
		}

		private bool TryExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount, Span<byte> destination, out int bytesWritten)
		{
			using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
			return CngKeyLite.TryExportPkcs8KeyBlob(keyHandle, pkcs8Password, kdfCount, destination, out bytesWritten);
		}

		private void ImportKeyBlob(byte[] dsaBlob, bool includePrivate)
		{
			ThrowIfDisposed();
			string blobType = (includePrivate ? "PRIVATEBLOB" : "PUBLICBLOB");
			SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, dsaBlob);
			SetKeyHandle(keyHandle);
		}

		private void SetKeyHandle(SafeNCryptKeyHandle keyHandle)
		{
			_keyHandle = keyHandle;
			int keyLength = CngKeyLite.GetKeyLength(keyHandle);
			ForceSetKeySize(keyLength);
			_lastKeySize = keyLength;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_keyHandle?.Dispose();
				_keyHandle = null;
				_disposed = true;
			}
			base.Dispose(disposing);
		}

		public DSACng()
			: this(s_defaultKeySize)
		{
		}

		public DSACng(int keySize)
		{
			LegalKeySizesValue = s_legalKeySizes;
			KeySize = keySize;
		}

		protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
		{
			return CngCommon.HashData(data, offset, count, hashAlgorithm);
		}

		protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
		{
			return CngCommon.HashData(data, hashAlgorithm);
		}

		protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
		{
			return CngCommon.TryHashData(source, destination, hashAlgorithm, out bytesWritten);
		}

		private void ForceSetKeySize(int newKeySize)
		{
			KeySizeValue = newKeySize;
		}

		private static bool Supports2048KeySize()
		{
			Version version = Environment.OSVersion.Version;
			return version.Major > 6 || (version.Major == 6 && version.Minor >= 2);
		}

		public override void ImportParameters(DSAParameters parameters)
		{
			if (parameters.P == null || parameters.Q == null || parameters.G == null || parameters.Y == null)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidDsaParameters_MissingFields);
			}
			if (parameters.J != null && parameters.J.Length >= parameters.P.Length)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidDsaParameters_MismatchedPJ);
			}
			bool flag = parameters.X != null;
			int num = parameters.P.Length;
			int num2 = num * 8;
			if (parameters.G.Length != num || parameters.Y.Length != num)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidDsaParameters_MismatchedPGY);
			}
			if (flag && parameters.X.Length != parameters.Q.Length)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidDsaParameters_MismatchedQX);
			}
			byte[] blob;
			if (num2 <= 1024)
			{
				GenerateV1DsaBlob(out blob, parameters, num, flag);
			}
			else
			{
				GenerateV2DsaBlob(out blob, parameters, num, flag);
			}
			ImportKeyBlob(blob, flag);
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

		public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
		{
			if (pbeParameters == null)
			{
				throw new ArgumentNullException("pbeParameters");
			}
			return CngPkcs8.ExportEncryptedPkcs8PrivateKey(this, passwordBytes, pbeParameters);
		}

		public override byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
		{
			if (pbeParameters == null)
			{
				throw new ArgumentNullException("pbeParameters");
			}
			PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
			if (CngPkcs8.IsPlatformScheme(pbeParameters))
			{
				return ExportEncryptedPkcs8(password, pbeParameters.IterationCount);
			}
			return CngPkcs8.ExportEncryptedPkcs8PrivateKey(this, password, pbeParameters);
		}

		public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
		{
			if (pbeParameters == null)
			{
				throw new ArgumentNullException("pbeParameters");
			}
			PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
			return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(this, passwordBytes, pbeParameters, destination, out bytesWritten);
		}

		public override bool TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<byte> destination, out int bytesWritten)
		{
			if (pbeParameters == null)
			{
				throw new ArgumentNullException("pbeParameters");
			}
			PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
			if (CngPkcs8.IsPlatformScheme(pbeParameters))
			{
				return TryExportEncryptedPkcs8(password, pbeParameters.IterationCount, destination, out bytesWritten);
			}
			return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(this, password, pbeParameters, destination, out bytesWritten);
		}

		private unsafe static void GenerateV1DsaBlob(out byte[] blob, DSAParameters parameters, int cbKey, bool includePrivate)
		{
			int num = sizeof(global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB) + cbKey + cbKey + cbKey;
			if (includePrivate)
			{
				num += 20;
			}
			blob = new byte[num];
			fixed (byte* ptr = &blob[0])
			{
				global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB* ptr2 = (global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB*)ptr;
				ptr2->Magic = (includePrivate ? global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC : global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC);
				ptr2->cbKey = cbKey;
				int offset = 8;
				if (parameters.Seed != null)
				{
					if (parameters.Seed.Length != 20)
					{
						throw new ArgumentException(System.SR.Cryptography_InvalidDsaParameters_SeedRestriction_ShortKey);
					}
					global::Interop.BCrypt.EmitBigEndian(blob, ref offset, parameters.Counter);
					global::Interop.BCrypt.Emit(blob, ref offset, parameters.Seed);
				}
				else
				{
					global::Interop.BCrypt.EmitByte(blob, ref offset, byte.MaxValue, 24);
				}
				if (parameters.Q.Length != 20)
				{
					throw new ArgumentException(System.SR.Cryptography_InvalidDsaParameters_QRestriction_ShortKey);
				}
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.Q);
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.P);
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.G);
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.Y);
				if (includePrivate)
				{
					global::Interop.BCrypt.Emit(blob, ref offset, parameters.X);
				}
			}
		}

		private unsafe static void GenerateV2DsaBlob(out byte[] blob, DSAParameters parameters, int cbKey, bool includePrivateParameters)
		{
			int num = sizeof(global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2) + ((parameters.Seed == null) ? parameters.Q.Length : parameters.Seed.Length) + parameters.Q.Length + parameters.P.Length + parameters.G.Length + parameters.Y.Length + (includePrivateParameters ? parameters.X.Length : 0);
			blob = new byte[num];
			fixed (byte* ptr = &blob[0])
			{
				global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2* ptr2 = (global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2*)ptr;
				ptr2->Magic = (includePrivateParameters ? global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC_V2 : global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC_V2);
				ptr2->cbKey = cbKey;
				ptr2->hashAlgorithm = parameters.Q.Length switch
				{
					20 => global::Interop.BCrypt.HASHALGORITHM_ENUM.DSA_HASH_ALGORITHM_SHA1, 
					32 => global::Interop.BCrypt.HASHALGORITHM_ENUM.DSA_HASH_ALGORITHM_SHA256, 
					64 => global::Interop.BCrypt.HASHALGORITHM_ENUM.DSA_HASH_ALGORITHM_SHA512, 
					_ => throw new PlatformNotSupportedException(System.SR.Cryptography_InvalidDsaParameters_QRestriction_LargeKey), 
				};
				ptr2->standardVersion = global::Interop.BCrypt.DSAFIPSVERSION_ENUM.DSA_FIPS186_3;
				int offset = sizeof(global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2) - 4;
				if (parameters.Seed != null)
				{
					global::Interop.BCrypt.EmitBigEndian(blob, ref offset, parameters.Counter);
					ptr2->cbSeedLength = parameters.Seed.Length;
					ptr2->cbGroupSize = parameters.Q.Length;
					global::Interop.BCrypt.Emit(blob, ref offset, parameters.Seed);
				}
				else
				{
					global::Interop.BCrypt.EmitByte(blob, ref offset, byte.MaxValue, 4);
					int count = (ptr2->cbSeedLength = parameters.Q.Length);
					ptr2->cbGroupSize = parameters.Q.Length;
					global::Interop.BCrypt.EmitByte(blob, ref offset, byte.MaxValue, count);
				}
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.Q);
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.P);
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.G);
				global::Interop.BCrypt.Emit(blob, ref offset, parameters.Y);
				if (includePrivateParameters)
				{
					global::Interop.BCrypt.Emit(blob, ref offset, parameters.X);
				}
			}
		}

		public unsafe override DSAParameters ExportParameters(bool includePrivateParameters)
		{
			byte[] array = ExportKeyBlob(includePrivateParameters);
			global::Interop.BCrypt.KeyBlobMagicNumber keyBlobMagicNumber = (global::Interop.BCrypt.KeyBlobMagicNumber)BitConverter.ToInt32(array, 0);
			CheckMagicValueOfKey(keyBlobMagicNumber, includePrivateParameters);
			DSAParameters result = default(DSAParameters);
			fixed (byte* ptr = array)
			{
				if (keyBlobMagicNumber == global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC || keyBlobMagicNumber == global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC)
				{
					if (array.Length < sizeof(global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB))
					{
						throw global::Interop.NCrypt.ErrorCode.E_FAIL.ToCryptographicException();
					}
					global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB* ptr2 = (global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB*)ptr;
					int offset = 8;
					result.Counter = BinaryPrimitives.ReadInt32BigEndian(global::Interop.BCrypt.Consume(array, ref offset, 4));
					result.Seed = global::Interop.BCrypt.Consume(array, ref offset, 20);
					result.Q = global::Interop.BCrypt.Consume(array, ref offset, 20);
					result.P = global::Interop.BCrypt.Consume(array, ref offset, ptr2->cbKey);
					result.G = global::Interop.BCrypt.Consume(array, ref offset, ptr2->cbKey);
					result.Y = global::Interop.BCrypt.Consume(array, ref offset, ptr2->cbKey);
					if (includePrivateParameters)
					{
						result.X = global::Interop.BCrypt.Consume(array, ref offset, 20);
					}
				}
				else
				{
					if (array.Length < sizeof(global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2))
					{
						throw global::Interop.NCrypt.ErrorCode.E_FAIL.ToCryptographicException();
					}
					global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2* ptr3 = (global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2*)ptr;
					int offset = sizeof(global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2) - 4;
					result.Counter = BinaryPrimitives.ReadInt32BigEndian(global::Interop.BCrypt.Consume(array, ref offset, 4));
					result.Seed = global::Interop.BCrypt.Consume(array, ref offset, ptr3->cbSeedLength);
					result.Q = global::Interop.BCrypt.Consume(array, ref offset, ptr3->cbGroupSize);
					result.P = global::Interop.BCrypt.Consume(array, ref offset, ptr3->cbKey);
					result.G = global::Interop.BCrypt.Consume(array, ref offset, ptr3->cbKey);
					result.Y = global::Interop.BCrypt.Consume(array, ref offset, ptr3->cbKey);
					if (includePrivateParameters)
					{
						result.X = global::Interop.BCrypt.Consume(array, ref offset, ptr3->cbGroupSize);
					}
				}
				if (result.Counter == -1)
				{
					result.Counter = 0;
					result.Seed = null;
				}
				return result;
			}
		}

		private static void CheckMagicValueOfKey(global::Interop.BCrypt.KeyBlobMagicNumber magic, bool includePrivateParameters)
		{
			if (includePrivateParameters)
			{
				if (magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC_V2)
				{
					throw new CryptographicException(System.SR.Cryptography_NotValidPrivateKey);
				}
			}
			else if (magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC && magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC_V2)
			{
				throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
			}
		}

		public unsafe override byte[] CreateSignature(byte[] rgbHash)
		{
			if (rgbHash == null)
			{
				throw new ArgumentNullException("rgbHash");
			}
			Span<byte> stackBuf = stackalloc byte[32];
			ReadOnlySpan<byte> hash = AdjustHashSizeIfNecessary(rgbHash, stackBuf);
			using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
			return keyHandle.SignHash(hash, global::Interop.NCrypt.AsymmetricPaddingMode.None, null, hash.Length * 2);
		}

		protected unsafe override bool TryCreateSignatureCore(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
		{
			Span<byte> stackBuf = stackalloc byte[32];
			ReadOnlySpan<byte> hash2 = AdjustHashSizeIfNecessary(hash, stackBuf);
			using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
			{
				if (!keyHandle.TrySignHash(hash2, destination, global::Interop.NCrypt.AsymmetricPaddingMode.None, null, out bytesWritten))
				{
					bytesWritten = 0;
					return false;
				}
			}
			return signatureFormat switch
			{
				DSASignatureFormat.IeeeP1363FixedFieldConcatenation => true, 
				DSASignatureFormat.Rfc3279DerSequence => AsymmetricAlgorithmHelpers.TryConvertIeee1363ToDer(destination.Slice(0, bytesWritten), destination, out bytesWritten), 
				_ => throw new CryptographicException(System.SR.Cryptography_UnknownSignatureFormat, signatureFormat.ToString()), 
			};
		}

		public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
		{
			if (rgbHash == null)
			{
				throw new ArgumentNullException("rgbHash");
			}
			if (rgbSignature == null)
			{
				throw new ArgumentNullException("rgbSignature");
			}
			return VerifySignatureCore(rgbHash, rgbSignature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
		}

		protected unsafe override bool VerifySignatureCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
		{
			Span<byte> stackBuf = stackalloc byte[32];
			ReadOnlySpan<byte> hash2 = AdjustHashSizeIfNecessary(hash, stackBuf);
			switch (signatureFormat)
			{
			case DSASignatureFormat.Rfc3279DerSequence:
			{
				int fieldSizeBits = hash2.Length * 8;
				signature = this.ConvertSignatureToIeeeP1363(signatureFormat, signature, fieldSizeBits);
				break;
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_UnknownSignatureFormat, signatureFormat.ToString());
			case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
				break;
			}
			using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
			return keyHandle.VerifyHash(hash2, signature, global::Interop.NCrypt.AsymmetricPaddingMode.None, null);
		}

		private ReadOnlySpan<byte> AdjustHashSizeIfNecessary(ReadOnlySpan<byte> hash, Span<byte> stackBuf)
		{
			int num = ComputeQLength();
			if (num == hash.Length)
			{
				return hash;
			}
			if (num < hash.Length)
			{
				return hash.Slice(0, num);
			}
			int num2 = num - hash.Length;
			stackBuf.Slice(0, num2).Clear();
			hash.CopyTo(stackBuf.Slice(num2));
			return stackBuf.Slice(0, num);
		}

		private unsafe int ComputeQLength()
		{
			byte[] array;
			using (GetDuplicatedKeyHandle())
			{
				array = ExportKeyBlob(includePrivateParameters: false);
			}
			if (array.Length < sizeof(global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2))
			{
				return 20;
			}
			fixed (byte* ptr = array)
			{
				global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2* ptr2 = (global::Interop.BCrypt.BCRYPT_DSA_KEY_BLOB_V2*)ptr;
				if (ptr2->Magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC_V2 && ptr2->Magic != global::Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC_V2)
				{
					return 20;
				}
				return ptr2->cbGroupSize;
			}
		}
	}
}
