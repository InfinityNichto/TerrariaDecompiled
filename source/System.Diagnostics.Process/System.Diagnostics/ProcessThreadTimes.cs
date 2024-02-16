namespace System.Diagnostics;

internal sealed class ProcessThreadTimes
{
	internal long _create;

	internal long _exit;

	internal long _kernel;

	internal long _user;

	public DateTime StartTime => DateTime.FromFileTime(_create);

	public DateTime ExitTime => DateTime.FromFileTime(_exit);

	public TimeSpan PrivilegedProcessorTime => new TimeSpan(_kernel);

	public TimeSpan UserProcessorTime => new TimeSpan(_user);

	public TimeSpan TotalProcessorTime => new TimeSpan(_user + _kernel);
}
