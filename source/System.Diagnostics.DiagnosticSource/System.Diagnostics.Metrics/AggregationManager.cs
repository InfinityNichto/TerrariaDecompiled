using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Metrics;

[UnsupportedOSPlatform("browser")]
internal sealed class AggregationManager
{
	private static readonly QuantileAggregation s_defaultHistogramConfig = new QuantileAggregation(0.5, 0.95, 0.99);

	private readonly List<Predicate<Instrument>> _instrumentConfigFuncs = new List<Predicate<Instrument>>();

	private TimeSpan _collectionPeriod;

	private readonly ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new ConcurrentDictionary<Instrument, InstrumentState>();

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private Thread _collectThread;

	private readonly MeterListener _listener;

	private int _currentTimeSeries;

	private int _currentHistograms;

	private readonly int _maxTimeSeries;

	private readonly int _maxHistograms;

	private readonly Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;

	private readonly Action<DateTime, DateTime> _beginCollection;

	private readonly Action<DateTime, DateTime> _endCollection;

	private readonly Action<Instrument> _beginInstrumentMeasurements;

	private readonly Action<Instrument> _endInstrumentMeasurements;

	private readonly Action<Instrument> _instrumentPublished;

	private readonly Action _initialInstrumentEnumerationComplete;

	private readonly Action<Exception> _collectionError;

	private readonly Action _timeSeriesLimitReached;

	private readonly Action _histogramLimitReached;

	private readonly Action<Exception> _observableInstrumentCallbackError;

