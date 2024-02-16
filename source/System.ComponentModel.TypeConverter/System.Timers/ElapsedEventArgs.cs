namespace System.Timers;

public class ElapsedEventArgs : EventArgs
{
	public DateTime SignalTime { get; }

	internal ElapsedEventArgs(DateTime localTime)
	{
		SignalTime = localTime;
	}
}
