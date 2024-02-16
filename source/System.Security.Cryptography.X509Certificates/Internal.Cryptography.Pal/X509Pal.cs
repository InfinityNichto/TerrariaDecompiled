using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal;

internal sealed class X509Pal : IX509Pal
{
	public static IX509Pal Instance = new X509Pal();

	public bool SupportsLegacyBasicConstraintsExtension => true;

	private X509Pal()
	{
	}

	public unsafe byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages)
	{
		ushort num = (ushort)keyUsages;
		CRYPT_BIT_BLOB cRYPT_BIT_BLOB = default(CRYPT_BIT_BLOB);
		cRYPT_BIT_BLOB.cbData = 2;
		cRYPT_BIT_BLOB.pbData = (byte*)(&num);
		cRYPT_BIT_BLOB.cUnusedBits = 0;
		CRYPT_BIT_BLOB cRYPT_BIT_BLOB2 = cRYPT_BIT_BLOB;
		return global::Interop.crypt32.EncodeObject(CryptDecodeObjectStructType.X509_KEY_USAGE, &cRYPT_BIT_BLOB2);
	}

	public unsafe void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages)
	{
		uint num = encoded.DecodeObject(CryptDecodeObjectStructType.X509_KEY_USAGE, delegate(void* pvDecoded, int cbDecoded)
		{
			byte* pbData = ((CRYPT_BIT_BLOB*)pvDecoded)->pbData;
			if (pbData != null)
			{
				switch (((CRYPT_BIT_BLOB*)pvDecoded)->cbData)
				{
				case 1:
					return *pbData;
				case 2:
					return *(ushort*)pbData;
				}
			}
			return 0u;
		});
		keyUsages = (X509KeyUsageFlags)num;
	}

	public unsafe byte[] EncodeX509BasicConstraints2Extension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint)
	{
		CERT_BASIC_CONSTRAINTS2_INFO cERT_BASIC_CONSTRAINTS2_INFO = default(CERT_BASIC_CONSTRAINTS2_INFO);
		cERT_BASIC_CONSTRAINTS2_INFO.fCA = (certificateAuthority ? 1 : 0);
		cERT_BASIC_CONSTRAINTS2_INFO.fPathLenConstraint = (hasPathLengthConstraint ? 1 : 0);
		cERT_BASIC_CONSTRAINTS2_INFO.dwPathLenConstraint = pathLengthConstraint;
		CERT_BASIC_CONSTRAINTS2_INFO cERT_BASIC_CONSTRAINTS2_INFO2 = cERT_BASIC_CONSTRAINTS2_INFO;
		return global::Interop.crypt32.EncodeObject("2.5.29.19", &cERT_BASIC_CONSTRAINTS2_INFO2);
	}

	public unsafe void DecodeX509BasicConstraintsExtension(byte[] encoded, out bool certificateAuthority, out bool hasPathLengthConstraint, out int pathLengthConstraint)
	{
		(certificateAuthority, hasPathLengthConstraint, pathLengthConstraint) = encoded.DecodeObject(CryptDecodeObjectStructType.X509_BASIC_CONSTRAINTS, (void* pvDecoded, int cbDecoded) => ((*((CERT_BASIC_CONSTRAINTS_INFO*)pvDecoded)->SubjectType.pbData & 0x80) != 0, ((CERT_BASIC_CONSTRAINTS_INFO*)pvDecoded)->fPathLenConstraint != 0, ((CERT_BASIC_CONSTRAINTS_INFO*)pvDecoded)->dwPathLenConstraint));
	}

	public unsafe void DecodeX509BasicConstraints2Extension(byte[] encoded, out bool certificateAuthority, out bool hasPathLengthConstraint, out int pathLengthConstraint)
	{
		(certificateAuthority, hasPathLengthConstraint, pathLengthConstraint) = encoded.DecodeObject(CryptDecodeObjectStructType.X509_BASIC_CONSTRAINTS2, (void* pvDecoded, int cbDecoded) => (((CERT_BASIC_CONSTRAINTS2_INFO*)pvDecoded)->fCA != 0, ((CERT_BASIC_CONSTRAINTS2_INFO*)pvDecoded)->fPathLenConstraint != 0, ((CERT_BASIC_CONSTRAINTS2_INFO*)pvDecoded)->dwPathLenConstraint));
	}

	public unsafe byte[] EncodeX509EnhancedKeyUsageExtension(OidCollection usages)
	{
		int numOids;
		using SafeHandle safeHandle = usages.ToLpstrArray(out numOids);
		CERT_ENHKEY_USAGE cERT_ENHKEY_USAGE = default(CERT_ENHKEY_USAGE);
		cERT_ENHKEY_USAGE.cUsageIdentifier = numOids;
		cERT_ENHKEY_USAGE.rgpszUsageIdentifier = (IntPtr*)(void*)safeHandle.DangerousGetHandle();
		CERT_ENHKEY_USAGE cERT_ENHKEY_USAGE2 = cERT_ENHKEY_USAGE;
		return global::Interop.crypt32.EncodeObject("2.5.29.37", &cERT_ENHKEY_USAGE2);
	}

	public unsafe void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages)
	{
		usages = encoded.DecodeObject(CryptDecodeObjectStructType.X509_ENHANCED_KEY_USAGE, delegate(void* pvDecoded, int cbDecoded)
		{
			OidCollection oidCollection = new OidCollection();
			int cUsageIdentifier = ((CERT_ENHKEY_USAGE*)pvDecoded)->cUsageIdentifier;
			for (int i = 0; i < cUsageIdentifier; i++)
			{
				IntPtr ptr = ((CERT_ENHKEY_USAGE*)pvDecoded)->rgpszUsageIdentifier[i];
				string oid = Marshal.PtrToStringAnsi(ptr);
				Oid oid2 = new Oid(oid);
				oidCollection.Add(oid2);
			}
			return oidCollection;
		});
	}

	public unsafe byte[] EncodeX509SubjectKeyIdentifierExtension(ReadOnlySpan<byte> subjectKeyIdentifier)
	{
		fixed (byte* pbData = subjectKeyIdentifier)
		{
			CRYPTOAPI_BLOB cRYPTOAPI_BLOB = new CRYPTOAPI_BLOB(subjectKeyIdentifier.Length, pbData);
			return global::Interop.crypt32.EncodeObject("2.5.29.14", &cRYPTOAPI_BLOB);
		}
	}

	public unsafe void DecodeX509SubjectKeyIdentifierExtension(byte[] encoded, out byte[] subjectKeyIdentifier)
	{
		subjectKeyIdentifier = encoded.DecodeObject("2.5.29.14", (void* pvDecoded, int cbDecoded) => ((CRYPTOAPI_BLOB*)pvDecoded)->ToByteArray());
	}

	public unsafe byte[] ComputeCapiSha1OfPublicKey(PublicKey key)
	{
		fixed (byte* value = key.Oid.ValueAsAscii())
		{
			byte[] rawData = key.EncodedParameters.RawData;
			fixed (byte* pbData = rawData)
			{
				byte[] rawData2 = key.EncodedKeyValue.RawData;
				fixed (byte* pbData2 = rawData2)
				{
					CERT_PUBLIC_KEY_INFO cERT_PUBLIC_KEY_INFO = default(CERT_PUBLIC_KEY_INFO);
					cERT_PUBLIC_KEY_INFO.Algorithm = new CRYPT_ALGORITHM_IDENTIFIER
					{
						pszObjId = new IntPtr(value),
						Parameters = new CRYPTOAPI_BLOB(rawData.Length, pbData)
					};
					cERT_PUBLIC_KEY_INFO.PublicKey = new CRYPT_BIT_BLOB
					{
						cbData = rawData2.Length,
						pbData = pbData2,
						cUnusedBits = 0
					};
					CERT_PUBLIC_KEY_INFO pInfo = cERT_PUBLIC_KEY_INFO;
					int pcbComputedHash = 20;
					byte[] array = new byte[pcbComputedHash];
					if (!global::Interop.crypt32.CryptHashPublicKeyInfo(IntPtr.Zero, 32772, 0, CertEncodingType.All, ref pInfo, array, ref pcbComputedHash))
					{
						throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
					}
					if (pcbComputedHash < array.Length)
					{
						byte[] array2 = new byte[pcbComputedHash];
						Buffer.BlockCopy(array, 0, array2, 0, pcbComputedHash);
						array = array2;
					}
					return array;
				}
			}
		}
	}

	public unsafe X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
	{
		ContentType pdwContentType;
		fixed (byte* pbData = rawData)
		{
			CRYPTOAPI_BLOB cRYPTOAPI_BLOB = new CRYPTOAPI_BLOB(rawData.Length, pbData);
			if (!global::Interop.crypt32.CryptQueryObject(CertQueryObjectType.CERT_QUERY_OBJECT_BLOB, &cRYPTOAPI_BLOB, ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_ALL, ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL, 0, IntPtr.Zero, out pdwContentType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
		}
		return MapContentType(pdwContentType);
	}

	public unsafe X509ContentType GetCertContentType(string fileName)
	{
		ContentType pdwContentType;
		fixed (char* pvObject = fileName)
		{
			if (!global::Interop.crypt32.CryptQueryObject(CertQueryObjectType.CERT_QUERY_OBJECT_FILE, pvObject, ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_ALL, ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL, 0, IntPtr.Zero, out pdwContentType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
		}
		return MapContentType(pdwContentType);
	}

	private static X509ContentType MapContentType(ContentType contentType)
	{
		switch (contentType)
		{
		case ContentType.CERT_QUERY_CONTENT_CERT:
			return X509ContentType.Cert;
		case ContentType.CERT_QUERY_CONTENT_SERIALIZED_STORE:
			return X509ContentType.SerializedStore;
		case ContentType.CERT_QUERY_CONTENT_SERIALIZED_CERT:
			return X509ContentType.SerializedCert;
		case ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED:
		case ContentType.CERT_QUERY_CONTENT_PKCS7_UNSIGNED:
			return X509ContentType.Pkcs7;
		case ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED:
			return X509ContentType.Authenticode;
		case ContentType.CERT_QUERY_CONTENT_PFX:
			return X509ContentType.Pfx;
		default:
			return X509ContentType.Unknown;
		}
	}

	public ECDsa DecodeECDsaPublicKey(ICertificatePal certificatePal)
	{
		if (certificatePal is CertificatePal certificatePal2)
		{
			return DecodeECPublicKey(certificatePal2, (CngKey cngKey) => new ECDsaCng(cngKey), delegate(ECDsaCng algorithm, ECParameters ecParams)
			{
				algorithm.ImportParameters(ecParams);
			});
		}
		throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
	}

	public ECDiffieHellman DecodeECDiffieHellmanPublicKey(ICertificatePal certificatePal)
	{
		if (certificatePal is CertificatePal certificatePal2)
		{
			return DecodeECPublicKey(certificatePal2, (CngKey cngKey) => new ECDiffieHellmanCng(cngKey), delegate(ECDiffieHellmanCng algorithm, ECParameters ecParams)
			{
				algorithm.ImportParameters(ecParams);
			}, CryptImportPublicKeyInfoFlags.CRYPT_OID_INFO_PUBKEY_ENCRYPT_KEY_FLAG);
		}
		throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
	}

	public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal certificatePal)
	{
		switch (global::Interop.Crypt32.FindOidInfo(global::Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid.Value, OidGroup.PublicKeyAlgorithm, fallBackToAllGroups: true).AlgId)
		{
		case 9216:
		case 41984:
		{
			byte[] keyBlob2 = DecodeKeyBlob(CryptDecodeObjectStructType.CNG_RSA_PUBLIC_KEY_BLOB, encodedKeyValue);
			CngKey key = CngKey.Import(keyBlob2, CngKeyBlobFormat.GenericPublicBlob);
			return new RSACng(key);
		}
		case 8704:
		{
			byte[] keyBlob = ConstructDSSPublicKeyCspBlob(encodedKeyValue, encodedParameters);
			DSACryptoServiceProvider dSACryptoServiceProvider = new DSACryptoServiceProvider();
			dSACryptoServiceProvider.ImportCspBlob(keyBlob);
			return dSACryptoServiceProvider;
		}
		default:
			throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
		}
	}

	private static TAlgorithm DecodeECPublicKey<TAlgorithm>(CertificatePal certificatePal, Func<CngKey, TAlgorithm> factory, Action<TAlgorithm, ECParameters> import, CryptImportPublicKeyInfoFlags importFlags = CryptImportPublicKeyInfoFlags.NONE) where TAlgorithm : AsymmetricAlgorithm, new()
	{
		TAlgorithm val;
		using (Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle safeBCryptKeyHandle = ImportPublicKeyInfo(certificatePal.CertContext, importFlags))
		{
			string curveName = GetCurveName(safeBCryptKeyHandle);
			if (curveName == null)
			{
				CngKeyBlobFormat cngKeyBlobFormat = ((!HasExplicitParameters(safeBCryptKeyHandle)) ? CngKeyBlobFormat.EccPublicBlob : CngKeyBlobFormat.EccFullPublicBlob);
				byte[] keyBlob = ExportKeyBlob(safeBCryptKeyHandle, cngKeyBlobFormat);
				using CngKey arg = CngKey.Import(keyBlob, cngKeyBlobFormat);
				val = factory(arg);
			}
			else
			{
				CngKeyBlobFormat cngKeyBlobFormat = CngKeyBlobFormat.EccPublicBlob;
				byte[] keyBlob = ExportKeyBlob(safeBCryptKeyHandle, cngKeyBlobFormat);
				ECParameters ecParams = default(ECParameters);
				ExportNamedCurveParameters(ref ecParams, keyBlob, includePrivateParameters: false);
				ecParams.Curve = ECCurve.CreateFromFriendlyName(curveName);
				val = new TAlgorithm();
				import(val, ecParams);
			}
		}
		return val;
	}

	private unsafe static Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle ImportPublicKeyInfo(SafeCertContextHandle certContext, CryptImportPublicKeyInfoFlags importFlags)
	{
		bool success = false;
		certContext.DangerousAddRef(ref success);
		try
		{
			if (!global::Interop.crypt32.CryptImportPublicKeyInfoEx2(CertEncodingType.X509_ASN_ENCODING, &certContext.CertContext->pCertInfo->SubjectPublicKeyInfo, importFlags, null, out var phKey))
			{
				throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
			}
			return phKey;
		}
		finally
		{
			if (success)
			{
				certContext.DangerousRelease();
			}
		}
	}

	private static byte[] ExportKeyBlob(Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle bCryptKeyHandle, CngKeyBlobFormat blobFormat)
	{
		string format = blobFormat.Format;
		int pcbResult = 0;
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptExportKey(bCryptKeyHandle, IntPtr.Zero, format, null, 0, out pcbResult, 0);
		if (nTSTATUS != 0)
		{
			throw new CryptographicException(global::Interop.Kernel32.GetMessage((int)nTSTATUS));
		}
		byte[] array = new byte[pcbResult];
		nTSTATUS = global::Interop.BCrypt.BCryptExportKey(bCryptKeyHandle, IntPtr.Zero, format, array, array.Length, out pcbResult, 0);
		if (nTSTATUS != 0)
		{
			throw new CryptographicException(global::Interop.Kernel32.GetMessage((int)nTSTATUS));
		}
		Array.Resize(ref array, pcbResult);
		return array;
	}

	private unsafe static void ExportNamedCurveParameters(ref ECParameters ecParams, byte[] ecBlob, bool includePrivateParameters)
	{
		fixed (byte* ptr = &ecBlob[0])
		{
			global::Interop.BCrypt.BCRYPT_ECCKEY_BLOB* ptr2 = (global::Interop.BCrypt.BCRYPT_ECCKEY_BLOB*)ptr;
			int offset = sizeof(global::Interop.BCrypt.BCRYPT_ECCKEY_BLOB);
			ecParams.Q = new ECPoint
			{
				X = global::Interop.BCrypt.Consume(ecBlob, ref offset, ptr2->cbKey),
				Y = global::Interop.BCrypt.Consume(ecBlob, ref offset, ptr2->cbKey)
			};
			if (includePrivateParameters)
			{
				ecParams.D = global::Interop.BCrypt.Consume(ecBlob, ref offset, ptr2->cbKey);
			}
		}
	}

	private static byte[] DecodeKeyBlob(CryptDecodeObjectStructType lpszStructType, byte[] encodedKeyValue)
	{
		int pcbStructInfo = 0;
		if (!global::Interop.crypt32.CryptDecodeObject(CertEncodingType.All, lpszStructType, encodedKeyValue, encodedKeyValue.Length, CryptDecodeObjectFlags.None, null, ref pcbStructInfo))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		byte[] array = new byte[pcbStructInfo];
		if (!global::Interop.crypt32.CryptDecodeObject(CertEncodingType.All, lpszStructType, encodedKeyValue, encodedKeyValue.Length, CryptDecodeObjectFlags.None, array, ref pcbStructInfo))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		return array;
	}

	private static byte[] ConstructDSSPublicKeyCspBlob(byte[] encodedKeyValue, byte[] encodedParameters)
	{
		byte[] array = DecodeDssKeyValue(encodedKeyValue);
		DecodeDssParameters(encodedParameters, out var p, out var q, out var g);
		int num = p.Length;
		if (num == 0)
		{
			throw (-2146893803).ToCryptographicException();
		}
		int capacity = 16 + num + 20 + num + num + 24;
		MemoryStream memoryStream = new MemoryStream(capacity);
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write((byte)6);
		binaryWriter.Write((byte)2);
		binaryWriter.Write((short)0);
		binaryWriter.Write(8704u);
		binaryWriter.Write(827544388);
		binaryWriter.Write(num * 8);
		binaryWriter.Write(p);
		int num2 = q.Length;
		if (num2 == 0 || num2 > 20)
		{
			throw (-2146893803).ToCryptographicException();
		}
		binaryWriter.Write(q);
		if (20 > num2)
		{
			binaryWriter.Write(new byte[20 - num2]);
		}
		num2 = g.Length;
		if (num2 == 0 || num2 > num)
		{
			throw (-2146893803).ToCryptographicException();
		}
		binaryWriter.Write(g);
		if (num > num2)
		{
			binaryWriter.Write(new byte[num - num2]);
		}
		num2 = array.Length;
		if (num2 == 0 || num2 > num)
		{
			throw (-2146893803).ToCryptographicException();
		}
		binaryWriter.Write(array);
		if (num > num2)
		{
			binaryWriter.Write(new byte[num - num2]);
		}
		binaryWriter.Write(uint.MaxValue);
		binaryWriter.Write(new byte[20]);
		return memoryStream.ToArray();
	}

	private unsafe static byte[] DecodeDssKeyValue(byte[] encodedKeyValue)
	{
		return encodedKeyValue.DecodeObject(CryptDecodeObjectStructType.X509_DSS_PUBLICKEY, (void* pvDecoded, int cbDecoded) => ((CRYPTOAPI_BLOB*)pvDecoded)->ToByteArray());
	}

	private unsafe static void DecodeDssParameters(byte[] encodedParameters, out byte[] p, out byte[] q, out byte[] g)
	{
		(p, q, g) = encodedParameters.DecodeObject(CryptDecodeObjectStructType.X509_DSS_PARAMETERS, (void* pvDecoded, int cbDecoded) => (((CERT_DSS_PARAMETERS*)pvDecoded)->p.ToByteArray(), ((CERT_DSS_PARAMETERS*)pvDecoded)->q.ToByteArray(), ((CERT_DSS_PARAMETERS*)pvDecoded)->g.ToByteArray()));
	}

	private static bool HasExplicitParameters(Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle bcryptHandle)
	{
		byte[] property = GetProperty(bcryptHandle, "ECCParameters");
		if (property != null)
		{
			return property.Length != 0;
		}
		return false;
	}

	private static string GetCurveName(Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle bcryptHandle)
	{
		return GetPropertyAsString(bcryptHandle, "ECCCurveName");
	}

	private unsafe static string GetPropertyAsString(Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle cryptHandle, string propertyName)
	{
		byte[] property = GetProperty(cryptHandle, propertyName);
		if (property == null || property.Length == 0)
		{
			return null;
		}
		fixed (byte* ptr = &property[0])
		{
			return Marshal.PtrToStringUni((IntPtr)ptr);
		}
	}

	private unsafe static byte[] GetProperty(Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle cryptHandle, string propertyName)
	{
		if (global::Interop.BCrypt.BCryptGetProperty(cryptHandle, propertyName, null, 0, out var pcbResult, 0) != 0)
		{
			return null;
		}
		byte[] array = new byte[pcbResult];
		global::Interop.BCrypt.NTSTATUS nTSTATUS;
		fixed (byte* pbOutput = array)
		{
			nTSTATUS = global::Interop.BCrypt.BCryptGetProperty(cryptHandle, propertyName, pbOutput, array.Length, out pcbResult, 0);
		}
		if (nTSTATUS != 0)
		{
			return null;
		}
		Array.Resize(ref array, pcbResult);
		return array;
	}

	public unsafe string X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flag)
	{
		int dwStrType = (int)(CertNameStrTypeAndFlags.CERT_X500_NAME_STR | MapNameToStrFlag(flag));
		fixed (byte* pbData = encodedDistinguishedName)
		{
			Unsafe.SkipInit(out CRYPTOAPI_BLOB cRYPTOAPI_BLOB);
			cRYPTOAPI_BLOB.cbData = encodedDistinguishedName.Length;
			cRYPTOAPI_BLOB.pbData = pbData;
			int num = global::Interop.Crypt32.CertNameToStr(65537, &cRYPTOAPI_BLOB, dwStrType, null, 0);
			if (num == 0)
			{
				throw (-2146762476).ToCryptographicException();
			}
			Span<char> span = ((num > 256) ? ((Span<char>)new char[num]) : stackalloc char[num]);
			Span<char> span2 = span;
			fixed (char* psz = span2)
			{
				if (global::Interop.Crypt32.CertNameToStr(65537, &cRYPTOAPI_BLOB, dwStrType, psz, num) == 0)
				{
					throw (-2146762476).ToCryptographicException();
				}
			}
			return new string(span2.Slice(0, num - 1));
		}
	}

	public byte[] X500DistinguishedNameEncode(string distinguishedName, X500DistinguishedNameFlags flag)
	{
		CertNameStrTypeAndFlags dwStrType = CertNameStrTypeAndFlags.CERT_X500_NAME_STR | MapNameToStrFlag(flag);
		int pcbEncoded = 0;
		if (!global::Interop.crypt32.CertStrToName(CertEncodingType.All, distinguishedName, dwStrType, IntPtr.Zero, null, ref pcbEncoded, IntPtr.Zero))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		byte[] array = new byte[pcbEncoded];
		if (!global::Interop.crypt32.CertStrToName(CertEncodingType.All, distinguishedName, dwStrType, IntPtr.Zero, array, ref pcbEncoded, IntPtr.Zero))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		return array;
	}

	public unsafe string X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine)
	{
		if (encodedDistinguishedName == null || encodedDistinguishedName.Length == 0)
		{
			return string.Empty;
		}
		int dwFormatStrType = (multiLine ? 1 : 0);
		int pcbFormat = 0;
		if (!global::Interop.Crypt32.CryptFormatObject(1, 0, dwFormatStrType, IntPtr.Zero, (byte*)7, encodedDistinguishedName, encodedDistinguishedName.Length, null, ref pcbFormat))
		{
			return encodedDistinguishedName.ToHexStringUpper();
		}
		int num = (pcbFormat + 1) / 2;
		Span<char> span = ((num > 256) ? ((Span<char>)new char[num]) : stackalloc char[num]);
		Span<char> span2 = span;
		fixed (char* pbFormat = span2)
		{
			if (!global::Interop.Crypt32.CryptFormatObject(1, 0, dwFormatStrType, IntPtr.Zero, (byte*)7, encodedDistinguishedName, encodedDistinguishedName.Length, pbFormat, ref pcbFormat))
			{
				return encodedDistinguishedName.ToHexStringUpper();
			}
		}
		return new string(span2.Slice(0, pcbFormat / 2 - 1));
	}

	private static CertNameStrTypeAndFlags MapNameToStrFlag(X500DistinguishedNameFlags flag)
	{
		uint num = 29169u;
		CertNameStrTypeAndFlags certNameStrTypeAndFlags = (CertNameStrTypeAndFlags)0;
		if (flag != 0)
		{
			if ((flag & X500DistinguishedNameFlags.Reversed) == X500DistinguishedNameFlags.Reversed)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_REVERSE_FLAG;
			}
			if ((flag & X500DistinguishedNameFlags.UseSemicolons) == X500DistinguishedNameFlags.UseSemicolons)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_SEMICOLON_FLAG;
			}
			else if ((flag & X500DistinguishedNameFlags.UseCommas) == X500DistinguishedNameFlags.UseCommas)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_COMMA_FLAG;
			}
			else if ((flag & X500DistinguishedNameFlags.UseNewLines) == X500DistinguishedNameFlags.UseNewLines)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_CRLF_FLAG;
			}
			if ((flag & X500DistinguishedNameFlags.DoNotUsePlusSign) == X500DistinguishedNameFlags.DoNotUsePlusSign)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_NO_PLUS_FLAG;
			}
			if ((flag & X500DistinguishedNameFlags.DoNotUseQuotes) == X500DistinguishedNameFlags.DoNotUseQuotes)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_NO_QUOTING_FLAG;
			}
			if ((flag & X500DistinguishedNameFlags.ForceUTF8Encoding) == X500DistinguishedNameFlags.ForceUTF8Encoding)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_FORCE_UTF8_DIR_STR_FLAG;
			}
			if ((flag & X500DistinguishedNameFlags.UseUTF8Encoding) == X500DistinguishedNameFlags.UseUTF8Encoding)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_ENABLE_UTF8_UNICODE_FLAG;
			}
			else if ((flag & X500DistinguishedNameFlags.UseT61Encoding) == X500DistinguishedNameFlags.UseT61Encoding)
			{
				certNameStrTypeAndFlags |= CertNameStrTypeAndFlags.CERT_NAME_STR_ENABLE_T61_UNICODE_FLAG;
			}
		}
		return certNameStrTypeAndFlags;
	}
}
