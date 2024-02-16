using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal static class AeadCommon
{
	public static void CheckArgumentsForNull(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag)
	{
		if (nonce == null)
		{
			throw new ArgumentNullException("nonce");
		}
		if (plaintext == null)
		{
			throw new ArgumentNullException("plaintext");
		}
		if (ciphertext == null)
		{
			throw new ArgumentNullException("ciphertext");
		}
		if (tag == null)
		{
			throw new ArgumentNullException("tag");
		}
	}

	public unsafe static void Encrypt(SafeKeyHandle keyHandle, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, Span<byte> tag)
	{
		fixed (byte* pbInput = &GetNonNullPinnableReference(plaintext))
		{
			fixed (byte* pbNonce = &GetNonNullPinnableReference(nonce))
			{
				fixed (byte* pbOutput = &GetNonNullPinnableReference(ciphertext))
				{
					fixed (byte* pbTag = &GetNonNullPinnableReference(tag))
					{
						fixed (byte* pbAuthData = &GetNonNullPinnableReference(associatedData))
						{
							global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO = global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Create();
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbNonce = pbNonce;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbNonce = nonce.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbTag = pbTag;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbTag = tag.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbAuthData = pbAuthData;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbAuthData = associatedData.Length;
							int cbResult;
							global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptEncrypt(keyHandle, pbInput, plaintext.Length, new IntPtr(&bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO), null, 0, pbOutput, ciphertext.Length, out cbResult, 0);
							if (nTSTATUS != 0)
							{
								throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
							}
						}
					}
				}
			}
		}
	}

	public unsafe static void Decrypt(SafeKeyHandle keyHandle, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, Span<byte> plaintext, bool clearPlaintextOnFailure)
	{
		fixed (byte* pbOutput = &GetNonNullPinnableReference(plaintext))
		{
			fixed (byte* pbNonce = &GetNonNullPinnableReference(nonce))
			{
				fixed (byte* pbInput = &GetNonNullPinnableReference(ciphertext))
				{
					fixed (byte* pbTag = &GetNonNullPinnableReference(tag))
					{
						fixed (byte* pbAuthData = &GetNonNullPinnableReference(associatedData))
						{
							global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO = global::Interop.BCrypt.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Create();
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbNonce = pbNonce;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbNonce = nonce.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbTag = pbTag;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbTag = tag.Length;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.pbAuthData = pbAuthData;
							bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.cbAuthData = associatedData.Length;
							int cbResult;
							global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptDecrypt(keyHandle, pbInput, ciphertext.Length, new IntPtr(&bCRYPT_AUTHENTICATED_CIPHER_MODE_INFO), null, 0, pbOutput, plaintext.Length, out cbResult, 0);
							switch (nTSTATUS)
							{
							case global::Interop.BCrypt.NTSTATUS.STATUS_SUCCESS:
								break;
							case global::Interop.BCrypt.NTSTATUS.STATUS_AUTH_TAG_MISMATCH:
								if (clearPlaintextOnFailure)
								{
									CryptographicOperations.ZeroMemory(plaintext);
								}
								throw new CryptographicException(System.SR.Cryptography_AuthTagMismatch);
							default:
								throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
							}
						}
					}
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static ref readonly byte GetNonNullPinnableReference(ReadOnlySpan<byte> buffer)
	{
		if (buffer.Length == 0)
		{
			return ref Unsafe.AsRef<byte>((void*)1);
		}
		return ref MemoryMarshal.GetReference(buffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static ref byte GetNonNullPinnableReference(Span<byte> buffer)
	{
		if (buffer.Length == 0)
		{
			return ref Unsafe.AsRef<byte>((void*)1);
		}
		return ref MemoryMarshal.GetReference(buffer);
	}
}
