using System.ComponentModel;

namespace System.Net;

public class DownloadProgressChangedEventArgs : ProgressChangedEventArgs
{
	public long BytesReceived { get; }

	public long TotalBytesToReceive { get; }

	internal DownloadProgressChangedEventArgs(int progressPercentage, object userToken, long bytesReceived, long totalBytesToReceive)
		: base(progressPercentage, userToken)
	{
		BytesReceived = bytesReceived;
		TotalBytesToReceive = totalBytesToReceive;
	}
}
