using System.ComponentModel;

namespace System.Net.Sockets;

[Flags]
public enum SocketInformationOptions
{
	NonBlocking = 1,
	Connected = 2,
	Listening = 4,
	[Obsolete("SocketInformationOptions.UseOnlyOverlappedIO has been deprecated and is not supported.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	UseOnlyOverlappedIO = 8
}
