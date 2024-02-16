using System.IO;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class ECDsaImplementation
{
	public sealed class ECDsaCng : ECDsa
	{
		private readonly ECCngKey _key = new ECCngKey("ECDSA", "ECDsa");

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

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_key?.FullDispose();
			}
			base.Dispose(disposing);
		}

		private void ThrowIfDisposed()
		{
			_key.ThrowIfDisposed();
		}

		private void ImportFullKeyBlob(byte[] ecfullKeyBlob, bool includePrivateParameters)
		{
			string blobType = (includePrivateParameters ? "ECCFULLPRIVATEBLOB" : "ECCFULLPUBLICBLOB");
			SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, ecfullKeyBlob);
			_key.SetHandle(keyHandle, "ECDSA");
			ForceSetKeySize(_key.KeySize);
		}

		private void ImportKeyBlob(byte[] ecKeyBlob, string curveName, bool includePrivateParameters)
		{
			string blobType = (includePrivateParameters ? "ECCPRIVATEBLOB" : "ECCPUBLICBLOB");
			SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, ecKeyBlob, curveName);
			_key.SetHandle(keyHandle, ECCng.EcdsaCurveNameToAlgorithm(curveName));
			ForceSetKeySize(_key.KeySize);
		}

		private byte[] ExportKeyBlob(bool includePrivateParameters)
		{
			string blobType = (includePrivateParameters ? "ECCPRIVATEBLOB" : "ECCPUBLICBLOB");
			using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
			return CngKeyLite.ExportKeyBlob(keyHandle, blobType);
		}

		private byte[] ExportFullKeyBlob(bool includePrivateParameters)
		{
			string blobType = (includePrivateParameters ? "ECCFULLPRIVATEBLOB" : "ECCFULLPUBLICBLOB");
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

		private void AcceptImport(CngPkcs8.Pkcs8Response response)
		{
			SafeNCryptKeyHandle keyHandle = response.KeyHandle;
			_key.SetHandle(keyHandle, CngKeyLite.GetPropertyAsString(keyHandle, "Algorithm Name", CngPropertyOptions.None));
			ForceSetKeySize(_key.KeySize);
		}

		private string GetCurveName(out string oidValue)
		{
			return _key.GetCurveName(KeySize, out oidValue);
		}

		public override void GenerateKey(ECCurve curve)
		{
			_key.GenerateKey(curve);
			ForceSetKeySize(_key.KeySize);
		}

		private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
		{
			return _key.GetDuplicatedKeyHandle(KeySize);
		}

		private void DisposeKey()
		{
			_key.DisposeKey();
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
					byte[] primeCurveBlob = ECCng.GetPrimeCurveBlob(ref parameters2, ecdh: false);
					ImportFullKeyBlob(primeCurveBlob, includePrivateParameters: true);
				}
				else
				{
					byte[] primeCurveBlob2 = ECCng.GetPrimeCurveBlob(ref parameters, ecdh: false);
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
					byte[] namedCurveBlob = ECCng.GetNamedCurveBlob(ref parameters3, ecdh: false);
					ImportKeyBlob(namedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters: true);
				}
				else
				{
					byte[] namedCurveBlob2 = ECCng.GetNamedCurveBlob(ref parameters, ecdh: false);
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
			ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters);
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
				ECCng.ExportPrimeCurveParameters(ref ecParams, ecBlob, includePrivateParameters);
			}
			else
			{
				byte[] ecBlob2 = ExportKeyBlob(includePrivateParameters);
				ECCng.ExportNamedCurveParameters(ref ecParams, ecBlob2, includePrivateParameters);
				ecParams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
			}
			return ecParams;
		}

		public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
		{
			ThrowIfDisposed();
			int bytesRead2;
			CngPkcs8.Pkcs8Response response = CngPkcs8.ImportPkcs8PrivateKey(source, out bytesRead2);
			ProcessPkcs8Response(response);
			bytesRead = bytesRead2;
		}

		public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source, out int bytesRead)
		{
			ThrowIfDisposed();
			int bytesRead2;
			CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead2);
			ProcessPkcs8Response(response);
			bytesRead = bytesRead2;
		}

		public override void ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source, out int bytesRead)
		{
			ThrowIfDisposed();
			int bytesRead2;
			CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead2);
			ProcessPkcs8Response(response);
			bytesRead = bytesRead2;
		}

		private void ProcessPkcs8Response(CngPkcs8.Pkcs8Response response)
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

		public override bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
		{
			return TrySignHashCore(source, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);
		}

		protected unsafe override bool TrySignHashCore(ReadOnlySpan<byte> hash, Span<byte> destination, DSASignatureFormat signatureFormat, out int bytesWritten)
		{
			using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
			{
				if (!keyHandle.TrySignHash(hash, destination, global::Interop.NCrypt.AsymmetricPaddingMode.None, null, out bytesWritten))
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
			return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
		}

		public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
		{
			return VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
		}

		protected unsafe override bool VerifyHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, DSASignatureFormat signatureFormat)
		{
			if (signatureFormat != 0)
			{
				signature = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);
			}
			using SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle();
			return keyHandle.VerifyHash(hash, signature, global::Interop.NCrypt.AsymmetricPaddingMode.None, null);
		}
	}
}
