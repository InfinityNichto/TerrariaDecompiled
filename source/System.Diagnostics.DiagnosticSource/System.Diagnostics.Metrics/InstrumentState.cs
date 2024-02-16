using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal abstract class InstrumentState
{
	public abstract void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object>> labels);

	public abstract void Collect(Instrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc);
}
internal sealed class InstrumentState<TAggregator> : InstrumentState where TAggregator : Aggregator
{
	private AggregatorStore<TAggregator> _aggregatorStore;

	public InstrumentState(Func<TAggregator> createAggregatorFunc)
	{
		_aggregatorStore = new AggregatorStore<TAggregator>(createAggregatorFunc);
	}

	public override void Collect(Instrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc)
	{
		_aggregatorStore.Collect(aggregationVisitFunc);
	}

	public override void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object>> labels)
	{
		_aggregatorStore.GetAggregator(labels)?.Update(measurement);
	}
}
