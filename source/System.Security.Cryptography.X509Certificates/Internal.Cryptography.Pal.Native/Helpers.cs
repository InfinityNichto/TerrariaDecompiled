using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Internal.Cryptography.Pal.Native;

internal static class Helpers
{
	public unsafe delegate void DecodedObjectReceiver(void* pvDecodedObject, int cbDecodedObject);

	public unsafe delegate TResult DecodedObjectReceiver<TResult>(void* pvDecodedObject, int cbDecodedObject);

	public unsafe static SafeHandle ToLpstrArray(this OidCollection oids, out int numOids)
	{
		if (oids == null || oids.Count == 0)
		{
			numOids = 0;
			return SafePointerHandle<SafeLocalAllocHandle>.InvalidHandle;
		}
		string[] array = new string[oids.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = oids[i].Value;
		}
		SafeLocalAllocHandle safeLocalAllocHandle;
		checked
		{
			int num = array.Length * sizeof(void*);
			string[] array2 = array;
			foreach (string text in array2)
			{
				num += text.Length + 1;
			}
			safeLocalAllocHandle = SafeLocalAllocHandle.Create(num);
		}
		byte** ptr = (byte**)(void*)safeLocalAllocHandle.DangerousGetHandle();
		byte* ptr2 = (byte*)(ptr + array.Length);
		for (int k = 0; k < array.Length; k++)
		{
			string text2 = array[k];
			ptr[k] = ptr2;
			int bytes = Encoding.ASCII.GetBytes(text2, new Span<byte>(ptr2, text2.Length));
			ptr2[text2.Length] = 0;
			ptr2 += text2.Length + 1;
		}
		numOids = array.Length;
		return safeLocalAllocHandle;
	}

	public static byte[] ValueAsAscii(this Oid oid)
	{
		return Encoding.ASCII.GetBytes(oid.Value);
	}

	public unsafe static TResult DecodeObject<TResult>(this byte[] encoded, CryptDecodeObjectStructType lpszStructType, DecodedObjectReceiver<TResult> receiver)
	{
		int pcbStructInfo = 0;
		if (!global::Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, null, ref pcbStructInfo))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		byte* ptr = stackalloc byte[(int)(uint)pcbStructInfo];
		if (!global::Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, ptr, ref pcbStructInfo))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		return receiver(ptr, pcbStructInfo);
	}

	public unsafe static TResult DecodeObject<TResult>(this byte[] encoded, string lpszStructType, DecodedObjectReceiver<TResult> receiver)
	{
		int pcbStructInfo = 0;
		if (!global::Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, null, ref pcbStructInfo))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		byte* ptr = stackalloc byte[(int)(uint)pcbStructInfo];
		if (!global::Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, ptr, ref pcbStructInfo))
		{
			throw Marshal.GetLastWin32Error().ToCryptographicException();
		}
		return receiver(ptr, pcbStructInfo);
	}

	public unsafe static bool DecodeObjectNoThrow(this byte[] encoded, CryptDecodeObjectStructType lpszStructType, DecodedObjectReceiver receiver)
	{
		int pcbStructInfo = 0;
		if (!global::Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, null, ref pcbStructInfo))
		{
			return false;
		}
		byte* ptr = stackalloc byte[(int)(uint)pcbStructInfo];
		if (!global::Interop.crypt32.CryptDecodeObjectPointer(CertEncodingType.All, lpszStructType, encoded, encoded.Length, CryptDecodeObjectFlags.None, ptr, ref pcbStructInfo))
		{
			return false;
		}
		receiver(ptr, pcbStructInfo);
		return true;
	}
}
