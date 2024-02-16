using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing;

[EventData]
internal sealed class CounterPayload : IEnumerable<KeyValuePair<string, object>>, IEnumerable
{
	public string Name { get; set; }

	public string DisplayName { get; set; }

	public double Mean { get; set; }

	public double StandardDeviation { get; set; }

	public int Count { get; set; }

	public double Min { get; set; }

	public double Max { get; set; }

	public float IntervalSec { get; internal set; }

	public string Series { get; set; }

	public string CounterType { get; set; }

	public string Metadata { get; set; }

	public string DisplayUnits { get; set; }

	private IEnumerable<KeyValuePair<string, object>> ForEnumeration
	{
		get
		{
			yield return new KeyValuePair<string, object>("Name", Name);
			yield return new KeyValuePair<string, object>("DisplayName", DisplayName);
			yield return new KeyValuePair<string, object>("DisplayUnits", DisplayUnits);
			yield return new KeyValuePair<string, object>("Mean", Mean);
			yield return new KeyValuePair<string, object>("StandardDeviation", StandardDeviation);
			yield return new KeyValuePair<string, object>("Count", Count);
			yield return new KeyValuePair<string, object>("Min", Min);
			yield return new KeyValuePair<string, object>("Max", Max);
			yield return new KeyValuePair<string, object>("IntervalSec", IntervalSec);
			yield return new KeyValuePair<string, object>("Series", $"Interval={IntervalSec}");
			yield return new KeyValuePair<string, object>("CounterType", "Mean");
			yield return new KeyValuePair<string, object>("Metadata", Metadata);
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
