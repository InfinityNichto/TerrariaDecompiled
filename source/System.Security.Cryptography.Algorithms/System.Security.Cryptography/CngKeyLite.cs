using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class CngKeyLite
{
	private static readonly SafeNCryptProviderHandle s_microsoftSoftwareProviderHandle = OpenNCryptProvider("Microsoft Software Key Storage Provider");

	private static readonly byte[] s_pkcs12TripleDesOidBytes = Encoding.ASCII.GetBytes("1.2.840.113549.1.12.1.3\0");

	internal unsafe static SafeNCryptKeyHandle ImportKeyBlob(string blobType, ReadOnlySpan<byte> keyBlob, bool encrypted = false, ReadOnlySpan<char> password = default(ReadOnlySpan<char>))
	{
		global::Interop.NCrypt.ErrorCode errorCode;
		SafeNCryptKeyHandle phKey;
		if (encrypted)
		{
			using SafeUnicodeStringHandle safeUnicodeStringHandle = new SafeUnicodeStringHandle(password);
			global::Interop.NCrypt.NCryptBuffer* ptr = stackalloc global::Interop.NCrypt.NCryptBuffer[1];
			*ptr = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsSecret,
				cbBuffer = checked(2 * (password.Length + 1)),
				pvBuffer = safeUnicodeStringHandle.DangerousGetHandle()
			};
			if (ptr->pvBuffer == IntPtr.Zero)
			{
				ptr->cbBuffer = 0;
			}
			global::Interop.NCrypt.NCryptBufferDesc nCryptBufferDesc = default(global::Interop.NCrypt.NCryptBufferDesc);
			nCryptBufferDesc.cBuffers = 1;
			nCryptBufferDesc.pBuffers = (IntPtr)ptr;
			nCryptBufferDesc.ulVersion = 0;
			global::Interop.NCrypt.NCryptBufferDesc pParameterList = nCryptBufferDesc;
			errorCode = global::Interop.NCrypt.NCryptImportKey(s_microsoftSoftwareProviderHandle, IntPtr.Zero, blobType, ref pParameterList, out phKey, ref MemoryMarshal.GetReference(keyBlob), keyBlob.Length, 0);
		}
		else
		{
			errorCode = global::Interop.NCrypt.NCryptImportKey(s_microsoftSoftwareProviderHandle, IntPtr.Zero, blobType, IntPtr.Zero, out phKey, ref MemoryMarshal.GetReference(keyBlob), keyBlob.Length, 0);
		}
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		SetExportable(phKey);
		return phKey;
	}

	internal static SafeNCryptKeyHandle ImportKeyBlob(string blobType, byte[] keyBlob, string curveName)
	{
		SafeNCryptKeyHandle safeNCryptKeyHandle = ECCng.ImportKeyBlob(blobType, keyBlob, curveName, s_microsoftSoftwareProviderHandle);
		SetExportable(safeNCryptKeyHandle);
		return safeNCryptKeyHandle;
	}

	internal static byte[] ExportKeyBlob(SafeNCryptKeyHandle keyHandle, string blobType)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptExportKey(keyHandle, IntPtr.Zero, blobType, IntPtr.Zero, null, 0, out var pcbResult, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		if (pcbResult == 0)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[pcbResult];
		errorCode = global::Interop.NCrypt.NCryptExportKey(keyHandle, IntPtr.Zero, blobType, IntPtr.Zero, ref array[0], array.Length, out pcbResult, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		if (array.Length != pcbResult)
		{
			Span<byte> buffer = array.AsSpan(0, pcbResult);
			byte[] result = buffer.ToArray();
			CryptographicOperations.ZeroMemory(buffer);
			return result;
		}
		return array;
	}

	internal static byte[] ExportPkcs8KeyBlob(SafeNCryptKeyHandle keyHandle, ReadOnlySpan<char> password, int kdfCount)
	{
		int bytesWritten;
		byte[] allocated;
		bool flag = ExportPkcs8KeyBlob(allocate: true, keyHandle, password, kdfCount, Span<byte>.Empty, out bytesWritten, out allocated);
		return allocated;
	}

	internal static bool TryExportPkcs8KeyBlob(SafeNCryptKeyHandle keyHandle, ReadOnlySpan<char> password, int kdfCount, Span<byte> destination, out int bytesWritten)
	{
		byte[] allocated;
		return ExportPkcs8KeyBlob(allocate: false, keyHandle, password, kdfCount, destination, out bytesWritten, out allocated);
	}

	internal unsafe static bool ExportPkcs8KeyBlob(bool allocate, SafeNCryptKeyHandle keyHandle, ReadOnlySpan<char> password, int kdfCount, Span<byte> destination, out int bytesWritten, out byte[] allocated)
	{
		using SafeUnicodeStringHandle safeUnicodeStringHandle = new SafeUnicodeStringHandle(password);
		fixed (byte* ptr2 = s_pkcs12TripleDesOidBytes)
		{
			global::Interop.NCrypt.NCryptBuffer* ptr = stackalloc global::Interop.NCrypt.NCryptBuffer[3];
			global::Interop.NCrypt.PBE_PARAMS pBE_PARAMS = default(global::Interop.NCrypt.PBE_PARAMS);
			Span<byte> data = new Span<byte>(pBE_PARAMS.rgbSalt, 8);
			RandomNumberGenerator.Fill(data);
			pBE_PARAMS.Params.cbSalt = data.Length;
			pBE_PARAMS.Params.iIterations = kdfCount;
			*ptr = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsSecret,
				cbBuffer = checked(2 * (password.Length + 1)),
				pvBuffer = safeUnicodeStringHandle.DangerousGetHandle()
			};
			if (ptr->pvBuffer == IntPtr.Zero)
			{
				ptr->cbBuffer = 0;
			}
			ptr[1] = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsAlgOid,
				cbBuffer = s_pkcs12TripleDesOidBytes.Length,
				pvBuffer = (IntPtr)ptr2
			};
			ptr[2] = new global::Interop.NCrypt.NCryptBuffer
			{
				BufferType = global::Interop.NCrypt.BufferType.PkcsAlgParam,
				cbBuffer = sizeof(global::Interop.NCrypt.PBE_PARAMS),
				pvBuffer = (IntPtr)(&pBE_PARAMS)
			};
			global::Interop.NCrypt.NCryptBufferDesc nCryptBufferDesc = default(global::Interop.NCrypt.NCryptBufferDesc);
			nCryptBufferDesc.cBuffers = 3;
			nCryptBufferDesc.pBuffers = (IntPtr)ptr;
			nCryptBufferDesc.ulVersion = 0;
			global::Interop.NCrypt.NCryptBufferDesc pParameterList = nCryptBufferDesc;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptExportKey(keyHandle, IntPtr.Zero, "PKCS8_PRIVATEKEY", ref pParameterList, ref MemoryMarshal.GetReference(default(Span<byte>)), 0, out var pcbResult, 0);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
			allocated = null;
			if (allocate)
			{
				allocated = new byte[pcbResult];
				destination = allocated;
			}
			else if (pcbResult > destination.Length)
			{
				bytesWritten = 0;
				return false;
			}
			errorCode = global::Interop.NCrypt.NCryptExportKey(keyHandle, IntPtr.Zero, "PKCS8_PRIVATEKEY", ref pParameterList, ref MemoryMarshal.GetReference(destination), destination.Length, out pcbResult, 0);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
			if (allocate && pcbResult != destination.Length)
			{
				byte[] array = new byte[pcbResult];
				destination.Slice(0, pcbResult).CopyTo(array);
				CryptographicOperations.ZeroMemory(allocated.AsSpan(0, pcbResult));
				allocated = array;
			}
			bytesWritten = pcbResult;
			return true;
		}
	}

	internal static SafeNCryptKeyHandle GenerateNewExportableKey(string algorithm, int keySize)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptCreatePersistedKey(s_microsoftSoftwareProviderHandle, out var phKey, algorithm, null, 0, CngKeyCreationOptions.None);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		SetExportable(phKey);
		SetKeyLength(phKey, keySize);
		errorCode = global::Interop.NCrypt.NCryptFinalizeKey(phKey, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		return phKey;
	}

	internal static SafeNCryptKeyHandle GenerateNewExportableKey(string algorithm, string curveName)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptCreatePersistedKey(s_microsoftSoftwareProviderHandle, out var phKey, algorithm, null, 0, CngKeyCreationOptions.None);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		SetExportable(phKey);
		SetCurveName(phKey, curveName);
		errorCode = global::Interop.NCrypt.NCryptFinalizeKey(phKey, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		return phKey;
	}

	internal static SafeNCryptKeyHandle GenerateNewExportableKey(string algorithm, ref ECCurve explicitCurve)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptCreatePersistedKey(s_microsoftSoftwareProviderHandle, out var phKey, algorithm, null, 0, CngKeyCreationOptions.None);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		SetExportable(phKey);
		byte[] primeCurveParameterBlob = ECCng.GetPrimeCurveParameterBlob(ref explicitCurve);
		SetProperty(phKey, "ECCParameters", primeCurveParameterBlob);
		errorCode = global::Interop.NCrypt.NCryptFinalizeKey(phKey, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		return phKey;
	}

	private unsafe static void SetExportable(SafeNCryptKeyHandle keyHandle)
	{
		CngExportPolicies cngExportPolicies = CngExportPolicies.AllowPlaintextExport;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "Export Policy", &cngExportPolicies, 4, CngPropertyOptions.Persist);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
	}

	private unsafe static void SetKeyLength(SafeNCryptKeyHandle keyHandle, int keySize)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(keyHandle, "Length", &keySize, 4, CngPropertyOptions.Persist);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
	}

	internal static int GetKeyLength(SafeNCryptKeyHandle keyHandle)
	{
		int result = 0;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptGetIntProperty(keyHandle, "PublicKeyLength", ref result);
		if (errorCode != 0)
		{
			errorCode = global::Interop.NCrypt.NCryptGetIntProperty(keyHandle, "Length", ref result);
		}
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		return result;
	}

	private static SafeNCryptProviderHandle OpenNCryptProvider(string providerName)
	{
		SafeNCryptProviderHandle phProvider;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptOpenStorageProvider(out phProvider, providerName, 0);
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		return phProvider;
	}

	private unsafe static byte[] GetProperty(SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
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

	internal unsafe static string GetPropertyAsString(SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
	{
		byte[] property = GetProperty(ncryptHandle, propertyName, options);
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

	internal static string GetCurveName(SafeNCryptHandle ncryptHandle)
	{
		return GetPropertyAsString(ncryptHandle, "ECCCurveName", CngPropertyOptions.None);
	}

	internal static void SetCurveName(SafeNCryptHandle keyHandle, string curveName)
	{
		byte[] array = new byte[(curveName.Length + 1) * 2];
		Encoding.Unicode.GetBytes(curveName, 0, curveName.Length, array, 0);
		SetProperty(keyHandle, "ECCCurveName", array);
	}

	private unsafe static void SetProperty(SafeNCryptHandle ncryptHandle, string propertyName, byte[] value)
	{
		fixed (byte* pbInput = value)
		{
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSetProperty(ncryptHandle, propertyName, pbInput, value.Length, CngPropertyOptions.None);
			if (errorCode != 0)
			{
				throw errorCode.ToCryptographicException();
			}
		}
	}
}
