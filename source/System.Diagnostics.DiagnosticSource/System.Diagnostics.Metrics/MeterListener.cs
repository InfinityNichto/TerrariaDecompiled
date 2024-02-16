using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Metrics;

public sealed class MeterListener : IDisposable
{
	private static List<MeterListener> s_allStartedListeners = new List<MeterListener>();

	private DiagLinkedList<Instrument> _enabledMeasurementInstruments = new DiagLinkedList<Instrument>();

	private bool _disposed;

	private MeasurementCallback<byte> _byteMeasurementCallback = delegate
	{
	};

	private MeasurementCallback<short> _shortMeasurementCallback = delegate
	{
	};

	private MeasurementCallback<int> _intMeasurementCallback = delegate
	{
	};

	private MeasurementCallback<long> _longMeasurementCallback = delegate
	{
	};

	private MeasurementCallback<float> _floatMeasurementCallback = delegate
	{
	};

	private MeasurementCallback<double> _doubleMeasurementCallback = delegate
	{
	};

	private MeasurementCallback<decimal> _decimalMeasurementCallback = delegate
	{
	};

	public Action<Instrument, MeterListener>? InstrumentPublished { get; set; }

	public Action<Instrument, object?>? MeasurementsCompleted { get; set; }

	public void EnableMeasurementEvents(Instrument instrument, object? state = null)
	{
		bool oldStateStored = false;
		bool flag = false;
		object arg = null;
		lock (Instrument.SyncObject)
		{
			if (instrument != null && !_disposed && !instrument.Meter.Disposed)
			{
				_enabledMeasurementInstruments.AddIfNotExist(instrument, (Instrument instrument1, Instrument instrument2) => instrument1 == instrument2);
				arg = instrument.EnableMeasurement(new ListenerSubscription(this, state), out oldStateStored);
				flag = true;
			}
		}
		if (flag)
		{
			if (oldStateStored && MeasurementsCompleted != null)
			{
				MeasurementsCompleted?.Invoke(instrument, arg);
			}
		}
		else
		{
			MeasurementsCompleted?.Invoke(instrument, state);
		}
	}

	public object? DisableMeasurementEvents(Instrument instrument)
	{
		object obj = null;
		lock (Instrument.SyncObject)
		{
			if (instrument == null || _enabledMeasurementInstruments.Remove(instrument, (Instrument instrument1, Instrument instrument2) => instrument1 == instrument2) == null)
			{
				return null;
			}
			obj = instrument.DisableMeasurements(this);
		}
		MeasurementsCompleted?.Invoke(instrument, obj);
		return obj;
	}

	public void SetMeasurementEventCallback<T>(MeasurementCallback<T>? measurementCallback) where T : struct
	{
		if (measurementCallback is MeasurementCallback<byte> measurementCallback2)
		{
			_byteMeasurementCallback = ((measurementCallback == null) ? ((MeasurementCallback<byte>)delegate
			{
			}) : measurementCallback2);
			return;
		}
		if (measurementCallback is MeasurementCallback<int> measurementCallback3)
		{
			_intMeasurementCallback = ((measurementCallback == null) ? ((MeasurementCallback<int>)delegate
			{
			}) : measurementCallback3);
			return;
		}
		if (measurementCallback is MeasurementCallback<float> measurementCallback4)
		{
			_floatMeasurementCallback = ((measurementCallback == null) ? ((MeasurementCallback<float>)delegate
			{
			}) : measurementCallback4);
			return;
		}
		if (measurementCallback is MeasurementCallback<double> measurementCallback5)
		{
			_doubleMeasurementCallback = ((measurementCallback == null) ? ((MeasurementCallback<double>)delegate
			{
			}) : measurementCallback5);
			return;
		}
		if (measurementCallback is MeasurementCallback<decimal> measurementCallback6)
		{
			_decimalMeasurementCallback = ((measurementCallback == null) ? ((MeasurementCallback<decimal>)delegate
			{
			}) : measurementCallback6);
			return;
		}
		if (measurementCallback is MeasurementCallback<short> measurementCallback7)
		{
			_shortMeasurementCallback = ((measurementCallback == null) ? ((MeasurementCallback<short>)delegate
			{
			}) : measurementCallback7);
			return;
		}
		if (measurementCallback is MeasurementCallback<long> measurementCallback8)
		{
			_longMeasurementCallback = ((measurementCallback == null) ? ((MeasurementCallback<long>)delegate
			{
			}) : measurementCallback8);
			return;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.UnsupportedType, typeof(T)));
	}

	public void Start()
	{
		List<Instrument> list = null;
		lock (Instrument.SyncObject)
		{
			if (_disposed)
			{
				return;
			}
			if (!s_allStartedListeners.Contains(this))
			{
				s_allStartedListeners.Add(this);
				list = Meter.GetPublishedInstruments();
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (Instrument item in list)
		{
			InstrumentPublished?.Invoke(item, this);
		}
	}

	public void RecordObservableInstruments()
	{
		List<Exception> list = null;
		for (DiagNode<Instrument> diagNode = _enabledMeasurementInstruments.First; diagNode != null; diagNode = diagNode.Next)
		{
			if (diagNode.Value.IsObservable)
			{
				try
				{
					diagNode.Value.Observe(this);
				}
				catch (Exception item)
				{
					if (list == null)
					{
						list = new List<Exception>();
					}
					list.Add(item);
				}
			}
		}
		if (list != null)
		{
			throw new AggregateException(list);
		}
	}

	public void Dispose()
	{
		Dictionary<Instrument, object> dictionary = null;
		Action<Instrument, object> measurementsCompleted = MeasurementsCompleted;
		lock (Instrument.SyncObject)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			s_allStartedListeners.Remove(this);
			DiagNode<Instrument> diagNode = _enabledMeasurementInstruments.First;
			if (diagNode != null && measurementsCompleted != null)
			{
				dictionary = new Dictionary<Instrument, object>();
				do
				{
					object value = diagNode.Value.DisableMeasurements(this);
					dictionary.Add(diagNode.Value, value);
					diagNode = diagNode.Next;
				}
				while (diagNode != null);
				_enabledMeasurementInstruments.Clear();
			}
		}
		if (dictionary == null)
		{
			return;
		}
		foreach (KeyValuePair<Instrument, object> item in dictionary)
		{
			measurementsCompleted?.Invoke(item.Key, item.Value);
		}
	}

	internal static List<MeterListener> GetAllListeners()
	{
		if (s_allStartedListeners.Count != 0)
		{
			return new List<MeterListener>(s_allStartedListeners);
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void NotifyMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object>> tags, object state) where T : struct
	{
		if (typeof(T) == typeof(byte))
		{
			_byteMeasurementCallback(instrument, (byte)(object)measurement, tags, state);
		}
		if (typeof(T) == typeof(short))
		{
			_shortMeasurementCallback(instrument, (short)(object)measurement, tags, state);
		}
		if (typeof(T) == typeof(int))
		{
			_intMeasurementCallback(instrument, (int)(object)measurement, tags, state);
		}
		if (typeof(T) == typeof(long))
		{
			_longMeasurementCallback(instrument, (long)(object)measurement, tags, state);
		}
		if (typeof(T) == typeof(float))
		{
			_floatMeasurementCallback(instrument, (float)(object)measurement, tags, state);
		}
		if (typeof(T) == typeof(double))
		{
			_doubleMeasurementCallback(instrument, (double)(object)measurement, tags, state);
		}
		if (typeof(T) == typeof(decimal))
		{
			_decimalMeasurementCallback(instrument, (decimal)(object)measurement, tags, state);
		}
	}
}
