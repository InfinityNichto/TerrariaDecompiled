namespace System.Security.Cryptography.X509Certificates;

public class X509ChainElement
{
	public X509Certificate2 Certificate { get; private set; }

	public X509ChainStatus[] ChainElementStatus { get; private set; }

	public string Information { get; private set; }

	internal X509ChainElement(X509Certificate2 certificate, X509ChainStatus[] chainElementStatus, string information)
	{
		Certificate = certificate;
		ChainElementStatus = chainElementStatus;
		Information = information;
	}
}
