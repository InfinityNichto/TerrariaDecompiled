using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class PKCS1MaskGenerationMethod : MaskGenerationMethod
{
	private string _hashNameValue;

	public string HashName
	{
		get
		{
			return _hashNameValue;
		}
		set
		{
			_hashNameValue = value ?? "SHA1";
		}
	}

	[RequiresUnreferencedCode("PKCS1MaskGenerationMethod is not trim compatible because the algorithm implementation referenced by HashName might be removed.")]
	public PKCS1MaskGenerationMethod()
	{
		_hashNameValue = "SHA1";
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The constructor of this class is marked as RequiresUnreferencedCode. Don't mark this method as RequiresUnreferencedCode because it is an override and would then need to mark the base method (and all other overrides) as well.")]
	public override byte[] GenerateMask(byte[] rgbSeed, int cbReturn)
	{
		using HashAlgorithm hashAlgorithm = CryptoConfig.CreateFromName(_hashNameValue) as HashAlgorithm;
		if (hashAlgorithm == null)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, _hashNameValue));
		}
		byte[] array = new byte[4];
		byte[] array2 = new byte[cbReturn];
		uint num = 0u;
		for (int i = 0; i < array2.Length; i += hashAlgorithm.Hash.Length)
		{
			BinaryPrimitives.WriteUInt32BigEndian(array, num++);
			hashAlgorithm.TransformBlock(rgbSeed, 0, rgbSeed.Length, rgbSeed, 0);
			hashAlgorithm.TransformFinalBlock(array, 0, 4);
			byte[] hash = hashAlgorithm.Hash;
			hashAlgorithm.Initialize();
			Buffer.BlockCopy(hash, 0, array2, i, Math.Min(array2.Length - i, hash.Length));
		}
		return array2;
	}
}
