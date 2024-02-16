using System.Diagnostics.CodeAnalysis;

namespace System.Web.Util;

internal static class UriUtil
{
	private static readonly char[] s_queryFragmentSeparators = new char[2] { '?', '#' };

	private static void ExtractQueryAndFragment(string input, out string path, out string queryAndFragment)
	{
		int num = input.IndexOfAny(s_queryFragmentSeparators);
		if (num != -1)
		{
			path = input.Substring(0, num);
			queryAndFragment = input.Substring(num);
		}
		else
		{
			path = input;
			queryAndFragment = null;
		}
	}

	internal static bool TrySplitUriForPathEncode(string input, [NotNullWhen(true)] out string schemeAndAuthority, [NotNullWhen(true)] out string path, out string queryAndFragment)
	{
		ExtractQueryAndFragment(input, out var path2, out queryAndFragment);
		if (Uri.TryCreate(path2, UriKind.Absolute, out Uri result))
		{
			string authority = result.Authority;
			if (!string.IsNullOrEmpty(authority))
			{
				int num = path2.IndexOf(authority, StringComparison.OrdinalIgnoreCase);
				if (num != -1)
				{
					int num2 = num + authority.Length;
					schemeAndAuthority = path2.Substring(0, num2);
					path = path2.Substring(num2);
					return true;
				}
			}
		}
		schemeAndAuthority = null;
		path = null;
		queryAndFragment = null;
		return false;
	}
}
