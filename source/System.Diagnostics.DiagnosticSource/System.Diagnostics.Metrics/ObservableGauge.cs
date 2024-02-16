using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public sealed class ObservableGauge<T> : ObservableInstrument<T> where T : struct
{
	private object _callback;

	internal ObservableGauge(Meter meter, string name, Func<T> observeValue, string unit, string description)
		: base(meter, name, unit, description)
	{
		if (observeValue == null)
		{
			throw new ArgumentNullException("observeValue");
		}
		_callback = observeValue;
		Publish();
	}

	internal ObservableGauge(Meter meter, string name, Func<Measurement<T>> observeValue, string unit, string description)
		: base(meter, name, unit, description)
	{
		if (observeValue == null)
		{
			throw new ArgumentNullException("observeValue");
		}
		_callback = observeValue;
		Publish();
	}

	internal ObservableGauge(Meter meter, string name, Func<IEnumerable<Measurement<T>>> observeValues, string unit, string description)
		: base(meter, name, unit, description)
	{
		if (observeValues == null)
		{
			throw new ArgumentNullException("observeValues");
		}
		_callback = observeValues;
		Publish();
	}

	protected override IEnumerable<Measurement<T>> Observe()
	{
		return Observe(_callback);
	}
}
