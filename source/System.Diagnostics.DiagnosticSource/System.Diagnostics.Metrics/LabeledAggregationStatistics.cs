using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal sealed class LabeledAggregationStatistics
{
	public KeyValuePair<string, string>[] Labels { get; }

	public IAggregationStatistics AggregationStatistics { get; }

	public LabeledAggregationStatistics(IAggregationStatistics stats, params KeyValuePair<string, string>[] labels)
	{
		AggregationStatistics = stats;
		Labels = labels;
	}
}
