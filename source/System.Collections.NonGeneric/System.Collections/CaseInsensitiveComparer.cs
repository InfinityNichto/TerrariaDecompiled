using System.Globalization;

namespace System.Collections;

public class CaseInsensitiveComparer : IComparer
{
	private readonly CompareInfo _compareInfo;

	private static volatile CaseInsensitiveComparer s_InvariantCaseInsensitiveComparer;

	public static CaseInsensitiveComparer Default => new CaseInsensitiveComparer(CultureInfo.CurrentCulture);

	public static CaseInsensitiveComparer DefaultInvariant
	{
		get
		{
			if (s_InvariantCaseInsensitiveComparer == null)
			{
				s_InvariantCaseInsensitiveComparer = new CaseInsensitiveComparer(CultureInfo.InvariantCulture);
			}
			return s_InvariantCaseInsensitiveComparer;
		}
	}

	public CaseInsensitiveComparer()
	{
		_compareInfo = CultureInfo.CurrentCulture.CompareInfo;
	}

	public CaseInsensitiveComparer(CultureInfo culture)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		_compareInfo = culture.CompareInfo;
	}

	public int Compare(object? a, object? b)
	{
		string text = a as string;
		string text2 = b as string;
		if (text != null && text2 != null)
		{
			return _compareInfo.Compare(text, text2, CompareOptions.IgnoreCase);
		}
		return Comparer.Default.Compare(a, b);
	}
}
