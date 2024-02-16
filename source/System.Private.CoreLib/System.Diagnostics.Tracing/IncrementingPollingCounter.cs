using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing;

[UnsupportedOSPlatform("browser")]
public class IncrementingPollingCounter : DiagnosticCounter
{
	private double _increment;

	private double _prevIncrement;

	private readonly Func<double> _totalValueProvider;

	public TimeSpan DisplayRateTimeScale { get; set; }

	public IncrementingPollingCounter(string name, EventSource eventSource, Func<double> totalValueProvider)
		: base(name, eventSource)
	{
		if (totalValueProvider == null)
		{
			throw new ArgumentNullException("totalValueProvider");
		}
		_totalValueProvider = totalValueProvider;
		Publish();
	}

	public override string ToString()
	{
		return $"IncrementingPollingCounter '{base.Name}' Increment {_increment}";
	}

	internal void UpdateMetric()
	{
		try
		{
			lock (this)
			{
				_prevIncrement = _increment;
				_increment = _totalValueProvider();
			}
		}
		catch (Exception ex)
		{
			ReportOutOfBandMessage("ERROR: Exception during EventCounter " + base.Name + " getMetricFunction callback: " + ex.Message);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The DynamicDependency will preserve the properties of IncrementingCounterPayload")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(IncrementingCounterPayload))]
	internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
	{
		UpdateMetric();
		lock (this)
		{
			IncrementingCounterPayload incrementingCounterPayload = new IncrementingCounterPayload();
			incrementingCounterPayload.Name = base.Name;
			incrementingCounterPayload.DisplayName = base.DisplayName ?? "";
			incrementingCounterPayload.DisplayRateTimeScale = ((DisplayRateTimeScale == TimeSpan.Zero) ? "" : DisplayRateTimeScale.ToString("c"));
			incrementingCounterPayload.IntervalSec = intervalSec;
			incrementingCounterPayload.Series = $"Interval={pollingIntervalMillisec}";
			incrementingCounterPayload.CounterType = "Sum";
			incrementingCounterPayload.Metadata = GetMetadataString();
			incrementingCounterPayload.Increment = _increment - _prevIncrement;
			incrementingCounterPayload.DisplayUnits = base.DisplayUnits ?? "";
			base.EventSource.Write("EventCounters", new EventSourceOptions
			{
				Level = EventLevel.LogAlways
			}, new IncrementingPollingCounterPayloadType(incrementingCounterPayload));
		}
	}
}
