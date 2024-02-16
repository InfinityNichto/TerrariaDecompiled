using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public static class RSACertificateExtensions
{
	public static RSA? GetRSAPublicKey(this X509Certificate2 certificate)
	{
		return certificate.GetPublicKey<RSA>();
	}

	public static RSA? GetRSAPrivateKey(this X509Certificate2 certificate)
	{
		return certificate.GetPrivateKey<RSA>();
	}

	public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, RSA privateKey)
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		if (privateKey == null)
		{
			throw new ArgumentNullException("privateKey");
		}
		if (certificate.HasPrivateKey)
		{
			throw new InvalidOperationException(System.SR.Cryptography_Cert_AlreadyHasPrivateKey);
		}
		using (RSA rSA = certificate.GetRSAPublicKey())
		{
			if (rSA == null)
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_WrongAlgorithm);
			}
			RSAParameters rSAParameters = rSA.ExportParameters(includePrivateParameters: false);
			RSAParameters rSAParameters2 = privateKey.ExportParameters(includePrivateParameters: false);
			if (!rSAParameters.Modulus.ContentsEqual(rSAParameters2.Modulus) || !rSAParameters.Exponent.ContentsEqual(rSAParameters2.Exponent))
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_DoesNotMatch, "privateKey");
			}
		}
		ICertificatePal pal = certificate.Pal.CopyWithPrivateKey(privateKey);
		return new X509Certificate2(pal);
	}
}
