namespace System.Security.Cryptography;

public sealed class PbeParameters
{
	public PbeEncryptionAlgorithm EncryptionAlgorithm { get; }

	public HashAlgorithmName HashAlgorithm { get; }

	public int IterationCount { get; }

	public PbeParameters(PbeEncryptionAlgorithm encryptionAlgorithm, HashAlgorithmName hashAlgorithm, int iterationCount)
	{
		if (iterationCount < 1)
		{
			throw new ArgumentOutOfRangeException("iterationCount", iterationCount, System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		EncryptionAlgorithm = encryptionAlgorithm;
		HashAlgorithm = hashAlgorithm;
		IterationCount = iterationCount;
	}
}
