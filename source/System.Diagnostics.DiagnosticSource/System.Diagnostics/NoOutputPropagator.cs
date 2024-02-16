using System.Collections.Generic;

namespace System.Diagnostics;

internal sealed class NoOutputPropagator : DistributedContextPropagator
{
	internal static DistributedContextPropagator Instance { get; } = new NoOutputPropagator();


	public override IReadOnlyCollection<string> Fields { get; } = LegacyPropagator.Instance.Fields;


	public override void Inject(Activity activity, object carrier, PropagatorSetterCallback setter)
	{
	}

	public override void ExtractTraceIdAndState(object carrier, PropagatorGetterCallback getter, out string traceId, out string traceState)
	{
		LegacyPropagator.Instance.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);
	}

	public override IEnumerable<KeyValuePair<string, string>> ExtractBaggage(object carrier, PropagatorGetterCallback getter)
	{
		return LegacyPropagator.Instance.ExtractBaggage(carrier, getter);
	}
}
