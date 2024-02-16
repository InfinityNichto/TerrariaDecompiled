using System;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal sealed class HashProviderCng : HashProvider
{
	private readonly SafeBCryptAlgorithmHandle _hAlgorithm;

	private SafeBCryptHashHandle _hHash;

	private byte[] _key;

	private readonly bool _reusable;

	private readonly int _hashSize;

	private bool _running;

	public sealed override int HashSizeInBytes => _hashSize;

	public HashProviderCng(string hashAlgId, byte[] key)
		: this(hashAlgId, key, key != null)
	{
	}

	internal HashProviderCng(string hashAlgId, ReadOnlySpan<byte> key, bool isHmac)
	{
		global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags bCryptOpenAlgorithmProviderFlags = global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None;
		if (isHmac)
		{
			_key = key.ToArray();
			bCryptOpenAlgorithmProviderFlags |= global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;
		}
		_hAlgorithm = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgId, bCryptOpenAlgorithmProviderFlags, out _hashSize);
		SafeBCryptHashHandle phHash;
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptCreateHash(_hAlgorithm, out phHash, IntPtr.Zero, 0, key, (!(key == null)) ? key.Length : 0, global::Interop.BCrypt.BCryptCreateHashFlags.BCRYPT_HASH_REUSABLE_FLAG);
		switch (nTSTATUS)
		{
		case global::Interop.BCrypt.NTSTATUS.STATUS_INVALID_PARAMETER:
			phHash.Dispose();
			Reset();
			break;
		default:
			phHash.Dispose();
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		case global::Interop.BCrypt.NTSTATUS.STATUS_SUCCESS:
			_hHash = phHash;
			_reusable = true;
			break;
		}
	}

	public sealed override void AppendHashData(ReadOnlySpan<byte> source)
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptHashData(_hHash, source, source.Length, 0);
		if (nTSTATUS != 0)
		{
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		_running = true;
	}

	public override int FinalizeHashAndReset(Span<byte> destination)
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptFinishHash(_hHash, destination, _hashSize, 0);
		if (nTSTATUS != 0)
		{
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		_running = false;
		Reset();
		return _hashSize;
	}

	public override int GetCurrentHash(Span<byte> destination)
	{
		using SafeBCryptHashHandle hHash = global::Interop.BCrypt.BCryptDuplicateHash(_hHash);
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptFinishHash(hHash, destination, _hashSize, 0);
		if (nTSTATUS != 0)
		{
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		return _hashSize;
	}

	public sealed override void Dispose(bool disposing)
	{
		if (disposing)
		{
			DestroyHash();
			if (_key != null)
			{
				byte[] key = _key;
				_key = null;
				Array.Clear(key);
			}
		}
	}

	public override void Reset()
	{
		if (!_reusable || _running)
		{
			DestroyHash();
			SafeBCryptHashHandle phHash;
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptCreateHash(_hAlgorithm, out phHash, IntPtr.Zero, 0, _key, (_key != null) ? _key.Length : 0, global::Interop.BCrypt.BCryptCreateHashFlags.None);
			if (nTSTATUS != 0)
			{
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
			_hHash = phHash;
		}
	}

	private void DestroyHash()
	{
		SafeBCryptHashHandle hHash = _hHash;
		_hHash = null;
		hHash?.Dispose();
	}
}
