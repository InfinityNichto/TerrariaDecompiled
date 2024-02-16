using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal delegate bool AggregatorLookupFunc<TAggregator>(ReadOnlySpan<KeyValuePair<string, object>> labels, out TAggregator aggregator);
