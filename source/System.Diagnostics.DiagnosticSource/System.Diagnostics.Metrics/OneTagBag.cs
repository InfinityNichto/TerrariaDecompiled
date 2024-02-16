using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal struct OneTagBag
{
	internal KeyValuePair<string, object> Tag1;

	internal OneTagBag(KeyValuePair<string, object> tag)
	{
		Tag1 = tag;
	}
}
