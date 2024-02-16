using System;
using System.Diagnostics.CodeAnalysis;

namespace Internal.Cryptography;

internal sealed class HMACCommon
{
	private readonly string _hashAlgorithmId;

	private HashProvider _hMacProvider;

	private volatile HashProvider _lazyHashProvider;

	private readonly int _blockSize;

	public int HashSizeInBits => _hMacProvider.HashSizeInBytes * 8;

	public int HashSizeInBytes => _hMacProvider.HashSizeInBytes;

	public byte[] ActualKey { get; private set; }

	public HMACCommon(string hashAlgorithmId, byte[] key, int blockSize)
		: this(hashAlgorithmId, (ReadOnlySpan<byte>)key, blockSize)
	{
		if (ActualKey == null)
		{
			byte[] array2 = (ActualKey = key);
		}
	}

	internal HMACCommon(string hashAlgorithmId, ReadOnlySpan<byte> key, int blockSize)
	{
		_hashAlgorithmId = hashAlgorithmId;
		_blockSize = blockSize;
		ActualKey = ChangeKeyImpl(key);
	}

	public void ChangeKey(byte[] key)
	{
		ActualKey = ChangeKeyImpl(key) ?? key;
	}

	[MemberNotNull("_hMacProvider")]
	private byte[] ChangeKeyImpl(ReadOnlySpan<byte> key)
	{
		byte[] result = null;
		if (key.Length > _blockSize && _blockSize > 0)
		{
			if (_lazyHashProvider == null)
			{
				_lazyHashProvider = HashProviderDispenser.CreateHashProvider(_hashAlgorithmId);
			}
			_lazyHashProvider.AppendHashData(key);
			result = _lazyHashProvider.FinalizeHashAndReset();
		}
		HashProvider hMacProvider = _hMacProvider;
		_hMacProvider = null;
		hMacProvider?.Dispose(disposing: true);
		_hMacProvider = HashProviderDispenser.CreateMacProvider(_hashAlgorithmId, key);
		return result;
	}

	public void AppendHashData(byte[] data, int offset, int count)
	{
		_hMacProvider.AppendHashData(data, offset, count);
	}

	public void AppendHashData(ReadOnlySpan<byte> source)
	{
		_hMacProvider.AppendHashData(source);
	}

	public byte[] FinalizeHashAndReset()
	{
		return _hMacProvider.FinalizeHashAndReset();
	}

	public int FinalizeHashAndReset(Span<byte> destination)
	{
		return _hMacProvider.FinalizeHashAndReset(destination);
	}

	public bool TryFinalizeHashAndReset(Span<byte> destination, out int bytesWritten)
	{
		return _hMacProvider.TryFinalizeHashAndReset(destination, out bytesWritten);
	}

	public int GetCurrentHash(Span<byte> destination)
	{
		return _hMacProvider.GetCurrentHash(destination);
	}

	public void Reset()
	{
		_hMacProvider.Reset();
	}

	public void Dispose(bool disposing)
	{
		if (disposing)
		{
			_hMacProvider?.Dispose(disposing: true);
			_hMacProvider = null;
			_lazyHashProvider?.Dispose(disposing: true);
			_lazyHashProvider = null;
		}
	}
}
