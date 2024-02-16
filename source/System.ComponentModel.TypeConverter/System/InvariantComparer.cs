using System.Collections;
using System.Globalization;

namespace System;

internal sealed class InvariantComparer : IComparer
{
	private readonly CompareInfo _compareInfo;

	internal static readonly InvariantComparer Default = new InvariantComparer();

	internal InvariantComparer()
	{
		_compareInfo = CultureInfo.InvariantCulture.CompareInfo;
	}

	public int Compare(object a, object b)
	{
		if (a is string @string && b is string string2)
		{
			return _compareInfo.Compare(@string, string2);
		}
		return Comparer.Default.Compare(a, b);
	}
}
