using System.IO;

namespace System.Net.Http;

public sealed class SocketsHttpPlaintextStreamFilterContext
{
	private readonly Stream _plaintextStream;

	private readonly Version _negotiatedHttpVersion;

	private readonly HttpRequestMessage _initialRequestMessage;

	public Stream PlaintextStream => _plaintextStream;

	public Version NegotiatedHttpVersion => _negotiatedHttpVersion;

	public HttpRequestMessage InitialRequestMessage => _initialRequestMessage;

	internal SocketsHttpPlaintextStreamFilterContext(Stream plaintextStream, Version negotiatedHttpVersion, HttpRequestMessage initialRequestMessage)
	{
		_plaintextStream = plaintextStream;
		_negotiatedHttpVersion = negotiatedHttpVersion;
		_initialRequestMessage = initialRequestMessage;
	}
}
