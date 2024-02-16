using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal struct ThreeTagsBag
{
	internal KeyValuePair<string, object> Tag1;

	internal KeyValuePair<string, object> Tag2;

	internal KeyValuePair<string, object> Tag3;

	internal ThreeTagsBag(KeyValuePair<string, object> tag1, KeyValuePair<string, object> tag2, KeyValuePair<string, object> tag3)
	{
		Tag1 = tag1;
		Tag2 = tag2;
		Tag3 = tag3;
	}
}
