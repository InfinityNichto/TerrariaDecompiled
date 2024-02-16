using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace System.Net;

[EventSource(Name = "System.Net.NameResolution")]
internal sealed class NameResolutionTelemetry : EventSource
{
	public static readonly NameResolutionTelemetry Log = new NameResolutionTelemetry();

	private PollingCounter _lookupsRequestedCounter;

	private PollingCounter _currentLookupsCounter;

	private EventCounter _lookupsDuration;

	private long _lookupsRequested;

	private long _currentLookups;

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command != EventCommand.Enable)
		{
			return;
		}
		if (_lookupsRequestedCounter == null)
		{
			_lookupsRequestedCounter = new PollingCounter("dns-lookups-requested", this, () => Interlocked.Read(ref _lookupsRequested))
			{
				DisplayName = "DNS Lookups Requested"
			};
		}
		if (_currentLookupsCounter == null)
		{
			_currentLookupsCounter = new PollingCounter("current-dns-lookups", this, () => Interlocked.Read(ref _currentLookups))
			{
				DisplayName = "Current DNS Lookups"
			};
		}
		if (_lookupsDuration == null)
		{
			_lookupsDuration = new EventCounter("dns-lookups-duration", this)
			{
				DisplayName = "Average DNS Lookup Duration",
				DisplayUnits = "ms"
			};
		}
	}

	[Event(1, Level = EventLevel.Informational)]
	private void ResolutionStart(string hostNameOrAddress)
	{
		WriteEvent(1, hostNameOrAddress);
	}

	[Event(2, Level = EventLevel.Informational)]
	private void ResolutionStop()
	{
		WriteEvent(2);
	}

	[Event(3, Level = EventLevel.Informational)]
	private void ResolutionFailed()
	{
		WriteEvent(3);
	}

	[NonEvent]
	public ValueStopwatch BeforeResolution(object hostNameOrAddress)
	{
		if (IsEnabled())
		{
			Interlocked.Increment(ref _lookupsRequested);
			Interlocked.Increment(ref _currentLookups);
			if (IsEnabled(EventLevel.Informational, EventKeywords.None))
			{
				string text2 = ((hostNameOrAddress is string text) ? text : ((hostNameOrAddress is KeyValuePair<string, AddressFamily> keyValuePair) ? keyValuePair.Key : ((hostNameOrAddress is IPAddress iPAddress) ? iPAddress.ToString() : ((!(hostNameOrAddress is KeyValuePair<IPAddress, AddressFamily> keyValuePair2)) ? null : keyValuePair2.Key.ToString()))));
				string hostNameOrAddress2 = text2;
				ResolutionStart(hostNameOrAddress2);
			}
			return ValueStopwatch.StartNew();
		}
		return default(ValueStopwatch);
	}

	[NonEvent]
	public void AfterResolution(ValueStopwatch stopwatch, bool successful)
	{
		if (!stopwatch.IsActive)
		{
			return;
		}
		Interlocked.Decrement(ref _currentLookups);
		_lookupsDuration.WriteMetric(stopwatch.GetElapsedTime().TotalMilliseconds);
		if (IsEnabled(EventLevel.Informational, EventKeywords.None))
		{
			if (!successful)
			{
				ResolutionFailed();
			}
			ResolutionStop();
		}
	}
}
