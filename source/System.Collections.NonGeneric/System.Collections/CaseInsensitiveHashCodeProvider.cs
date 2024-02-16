using System.Globalization;

namespace System.Collections;

[Obsolete("CaseInsensitiveHashCodeProvider has been deprecated. Use StringComparer instead.")]
public class CaseInsensitiveHashCodeProvider : IHashCodeProvider
{
	private static volatile CaseInsensitiveHashCodeProvider s_invariantCaseInsensitiveHashCodeProvider;

	private readonly CompareInfo _compareInfo;

	public static CaseInsensitiveHashCodeProvider Default => new CaseInsensitiveHashCodeProvider();

	public static CaseInsensitiveHashCodeProvider DefaultInvariant => s_invariantCaseInsensitiveHashCodeProvider ?? (s_invariantCaseInsensitiveHashCodeProvider = new CaseInsensitiveHashCodeProvider(CultureInfo.InvariantCulture));

	public CaseInsensitiveHashCodeProvider()
	{
		_compareInfo = CultureInfo.CurrentCulture.CompareInfo;
	}

	public CaseInsensitiveHashCodeProvider(CultureInfo culture)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		_compareInfo = culture.CompareInfo;
	}

	public int GetHashCode(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (!(obj is string source))
		{
			return obj.GetHashCode();
		}
		return _compareInfo.GetHashCode(source, CompareOptions.IgnoreCase);
	}
}
