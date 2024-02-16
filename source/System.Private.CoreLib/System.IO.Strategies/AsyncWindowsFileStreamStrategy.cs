using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal sealed class AsyncWindowsFileStreamStrategy : OSFileStreamStrategy
{
	internal override bool IsAsync => true;

	internal AsyncWindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access)
		: base(handle, access)
	{
	}

	internal AsyncWindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
		: base(path, mode, access, share, options, preallocationSize)
	{
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		if (_fileHandle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		return AsyncModeCopyToAsync(destination, bufferSize, cancellationToken);
	}

	private async Task AsyncModeCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		try
		{
			await FileStreamHelpers.AsyncModeCopyToAsync(_fileHandle, CanSeek, _filePosition, destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (!_fileHandle.IsClosed && CanSeek)
			{
				_filePosition = Length;
			}
		}
	}
}
