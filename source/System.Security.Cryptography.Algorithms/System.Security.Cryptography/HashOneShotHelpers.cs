namespace System.Security.Cryptography;

internal static class HashOneShotHelpers
{
	public static int MacData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
	{
		if (hashAlgorithm == HashAlgorithmName.SHA256)
		{
			return HMACSHA256.HashData(key, source, destination);
		}
		if (hashAlgorithm == HashAlgorithmName.SHA1)
		{
			return HMACSHA1.HashData(key, source, destination);
		}
		if (hashAlgorithm == HashAlgorithmName.SHA512)
		{
			return HMACSHA512.HashData(key, source, destination);
		}
		if (hashAlgorithm == HashAlgorithmName.SHA384)
		{
			return HMACSHA384.HashData(key, source, destination);
		}
		if (hashAlgorithm == HashAlgorithmName.MD5)
		{
			return HMACMD5.HashData(key, source, destination);
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
	}
}
