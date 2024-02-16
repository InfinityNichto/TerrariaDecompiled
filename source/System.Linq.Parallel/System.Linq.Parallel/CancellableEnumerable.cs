using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal static class CancellableEnumerable
{
	internal static IEnumerable<TElement> Wrap<TElement>(IEnumerable<TElement> source, CancellationToken token)
	{
		int count = 0;
		foreach (TElement item in source)
		{
			if ((count++ & 0x3F) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			yield return item;
		}
	}
}
