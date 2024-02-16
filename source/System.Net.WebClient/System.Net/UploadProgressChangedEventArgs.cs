using System.ComponentModel;

namespace System.Net;

public class UploadProgressChangedEventArgs : ProgressChangedEventArgs
{
	public long BytesReceived { get; }

	public long TotalBytesToReceive { get; }

	public long BytesSent { get; }

	public long TotalBytesToSend { get; }

	internal UploadProgressChangedEventArgs(int progressPercentage, object userToken, long bytesSent, long totalBytesToSend, long bytesReceived, long totalBytesToReceive)
		: base(progressPercentage, userToken)
	{
		BytesReceived = bytesReceived;
		TotalBytesToReceive = totalBytesToReceive;
		BytesSent = bytesSent;
		TotalBytesToSend = totalBytesToSend;
	}
}
