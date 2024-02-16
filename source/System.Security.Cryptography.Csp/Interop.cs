using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

internal static class Interop
{
	internal static class Advapi32
	{
		[Flags]
		internal enum CryptCreateHashFlags
		{
			None = 0
		}

		internal enum GetDefaultProviderFlags
		{
			CRYPT_MACHINE_DEFAULT = 1,
			CRYPT_USER_DEFAULT
		}

		internal enum CryptHashProperty
		{
			HP_ALGID = 1,
			HP_HASHVAL = 2,
			HP_HASHSIZE = 4,
			HP_HMAC_INFO = 5,
			HP_TLS1PRF_LABEL = 6,
			HP_TLS1PRF_SEED = 7
		}

		internal enum CryptGetKeyParamFlags
		{
			CRYPT_EXPORT = 4,
			KP_IV = 1,
			KP_PERMISSIONS = 6,
			KP_ALGID = 7,
			KP_KEYLEN = 9
		}

		internal enum CryptProvParam
		{
			PP_CLIENT_HWND = 1,
			PP_IMPTYPE = 3,
			PP_NAME = 4,
			PP_CONTAINER = 6,
			PP_PROVTYPE = 16,
			PP_KEYSET_TYPE = 27,
			PP_KEYEXCHANGE_PIN = 32,
			PP_SIGNATURE_PIN = 33,
			PP_UNIQUE_CONTAINER = 36
		}

		internal enum KeySpec
		{
			AT_KEYEXCHANGE = 1,
			AT_SIGNATURE
		}

		[Flags]
		internal enum CryptSignAndVerifyHashFlags
		{
			None = 0,
			CRYPT_NOHASHOID = 1,
			CRYPT_TYPE2_FORMAT = 2,
			CRYPT_X931_FORMAT = 4
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CryptAcquireContextW", SetLastError = true)]
		public static extern bool CryptAcquireContext(out SafeProvHandle phProv, string szContainer, string szProvider, int dwProvType, uint dwFlags);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CryptCreateHash(SafeProvHandle hProv, int Algid, SafeKeyHandle hKey, CryptCreateHashFlags dwFlags, out SafeHashHandle phHash);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptDecrypt(SafeKeyHandle hKey, SafeHashHandle hHash, bool Final, int dwFlags, byte[] pbData, ref int pdwDataLen);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CryptDeriveKey(SafeProvHandle hProv, int Algid, SafeHashHandle hBaseData, int dwFlags, out SafeKeyHandle phKey);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptDestroyHash(IntPtr hHash);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptDestroyKey(IntPtr hKey);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptEncrypt(SafeKeyHandle hKey, SafeHashHandle hHash, bool Final, int dwFlags, byte[] pbData, ref int pdwDataLen, int dwBufLen);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptExportKey(SafeKeyHandle hKey, SafeKeyHandle hExpKey, int dwBlobType, int dwFlags, [In][Out] byte[] pbData, ref int dwDataLen);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CryptGenKey(SafeProvHandle hProv, int Algid, int dwFlags, out SafeKeyHandle phKey);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CryptGetDefaultProviderW", SetLastError = true)]
		public static extern bool CryptGetDefaultProvider(int dwProvType, IntPtr pdwReserved, GetDefaultProviderFlags dwFlags, [Out] StringBuilder pszProvName, ref int pcbProvName);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptGetHashParam(SafeHashHandle hHash, CryptHashProperty dwParam, out int pbData, [In][Out] ref int pdwDataLen, int dwFlags);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptSetHashParam(SafeHashHandle hHash, CryptHashProperty dwParam, byte[] buffer, int dwFlags);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptGetKeyParam(SafeKeyHandle hKey, CryptGetKeyParamFlags dwParam, byte[] pbData, ref int pdwDataLen, int dwFlags);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptSetProvParam(SafeHandle safeProvHandle, CryptProvParam dwParam, IntPtr pbData, int dwFlags);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptSetProvParam(SafeProvHandle hProv, CryptProvParam dwParam, ref IntPtr pbData, int dwFlags);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptGetProvParam(SafeHandle safeProvHandle, CryptProvParam dwParam, IntPtr pbData, ref int dwDataLen, int dwFlags);

		public unsafe static bool CryptGetProvParam(SafeHandle safeProvHandle, CryptProvParam dwParam, Span<byte> pbData, ref int dwDataLen)
		{
			if (pbData.IsEmpty)
			{
				return CryptGetProvParam(safeProvHandle, dwParam, IntPtr.Zero, ref dwDataLen, 0);
			}
			if (dwDataLen > pbData.Length)
			{
				throw new IndexOutOfRangeException();
			}
			fixed (byte* ptr = &MemoryMarshal.GetReference(pbData))
			{
				return CryptGetProvParam(safeProvHandle, dwParam, (IntPtr)ptr, ref dwDataLen, 0);
			}
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool CryptGetUserKey(SafeProvHandle hProv, int dwKeySpec, out SafeKeyHandle phUserKey);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CryptHashData(SafeHashHandle hHash, byte[] pbData, int dwDataLen, int dwFlags);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern bool CryptImportKey(SafeProvHandle hProv, byte* pbData, int dwDataLen, SafeKeyHandle hPubKey, int dwFlags, out SafeKeyHandle phKey);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptReleaseContext(IntPtr hProv, int dwFlags);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptSetKeyParam(SafeKeyHandle hKey, int dwParam, byte[] pbData, int dwFlags);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool CryptSetKeyParam(SafeKeyHandle safeKeyHandle, int dwParam, ref int pdw, int dwFlags);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CryptSignHashW", SetLastError = true)]
		public static extern bool CryptSignHash(SafeHashHandle hHash, KeySpec dwKeySpec, string szDescription, CryptSignAndVerifyHashFlags dwFlags, [Out] byte[] pbSignature, [In][Out] ref int pdwSigLen);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CryptVerifySignatureW", SetLastError = true)]
		public static extern bool CryptVerifySignature(SafeHashHandle hHash, byte[] pbSignature, int dwSigLen, SafeKeyHandle hPubKey, string szDescription, CryptSignAndVerifyHashFlags dwFlags);
	}

