namespace System.Net;

internal sealed class FileWebRequestCreator : IWebRequestCreate
{
	public WebRequest Create(Uri uri)
	{
		return new FileWebRequest(uri);
	}
}
