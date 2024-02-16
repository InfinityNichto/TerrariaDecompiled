using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace System.Security.Cryptography;

internal sealed class RsaPaddingProcessor
{
	private static readonly byte[] s_digestInfoMD5 = new byte[18]
	{
		48, 32, 48, 12, 6, 8, 42, 134, 72, 134,
		247, 13, 2, 5, 5, 0, 4, 16
	};

	private static readonly byte[] s_digestInfoSha1 = new byte[15]
	{
		48, 33, 48, 9, 6, 5, 43, 14, 3, 2,
		26, 5, 0, 4, 20
	};

	private static readonly byte[] s_digestInfoSha256 = new byte[19]
	{
		48, 49, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 1, 5, 0, 4, 32
	};

	private static readonly byte[] s_digestInfoSha384 = new byte[19]
	{
		48, 65, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 2, 5, 0, 4, 48
	};

	private static readonly byte[] s_digestInfoSha512 = new byte[19]
	{
		48, 81, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 3, 5, 0, 4, 64
	};

	private static readonly ConcurrentDictionary<HashAlgorithmName, System.Security.Cryptography.RsaPaddingProcessor> s_lookup = new ConcurrentDictionary<HashAlgorithmName, System.Security.Cryptography.RsaPaddingProcessor>();

	private readonly HashAlgorithmName _hashAlgorithmName;

	private readonly int _hLen;

	private readonly ReadOnlyMemory<byte> _digestInfoPrefix;

	private RsaPaddingProcessor(HashAlgorithmName hashAlgorithmName, int hLen, ReadOnlyMemory<byte> digestInfoPrefix)
	{
		_hashAlgorithmName = hashAlgorithmName;
		_hLen = hLen;
		_digestInfoPrefix = digestInfoPrefix;
	}

	internal static int BytesRequiredForBitCount(int keySizeInBits)
	{
		return (int)((uint)(keySizeInBits + 7) / 8u);
	}

	internal static System.Security.Cryptography.RsaPaddingProcessor OpenProcessor(HashAlgorithmName hashAlgorithmName)
	{
		return s_lookup.GetOrAdd(hashAlgorithmName, delegate(HashAlgorithmName hashAlgorithmName)
		{
			using IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithmName);
			Span<byte> destination = stackalloc byte[64];
			ReadOnlyMemory<byte> digestInfoPrefix;
			if (hashAlgorithmName == HashAlgorithmName.MD5)
			{
				digestInfoPrefix = s_digestInfoMD5;
			}
			else if (hashAlgorithmName == HashAlgorithmName.SHA1)
			{
				digestInfoPrefix = s_digestInfoSha1;
			}
			else if (hashAlgorithmName == HashAlgorithmName.SHA256)
			{
				digestInfoPrefix = s_digestInfoSha256;
			}
			else if (hashAlgorithmName == HashAlgorithmName.SHA384)
			{
				digestInfoPrefix = s_digestInfoSha384;
			}
			else
			{
				if (!(hashAlgorithmName == HashAlgorithmName.SHA512))
				{
					throw new CryptographicException();
				}
				digestInfoPrefix = s_digestInfoSha512;
			}
			if (incrementalHash.TryGetHashAndReset(destination, out var bytesWritten))
			{
				return new System.Security.Cryptography.RsaPaddingProcessor(hashAlgorithmName, bytesWritten, digestInfoPrefix);
			}
			byte[] hashAndReset = incrementalHash.GetHashAndReset();
			return new System.Security.Cryptography.RsaPaddingProcessor(hashAlgorithmName, hashAndReset.Length, digestInfoPrefix);
		});
	}

	internal static void PadPkcs1Encryption(ReadOnlySpan<byte> source, Span<byte> destination)
	{
		int length = source.Length;
		int length2 = destination.Length;
		if (length > length2 - 11)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyTooSmall);
		}
		Span<byte> destination2 = destination.Slice(destination.Length - source.Length);
		Span<byte> data = destination.Slice(2, destination.Length - source.Length - 3);
		destination[0] = 0;
		destination[1] = 2;
		destination[data.Length + 2] = 0;
		FillNonZeroBytes(data);
		source.CopyTo(destination2);
	}

	internal void PadOaep(ReadOnlySpan<byte> source, Span<byte> destination)
	{
		byte[] array = null;
		Span<byte> span = Span<byte>.Empty;
		try
		{
			int num = checked(destination.Length - _hLen - _hLen - 2);
			if (source.Length > num)
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Encryption_MessageTooLong, num));
			}
			Span<byte> span2 = destination.Slice(1, _hLen);
			Span<byte> span3 = destination.Slice(1 + _hLen);
			using IncrementalHash incrementalHash = IncrementalHash.CreateHash(_hashAlgorithmName);
			Span<byte> destination2 = span3.Slice(0, _hLen);
			Span<byte> destination3 = span3.Slice(span3.Length - source.Length);
			Span<byte> span4 = span3.Slice(_hLen, span3.Length - _hLen - 1 - destination3.Length);
			Span<byte> span5 = span3.Slice(_hLen + span4.Length, 1);
			if (!incrementalHash.TryGetHashAndReset(destination2, out var bytesWritten) || bytesWritten != _hLen)
			{
				throw new CryptographicException();
			}
			span4.Clear();
			span5[0] = 1;
			source.CopyTo(destination3);
			RandomNumberGenerator.Fill(span2);
			array = System.Security.Cryptography.CryptoPool.Rent(span3.Length);
			span = new Span<byte>(array, 0, span3.Length);
			Mgf1(incrementalHash, span2, span);
			Xor(span3, span);
			Span<byte> span6 = stackalloc byte[_hLen];
			Mgf1(incrementalHash, span3, span6);
			Xor(span2, span6);
			destination[0] = 0;
		}
		catch (Exception ex) when (!(ex is CryptographicException))
		{
			throw new CryptographicException();
		}
		finally
		{
			if (array != null)
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
	}

	private void Mgf1(IncrementalHash hasher, ReadOnlySpan<byte> mgfSeed, Span<byte> mask)
	{
		Span<byte> destination = mask;
		int num = 0;
		Span<byte> span = stackalloc byte[4];
		while (destination.Length > 0)
		{
			hasher.AppendData(mgfSeed);
			BinaryPrimitives.WriteInt32BigEndian(span, num);
			hasher.AppendData(span);
			if (destination.Length >= _hLen)
			{
				if (!hasher.TryGetHashAndReset(destination, out var bytesWritten))
				{
					throw new CryptographicException();
				}
				destination = destination.Slice(bytesWritten);
				num++;
				continue;
			}
			Span<byte> destination2 = stackalloc byte[_hLen];
			if (!hasher.TryGetHashAndReset(destination2, out var _))
			{
				throw new CryptographicException();
			}
			destination2.Slice(0, destination.Length).CopyTo(destination);
			break;
		}
	}

	private static void FillNonZeroBytes(Span<byte> data)
	{
		while (data.Length > 0)
		{
			RandomNumberGenerator.Fill(data);
			int num = data.Length;
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] == 0)
				{
					num = i;
					break;
				}
			}
			for (int j = num + 1; j < data.Length; j++)
			{
				if (data[j] != 0)
				{
					data[num++] = data[j];
				}
			}
			data = data.Slice(num);
		}
	}

	private static void Xor(Span<byte> a, ReadOnlySpan<byte> b)
	{
		if (a.Length != b.Length)
		{
			throw new InvalidOperationException();
		}
		for (int i = 0; i < b.Length; i++)
		{
			a[i] ^= b[i];
		}
	}
}
