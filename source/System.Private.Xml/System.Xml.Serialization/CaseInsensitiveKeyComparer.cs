using System.Collections;
using System.Globalization;

namespace System.Xml.Serialization;

internal sealed class CaseInsensitiveKeyComparer : CaseInsensitiveComparer, IEqualityComparer
{
	public CaseInsensitiveKeyComparer()
		: base(CultureInfo.CurrentCulture)
	{
	}

	bool IEqualityComparer.Equals(object x, object y)
	{
		return Compare(x, y) == 0;
	}

	int IEqualityComparer.GetHashCode(object obj)
	{
		if (!(obj is string str))
		{
			throw new ArgumentException(null, "obj");
		}
		return CultureInfo.CurrentCulture.TextInfo.ToUpper(str).GetHashCode();
	}
}
