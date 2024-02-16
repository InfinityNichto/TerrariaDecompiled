using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal static class StreamToStreamCopy
{
	public static void Copy(Stream source, Stream destination, int bufferSize, bool disposeSource)
	{
		if (bufferSize == 0)
		{
			source.CopyTo(destination);
		}
		else
		{
			source.CopyTo(destination, bufferSize);
		}
		if (disposeSource)
		{
			DisposeSource(source);
		}
	}

	public static Task CopyAsync(Stream source, Stream destination, int bufferSize, bool disposeSource, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			Task task = ((bufferSize == 0) ? source.CopyToAsync(destination, cancellationToken) : source.CopyToAsync(destination, bufferSize, cancellationToken));
			if (!disposeSource)
			{
				return task;
			}
			switch (task.Status)
			{
			case TaskStatus.RanToCompletion:
				DisposeSource(source);
				return Task.CompletedTask;
			case TaskStatus.Canceled:
			case TaskStatus.Faulted:
				return task;
			default:
				return DisposeSourceAsync(task, source);
			}
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
		static async Task DisposeSourceAsync(Task copyTask, Stream source)
		{
			await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			DisposeSource(source);
		}
	}

	private static void DisposeSource(Stream source)
	{
		try
		{
			source.Dispose();
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(null, message, "DisposeSource");
			}
		}
	}
}
