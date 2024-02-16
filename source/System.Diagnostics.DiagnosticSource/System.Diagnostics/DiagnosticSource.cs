using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics;

public abstract class DiagnosticSource
{
	[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
	public abstract void Write(string name, object? value);

	public abstract bool IsEnabled(string name);

	public virtual bool IsEnabled(string name, object? arg1, object? arg2 = null)
	{
		return IsEnabled(name);
	}

	[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
	public Activity StartActivity(Activity activity, object? args)
	{
		activity.Start();
		Write(activity.OperationName + ".Start", args);
		return activity;
	}

	[RequiresUnreferencedCode("The type of object being written to DiagnosticSource cannot be discovered statically.")]
	public void StopActivity(Activity activity, object? args)
	{
		if (activity.Duration == TimeSpan.Zero)
		{
			activity.SetEndTime(Activity.GetUtcNow());
		}
		Write(activity.OperationName + ".Stop", args);
		activity.Stop();
	}

	public virtual void OnActivityImport(Activity activity, object? payload)
	{
	}

	public virtual void OnActivityExport(Activity activity, object? payload)
	{
	}
}
