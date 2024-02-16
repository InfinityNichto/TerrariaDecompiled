using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal sealed class BasicSymmetricCipherNCrypt : Internal.Cryptography.BasicSymmetricCipher
{
	private CngKey _cngKey;

	private readonly bool _encrypting;

	private static readonly CngProperty s_ECBMode = new CngProperty("Chaining Mode", Encoding.Unicode.GetBytes("ChainingModeECB\0"), CngPropertyOptions.None);

	private static readonly CngProperty s_CBCMode = new CngProperty("Chaining Mode", Encoding.Unicode.GetBytes("ChainingModeCBC\0"), CngPropertyOptions.None);

	private static readonly CngProperty s_CFBMode = new CngProperty("Chaining Mode", Encoding.Unicode.GetBytes("ChainingModeCFB\0"), CngPropertyOptions.None);

	public BasicSymmetricCipherNCrypt(Func<CngKey> cngKeyFactory, CipherMode cipherMode, int blockSizeInBytes, byte[] iv, bool encrypting, int paddingSize)
		: base(iv, blockSizeInBytes, paddingSize)
	{
		_encrypting = encrypting;
		_cngKey = cngKeyFactory();
		CngProperty property = cipherMode switch
		{
			CipherMode.ECB => s_ECBMode, 
			CipherMode.CBC => s_CBCMode, 
			CipherMode.CFB => s_CFBMode, 
			_ => throw new CryptographicException(System.SR.Cryptography_InvalidCipherMode), 
		};
		_cngKey.SetProperty(property);
		Reset();
	}

	public unsafe sealed override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
	{
		global::Interop.NCrypt.ErrorCode errorCode;
		int pcbResult;
		using (SafeNCryptKeyHandle hKey = _cngKey.Handle)
		{
			errorCode = (_encrypting ? global::Interop.NCrypt.NCryptEncrypt(hKey, input, input.Length, null, output, output.Length, out pcbResult, global::Interop.NCrypt.AsymmetricPaddingMode.None) : global::Interop.NCrypt.NCryptDecrypt(hKey, input, input.Length, null, output, output.Length, out pcbResult, global::Interop.NCrypt.AsymmetricPaddingMode.None));
		}
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		if (pcbResult != input.Length)
		{
			throw new CryptographicException(System.SR.Cryptography_UnexpectedTransformTruncation);
		}
		return pcbResult;
	}

	public sealed override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int result = 0;
		if (input.Length != 0)
		{
			result = Transform(input, output);
		}
		Reset();
		return result;
	}

	protected sealed override void Dispose(bool disposing)
	{
		if (disposing && _cngKey != null)
		{
			_cngKey.Dispose();
			_cngKey = null;
		}
		base.Dispose(disposing);
	}

	private void Reset()
	{
		if (base.IV != null)
		{
			CngProperty property = new CngProperty("IV", base.IV, CngPropertyOptions.None);
			_cngKey.SetProperty(property);
		}
	}
}
