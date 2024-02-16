using System.ComponentModel;

namespace System.Net.NetworkInformation;

public class PingCompletedEventArgs : AsyncCompletedEventArgs
{
	public PingReply? Reply { get; }

	internal PingCompletedEventArgs(PingReply reply, Exception error, bool cancelled, object userToken)
		: base(error, cancelled, userToken)
	{
		Reply = reply;
	}
}
