using System.Text;

namespace System.Security.Cryptography.Pkcs;

internal static class Pkcs12Kdf
{
	private static readonly (HashAlgorithmName, int, int)[] s_uvLookup = new(HashAlgorithmName, int, int)[5]
	{
		(HashAlgorithmName.MD5, 128, 512),
		(HashAlgorithmName.SHA1, 160, 512),
		(HashAlgorithmName.SHA256, 256, 512),
		(HashAlgorithmName.SHA384, 384, 1024),
		(HashAlgorithmName.SHA512, 512, 1024)
	};

	internal static void DeriveCipherKey(ReadOnlySpan<char> password, HashAlgorithmName hashAlgorithm, int iterationCount, ReadOnlySpan<byte> salt, Span<byte> destination)
	{
		Derive(password, hashAlgorithm, iterationCount, 1, salt, destination);
	}

	internal static void DeriveIV(ReadOnlySpan<char> password, HashAlgorithmName hashAlgorithm, int iterationCount, ReadOnlySpan<byte> salt, Span<byte> destination)
	{
		Derive(password, hashAlgorithm, iterationCount, 2, salt, destination);
	}

	private static void Derive(ReadOnlySpan<char> password, HashAlgorithmName hashAlgorithm, int iterationCount, byte id, ReadOnlySpan<byte> salt, Span<byte> destination)
	{
		int num = -1;
		int num2 = -1;
		(HashAlgorithmName, int, int)[] array = s_uvLookup;
		for (int i = 0; i < array.Length; i++)
		{
			(HashAlgorithmName, int, int) tuple = array[i];
			if (tuple.Item1 == hashAlgorithm)
			{
				num = tuple.Item2;
				num2 = tuple.Item3;
				break;
			}
		}
		if (num == -1)
		{
			throw new CryptographicException(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
		}
		int num3 = num2 >> 3;
		Span<byte> span = stackalloc byte[num3];
		span.Fill(id);
		int num4 = (salt.Length - 1 + num3) / num3 * num3;
		int num5 = checked((password.Length + 1) * 2);
		if (password == default(ReadOnlySpan<char>))
		{
			num5 = 0;
		}
		int num6 = (num5 - 1 + num3) / num3 * num3;
		int num7 = num4 + num6;
		Span<byte> span2 = default(Span<byte>);
		byte[] array2 = null;
		if (num7 <= 1024)
		{
			span2 = stackalloc byte[num7];
		}
		else
		{
			array2 = System.Security.Cryptography.CryptoPool.Rent(num7);
			span2 = array2.AsSpan(0, num7);
		}
		IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm);
		try
		{
			CircularCopy(salt, span2.Slice(0, num4));
			CircularCopyUtf16BE(password, span2.Slice(num4));
			Span<byte> span3 = stackalloc byte[num >> 3];
			Span<byte> span4 = stackalloc byte[num3];
			while (true)
			{
				incrementalHash.AppendData(span);
				incrementalHash.AppendData(span2);
				for (int num8 = iterationCount; num8 > 0; num8--)
				{
					if (!incrementalHash.TryGetHashAndReset(span3, out var bytesWritten) || bytesWritten != span3.Length)
					{
						throw new CryptographicException();
					}
					if (num8 != 1)
					{
						incrementalHash.AppendData(span3);
					}
				}
				if (span3.Length >= destination.Length)
				{
					break;
				}
				span3.CopyTo(destination);
				destination = destination.Slice(span3.Length);
				CircularCopy(span3, span4);
				for (int num9 = span2.Length / num3 - 1; num9 >= 0; num9--)
				{
					Span<byte> into = span2.Slice(num9 * num3, num3);
					AddPlusOne(into, span4);
				}
			}
			span3.Slice(0, destination.Length).CopyTo(destination);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(span2);
			if (array2 != null)
			{
				System.Security.Cryptography.CryptoPool.Return(array2, 0);
			}
			incrementalHash.Dispose();
		}
	}

	private static void AddPlusOne(Span<byte> into, Span<byte> addend)
	{
		int num = 1;
		for (int num2 = into.Length - 1; num2 >= 0; num2--)
		{
			int num3 = num + into[num2] + addend[num2];
			into[num2] = (byte)num3;
			num = num3 >> 8;
		}
	}

	private static void CircularCopy(ReadOnlySpan<byte> bytes, Span<byte> destination)
	{
		while (destination.Length > 0)
		{
			if (destination.Length >= bytes.Length)
			{
				bytes.CopyTo(destination);
				destination = destination.Slice(bytes.Length);
				continue;
			}
			bytes.Slice(0, destination.Length).CopyTo(destination);
			break;
		}
	}

	private static void CircularCopyUtf16BE(ReadOnlySpan<char> password, Span<byte> destination)
	{
		int num = password.Length * 2;
		Encoding bigEndianUnicode = Encoding.BigEndianUnicode;
		while (destination.Length > 0)
		{
			if (destination.Length >= num)
			{
				int bytes = bigEndianUnicode.GetBytes(password, destination);
				if (bytes != num)
				{
					throw new CryptographicException();
				}
				destination = destination.Slice(bytes);
				Span<byte> span = destination.Slice(0, Math.Min(2, destination.Length));
				span.Clear();
				destination = destination.Slice(span.Length);
				continue;
			}
			ReadOnlySpan<char> chars = password.Slice(0, destination.Length / 2);
			int bytes2 = bigEndianUnicode.GetBytes(chars, destination);
			if (bytes2 != destination.Length)
			{
				throw new CryptographicException();
			}
			break;
		}
	}
}
