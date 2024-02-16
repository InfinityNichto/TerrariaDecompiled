using System.Collections.Generic;

namespace System.Diagnostics;

internal sealed class PassThroughPropagator : DistributedContextPropagator
{
	internal static DistributedContextPropagator Instance { get; } = new PassThroughPropagator();


	public override IReadOnlyCollection<string> Fields { get; } = LegacyPropagator.Instance.Fields;


	public override void Inject(Activity activity, object carrier, PropagatorSetterCallback setter)
	{
		if (setter == null)
		{
			return;
		}
		GetRootId(out var parentId, out var traceState, out var isW3c, out var baggage);
		if (parentId != null)
		{
			setter(carrier, isW3c ? "traceparent" : "Request-Id", parentId);
			if (!string.IsNullOrEmpty(traceState))
			{
				setter(carrier, "tracestate", traceState);
			}
			if (baggage != null)
			{
				DistributedContextPropagator.InjectBaggage(carrier, baggage, setter);
			}
		}
	}

	public override void ExtractTraceIdAndState(object carrier, PropagatorGetterCallback getter, out string traceId, out string traceState)
	{
		LegacyPropagator.Instance.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);
	}

	public override IEnumerable<KeyValuePair<string, string>> ExtractBaggage(object carrier, PropagatorGetterCallback getter)
	{
		return LegacyPropagator.Instance.ExtractBaggage(carrier, getter);
	}

	private static void GetRootId(out string parentId, out string traceState, out bool isW3c, out IEnumerable<KeyValuePair<string, string>> baggage)
	{
		Activity activity = Activity.Current;
		while (true)
		{
			Activity activity2 = activity?.Parent;
			if (activity2 == null)
			{
				break;
			}
			activity = activity2;
		}
		traceState = activity?.TraceStateString;
		parentId = activity?.ParentId ?? activity?.Id;
		isW3c = parentId != null && Activity.TryConvertIdToContext(parentId, traceState, out var _);
		baggage = activity?.Baggage;
	}
}
