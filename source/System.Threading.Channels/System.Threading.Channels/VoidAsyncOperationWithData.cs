namespace System.Threading.Channels;

internal sealed class VoidAsyncOperationWithData<TData> : AsyncOperation<VoidResult>
{
	public TData Item { get; set; }

	public VoidAsyncOperationWithData(bool runContinuationsAsynchronously, CancellationToken cancellationToken = default(CancellationToken), bool pooled = false)
		: base(runContinuationsAsynchronously, cancellationToken, pooled)
	{
	}
}
