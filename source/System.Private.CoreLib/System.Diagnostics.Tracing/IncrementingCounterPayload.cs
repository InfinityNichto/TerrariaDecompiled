using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing;

[EventData]
internal sealed class IncrementingCounterPayload : IEnumerable<KeyValuePair<string, object>>, IEnumerable
{
	public string Name { get; set; }

	public string DisplayName { get; set; }

	public string DisplayRateTimeScale { get; set; }

	public double Increment { get; set; }

	public float IntervalSec { get; internal set; }

	public string Metadata { get; set; }

	public string Series { get; set; }

	public string CounterType { get; set; }

	public string DisplayUnits { get; set; }

	private IEnumerable<KeyValuePair<string, object>> ForEnumeration
	{
		get
		{
			yield return new KeyValuePair<string, object>("Name", Name);
			yield return new KeyValuePair<string, object>("DisplayName", DisplayName);
			yield return new KeyValuePair<string, object>("DisplayRateTimeScale", DisplayRateTimeScale);
			yield return new KeyValuePair<string, object>("Increment", Increment);
			yield return new KeyValuePair<string, object>("IntervalSec", IntervalSec);
			yield return new KeyValuePair<string, object>("Series", $"Interval={IntervalSec}");
			yield return new KeyValuePair<string, object>("CounterType", "Sum");
			yield return new KeyValuePair<string, object>("Metadata", Metadata);
			yield return new KeyValuePair<string, object>("DisplayUnits", DisplayUnits);
		}
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
	{
		return ForEnumeration.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ForEnumeration.GetEnumerator();
	}
}
