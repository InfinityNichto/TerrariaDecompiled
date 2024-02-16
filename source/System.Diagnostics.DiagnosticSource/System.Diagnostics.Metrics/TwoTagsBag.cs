using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal struct TwoTagsBag
{
	internal KeyValuePair<string, object> Tag1;

	internal KeyValuePair<string, object> Tag2;

	internal TwoTagsBag(KeyValuePair<string, object> tag1, KeyValuePair<string, object> tag2)
	{
		Tag1 = tag1;
		Tag2 = tag2;
	}
}
