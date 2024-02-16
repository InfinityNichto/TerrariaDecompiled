using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

public abstract class Instrument
{
	internal readonly DiagLinkedList<ListenerSubscription> _subscriptions = new DiagLinkedList<ListenerSubscription>();

	internal static KeyValuePair<string, object?>[] EmptyTags => Array.Empty<KeyValuePair<string, object>>();

	internal static object SyncObject { get; } = new object();


	public Meter Meter { get; }

	public string Name { get; }

	public string? Description { get; }

	public string? Unit { get; }

	public bool Enabled => _subscriptions.First != null;

	public virtual bool IsObservable => false;

	protected Instrument(Meter meter, string name, string? unit, string? description)
	{
		if (meter == null)
		{
			throw new ArgumentNullException("meter");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		Meter = meter;
		Name = name;
		Description = description;
		Unit = unit;
	}

	protected void Publish()
	{
		List<MeterListener> list = null;
		lock (SyncObject)
		{
			if (Meter.Disposed || !Meter.AddInstrument(this))
			{
				return;
			}
			list = MeterListener.GetAllListeners();
		}
		if (list == null)
		{
			return;
		}
		foreach (MeterListener item in list)
		{
			item.InstrumentPublished?.Invoke(this, item);
		}
	}

	internal void NotifyForUnpublishedInstrument()
	{
		for (DiagNode<ListenerSubscription> diagNode = _subscriptions.First; diagNode != null; diagNode = diagNode.Next)
		{
			diagNode.Value.Listener.DisableMeasurementEvents(this);
		}
		_subscriptions.Clear();
	}

	internal static void ValidateTypeParameter<T>()
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle != typeof(byte) && typeFromHandle != typeof(short) && typeFromHandle != typeof(int) && typeFromHandle != typeof(long) && typeFromHandle != typeof(double) && typeFromHandle != typeof(float) && typeFromHandle != typeof(decimal))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.UnsupportedType, typeFromHandle));
		}
	}

	internal object EnableMeasurement(ListenerSubscription subscription, out bool oldStateStored)
	{
		oldStateStored = false;
		if (!_subscriptions.AddIfNotExist(subscription, (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener))
		{
			ListenerSubscription listenerSubscription = _subscriptions.Remove(subscription, (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener);
			_subscriptions.AddIfNotExist(subscription, (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener);
			oldStateStored = listenerSubscription.Listener == subscription.Listener;
			return listenerSubscription.State;
		}
		return false;
	}

	internal object DisableMeasurements(MeterListener listener)
	{
		return _subscriptions.Remove(new ListenerSubscription(listener), (ListenerSubscription s1, ListenerSubscription s2) => s1.Listener == s2.Listener).State;
	}

	internal virtual void Observe(MeterListener listener)
	{
		throw new InvalidOperationException();
	}

	internal object GetSubscriptionState(MeterListener listener)
	{
		for (DiagNode<ListenerSubscription> diagNode = _subscriptions.First; diagNode != null; diagNode = diagNode.Next)
		{
			if (listener == diagNode.Value.Listener)
			{
				return diagNode.Value.State;
			}
		}
		return null;
	}
}
public abstract class Instrument<T> : Instrument where T : struct
{
	protected Instrument(Meter meter, string name, string? unit, string? description)
		: base(meter, name, unit, description)
	{
		Instrument.ValidateTypeParameter<T>();
	}

	protected void RecordMeasurement(T measurement)
	{
		RecordMeasurement(measurement, Instrument.EmptyTags.AsSpan());
	}

	protected void RecordMeasurement(T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags)
	{
		for (DiagNode<ListenerSubscription> diagNode = _subscriptions.First; diagNode != null; diagNode = diagNode.Next)
		{
			diagNode.Value.Listener.NotifyMeasurement(this, measurement, tags, diagNode.Value.State);
		}
	}

	protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag)
	{
		OneTagBag oneTagBag = new OneTagBag(tag);
		RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref oneTagBag.Tag1, 1));
	}

	protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2)
	{
		TwoTagsBag twoTagsBag = new TwoTagsBag(tag1, tag2);
		RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref twoTagsBag.Tag1, 2));
	}

	protected void RecordMeasurement(T measurement, KeyValuePair<string, object?> tag1, KeyValuePair<string, object?> tag2, KeyValuePair<string, object?> tag3)
	{
		ThreeTagsBag threeTagsBag = new ThreeTagsBag(tag1, tag2, tag3);
		RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref threeTagsBag.Tag1, 3));
	}

	protected void RecordMeasurement(T measurement, in TagList tagList)
	{
		KeyValuePair<string, object>[] tags = tagList.Tags;
		if (tags != null)
		{
			RecordMeasurement(measurement, tags.AsSpan().Slice(0, tagList.Count));
		}
		else
		{
			RecordMeasurement(measurement, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in tagList.Tag1), tagList.Count));
		}
	}
}
