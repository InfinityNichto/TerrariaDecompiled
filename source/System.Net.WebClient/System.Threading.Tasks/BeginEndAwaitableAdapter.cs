namespace System.Threading.Tasks;

internal sealed class BeginEndAwaitableAdapter : RendezvousAwaitable<IAsyncResult>
{
	public static readonly AsyncCallback Callback = delegate(IAsyncResult asyncResult)
	{
		BeginEndAwaitableAdapter beginEndAwaitableAdapter = (BeginEndAwaitableAdapter)asyncResult.AsyncState;
		beginEndAwaitableAdapter.SetResult(asyncResult);
	};

	public BeginEndAwaitableAdapter()
	{
		base.RunContinuationsAsynchronously = false;
	}
}
