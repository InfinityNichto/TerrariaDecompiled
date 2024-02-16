using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
public sealed class AesCcm : IDisposable
{
	private SafeKeyHandle _keyHandle;

	public static KeySizes NonceByteSizes { get; } = new KeySizes(7, 13, 1);


	public static KeySizes TagByteSizes { get; } = new KeySizes(4, 16, 2);


	public static bool IsSupported => true;

	public AesCcm(ReadOnlySpan<byte> key)
	{
		ThrowIfNotSupported();
		AesAEAD.CheckKeySize(key.Length);
		ImportKey(key);
	}

	public AesCcm(byte[] key)
	{
		ThrowIfNotSupported();
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		AesAEAD.CheckKeySize(key.Length);
		ImportKey(key);
	}

	public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null)
	{
		AeadCommon.CheckArgumentsForNull(nonce, plaintext, ciphertext, tag);
		Encrypt((ReadOnlySpan<byte>)nonce, (ReadOnlySpan<byte>)plaintext, (Span<byte>)ciphertext, (Span<byte>)tag, (ReadOnlySpan<byte>)associatedData);
	}

	public void Encrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		CheckParameters(plaintext, ciphertext, nonce, tag);
		EncryptCore(nonce, plaintext, ciphertext, tag, associatedData);
	}

	public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null)
	{
		AeadCommon.CheckArgumentsForNull(nonce, plaintext, ciphertext, tag);
		Decrypt((ReadOnlySpan<byte>)nonce, (ReadOnlySpan<byte>)ciphertext, (ReadOnlySpan<byte>)tag, (Span<byte>)plaintext, (ReadOnlySpan<byte>)associatedData);
	}

	public void Decrypt(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		CheckParameters(plaintext, ciphertext, nonce, tag);
		DecryptCore(nonce, ciphertext, tag, plaintext, associatedData);
	}

	private static void CheckParameters(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> tag)
	{
		if (plaintext.Length != ciphertext.Length)
		{
			throw new ArgumentException(System.SR.Cryptography_PlaintextCiphertextLengthMismatch);
		}
		if (!nonce.Length.IsLegalSize(NonceByteSizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidNonceLength, "nonce");
		}
		if (!tag.Length.IsLegalSize(TagByteSizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidTagLength, "tag");
		}
	}

	private static void ThrowIfNotSupported()
	{
		if (IsSupported)
		{
		}
	}

	[MemberNotNull("_keyHandle")]
	private void ImportKey(ReadOnlySpan<byte> key)
	{
		_keyHandle = global::Interop.BCrypt.BCryptImportKey(BCryptAeadHandleCache.AesCcm, key);
	}

	private void EncryptCore(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		AeadCommon.Encrypt(_keyHandle, nonce, associatedData, plaintext, ciphertext, tag);
	}

	private void DecryptCore(ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext, ReadOnlySpan<byte> associatedData = default(ReadOnlySpan<byte>))
	{
		AeadCommon.Decrypt(_keyHandle, nonce, associatedData, ciphertext, tag, plaintext, clearPlaintextOnFailure: false);
	}

	public void Dispose()
	{
		_keyHandle.Dispose();
	}
}
