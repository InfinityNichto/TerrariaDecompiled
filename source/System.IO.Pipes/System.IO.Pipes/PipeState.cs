namespace System.IO.Pipes;

internal enum PipeState
{
	WaitingToConnect,
	Connected,
	Broken,
	Disconnected,
	Closed
}
