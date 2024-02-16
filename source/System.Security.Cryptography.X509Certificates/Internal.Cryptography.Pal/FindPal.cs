using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography.Pal.Native;

namespace Internal.Cryptography.Pal;

internal sealed class FindPal : IFindPal, IDisposable
{
	private static readonly Dictionary<string, X509KeyUsageFlags> s_keyUsages = new Dictionary<string, X509KeyUsageFlags>(9, StringComparer.OrdinalIgnoreCase)
	{
		{
			"DigitalSignature",
			X509KeyUsageFlags.DigitalSignature
		},
		{
			"NonRepudiation",
			X509KeyUsageFlags.NonRepudiation
		},
		{
			"KeyEncipherment",
			X509KeyUsageFlags.KeyEncipherment
		},
		{
			"DataEncipherment",
			X509KeyUsageFlags.DataEncipherment
		},
		{
			"KeyAgreement",
			X509KeyUsageFlags.KeyAgreement
		},
		{
			"KeyCertSign",
			X509KeyUsageFlags.KeyCertSign
		},
		{
			"CrlSign",
			X509KeyUsageFlags.CrlSign
		},
		{
			"EncipherOnly",
			X509KeyUsageFlags.EncipherOnly
		},
		{
			"DecipherOnly",
			X509KeyUsageFlags.DecipherOnly
		}
	};

	private readonly StorePal _storePal;

	private readonly X509Certificate2Collection _copyTo;

	private readonly bool _validOnly;

	public static X509Certificate2Collection FindFromCollection(X509Certificate2Collection coll, X509FindType findType, object findValue, bool validOnly)
	{
		X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
		using (IFindPal findPal = OpenPal(coll, x509Certificate2Collection, validOnly))
		{
			switch (findType)
			{
			case X509FindType.FindByThumbprint:
			{
				byte[] thumbprint = ConfirmedCast<string>(findValue).DecodeHexString();
				findPal.FindByThumbprint(thumbprint);
				break;
			}
			case X509FindType.FindBySubjectName:
			{
				string subjectName = ConfirmedCast<string>(findValue);
				findPal.FindBySubjectName(subjectName);
				break;
			}
			case X509FindType.FindBySubjectDistinguishedName:
			{
				string subjectDistinguishedName = ConfirmedCast<string>(findValue);
				findPal.FindBySubjectDistinguishedName(subjectDistinguishedName);
				break;
			}
			case X509FindType.FindByIssuerName:
			{
				string issuerName = ConfirmedCast<string>(findValue);
				findPal.FindByIssuerName(issuerName);
				break;
			}
			case X509FindType.FindByIssuerDistinguishedName:
			{
				string issuerDistinguishedName = ConfirmedCast<string>(findValue);
				findPal.FindByIssuerDistinguishedName(issuerDistinguishedName);
				break;
			}
			case X509FindType.FindBySerialNumber:
			{
				string text = ConfirmedCast<string>(findValue);
				byte[] array = text.DecodeHexString();
				Array.Reverse(array);
				BigInteger hexValue = PositiveBigIntegerFromByteArray(array);
				BigInteger decimalValue = LaxParseDecimalBigInteger(text);
				findPal.FindBySerialNumber(hexValue, decimalValue);
				break;
			}
			case X509FindType.FindByTimeValid:
			{
				DateTime dateTime3 = ConfirmedCast<DateTime>(findValue);
				findPal.FindByTimeValid(dateTime3);
				break;
			}
			case X509FindType.FindByTimeNotYetValid:
			{
				DateTime dateTime2 = ConfirmedCast<DateTime>(findValue);
				findPal.FindByTimeNotYetValid(dateTime2);
				break;
			}
			case X509FindType.FindByTimeExpired:
			{
				DateTime dateTime = ConfirmedCast<DateTime>(findValue);
				findPal.FindByTimeExpired(dateTime);
				break;
			}
			case X509FindType.FindByTemplateName:
			{
				string templateName = ConfirmedCast<string>(findValue);
				findPal.FindByTemplateName(templateName);
				break;
			}
			case X509FindType.FindByApplicationPolicy:
			{
				string oidValue3 = ConfirmedOidValue(findPal, findValue, OidGroup.Policy);
				findPal.FindByApplicationPolicy(oidValue3);
				break;
			}
			case X509FindType.FindByCertificatePolicy:
			{
				string oidValue2 = ConfirmedOidValue(findPal, findValue, OidGroup.Policy);
				findPal.FindByCertificatePolicy(oidValue2);
				break;
			}
			case X509FindType.FindByExtension:
			{
				string oidValue = ConfirmedOidValue(findPal, findValue, OidGroup.ExtensionOrAttribute);
				findPal.FindByExtension(oidValue);
				break;
			}
			case X509FindType.FindByKeyUsage:
			{
				X509KeyUsageFlags keyUsage = ConfirmedX509KeyUsage(findValue);
				findPal.FindByKeyUsage(keyUsage);
				break;
			}
			case X509FindType.FindBySubjectKeyIdentifier:
			{
				byte[] keyIdentifier = ConfirmedCast<string>(findValue).DecodeHexString();
				findPal.FindBySubjectKeyIdentifier(keyIdentifier);
				break;
			}
			default:
				throw new CryptographicException(System.SR.Cryptography_X509_InvalidFindType);
			}
		}
		return x509Certificate2Collection;
	}

