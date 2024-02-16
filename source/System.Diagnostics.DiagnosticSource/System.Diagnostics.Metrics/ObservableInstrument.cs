using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics;

public abstract class ObservableInstrument<T> : Instrument where T : struct
{
	public override bool IsObservable => true;

	protected ObservableInstrument(Meter meter, string name, string? unit, string? description)
		: base(meter, name, unit, description)
	{
		Instrument.ValidateTypeParameter<T>();
	}

	protected abstract IEnumerable<Measurement<T>> Observe();

	internal override void Observe(MeterListener listener)
	{
		object subscriptionState = GetSubscriptionState(listener);
		IEnumerable<Measurement<T>> enumerable = Observe();
		if (enumerable == null)
		{
			return;
		}
		foreach (Measurement<T> item in enumerable)
		{
			listener.NotifyMeasurement(this, item.Value, item.Tags, subscriptionState);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal IEnumerable<Measurement<T>> Observe(object callback)
	{
		if (callback is Func<T> func)
		{
			return new Measurement<T>[1]
			{
				new Measurement<T>(func())
			};
		}
		if (callback is Func<Measurement<T>> func2)
		{
			return new Measurement<T>[1] { func2() };
		}
		if (callback is Func<IEnumerable<Measurement<T>>> func3)
		{
			return func3();
		}
		return null;
	}
}
