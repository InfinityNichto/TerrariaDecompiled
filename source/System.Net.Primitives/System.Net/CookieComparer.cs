namespace System.Net;

internal static class CookieComparer
{
	internal static int Compare(Cookie left, Cookie right)
	{
		int result;
		if ((result = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)) != 0)
		{
			return result;
		}
		if ((result = string.Compare(left.Domain, right.Domain, StringComparison.OrdinalIgnoreCase)) != 0)
		{
			return result;
		}
		return string.Compare(left.Path, right.Path, StringComparison.Ordinal);
	}
}