	private static T ConfirmedCast<T>(object findValue)
	{
		if (findValue.GetType() != typeof(T))
		{
			throw new CryptographicException(System.SR.Cryptography_X509_InvalidFindValue);
		}
		return (T)findValue;
	}

	private static string ConfirmedOidValue(IFindPal findPal, object findValue, OidGroup oidGroup)
	{
		string text = ConfirmedCast<string>(findValue);
		if (text.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOidValue);
		}
		return findPal.NormalizeOid(text, oidGroup);
	}

	private static X509KeyUsageFlags ConfirmedX509KeyUsage(object findValue)
	{
		if (findValue is X509KeyUsageFlags)
		{
			return (X509KeyUsageFlags)findValue;
		}
		if (findValue is int)
		{
			return (X509KeyUsageFlags)(int)findValue;
		}
		if (findValue is uint)
		{
			return (X509KeyUsageFlags)(uint)findValue;
		}
		if (findValue is string key && s_keyUsages.TryGetValue(key, out var value))
		{
			return value;
		}
		throw new CryptographicException(System.SR.Cryptography_X509_InvalidFindValue);
	}

	internal static void ValidateOidValue(string keyValue)
	{
		if (keyValue == null)
		{
			throw new ArgumentNullException("keyValue");
		}
		int length = keyValue.Length;
		if (length < 2)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOidValue);
		}
		char c = keyValue[0];
		if (c != '0' && c != '1' && c != '2')
		{
			throw new ArgumentException(System.SR.Argument_InvalidOidValue);
		}
		if (keyValue[1] != '.' || keyValue[length - 1] == '.')
		{
			throw new ArgumentException(System.SR.Argument_InvalidOidValue);
		}
		for (int i = 1; i < length; i++)
		{
			if (!char.IsDigit(keyValue[i]) && (keyValue[i] != '.' || keyValue[i + 1] == '.'))
			{
				throw new ArgumentException(System.SR.Argument_InvalidOidValue);
			}
		}
	}

	internal static BigInteger PositiveBigIntegerFromByteArray(byte[] bytes)
	{
		if (bytes.Length == 0 || bytes[^1] < 128)
		{
			return new BigInteger(bytes);
		}
		byte[] array = new byte[bytes.Length + 1];
		Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
		return new BigInteger(array);
	}

	private static BigInteger LaxParseDecimalBigInteger(string decimalString)
	{
		BigInteger right = new BigInteger(10);
		BigInteger bigInteger = BigInteger.Zero;
		foreach (char c in decimalString)
		{
			if (c >= '0' && c <= '9')
			{
				bigInteger = BigInteger.Multiply(bigInteger, right);
				bigInteger = BigInteger.Add(bigInteger, c - 48);
			}
		}
		return bigInteger;
	}

	internal FindPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
	{
		_storePal = (StorePal)StorePal.LinkFromCertificateCollection(findFrom);
		_copyTo = copyTo;
		_validOnly = validOnly;
	}

	internal static IFindPal OpenPal(X509Certificate2Collection findFrom, X509Certificate2Collection copyTo, bool validOnly)
	{
		return new FindPal(findFrom, copyTo, validOnly);
	}

	public string NormalizeOid(string maybeOid, OidGroup expectedGroup)
	{
		string text = global::Interop.Crypt32.FindOidInfo(global::Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, maybeOid, expectedGroup, fallBackToAllGroups: true).OID;
		if (text == null)
		{
			text = maybeOid;
			ValidateOidValue(text);
		}
		return text;
	}

	public unsafe void FindByThumbprint(byte[] thumbPrint)
	{
		fixed (byte* pbData = thumbPrint)
		{
			CRYPTOAPI_BLOB cRYPTOAPI_BLOB = new CRYPTOAPI_BLOB(thumbPrint.Length, pbData);
			FindCore<object>(CertFindType.CERT_FIND_HASH, &cRYPTOAPI_BLOB);
		}
	}

	public unsafe void FindBySubjectName(string subjectName)
	{
		fixed (char* pvFindPara = subjectName)
		{
			FindCore<object>(CertFindType.CERT_FIND_SUBJECT_STR, pvFindPara);
		}
	}

	public void FindBySubjectDistinguishedName(string subjectDistinguishedName)
	{
		FindCore(subjectDistinguishedName, delegate(string subjectDistinguishedName, SafeCertContextHandle pCertContext)
		{
			string certNameInfo = GetCertNameInfo(pCertContext, CertNameType.CERT_NAME_RDN_TYPE, CertNameFlags.None);
			return subjectDistinguishedName.Equals(certNameInfo, StringComparison.OrdinalIgnoreCase);
		});
	}

	public unsafe void FindByIssuerName(string issuerName)
	{
		fixed (char* pvFindPara = issuerName)
		{
			FindCore<object>(CertFindType.CERT_FIND_ISSUER_STR, pvFindPara);
		}
	}

	public void FindByIssuerDistinguishedName(string issuerDistinguishedName)
	{
		FindCore(issuerDistinguishedName, delegate(string issuerDistinguishedName, SafeCertContextHandle pCertContext)
		{
			string certNameInfo = GetCertNameInfo(pCertContext, CertNameType.CERT_NAME_RDN_TYPE, CertNameFlags.CERT_NAME_ISSUER_FLAG);
			return issuerDistinguishedName.Equals(certNameInfo, StringComparison.OrdinalIgnoreCase);
		});
	}

	public unsafe void FindBySerialNumber(BigInteger hexValue, BigInteger decimalValue)
	{
		FindCore((hexValue, decimalValue), delegate((BigInteger hexValue, BigInteger decimalValue) state, SafeCertContextHandle pCertContext)
		{
			byte[] bytes = pCertContext.CertContext->pCertInfo->SerialNumber.ToByteArray();
			GC.KeepAlive(pCertContext);
			BigInteger other = PositiveBigIntegerFromByteArray(bytes);
			return state.hexValue.Equals(other) || state.decimalValue.Equals(other);
		});
	}

	public void FindByTimeValid(DateTime dateTime)
	{
		FindByTime(dateTime, 0);
	}

	public void FindByTimeNotYetValid(DateTime dateTime)
	{
		FindByTime(dateTime, -1);
	}

	public void FindByTimeExpired(DateTime dateTime)
	{
		FindByTime(dateTime, 1);
	}

	private unsafe void FindByTime(DateTime dateTime, int compareResult)
	{
		FILETIME item = FILETIME.FromDateTime(dateTime);
		FindCore((item, compareResult), delegate((FILETIME fileTime, int compareResult) state, SafeCertContextHandle pCertContext)
		{
			int num = global::Interop.crypt32.CertVerifyTimeValidity(ref state.fileTime, pCertContext.CertContext->pCertInfo);
			GC.KeepAlive(pCertContext);
			return num == state.compareResult;
		});
	}

	public unsafe void FindByTemplateName(string templateName)
	{
		FindCore(templateName, delegate(string templateName, SafeCertContextHandle pCertContext)
		{
			bool foundMatch = false;
			CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
			CERT_EXTENSION* ptr = global::Interop.crypt32.CertFindExtension("1.3.6.1.4.1.311.20.2", pCertInfo->cExtension, pCertInfo->rgExtension);
			if (ptr != null)
			{
				byte[] encoded = ptr->Value.ToByteArray();
				if (!encoded.DecodeObjectNoThrow(CryptDecodeObjectStructType.X509_UNICODE_ANY_STRING, delegate(void* pvDecoded, int cbDecoded)
				{
					string value2 = Marshal.PtrToStringUni(new IntPtr(((CERT_NAME_VALUE*)pvDecoded)->Value.pbData));
					if (templateName.Equals(value2, StringComparison.OrdinalIgnoreCase))
					{
						foundMatch = true;
					}
				}))
				{
					return false;
				}
			}
			if (!foundMatch)
			{
				CERT_EXTENSION* ptr2 = global::Interop.crypt32.CertFindExtension("1.3.6.1.4.1.311.21.7", pCertInfo->cExtension, pCertInfo->rgExtension);
				if (ptr2 != null)
				{
					byte[] encoded2 = ptr2->Value.ToByteArray();
					if (!encoded2.DecodeObjectNoThrow(CryptDecodeObjectStructType.X509_CERTIFICATE_TEMPLATE, delegate(void* pvDecoded, int cbDecoded)
					{
						string value = Marshal.PtrToStringAnsi(((CERT_TEMPLATE_EXT*)pvDecoded)->pszObjId);
						string text = global::Interop.Crypt32.FindOidInfo(global::Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY, templateName, OidGroup.Template, fallBackToAllGroups: true).OID;
						if (text == null)
						{
							text = templateName;
						}
						if (text.Equals(value, StringComparison.OrdinalIgnoreCase))
						{
							foundMatch = true;
						}
					}))
					{
						return false;
					}
				}
			}
			GC.KeepAlive(pCertContext);
			return foundMatch;
		});
	}

	public unsafe void FindByApplicationPolicy(string oidValue)
	{
		FindCore(oidValue, delegate(string oidValue, SafeCertContextHandle pCertContext)
		{
			int pcbOIDs = 0;
			if (!global::Interop.crypt32.CertGetValidUsages(1, ref pCertContext, out var cNumOIDs, null, ref pcbOIDs))
			{
				return false;
			}
			if (cNumOIDs == -1)
			{
				return true;
			}
			fixed (byte* ptr = new byte[pcbOIDs])
			{
				if (!global::Interop.crypt32.CertGetValidUsages(1, ref pCertContext, out cNumOIDs, ptr, ref pcbOIDs))
				{
					return false;
				}
				IntPtr* ptr2 = (IntPtr*)ptr;
				for (int i = 0; i < cNumOIDs; i++)
				{
					string value = Marshal.PtrToStringAnsi(ptr2[i]);
					if (oidValue.Equals(value, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				return false;
			}
		});
	}

	public unsafe void FindByCertificatePolicy(string oidValue)
	{
		FindCore(oidValue, delegate(string oidValue, SafeCertContextHandle pCertContext)
		{
			CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
			CERT_EXTENSION* ptr = global::Interop.crypt32.CertFindExtension("2.5.29.32", pCertInfo->cExtension, pCertInfo->rgExtension);
			if (ptr == null)
			{
				return false;
			}
			bool foundMatch = false;
			byte[] encoded = ptr->Value.ToByteArray();
			if (!encoded.DecodeObjectNoThrow(CryptDecodeObjectStructType.X509_CERT_POLICIES, delegate(void* pvDecoded, int cbDecoded)
			{
				for (int i = 0; i < ((CERT_POLICIES_INFO*)pvDecoded)->cPolicyInfo; i++)
				{
					CERT_POLICY_INFO* ptr2 = ((CERT_POLICIES_INFO*)pvDecoded)->rgPolicyInfo + i;
					string value = Marshal.PtrToStringAnsi(ptr2->pszPolicyIdentifier);
					if (oidValue.Equals(value, StringComparison.OrdinalIgnoreCase))
					{
						foundMatch = true;
						break;
					}
				}
			}))
			{
				return false;
			}
			GC.KeepAlive(pCertContext);
			return foundMatch;
		});
	}

	public unsafe void FindByExtension(string oidValue)
	{
		FindCore(oidValue, delegate(string oidValue, SafeCertContextHandle pCertContext)
		{
			CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
			CERT_EXTENSION* ptr = global::Interop.crypt32.CertFindExtension(oidValue, pCertInfo->cExtension, pCertInfo->rgExtension);
			GC.KeepAlive(pCertContext);
			return ptr != null;
		});
	}

	public unsafe void FindByKeyUsage(X509KeyUsageFlags keyUsage)
	{
		FindCore(keyUsage, delegate(X509KeyUsageFlags keyUsage, SafeCertContextHandle pCertContext)
		{
			CERT_INFO* pCertInfo = pCertContext.CertContext->pCertInfo;
			if (!global::Interop.crypt32.CertGetIntendedKeyUsage(CertEncodingType.All, pCertInfo, out var pbKeyUsage, 4))
			{
				return true;
			}
			GC.KeepAlive(pCertContext);
			return (pbKeyUsage & keyUsage) == keyUsage;
		});
	}

	public void FindBySubjectKeyIdentifier(byte[] keyIdentifier)
	{
		FindCore(keyIdentifier, delegate(byte[] keyIdentifier, SafeCertContextHandle pCertContext)
		{
			int pcbData = 0;
			if (!global::Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, null, ref pcbData))
			{
				return false;
			}
			byte[] array = new byte[pcbData];
			return global::Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_IDENTIFIER_PROP_ID, array, ref pcbData) && keyIdentifier.ContentsEqual(array);
		});
	}

	public void Dispose()
	{
		_storePal.Dispose();
	}

	private unsafe void FindCore<TState>(TState state, Func<TState, SafeCertContextHandle, bool> filter)
	{
		FindCore(CertFindType.CERT_FIND_ANY, null, state, filter);
	}

	private unsafe void FindCore<TState>(CertFindType dwFindType, void* pvFindPara, TState state = default(TState), Func<TState, SafeCertContextHandle, bool> filter = null)
	{
		SafeCertStoreHandle safeCertStoreHandle = global::Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_MEMORY, CertEncodingType.All, IntPtr.Zero, CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG, null);
		if (safeCertStoreHandle.IsInvalid)
		{
			throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
		}
		SafeCertContextHandle pCertContext = null;
		while (global::Interop.crypt32.CertFindCertificateInStore(_storePal.SafeCertStoreHandle, dwFindType, pvFindPara, ref pCertContext))
		{
			if ((filter == null || filter(state, pCertContext)) && (!_validOnly || VerifyCertificateIgnoringErrors(pCertContext)) && !global::Interop.crypt32.CertAddCertificateLinkToStore(safeCertStoreHandle, pCertContext, CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
			{
				throw Marshal.GetLastWin32Error().ToCryptographicException();
			}
		}
		using StorePal storePal = new StorePal(safeCertStoreHandle);
		storePal.CopyTo(_copyTo);
	}

	private static bool VerifyCertificateIgnoringErrors(SafeCertContextHandle pCertContext)
	{
		ChainPal chainPal = ChainPal.BuildChain(useMachineContext: false, CertificatePal.FromHandle(pCertContext.DangerousGetHandle()), null, null, null, X509RevocationMode.NoCheck, X509RevocationFlag.ExcludeRoot, null, X509ChainTrustMode.System, DateTime.Now, new TimeSpan(0, 0, 0), disableAia: false);
		if (chainPal == null)
		{
			return false;
		}
		using (chainPal)
		{
			if (!chainPal.Verify(X509VerificationFlags.NoFlag, out var _).GetValueOrDefault())
			{
				return false;
			}
		}
		return true;
	}

	private static string GetCertNameInfo(SafeCertContextHandle pCertContext, CertNameType dwNameType, CertNameFlags dwNameFlags)
	{
		return global::Interop.crypt32.CertGetNameString(pCertContext, dwNameType, dwNameFlags, (CertNameStringType)33554435);
	}
}
