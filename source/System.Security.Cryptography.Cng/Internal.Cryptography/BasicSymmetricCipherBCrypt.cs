using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal sealed class BasicSymmetricCipherBCrypt : Internal.Cryptography.BasicSymmetricCipher
{
	private readonly bool _encrypting;

	private Internal.NativeCrypto.SafeKeyHandle _hKey;

	private byte[] _currentIv;

	public BasicSymmetricCipherBCrypt(Internal.NativeCrypto.SafeAlgorithmHandle algorithm, CipherMode cipherMode, int blockSizeInBytes, int paddingSizeInBytes, byte[] key, bool ownsParentHandle, byte[] iv, bool encrypting)
		: base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
	{
		_encrypting = encrypting;
		if (base.IV != null)
		{
			_currentIv = new byte[base.IV.Length];
		}
		_hKey = global::Interop.BCrypt.BCryptImportKey(algorithm, key);
		if (ownsParentHandle)
		{
			_hKey.SetParentHandle(algorithm);
		}
		Reset();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Internal.NativeCrypto.SafeKeyHandle hKey = _hKey;
			_hKey = null;
			hKey?.Dispose();
			byte[] currentIv = _currentIv;
			_currentIv = null;
			if (currentIv != null)
			{
				Array.Clear(currentIv);
			}
		}
		base.Dispose(disposing);
	}

	public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int num = 0;
		if (input.Overlaps(output, out var elementOffset) && elementOffset != 0)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(output.Length);
			try
			{
				num = BCryptTransform(input, array);
				array.AsSpan(0, num).CopyTo(output);
			}
			finally
			{
				System.Security.Cryptography.CryptoPool.Return(array, num);
			}
		}
		else
		{
			num = BCryptTransform(input, output);
		}
		if (num != input.Length)
		{
			throw new CryptographicException(System.SR.Cryptography_UnexpectedTransformTruncation);
		}
		return num;
		int BCryptTransform(ReadOnlySpan<byte> input, Span<byte> output)
		{
			if (!_encrypting)
			{
				return global::Interop.BCrypt.BCryptDecrypt(_hKey, input, _currentIv, output);
			}
			return global::Interop.BCrypt.BCryptEncrypt(_hKey, input, _currentIv, output);
		}
	}

	public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int result = 0;
		if (input.Length != 0)
		{
			result = Transform(input, output);
		}
		Reset();
		return result;
	}

	private void Reset()
	{
		if (base.IV != null)
		{
			Buffer.BlockCopy(base.IV, 0, _currentIv, 0, base.IV.Length);
		}
	}
}
