using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public abstract class MaskGenerationMethod
{
	public abstract byte[] GenerateMask(byte[] rgbSeed, int cbReturn);
}
