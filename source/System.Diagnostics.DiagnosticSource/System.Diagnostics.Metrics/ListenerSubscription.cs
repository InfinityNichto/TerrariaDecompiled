namespace System.Diagnostics.Metrics;

internal readonly struct ListenerSubscription
{
	internal MeterListener Listener { get; }

	internal object State { get; }

	internal ListenerSubscription(MeterListener listener, object state = null)
	{
		Listener = listener;
		State = state;
	}
}
