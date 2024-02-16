using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;

namespace System.Diagnostics.Metrics;

[EventSource(Name = "System.Diagnostics.Metrics")]
internal sealed class MetricsEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Messages = (EventKeywords)1L;

		public const EventKeywords TimeSeriesValues = (EventKeywords)2L;

		public const EventKeywords InstrumentPublishing = (EventKeywords)4L;
	}

	private sealed class CommandHandler
	{
		private AggregationManager _aggregationManager;

		private string _sessionId = "";

		private static readonly char[] s_instrumentSeperators = new char[4] { '\r', '\n', ',', ';' };

		public void OnEventCommand(EventCommandEventArgs command)
		{
			try
			{
				if (OperatingSystem.IsBrowser())
				{
					Log.Error("", "System.Diagnostics.Metrics EventSource not supported on browser");
					return;
				}
				if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable || command.Command == EventCommand.Enable)
				{
					if (_aggregationManager != null)
					{
						if (command.Command == EventCommand.Enable || command.Command == EventCommand.Update)
						{
							Log.MultipleSessionsNotSupportedError(_sessionId);
							return;
						}
						_aggregationManager.Dispose();
						_aggregationManager = null;
						Log.Message("Previous session with id " + _sessionId + " is stopped");
					}
					_sessionId = "";
				}
				if ((command.Command != 0 && command.Command != EventCommand.Enable) || command.Arguments == null)
				{
					return;
				}
				if (command.Arguments.TryGetValue("SessionId", out string value))
				{
					_sessionId = value;
					Log.Message("SessionId argument received: " + _sessionId);
				}
				else
				{
					_sessionId = Guid.NewGuid().ToString();
					Log.Message("New session started. SessionId auto-generated: " + _sessionId);
				}
				double num = 1.0;
				double result = num;
				if (command.Arguments.TryGetValue("RefreshInterval", out string value2))
				{
					Log.Message("RefreshInterval argument received: " + value2);
					if (!double.TryParse(value2, out result))
					{
						Log.Message($"Failed to parse RefreshInterval. Using default {num}s.");
						result = num;
					}
					else if (result < 0.1)
					{
						Log.Message($"RefreshInterval too small. Using minimum interval {0.1} seconds.");
						result = 0.1;
					}
				}
				else
				{
					Log.Message($"No RefreshInterval argument received. Using default {num}s.");
					result = num;
				}
				int num2 = 1000;
				int result2;
				if (command.Arguments.TryGetValue("MaxTimeSeries", out string value3))
				{
					Log.Message("MaxTimeSeries argument received: " + value3);
					if (!int.TryParse(value3, out result2))
					{
						Log.Message($"Failed to parse MaxTimeSeries. Using default {num2}");
						result2 = num2;
					}
				}
				else
				{
					Log.Message($"No MaxTimeSeries argument received. Using default {num2}");
					result2 = num2;
				}
				int num3 = 20;
				int result3;
				if (command.Arguments.TryGetValue("MaxHistograms", out string value4))
				{
					Log.Message("MaxHistograms argument received: " + value4);
					if (!int.TryParse(value4, out result3))
					{
						Log.Message($"Failed to parse MaxHistograms. Using default {num3}");
						result3 = num3;
					}
				}
				else
				{
					Log.Message($"No MaxHistogram argument received. Using default {num3}");
					result3 = num3;
				}
				string sessionId = _sessionId;
				_aggregationManager = new AggregationManager(result2, result3, delegate(Instrument i, LabeledAggregationStatistics s)
				{
					TransmitMetricValue(i, s, sessionId);
				}, delegate(DateTime startIntervalTime, DateTime endIntervalTime)
				{
					Log.CollectionStart(sessionId, startIntervalTime, endIntervalTime);
				}, delegate(DateTime startIntervalTime, DateTime endIntervalTime)
				{
					Log.CollectionStop(sessionId, startIntervalTime, endIntervalTime);
				}, delegate(Instrument i)
				{
					Log.BeginInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description);
				}, delegate(Instrument i)
				{
					Log.EndInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description);
				}, delegate(Instrument i)
				{
					Log.InstrumentPublished(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description);
				}, delegate
				{
					Log.InitialInstrumentEnumerationComplete(sessionId);
				}, delegate(Exception e)
				{
					Log.Error(sessionId, e.ToString());
				}, delegate
				{
					Log.TimeSeriesLimitReached(sessionId);
				}, delegate
				{
					Log.HistogramLimitReached(sessionId);
				}, delegate(Exception e)
				{
					Log.ObservableInstrumentCallbackError(sessionId, e.ToString());
				});
				_aggregationManager.SetCollectionPeriod(TimeSpan.FromSeconds(result));
				if (command.Arguments.TryGetValue("Metrics", out string value5))
				{
					Log.Message("Metrics argument received: " + value5);
					ParseSpecs(value5);
				}
				else
				{
					Log.Message("No Metrics argument received");
				}
				_aggregationManager.Start();
			}
			catch (Exception e2) when (LogError(e2))
			{
			}
		}

		private bool LogError(Exception e)
		{
			Log.Error(_sessionId, e.ToString());
			return false;
		}

		[UnsupportedOSPlatform("browser")]
		private void ParseSpecs(string metricsSpecs)
		{
			if (metricsSpecs == null)
			{
				return;
			}
			string[] array = metricsSpecs.Split(s_instrumentSeperators, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!MetricSpec.TryParse(text, out var spec))
				{
					Log.Message("Failed to parse metric spec: {specString}");
					continue;
				}
				Log.Message("Parsed metric: {spec}");
				if (spec.InstrumentName != null)
				{
					_aggregationManager.Include(spec.MeterName, spec.InstrumentName);
				}
				else
				{
					_aggregationManager.Include(spec.MeterName);
				}
			}
		}

		private void TransmitMetricValue(Instrument instrument, LabeledAggregationStatistics stats, string sessionId)
		{
			if (stats.AggregationStatistics is RateStatistics rateStatistics)
			{
				Log.CounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), rateStatistics.Delta.HasValue ? rateStatistics.Delta.Value.ToString(CultureInfo.InvariantCulture) : "");
			}
			else if (stats.AggregationStatistics is LastValueStatistics lastValueStatistics)
			{
				Log.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), lastValueStatistics.LastValue.HasValue ? lastValueStatistics.LastValue.Value.ToString(CultureInfo.InvariantCulture) : "");
			}
			else if (stats.AggregationStatistics is HistogramStatistics histogramStatistics)
			{
				Log.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), FormatQuantiles(histogramStatistics.Quantiles));
			}
		}

		private string FormatTags(KeyValuePair<string, string>[] labels)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < labels.Length; i++)
			{
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", labels[i].Key, labels[i].Value);
				if (i != labels.Length - 1)
				{
					stringBuilder.Append(',');
				}
			}
			return stringBuilder.ToString();
		}

		private string FormatQuantiles(QuantileValue[] quantiles)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < quantiles.Length; i++)
			{
				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", quantiles[i].Quantile, quantiles[i].Value);
				if (i != quantiles.Length - 1)
				{
					stringBuilder.Append(';');
				}
			}
			return stringBuilder.ToString();
		}
	}

	private class MetricSpec
	{
		public string MeterName { get; private set; }

		public string InstrumentName { get; private set; }

		public MetricSpec(string meterName, string instrumentName)
		{
			MeterName = meterName;
			InstrumentName = instrumentName;
		}

		public static bool TryParse(string text, out MetricSpec spec)
		{
			int num = text.IndexOf('\\');
			if (num == -1)
			{
				spec = new MetricSpec(text.Trim(), null);
				return true;
			}
			string meterName = text.Substring(0, num).Trim();
			string instrumentName = text.Substring(num + 1).Trim();
			spec = new MetricSpec(meterName, instrumentName);
			return true;
		}

		public override string ToString()
		{
			if (InstrumentName == null)
			{
				return MeterName;
			}
			return MeterName + "\\" + InstrumentName;
		}
	}

	public static readonly MetricsEventSource Log = new MetricsEventSource();

	private CommandHandler _handler;

	private MetricsEventSource()
	{
		_handler = new CommandHandler();
	}

	[Event(1, Keywords = (EventKeywords)1L)]
	public void Message(string Message)
	{
		WriteEvent(1, Message);
	}

	[Event(2, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void CollectionStart(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
	{
		WriteEvent(2, sessionId, intervalStartTime, intervalEndTime);
	}

	[Event(3, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void CollectionStop(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
	{
		WriteEvent(3, sessionId, intervalStartTime, intervalEndTime);
	}

	[Event(4, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void CounterRateValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string rate)
	{
		WriteEvent(4, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate);
	}

	[Event(5, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void GaugeValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string lastValue)
	{
		WriteEvent(5, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, lastValue);
	}

	[Event(6, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void HistogramValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string quantiles)
	{
		WriteEvent(6, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, quantiles);
	}

	[Event(7, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void BeginInstrumentReporting(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description)
	{
		WriteEvent(7, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "");
	}

	[Event(8, Keywords = (EventKeywords)2L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void EndInstrumentReporting(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description)
	{
		WriteEvent(8, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "");
	}

	[Event(9, Keywords = (EventKeywords)7L)]
	public void Error(string sessionId, string errorMessage)
	{
		WriteEvent(9, sessionId, errorMessage);
	}

	[Event(10, Keywords = (EventKeywords)6L)]
	public void InitialInstrumentEnumerationComplete(string sessionId)
	{
		WriteEvent(10, sessionId);
	}

	[Event(11, Keywords = (EventKeywords)4L)]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
	public void InstrumentPublished(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description)
	{
		WriteEvent(11, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "");
	}

	[Event(12, Keywords = (EventKeywords)2L)]
	public void TimeSeriesLimitReached(string sessionId)
	{
		WriteEvent(12, sessionId);
	}

	[Event(13, Keywords = (EventKeywords)2L)]
	public void HistogramLimitReached(string sessionId)
	{
		WriteEvent(13, sessionId);
	}

	[Event(14, Keywords = (EventKeywords)2L)]
	public void ObservableInstrumentCallbackError(string sessionId, string errorMessage)
	{
		WriteEvent(14, sessionId, errorMessage);
	}

	[Event(15, Keywords = (EventKeywords)7L)]
	public void MultipleSessionsNotSupportedError(string runningSessionId)
	{
		WriteEvent(15, runningSessionId);
	}

	[NonEvent]
	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		lock (this)
		{
			_handler.OnEventCommand(command);
		}
	}
}
