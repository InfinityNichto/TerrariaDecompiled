using System.Collections.Generic;
using System.Net;
using System.Text;

namespace System.Diagnostics;

public abstract class DistributedContextPropagator
{
	public delegate void PropagatorGetterCallback(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues);

	public delegate void PropagatorSetterCallback(object? carrier, string fieldName, string fieldValue);

	private static DistributedContextPropagator s_current = CreateDefaultPropagator();

	internal static readonly char[] s_trimmingSpaceCharacters = new char[2] { ' ', '\t' };

	public abstract IReadOnlyCollection<string> Fields { get; }

	public static DistributedContextPropagator Current
	{
		get
		{
			return s_current;
		}
		set
		{
			s_current = value ?? throw new ArgumentNullException("value");
		}
	}

	public abstract void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter);

	public abstract void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState);

	public abstract IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter);

	public static DistributedContextPropagator CreateDefaultPropagator()
	{
		return LegacyPropagator.Instance;
	}

	public static DistributedContextPropagator CreatePassThroughPropagator()
	{
		return PassThroughPropagator.Instance;
	}

	public static DistributedContextPropagator CreateNoOutputPropagator()
	{
		return NoOutputPropagator.Instance;
	}

	internal static void InjectBaggage(object carrier, IEnumerable<KeyValuePair<string, string>> baggage, PropagatorSetterCallback setter)
	{
		using IEnumerator<KeyValuePair<string, string>> enumerator = baggage.GetEnumerator();
		if (enumerator.MoveNext())
		{
			StringBuilder stringBuilder = new StringBuilder();
			do
			{
				KeyValuePair<string, string> current = enumerator.Current;
				stringBuilder.Append(WebUtility.UrlEncode(current.Key)).Append('=').Append(WebUtility.UrlEncode(current.Value))
					.Append(", ");
			}
			while (enumerator.MoveNext());
			setter(carrier, "Correlation-Context", stringBuilder.ToString(0, stringBuilder.Length - 2));
		}
	}
}
