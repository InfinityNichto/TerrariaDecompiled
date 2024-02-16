namespace System.Net;

internal sealed class FtpWebRequestCreator : IWebRequestCreate
{
	internal FtpWebRequestCreator()
	{
	}

	public WebRequest Create(Uri uri)
	{
		return new FtpWebRequest(uri);
	}
}
