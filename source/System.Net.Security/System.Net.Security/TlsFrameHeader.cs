using System.Security.Authentication;

namespace System.Net.Security;

internal struct TlsFrameHeader
{
	public TlsContentType Type;

	public SslProtocols Version;

	public int Length;

	public override string ToString()
	{
		return $"{Version}:{Type}[{Length}]";
	}
}
