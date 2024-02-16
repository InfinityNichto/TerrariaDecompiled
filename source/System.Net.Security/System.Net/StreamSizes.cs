namespace System.Net;

internal struct StreamSizes
{
	public int Header { get; private set; }

	public int Trailer { get; private set; }

	public int MaximumMessage { get; private set; }

	public StreamSizes(SecPkgContext_StreamSizes interopStreamSizes)
	{
		Header = interopStreamSizes.cbHeader;
		Trailer = interopStreamSizes.cbTrailer;
		MaximumMessage = interopStreamSizes.cbMaximumMessage;
	}
}
