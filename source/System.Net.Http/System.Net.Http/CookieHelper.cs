using System.Net.Http.Headers;

namespace System.Net.Http;

internal static class CookieHelper
{
	public static void ProcessReceivedCookies(HttpResponseMessage response, CookieContainer cookieContainer)
	{
		if (!response.Headers.TryGetValues(KnownHeaders.SetCookie.Descriptor, out var values))
		{
			return;
		}
		string[] array = (string[])values;
		Uri requestUri = response.RequestMessage.RequestUri;
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				cookieContainer.SetCookies(requestUri, array[i]);
			}
			catch (CookieException)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(response, $"Invalid Set-Cookie '{array[i]}' ignored.", "ProcessReceivedCookies");
				}
			}
		}
	}
}
