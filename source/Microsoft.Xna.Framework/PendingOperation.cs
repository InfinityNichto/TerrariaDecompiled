using System;

namespace Microsoft.Xna.Framework;

internal struct PendingOperation
{
	public XOverlappedAsyncResult Async;

	public AsyncCallback Callback;

	public PendingOperation(XOverlappedAsyncResult async, AsyncCallback callback)
	{
		Async = async;
		Callback = callback;
	}
}
