using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Tracing;

[UnsupportedOSPlatform("browser")]
public class EventCounter : DiagnosticCounter
{
	private int _count;

	private double _sum;

	private double _sumSquared;

	private double _min;

	private double _max;

	private readonly double[] _bufferedValues;

	private volatile int _bufferedValuesIndex;

	public EventCounter(string name, EventSource eventSource)
		: base(name, eventSource)
	{
		_min = double.PositiveInfinity;
		_max = double.NegativeInfinity;
		double[] array = new double[10];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = double.NegativeInfinity;
		}
		_bufferedValues = array;
		Publish();
	}

	public void WriteMetric(float value)
	{
		Enqueue(value);
	}

	public void WriteMetric(double value)
	{
		Enqueue(value);
	}

	public override string ToString()
	{
		int num = Volatile.Read(ref _count);
		if (num != 0)
		{
			return $"EventCounter '{base.Name}' Count {num} Mean {_sum / (double)num:n3}";
		}
		return "EventCounter '" + base.Name + "' Count 0";
	}

	internal void OnMetricWritten(double value)
	{
		_sum += value;
		_sumSquared += value * value;
		if (value > _max)
		{
			_max = value;
		}
		if (value < _min)
		{
			_min = value;
		}
		_count++;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The DynamicDependency will preserve the properties of CounterPayload")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(CounterPayload))]
	internal override void WritePayload(float intervalSec, int pollingIntervalMillisec)
	{
		lock (this)
		{
			Flush();
			CounterPayload counterPayload = new CounterPayload();
			counterPayload.Count = _count;
			counterPayload.IntervalSec = intervalSec;
			if (0 < _count)
			{
				counterPayload.Mean = _sum / (double)_count;
				counterPayload.StandardDeviation = Math.Sqrt(_sumSquared / (double)_count - _sum * _sum / (double)_count / (double)_count);
			}
			else
			{
				counterPayload.Mean = 0.0;
				counterPayload.StandardDeviation = 0.0;
			}
			counterPayload.Min = _min;
			counterPayload.Max = _max;
			counterPayload.Series = $"Interval={pollingIntervalMillisec}";
			counterPayload.CounterType = "Mean";
			counterPayload.Metadata = GetMetadataString();
			counterPayload.DisplayName = base.DisplayName ?? "";
			counterPayload.DisplayUnits = base.DisplayUnits ?? "";
			counterPayload.Name = base.Name;
			ResetStatistics();
			base.EventSource.Write("EventCounters", new EventSourceOptions
			{
				Level = EventLevel.LogAlways
			}, new CounterPayloadType(counterPayload));
		}
	}

	internal void ResetStatistics()
	{
		lock (this)
		{
			_count = 0;
			_sum = 0.0;
			_sumSquared = 0.0;
			_min = double.PositiveInfinity;
			_max = double.NegativeInfinity;
		}
	}

	private void Enqueue(double value)
	{
		int num = _bufferedValuesIndex;
		double num2;
		do
		{
			num2 = Interlocked.CompareExchange(ref _bufferedValues[num], value, double.NegativeInfinity);
			num++;
			if (_bufferedValues.Length <= num)
			{
				lock (this)
				{
					Flush();
				}
				num = 0;
			}
		}
		while (num2 != double.NegativeInfinity);
		_bufferedValuesIndex = num;
	}

	protected void Flush()
	{
		for (int i = 0; i < _bufferedValues.Length; i++)
		{
			double num = Interlocked.Exchange(ref _bufferedValues[i], double.NegativeInfinity);
			if (num != double.NegativeInfinity)
			{
				OnMetricWritten(num);
			}
		}
		_bufferedValuesIndex = 0;
	}
}
