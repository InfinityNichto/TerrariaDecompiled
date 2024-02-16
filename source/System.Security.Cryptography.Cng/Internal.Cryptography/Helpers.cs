using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal static class Helpers
{
	[UnsupportedOSPlatformGuard("android")]
	public static bool IsRC2Supported => !OperatingSystem.IsAndroid();

	public static bool UsesIv(this CipherMode cipherMode)
	{
		return cipherMode != CipherMode.ECB;
	}

	public static byte[] GetCipherIv(this CipherMode cipherMode, byte[] iv)
	{
		if (cipherMode.UsesIv())
		{
			if (iv == null)
			{
				throw new CryptographicException(System.SR.Cryptography_MissingIV);
			}
			return iv;
		}
		return null;
	}

	public static byte[] MapZeroLengthArrayToNonNullPointer(this byte[] src)
	{
		if (src != null && src.Length == 0)
		{
			return new byte[1];
		}
		return src;
	}

	public static SafeNCryptProviderHandle OpenStorageProvider(this CngProvider provider)
	{
		string provider2 = provider.Provider;
		SafeNCryptProviderHandle phProvider;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptOpenStorageProvider(out phProvider, provider2, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		return phProvider;
	}

	public unsafe static byte[] GetProperty(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, null, 0, out var pcbResult, options);
		switch (errorCode)
		{
		case global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND:
			return null;
		default:
			throw errorCode.ToCryptographicException();
		case global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS:
		{
			byte[] array = new byte[pcbResult];
			fixed (byte* pbOutput = array)
			{
				errorCode = global::Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, pbOutput, array.Length, out pcbResult, options);
			}
			switch (errorCode)
			{
			case global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND:
				return null;
			default:
				throw errorCode.ToCryptographicException();
			case global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS:
				Array.Resize(ref array, pcbResult);
				return array;
			}
		}
		}
	}

	public unsafe static string GetPropertyAsString(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		byte[] property = ncryptHandle.GetProperty(propertyName, options);
		if (property == null)
		{
			return null;
		}
		if (property.Length == 0)
		{
			return string.Empty;
		}
		fixed (byte* ptr = &property[0])
		{
			return Marshal.PtrToStringUni((IntPtr)ptr);
		}
	}

	public static int GetPropertyAsDword(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		byte[] property = ncryptHandle.GetProperty(propertyName, options);
		if (property == null)
		{
			return 0;
		}
		return BitConverter.ToInt32(property, 0);
	}

	public unsafe static IntPtr GetPropertyAsIntPtr(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		Unsafe.SkipInit(out IntPtr intPtr);
		int pcbResult;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, &intPtr, IntPtr.Size, out pcbResult, options);
		return errorCode switch
		{
			global::Interop.NCrypt.ErrorCode.NTE_NOT_FOUND => IntPtr.Zero, 
			global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS => intPtr, 
			_ => throw errorCode.ToCryptographicException(), 
		};
	}

	public unsafe static void SetExportPolicy(this SafeNCryptKeyHandle keyHandle, CngExportPolicies exportPolicy)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "Export Policy", &exportPolicy, 4, CngPropertyOptions.Persist);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
	}

	public static int BitSizeToByteSize(this int bits)
	{
		return (bits + 7) / 8;
	}

	public static byte[] GenerateRandom(int count)
	{
		byte[] array = new byte[count];
		RandomNumberGenerator.Fill(array);
		return array;
	}

	[return: NotNullIfNotNull("src")]
	public static byte[] CloneByteArray(this byte[] src)
	{
		if (src == null)
		{
			return null;
		}
		return (byte[])src.Clone();
	}

	public static int GetPaddingSize(this SymmetricAlgorithm algorithm, CipherMode mode, int feedbackSizeInBits)
	{
		return ((mode == CipherMode.CFB) ? feedbackSizeInBits : algorithm.BlockSize) / 8;
	}
}
