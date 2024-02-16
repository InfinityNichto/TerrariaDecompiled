using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing;

[UnsupportedOSPlatform("browser")]
public class IncrementingEventCounter : DiagnosticCounter
{
	private double _increment;

	private double _prevIncrement;

	public TimeSpan DisplayRateTimeScale { get; set; }

	public IncrementingEventCounter(string name, EventSource eventSource)
		: base(name, eventSource)
	{
		Publish();
	}

	public void Increment(double increment = 1.0)
	{
		lock (this)
		{
			_increment += increment;
		}
	}

	public override string ToString()
	{
		return $"IncrementingEventCounter '{base.Name}' Increment {_increment}";
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The DynamicDependency will preserve the properties of IncrementingCounterPayload")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(IncrementingCounterPayload))]
	internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
	{
		lock (this)
		{
			IncrementingCounterPayload incrementingCounterPayload = new IncrementingCounterPayload();
			incrementingCounterPayload.Name = base.Name;
			incrementingCounterPayload.IntervalSec = intervalSec;
			incrementingCounterPayload.DisplayName = base.DisplayName ?? "";
			incrementingCounterPayload.DisplayRateTimeScale = ((DisplayRateTimeScale == TimeSpan.Zero) ? "" : DisplayRateTimeScale.ToString("c"));
			incrementingCounterPayload.Series = $"Interval={pollingIntervalMillisec}";
			incrementingCounterPayload.CounterType = "Sum";
			incrementingCounterPayload.Metadata = GetMetadataString();
			incrementingCounterPayload.Increment = _increment - _prevIncrement;
			incrementingCounterPayload.DisplayUnits = base.DisplayUnits ?? "";
			_prevIncrement = _increment;
			base.EventSource.Write("EventCounters", new EventSourceOptions
			{
				Level = EventLevel.LogAlways
			}, new IncrementingEventCounterPayloadType(incrementingCounterPayload));
		}
	}

	internal void UpdateMetric()
	{
		lock (this)
		{
			_prevIncrement = _increment;
		}
	}
}
