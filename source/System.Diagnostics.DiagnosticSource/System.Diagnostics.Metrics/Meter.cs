using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public class Meter : IDisposable
{
	private static readonly List<Meter> s_allMeters = new List<Meter>();

	private List<Instrument> _instruments = new List<Instrument>();

	internal bool Disposed { get; private set; }

	public string Name { get; }

	public string? Version { get; }

	public Meter(string name)
		: this(name, null)
	{
	}

	public Meter(string name, string? version)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		Name = name;
		Version = version;
		lock (Instrument.SyncObject)
		{
			s_allMeters.Add(this);
		}
		GC.KeepAlive(MetricsEventSource.Log);
	}

	public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct
	{
		return new Counter<T>(this, name, unit, description);
	}

	public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct
	{
		return new Histogram<T>(this, name, unit, description);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValue, unit, description);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValue, unit, description);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValues, unit, description);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValue, unit, description);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValue, unit, description);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValues, unit, description);
	}

	public void Dispose()
	{
		List<Instrument> list = null;
		lock (Instrument.SyncObject)
		{
			if (Disposed)
			{
				return;
			}
			Disposed = true;
			s_allMeters.Remove(this);
			list = _instruments;
			_instruments = new List<Instrument>();
		}
		if (list == null)
		{
			return;
		}
		foreach (Instrument item in list)
		{
			item.NotifyForUnpublishedInstrument();
		}
	}

	internal bool AddInstrument(Instrument instrument)
	{
		if (!_instruments.Contains(instrument))
		{
			_instruments.Add(instrument);
			return true;
		}
		return false;
	}

	internal static List<Instrument> GetPublishedInstruments()
	{
		List<Instrument> list = null;
		if (s_allMeters.Count > 0)
		{
			list = new List<Instrument>();
			foreach (Meter s_allMeter in s_allMeters)
			{
				foreach (Instrument instrument in s_allMeter._instruments)
				{
					list.Add(instrument);
				}
			}
		}
		return list;
	}
}
