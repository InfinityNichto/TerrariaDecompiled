using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class ECDiffieHellmanImplementation
{
	public sealed class ECDiffieHellmanCng : ECDiffieHellman
	{
		private readonly ECCngKey _key = new ECCngKey("ECDH", "ECDiffieHellman");

		public override ECDiffieHellmanPublicKey PublicKey
		{
			get
			{
				string oidValue;
				string curveName = GetCurveName(out oidValue);
				return new ECDiffieHellmanCngPublicKey((curveName == null) ? ExportFullKeyBlob(includePrivateParameters: false) : ExportKeyBlob(includePrivateParameters: false), curveName);
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

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_key.FullDispose();
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
			_key.SetHandle(keyHandle, "ECDH");
			ForceSetKeySize(_key.KeySize);
		}

		private void ImportKeyBlob(byte[] ecKeyBlob, string curveName, bool includePrivateParameters)
		{
			string blobType = (includePrivateParameters ? "ECCPRIVATEBLOB" : "ECCPUBLICBLOB");
			SafeNCryptKeyHandle keyHandle = CngKeyLite.ImportKeyBlob(blobType, ecKeyBlob, curveName);
			_key.SetHandle(keyHandle, ECCng.EcdhCurveNameToAlgorithm(curveName));
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

		public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
		{
			if (otherPartyPublicKey == null)
			{
				throw new ArgumentNullException("otherPartyPublicKey");
			}
			return DeriveKeyFromHash(otherPartyPublicKey, HashAlgorithmName.SHA256);
		}

		private SafeNCryptSecretHandle DeriveSecretAgreementHandle(ECDiffieHellmanPublicKey otherPartyPublicKey)
		{
			if (otherPartyPublicKey == null)
			{
				throw new ArgumentNullException("otherPartyPublicKey");
			}
			ECParameters parameters = otherPartyPublicKey.ExportParameters();
			using ECDiffieHellmanCng eCDiffieHellmanCng = (ECDiffieHellmanCng)ECDiffieHellman.Create(parameters);
			using SafeNCryptKeyHandle safeNCryptKeyHandle = eCDiffieHellmanCng.GetDuplicatedKeyHandle();
			string propertyAsString = CngKeyLite.GetPropertyAsString(safeNCryptKeyHandle, "Algorithm Group", CngPropertyOptions.None);
			if (propertyAsString != "ECDH")
			{
				throw new ArgumentException(System.SR.Cryptography_ArgECDHRequiresECDHKey, "otherPartyPublicKey");
			}
			if (CngKeyLite.GetKeyLength(safeNCryptKeyHandle) != KeySize)
			{
				throw new ArgumentException(System.SR.Cryptography_ArgECDHKeySizeMismatch, "otherPartyPublicKey");
			}
			using SafeNCryptKeyHandle privateKey = GetDuplicatedKeyHandle();
			return global::Interop.NCrypt.DeriveSecretAgreement(privateKey, safeNCryptKeyHandle);
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

		public ECDiffieHellmanCng()
			: this(521)
		{
		}

		public ECDiffieHellmanCng(int keySize)
		{
			KeySize = keySize;
		}

		public ECDiffieHellmanCng(ECCurve curve)
		{
			GenerateKey(curve);
		}

		private void ForceSetKeySize(int newKeySize)
		{
			KeySizeValue = newKeySize;
		}

		public override byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[] secretPrepend, byte[] secretAppend)
		{
			if (otherPartyPublicKey == null)
			{
				throw new ArgumentNullException("otherPartyPublicKey");
			}
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
			}
			using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
			return global::Interop.NCrypt.DeriveKeyMaterialHash(secretAgreement, hashAlgorithm.Name, secretPrepend, secretAppend, global::Interop.NCrypt.SecretAgreementFlags.None);
		}

		public override byte[] DeriveKeyFromHmac(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm, byte[] hmacKey, byte[] secretPrepend, byte[] secretAppend)
		{
			if (otherPartyPublicKey == null)
			{
				throw new ArgumentNullException("otherPartyPublicKey");
			}
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
			}
			using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
			global::Interop.NCrypt.SecretAgreementFlags flags = ((hmacKey == null) ? global::Interop.NCrypt.SecretAgreementFlags.UseSecretAsHmacKey : global::Interop.NCrypt.SecretAgreementFlags.None);
			return global::Interop.NCrypt.DeriveKeyMaterialHmac(secretAgreement, hashAlgorithm.Name, hmacKey, secretPrepend, secretAppend, flags);
		}

		public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
		{
			if (otherPartyPublicKey == null)
			{
				throw new ArgumentNullException("otherPartyPublicKey");
			}
			if (prfLabel == null)
			{
				throw new ArgumentNullException("prfLabel");
			}
			if (prfSeed == null)
			{
				throw new ArgumentNullException("prfSeed");
			}
			using SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey);
			return global::Interop.NCrypt.DeriveKeyMaterialTls(secretAgreement, prfLabel, prfSeed, global::Interop.NCrypt.SecretAgreementFlags.None);
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
					byte[] primeCurveBlob = ECCng.GetPrimeCurveBlob(ref parameters2, ecdh: true);
					ImportFullKeyBlob(primeCurveBlob, includePrivateParameters: true);
				}
				else
				{
					byte[] primeCurveBlob2 = ECCng.GetPrimeCurveBlob(ref parameters, ecdh: true);
					ImportFullKeyBlob(primeCurveBlob2, flag);
				}
				return;
			}
			if (curve.IsNamed)
			{
				if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
				{
					throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_InvalidCurveOid, curve.Oid.Value));
				}
				if (!flag2 && flag)
				{
					byte[] array2 = new byte[parameters.D.Length];
					ECParameters parameters3 = parameters;
					parameters3.Q.X = array2;
					parameters3.Q.Y = array2;
					byte[] namedCurveBlob = ECCng.GetNamedCurveBlob(ref parameters3, ecdh: true);
					ImportKeyBlob(namedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters: true);
				}
				else
				{
					byte[] namedCurveBlob2 = ECCng.GetNamedCurveBlob(ref parameters, ecdh: true);
					ImportKeyBlob(namedCurveBlob2, curve.Oid.FriendlyName, flag);
				}
				return;
			}
			throw new PlatformNotSupportedException(System.SR.Format(System.SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
		}

		public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
		{
			byte[] array = ExportFullKeyBlob(includePrivateParameters);
			try
			{
				ECParameters ecParams = default(ECParameters);
				ECCng.ExportPrimeCurveParameters(ref ecParams, array, includePrivateParameters);
				return ecParams;
			}
			finally
			{
				Array.Clear(array);
			}
		}

		public override ECParameters ExportParameters(bool includePrivateParameters)
		{
			ECParameters ecParams = default(ECParameters);
			string oidValue;
			string curveName = GetCurveName(out oidValue);
			byte[] array = null;
			try
			{
				if (string.IsNullOrEmpty(curveName))
				{
					array = ExportFullKeyBlob(includePrivateParameters);
					ECCng.ExportPrimeCurveParameters(ref ecParams, array, includePrivateParameters);
				}
				else
				{
					array = ExportKeyBlob(includePrivateParameters);
					ECCng.ExportNamedCurveParameters(ref ecParams, array, includePrivateParameters);
					ecParams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
				}
				return ecParams;
			}
			finally
			{
				if (array != null)
				{
					Array.Clear(array);
				}
			}
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
			if (response.GetAlgorithmGroup() != "ECDH")
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
	}

	public sealed class ECDiffieHellmanCngPublicKey : ECDiffieHellmanPublicKey
	{
		private byte[] _keyBlob;

		internal string _curveName;

		protected override void Dispose(bool disposing)
		{
			_keyBlob = null;
			base.Dispose(disposing);
		}

		public override string ToXmlString()
		{
			throw new PlatformNotSupportedException();
		}

		internal ECDiffieHellmanCngPublicKey(byte[] keyBlob, string curveName)
			: base(keyBlob)
		{
			_keyBlob = keyBlob;
			_curveName = curveName;
		}

		public override ECParameters ExportExplicitParameters()
		{
			if (_keyBlob == null)
			{
				throw new ObjectDisposedException("ECDiffieHellmanPublicKey");
			}
			ECParameters ecParams = default(ECParameters);
			ECCng.ExportPrimeCurveParameters(ref ecParams, _keyBlob, includePrivateParameters: false);
			return ecParams;
		}

		public override ECParameters ExportParameters()
		{
			if (_keyBlob == null)
			{
				throw new ObjectDisposedException("ECDiffieHellmanPublicKey");
			}
			if (string.IsNullOrEmpty(_curveName))
			{
				return ExportExplicitParameters();
			}
			ECParameters ecParams = default(ECParameters);
			ECCng.ExportNamedCurveParameters(ref ecParams, _keyBlob, includePrivateParameters: false);
			ecParams.Curve = ECCurve.CreateFromFriendlyName(_curveName);
			return ecParams;
		}
	}
}
