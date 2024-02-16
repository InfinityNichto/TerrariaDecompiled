using System.Reflection;

namespace System.Net;

internal static class CookieCollectionExtensions
{
	private static Func<CookieCollection, Cookie, bool, int> s_internalAddFunc;

	public static int InternalAdd(this CookieCollection cookieCollection, Cookie cookie, bool isStrict)
	{
		if (s_internalAddFunc == null)
		{
			s_internalAddFunc = (Func<CookieCollection, Cookie, bool, int>)typeof(CookieCollection).GetMethod("InternalAdd", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<CookieCollection, Cookie, bool, int>));
		}
		return s_internalAddFunc(cookieCollection, cookie, isStrict);
	}
}
