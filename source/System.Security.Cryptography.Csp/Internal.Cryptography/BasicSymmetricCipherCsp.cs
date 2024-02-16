using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal sealed class BasicSymmetricCipherCsp : Internal.Cryptography.BasicSymmetricCipher
{
	private readonly bool _encrypting;

	private SafeProvHandle _hProvider;

	private SafeKeyHandle _hKey;

	public BasicSymmetricCipherCsp(int algId, CipherMode cipherMode, int blockSizeInBytes, byte[] key, int effectiveKeyLength, bool addNoSaltFlag, byte[] iv, bool encrypting, int feedbackSize, int paddingSizeInBytes)
		: base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
	{
		_encrypting = encrypting;
		_hProvider = AcquireSafeProviderHandle();
		_hKey = ImportCspBlob(_hProvider, algId, key, addNoSaltFlag);
		CapiHelper.SetKeyParameter(_hKey, CapiHelper.CryptGetKeyParamQueryType.KP_MODE, (int)cipherMode);
		if (cipherMode == CipherMode.CFB)
		{
			CapiHelper.SetKeyParameter(_hKey, CapiHelper.CryptGetKeyParamQueryType.KP_MODE_BITS, feedbackSize);
		}
		byte[] cipherIv = cipherMode.GetCipherIv(iv);
		if (cipherIv != null)
		{
			CapiHelper.SetKeyParameter(_hKey, CapiHelper.CryptGetKeyParamQueryType.KP_IV, cipherIv);
		}
		if (effectiveKeyLength != 0)
		{
			CapiHelper.SetKeyParameter(_hKey, CapiHelper.CryptGetKeyParamQueryType.KP_EFFECTIVE_KEYLEN, effectiveKeyLength);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			SafeKeyHandle hKey = _hKey;
			_hKey = null;
			hKey?.Dispose();
			SafeProvHandle hProvider = _hProvider;
			_hProvider = null;
			hProvider?.Dispose();
		}
		base.Dispose(disposing);
	}

	public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
	{
		return Transform(input, output, isFinal: false);
	}

	public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int result = 0;
		if (input.Length != 0)
		{
			result = Transform(input, output, isFinal: true);
		}
		Reset();
		return result;
	}

	private void Reset()
	{
		CapiHelper.EncryptData(_hKey, default(ReadOnlySpan<byte>), default(Span<byte>), isFinal: true);
	}

	private int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool isFinal)
	{
		if (_encrypting)
		{
			return CapiHelper.EncryptData(_hKey, input, output, isFinal);
		}
		return CapiHelper.DecryptData(_hKey, input, output);
	}

	private static SafeKeyHandle ImportCspBlob(SafeProvHandle safeProvHandle, int algId, byte[] rawKey, bool addNoSaltFlag)
	{
		byte[] keyBlob = CapiHelper.ToPlainTextKeyBlob(algId, rawKey);
		CapiHelper.ImportKeyBlob(safeProvHandle, CspProviderFlags.NoFlags, addNoSaltFlag, keyBlob, out var safeKeyHandle);
		return safeKeyHandle;
	}

	private static SafeProvHandle AcquireSafeProviderHandle()
	{
		CspParameters cspParameters = new CspParameters(1);
		CapiHelper.AcquireCsp(cspParameters, out var safeProvHandle);
		return safeProvHandle;
	}
}
