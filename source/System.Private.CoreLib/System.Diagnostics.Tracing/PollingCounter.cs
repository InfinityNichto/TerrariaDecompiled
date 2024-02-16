using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing;

[UnsupportedOSPlatform("browser")]
public class PollingCounter : DiagnosticCounter
{
	private readonly Func<double> _metricProvider;

	private double _lastVal;

	public PollingCounter(string name, EventSource eventSource, Func<double> metricProvider)
		: base(name, eventSource)
	{
		if (metricProvider == null)
		{
			throw new ArgumentNullException("metricProvider");
		}
		_metricProvider = metricProvider;
		Publish();
	}

	public override string ToString()
	{
		return $"PollingCounter '{base.Name}' Count 1 Mean {_lastVal:n3}";
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The DynamicDependency will preserve the properties of CounterPayload")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(CounterPayload))]
	internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
	{
		lock (this)
		{
			double num = 0.0;
			try
			{
				num = _metricProvider();
			}
			catch (Exception ex)
			{
				ReportOutOfBandMessage("ERROR: Exception during EventCounter " + base.Name + " metricProvider callback: " + ex.Message);
			}
			CounterPayload counterPayload = new CounterPayload();
			counterPayload.Name = base.Name;
			counterPayload.DisplayName = base.DisplayName ?? "";
			counterPayload.Count = 1;
			counterPayload.IntervalSec = intervalSec;
			counterPayload.Series = $"Interval={pollingIntervalMillisec}";
			counterPayload.CounterType = "Mean";
			counterPayload.Mean = num;
			counterPayload.Max = num;
			counterPayload.Min = num;
			counterPayload.Metadata = GetMetadataString();
			counterPayload.StandardDeviation = 0.0;
			counterPayload.DisplayUnits = base.DisplayUnits ?? "";
			_lastVal = num;
			base.EventSource.Write("EventCounters", new EventSourceOptions
			{
				Level = EventLevel.LogAlways
			}, new PollingPayloadType(counterPayload));
		}
	}
}
