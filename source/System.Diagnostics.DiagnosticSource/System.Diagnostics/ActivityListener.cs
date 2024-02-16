namespace System.Diagnostics;

public sealed class ActivityListener : IDisposable
{
	public Action<Activity>? ActivityStarted { get; set; }

	public Action<Activity>? ActivityStopped { get; set; }

	public Func<ActivitySource, bool>? ShouldListenTo { get; set; }

	public SampleActivity<string>? SampleUsingParentId { get; set; }

	public SampleActivity<ActivityContext>? Sample { get; set; }

	public void Dispose()
	{
		ActivitySource.DetachListener(this);
	}
}
