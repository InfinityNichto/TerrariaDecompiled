using System.Reflection;

namespace System.Net;

internal static class CookieExtensions
{
	private enum CookieVariant
	{
		Unknown = 0,
		Plain = 1,
		Rfc2109 = 2,
		Rfc2965 = 3,
		Default = 2
	}

	private static Func<Cookie, string> s_toServerStringFunc;

	private static Func<Cookie, Cookie> s_cloneFunc;

	private static Func<Cookie, CookieVariant> s_getVariantFunc;

	public static string ToServerString(this Cookie cookie)
	{
		if (s_toServerStringFunc == null)
		{
			s_toServerStringFunc = (Func<Cookie, string>)typeof(Cookie).GetMethod("ToServerString", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<Cookie, string>));
		}
		return s_toServerStringFunc(cookie);
	}

	public static Cookie Clone(this Cookie cookie)
	{
		if (s_cloneFunc == null)
		{
			s_cloneFunc = (Func<Cookie, Cookie>)typeof(Cookie).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<Cookie, Cookie>));
		}
		return s_cloneFunc(cookie);
	}

	public static bool IsRfc2965Variant(this Cookie cookie)
	{
		if (s_getVariantFunc == null)
		{
			s_getVariantFunc = (Func<Cookie, CookieVariant>)typeof(Cookie).GetProperty("Variant", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(nonPublic: true).CreateDelegate(typeof(Func<Cookie, CookieVariant>));
		}
		return s_getVariantFunc(cookie) == CookieVariant.Rfc2965;
	}
}
