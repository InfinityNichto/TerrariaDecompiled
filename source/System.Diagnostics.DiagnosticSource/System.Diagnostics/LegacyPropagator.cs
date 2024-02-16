using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace System.Diagnostics;

internal sealed class LegacyPropagator : DistributedContextPropagator
{
	internal static DistributedContextPropagator Instance { get; } = new LegacyPropagator();


	public override IReadOnlyCollection<string> Fields { get; } = new ReadOnlyCollection<string>(new string[5] { "traceparent", "Request-Id", "tracestate", "baggage", "Correlation-Context" });


	public override void Inject(Activity activity, object carrier, PropagatorSetterCallback setter)
	{
		if (activity == null || setter == null)
		{
			return;
		}
		string id = activity.Id;
		if (id == null)
		{
			return;
		}
		if (activity.IdFormat == ActivityIdFormat.W3C)
		{
			setter(carrier, "traceparent", id);
			if (!string.IsNullOrEmpty(activity.TraceStateString))
			{
				setter(carrier, "tracestate", activity.TraceStateString);
			}
		}
		else
		{
			setter(carrier, "Request-Id", id);
		}
		DistributedContextPropagator.InjectBaggage(carrier, activity.Baggage, setter);
	}

	public override void ExtractTraceIdAndState(object carrier, PropagatorGetterCallback getter, out string traceId, out string traceState)
	{
		if (getter == null)
		{
			traceId = null;
			traceState = null;
			return;
		}
		getter(carrier, "traceparent", out traceId, out var fieldValues);
		if (traceId == null)
		{
			getter(carrier, "Request-Id", out traceId, out fieldValues);
		}
		getter(carrier, "tracestate", out traceState, out fieldValues);
	}

	public override IEnumerable<KeyValuePair<string, string>> ExtractBaggage(object carrier, PropagatorGetterCallback getter)
	{
		if (getter == null)
		{
			return null;
		}
		getter(carrier, "baggage", out var fieldValue, out var fieldValues);
		IEnumerable<KeyValuePair<string, string>> baggage = null;
		if (fieldValue == null || !TryExtractBaggage(fieldValue, out baggage))
		{
			getter(carrier, "Correlation-Context", out fieldValue, out fieldValues);
			if (fieldValue != null)
			{
				TryExtractBaggage(fieldValue, out baggage);
			}
		}
		return baggage;
	}

	internal static bool TryExtractBaggage(string baggageString, out IEnumerable<KeyValuePair<string, string>> baggage)
	{
		baggage = null;
		List<KeyValuePair<string, string>> list = null;
		if (string.IsNullOrEmpty(baggageString))
		{
			return true;
		}
		int i = 0;
		while (true)
		{
			if (i < baggageString.Length && (baggageString[i] == ' ' || baggageString[i] == '\t'))
			{
				i++;
				continue;
			}
			if (i >= baggageString.Length)
			{
				break;
			}
			int num = i;
			for (; i < baggageString.Length && baggageString[i] != ' ' && baggageString[i] != '\t' && baggageString[i] != '='; i++)
			{
			}
			if (i >= baggageString.Length)
			{
				break;
			}
			int num2 = i;
			if (baggageString[i] != '=')
			{
				for (; i < baggageString.Length && (baggageString[i] == ' ' || baggageString[i] == '\t'); i++)
				{
				}
				if (i >= baggageString.Length || baggageString[i] != '=')
				{
					break;
				}
			}
			for (i++; i < baggageString.Length && (baggageString[i] == ' ' || baggageString[i] == '\t'); i++)
			{
			}
			if (i >= baggageString.Length)
			{
				break;
			}
			int num3 = i;
			for (; i < baggageString.Length && baggageString[i] != ' ' && baggageString[i] != '\t' && baggageString[i] != ',' && baggageString[i] != ';'; i++)
			{
			}
			if (num < num2 && num3 < i)
			{
				if (list == null)
				{
					list = new List<KeyValuePair<string, string>>();
				}
				list.Insert(0, new KeyValuePair<string, string>(WebUtility.UrlDecode(baggageString.Substring(num, num2 - num)).Trim(DistributedContextPropagator.s_trimmingSpaceCharacters), WebUtility.UrlDecode(baggageString.Substring(num3, i - num3)).Trim(DistributedContextPropagator.s_trimmingSpaceCharacters)));
			}
			for (; i < baggageString.Length && baggageString[i] != ','; i++)
			{
			}
			i++;
			if (i >= baggageString.Length)
			{
				break;
			}
		}
		baggage = list;
		return list != null;
	}
}
