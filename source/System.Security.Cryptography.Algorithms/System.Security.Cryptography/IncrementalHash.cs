using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public sealed class IncrementalHash : IDisposable
{
	private readonly HashAlgorithmName _algorithmName;

	private HashProvider _hash;

	private HMACCommon _hmac;

	private bool _disposed;

	public int HashLengthInBytes { get; }

	public HashAlgorithmName AlgorithmName => _algorithmName;

	private IncrementalHash(HashAlgorithmName name, HashProvider hash)
	{
		_algorithmName = name;
		_hash = hash;
		HashLengthInBytes = _hash.HashSizeInBytes;
	}

	private IncrementalHash(HashAlgorithmName name, HMACCommon hmac)
	{
		_algorithmName = new HashAlgorithmName("HMAC" + name.Name);
		_hmac = hmac;
		HashLengthInBytes = _hmac.HashSizeInBytes;
	}

	public void AppendData(byte[] data)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		AppendData(new ReadOnlySpan<byte>(data));
	}

	public void AppendData(byte[] data, int offset, int count)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0 || count > data.Length)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (data.Length - count < offset)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		AppendData(new ReadOnlySpan<byte>(data, offset, count));
	}

	public void AppendData(ReadOnlySpan<byte> data)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		if (_hash != null)
		{
			_hash.AppendHashData(data);
		}
		else
		{
			_hmac.AppendHashData(data);
		}
	}

	public byte[] GetHashAndReset()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		byte[] array = new byte[HashLengthInBytes];
		int hashAndResetCore = GetHashAndResetCore(array);
		return array;
	}

	public int GetHashAndReset(Span<byte> destination)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		if (destination.Length < HashLengthInBytes)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return GetHashAndResetCore(destination);
	}

	public bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		if (destination.Length < HashLengthInBytes)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = GetHashAndResetCore(destination);
		return true;
	}

	private int GetHashAndResetCore(Span<byte> destination)
	{
		if (_hash == null)
		{
			return _hmac.FinalizeHashAndReset(destination);
		}
		return _hash.FinalizeHashAndReset(destination);
	}

	public byte[] GetCurrentHash()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		byte[] array = new byte[HashLengthInBytes];
		int currentHashCore = GetCurrentHashCore(array);
		return array;
	}

	public int GetCurrentHash(Span<byte> destination)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		if (destination.Length < HashLengthInBytes)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "destination");
		}
		return GetCurrentHashCore(destination);
	}

	public bool TryGetCurrentHash(Span<byte> destination, out int bytesWritten)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("IncrementalHash");
		}
		if (destination.Length < HashLengthInBytes)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = GetCurrentHashCore(destination);
		return true;
	}

	private int GetCurrentHashCore(Span<byte> destination)
	{
		if (_hash == null)
		{
			return _hmac.GetCurrentHash(destination);
		}
		return _hash.GetCurrentHash(destination);
	}

	public void Dispose()
	{
		_disposed = true;
		if (_hash != null)
		{
			_hash.Dispose();
			_hash = null;
		}
		if (_hmac != null)
		{
			_hmac.Dispose(disposing: true);
			_hmac = null;
		}
	}

	public static IncrementalHash CreateHash(HashAlgorithmName hashAlgorithm)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		return new IncrementalHash(hashAlgorithm, HashProviderDispenser.CreateHashProvider(hashAlgorithm.Name));
	}

	[UnsupportedOSPlatform("browser")]
	public static IncrementalHash CreateHMAC(HashAlgorithmName hashAlgorithm, byte[] key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return CreateHMAC(hashAlgorithm, (ReadOnlySpan<byte>)key);
	}

	[UnsupportedOSPlatform("browser")]
	public static IncrementalHash CreateHMAC(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> key)
	{
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new ArgumentException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
		}
		return new IncrementalHash(hashAlgorithm, new HMACCommon(hashAlgorithm.Name, key, -1));
	}
}