	public AggregationManager(int maxTimeSeries, int maxHistograms, Action<Instrument, LabeledAggregationStatistics> collectMeasurement, Action<DateTime, DateTime> beginCollection, Action<DateTime, DateTime> endCollection, Action<Instrument> beginInstrumentMeasurements, Action<Instrument> endInstrumentMeasurements, Action<Instrument> instrumentPublished, Action initialInstrumentEnumerationComplete, Action<Exception> collectionError, Action timeSeriesLimitReached, Action histogramLimitReached, Action<Exception> observableInstrumentCallbackError)
	{
		_maxTimeSeries = maxTimeSeries;
		_maxHistograms = maxHistograms;
		_collectMeasurement = collectMeasurement;
		_beginCollection = beginCollection;
		_endCollection = endCollection;
		_beginInstrumentMeasurements = beginInstrumentMeasurements;
		_endInstrumentMeasurements = endInstrumentMeasurements;
		_instrumentPublished = instrumentPublished;
		_initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;
		_collectionError = collectionError;
		_timeSeriesLimitReached = timeSeriesLimitReached;
		_histogramLimitReached = histogramLimitReached;
		_observableInstrumentCallbackError = observableInstrumentCallbackError;
		_listener = new MeterListener
		{
			InstrumentPublished = delegate(Instrument instrument, MeterListener listener)
			{
				_instrumentPublished(instrument);
				InstrumentState instrumentState = GetInstrumentState(instrument);
				if (instrumentState != null)
				{
					_beginInstrumentMeasurements(instrument);
					listener.EnableMeasurementEvents(instrument, instrumentState);
				}
			},
			MeasurementsCompleted = delegate(Instrument instrument, object cookie)
			{
				_endInstrumentMeasurements(instrument);
				RemoveInstrumentState(instrument, (InstrumentState)cookie);
			}
		};
		_listener.SetMeasurementEventCallback(delegate(Instrument i, double m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, float m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, long m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, int m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, short m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, byte m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update((int)m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, decimal m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update((double)m, l);
		});
	}

	public void Include(string meterName)
	{
		Include((Instrument i) => i.Meter.Name == meterName);
	}

	public void Include(string meterName, string instrumentName)
	{
		Include((Instrument i) => i.Meter.Name == meterName && i.Name == instrumentName);
	}

	private void Include(Predicate<Instrument> instrumentFilter)
	{
		lock (this)
		{
			_instrumentConfigFuncs.Add(instrumentFilter);
		}
	}

	public AggregationManager SetCollectionPeriod(TimeSpan collectionPeriod)
	{
		lock (this)
		{
			_collectionPeriod = collectionPeriod;
			return this;
		}
	}

	public void Start()
	{
		_collectThread = new Thread((ThreadStart)delegate
		{
			CollectWorker(_cts.Token);
		});
		_collectThread.IsBackground = true;
		_collectThread.Name = "MetricsEventSource CollectWorker";
		_collectThread.Start();
		_listener.Start();
		_initialInstrumentEnumerationComplete();
	}

	private void CollectWorker(CancellationToken cancelToken)
	{
		try
		{
			double num = -1.0;
			lock (this)
			{
				num = _collectionPeriod.TotalSeconds;
			}
			DateTime utcNow = DateTime.UtcNow;
			DateTime arg = utcNow;
			while (!cancelToken.IsCancellationRequested)
			{
				DateTime utcNow2 = DateTime.UtcNow;
				double totalSeconds = (utcNow2 - utcNow).TotalSeconds;
				double value = Math.Ceiling(totalSeconds / num) * num;
				DateTime dateTime = utcNow.AddSeconds(value);
				DateTime dateTime2 = arg.AddSeconds(num);
				if (dateTime <= dateTime2)
				{
					dateTime = dateTime2;
				}
				TimeSpan timeout = dateTime - utcNow2;
				if (!cancelToken.WaitHandle.WaitOne(timeout))
				{
					_beginCollection(arg, dateTime);
					Collect();
					_endCollection(arg, dateTime);
					arg = dateTime;
					continue;
				}
				break;
			}
		}
		catch (Exception obj)
		{
			_collectionError(obj);
		}
	}

	public void Dispose()
	{
		_cts.Cancel();
		if (_collectThread != null)
		{
			_collectThread.Join();
			_collectThread = null;
		}
		_listener.Dispose();
	}

	private void RemoveInstrumentState(Instrument instrument, InstrumentState state)
	{
		_instrumentStates.TryRemove(instrument, out var _);
	}

	private InstrumentState GetInstrumentState(Instrument instrument)
	{
		if (!_instrumentStates.TryGetValue(instrument, out var value))
		{
			lock (this)
			{
				foreach (Predicate<Instrument> instrumentConfigFunc in _instrumentConfigFuncs)
				{
					if (instrumentConfigFunc(instrument))
					{
						value = BuildInstrumentState(instrument);
						if (value != null)
						{
							_instrumentStates.TryAdd(instrument, value);
							_instrumentStates.TryGetValue(instrument, out value);
						}
						break;
					}
				}
			}
		}
		return value;
	}

	internal InstrumentState BuildInstrumentState(Instrument instrument)
	{
		Func<Aggregator> aggregatorFactory = GetAggregatorFactory(instrument);
		if (aggregatorFactory == null)
		{
			return null;
		}
		Type type = aggregatorFactory.GetType().GenericTypeArguments[0];
		Type type2 = typeof(InstrumentState<>).MakeGenericType(type);
		return (InstrumentState)Activator.CreateInstance(type2, aggregatorFactory);
	}

	private Func<Aggregator> GetAggregatorFactory(Instrument instrument)
	{
		Type type = instrument.GetType();
		Type type2 = null;
		type2 = (type.IsGenericType ? type.GetGenericTypeDefinition() : null);
		if (type2 == typeof(Counter<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new RateSumAggregator() : null;
				}
			};
		}
		if (type2 == typeof(ObservableCounter<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new RateAggregator() : null;
				}
			};
		}
		if (type2 == typeof(ObservableGauge<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new LastValue() : null;
				}
			};
		}
		if (type2 == typeof(Histogram<>))
		{
			return delegate
			{
				lock (this)
				{
					return (!CheckTimeSeriesAllowed() || !CheckHistogramAllowed()) ? null : new ExponentialHistogramAggregator(s_defaultHistogramConfig);
				}
			};
		}
		return null;
	}

	private bool CheckTimeSeriesAllowed()
	{
		if (_currentTimeSeries < _maxTimeSeries)
		{
			_currentTimeSeries++;
			return true;
		}
		if (_currentTimeSeries == _maxTimeSeries)
		{
			_currentTimeSeries++;
			_timeSeriesLimitReached();
			return false;
		}
		return false;
	}

	private bool CheckHistogramAllowed()
	{
		if (_currentHistograms < _maxHistograms)
		{
			_currentHistograms++;
			return true;
		}
		if (_currentHistograms == _maxHistograms)
		{
			_currentHistograms++;
			_histogramLimitReached();
			return false;
		}
		return false;
	}

	internal void Collect()
	{
		try
		{
			_listener.RecordObservableInstruments();
		}
		catch (Exception obj)
		{
			_observableInstrumentCallbackError(obj);
		}
		foreach (KeyValuePair<Instrument, InstrumentState> kv in _instrumentStates)
		{
			kv.Value.Collect(kv.Key, delegate(LabeledAggregationStatistics labeledAggStats)
			{
				_collectMeasurement(kv.Key, labeledAggStats);
			});
		}
	}
}
