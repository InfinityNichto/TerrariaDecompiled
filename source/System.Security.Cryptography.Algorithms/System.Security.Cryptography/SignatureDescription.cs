using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class SignatureDescription
{
	public string? KeyAlgorithm { get; set; }

	public string? DigestAlgorithm { get; set; }

	public string? FormatterAlgorithm { get; set; }

	public string? DeformatterAlgorithm { get; set; }

	public SignatureDescription()
	{
	}

	public SignatureDescription(SecurityElement el)
	{
		if (el == null)
		{
			throw new ArgumentNullException("el");
		}
		KeyAlgorithm = el.SearchForTextOfTag("Key");
		DigestAlgorithm = el.SearchForTextOfTag("Digest");
		FormatterAlgorithm = el.SearchForTextOfTag("Formatter");
		DeformatterAlgorithm = el.SearchForTextOfTag("Deformatter");
	}

	[RequiresUnreferencedCode("CreateDeformatter is not trim compatible because the algorithm implementation referenced by DeformatterAlgorithm might be removed.")]
	public virtual AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
	{
		AsymmetricSignatureDeformatter asymmetricSignatureDeformatter = (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(DeformatterAlgorithm);
		asymmetricSignatureDeformatter.SetKey(key);
		return asymmetricSignatureDeformatter;
	}

	[RequiresUnreferencedCode("CreateFormatter is not trim compatible because the algorithm implementation referenced by FormatterAlgorithm might be removed.")]
	public virtual AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
	{
		AsymmetricSignatureFormatter asymmetricSignatureFormatter = (AsymmetricSignatureFormatter)CryptoConfig.CreateFromName(FormatterAlgorithm);
		asymmetricSignatureFormatter.SetKey(key);
		return asymmetricSignatureFormatter;
	}

	[RequiresUnreferencedCode("CreateDigest is not trim compatible because the algorithm implementation referenced by DigestAlgorithm might be removed.")]
	public virtual HashAlgorithm? CreateDigest()
	{
		return (HashAlgorithm)CryptoConfig.CreateFromName(DigestAlgorithm);
	}
}