	internal static class Crypt32
	{
		internal struct CRYPT_OID_INFO
		{
			public int cbSize;

			public IntPtr pszOID;

			public IntPtr pwszName;

			public OidGroup dwGroupId;

			public int AlgId;

			public int cbData;

			public IntPtr pbData;
		}

		internal enum CryptOidInfoKeyType
		{
			CRYPT_OID_INFO_OID_KEY = 1,
			CRYPT_OID_INFO_NAME_KEY,
			CRYPT_OID_INFO_ALGID_KEY,
			CRYPT_OID_INFO_SIGN_KEY,
			CRYPT_OID_INFO_CNG_ALGID_KEY,
			CRYPT_OID_INFO_CNG_SIGN_KEY
		}

		internal static CRYPT_OID_INFO FindOidInfo(CryptOidInfoKeyType keyType, string key, OidGroup group, bool fallBackToAllGroups)
		{
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = keyType switch
				{
					CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY => Marshal.StringToCoTaskMemAnsi(key), 
					CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY => Marshal.StringToCoTaskMemUni(key), 
					_ => throw new NotSupportedException(), 
				};
				if (!OidGroupWillNotUseActiveDirectory(group))
				{
					OidGroup group2 = group | (OidGroup)(-2147483648);
					IntPtr intPtr2 = CryptFindOIDInfo(keyType, intPtr, group2);
					if (intPtr2 != IntPtr.Zero)
					{
						return Marshal.PtrToStructure<CRYPT_OID_INFO>(intPtr2);
					}
				}
				IntPtr intPtr3 = CryptFindOIDInfo(keyType, intPtr, group);
				if (intPtr3 != IntPtr.Zero)
				{
					return Marshal.PtrToStructure<CRYPT_OID_INFO>(intPtr3);
				}
				if (fallBackToAllGroups && group != 0)
				{
					IntPtr intPtr4 = CryptFindOIDInfo(keyType, intPtr, OidGroup.All);
					if (intPtr4 != IntPtr.Zero)
					{
						return Marshal.PtrToStructure<CRYPT_OID_INFO>(intPtr4);
					}
				}
				CRYPT_OID_INFO result = default(CRYPT_OID_INFO);
				result.AlgId = -1;
				return result;
			}
			finally
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(intPtr);
				}
			}
		}

		private static bool OidGroupWillNotUseActiveDirectory(OidGroup group)
		{
			if (group != OidGroup.HashAlgorithm && group != OidGroup.EncryptionAlgorithm && group != OidGroup.PublicKeyAlgorithm && group != OidGroup.SignatureAlgorithm && group != OidGroup.Attribute && group != OidGroup.ExtensionOrAttribute)
			{
				return group == OidGroup.KeyDerivationFunction;
			}
			return true;
		}

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr CryptFindOIDInfo(CryptOidInfoKeyType dwKeyType, IntPtr pvKey, OidGroup group);
	}

	internal static class Kernel32
	{
		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Unicode, EntryPoint = "FormatMessageW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId, int dwLanguageId, void* lpBuffer, int nSize, IntPtr arguments);

		internal static string GetMessage(int errorCode)
		{
			return GetMessage(errorCode, IntPtr.Zero);
		}

		internal unsafe static string GetMessage(int errorCode, IntPtr moduleHandle)
		{
			int num = 12800;
			if (moduleHandle != IntPtr.Zero)
			{
				num |= 0x800;
			}
			Span<char> span = stackalloc char[256];
			fixed (char* lpBuffer = span)
			{
				int num2 = FormatMessage(num, moduleHandle, (uint)errorCode, 0, lpBuffer, span.Length, IntPtr.Zero);
				if (num2 > 0)
				{
					return GetAndTrimString(span.Slice(0, num2));
				}
			}
			if (Marshal.GetLastWin32Error() == 122)
			{
				IntPtr intPtr = default(IntPtr);
				try
				{
					int num3 = FormatMessage(num | 0x100, moduleHandle, (uint)errorCode, 0, &intPtr, 0, IntPtr.Zero);
					if (num3 > 0)
					{
						return GetAndTrimString(new Span<char>((void*)intPtr, num3));
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
			return $"Unknown error (0x{errorCode:x})";
		}

		private static string GetAndTrimString(Span<char> buffer)
		{
			int num = buffer.Length;
			while (num > 0 && buffer[num - 1] <= ' ')
			{
				num--;
			}
			return buffer.Slice(0, num).ToString();
		}
	}
}
