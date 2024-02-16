using System.ComponentModel;

namespace System.Net;

[EditorBrowsable(EditorBrowsableState.Never)]
public class WriteStreamClosedEventArgs : EventArgs
{
	[Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public Exception? Error => null;

	[Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public WriteStreamClosedEventArgs()
	{
	}
}
