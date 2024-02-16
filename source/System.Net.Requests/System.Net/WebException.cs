using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class WebException : InvalidOperationException, ISerializable
{
	private readonly WebExceptionStatus _status = WebExceptionStatus.UnknownError;

	private readonly WebResponse _response;

	public WebExceptionStatus Status => _status;

	public WebResponse? Response => _response;

	public WebException()
	{
	}

	public WebException(string? message)
		: base(message)
	{
	}

	public WebException(string? message, Exception? innerException)
		: this(message, innerException, WebExceptionStatus.UnknownError, null)
	{
	}

	public WebException(string? message, WebExceptionStatus status)
		: this(message, null, status, null)
	{
	}

	public WebException(string? message, Exception? innerException, WebExceptionStatus status, WebResponse? response)
		: base(message, innerException)
	{
		_status = status;
		_response = response;
		if (innerException != null)
		{
			base.HResult = innerException.HResult;
		}
	}

	protected WebException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
	}

	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
	}

	internal static Exception CreateCompatibleException(Exception exception)
	{
		if (exception is HttpRequestException ex)
		{
			return new WebException(exception.Message, exception, GetStatusFromException(ex), null);
		}
		if (exception is TaskCanceledException)
		{
			return new WebException(System.SR.net_webstatus_Timeout, null, WebExceptionStatus.Timeout, null);
		}
		return exception;
	}

	private static WebExceptionStatus GetStatusFromExceptionHelper(HttpRequestException ex)
	{
		if (!(ex.InnerException is SocketException { SocketErrorCode: var socketErrorCode }))
		{
			return WebExceptionStatus.UnknownError;
		}
		if (socketErrorCode == SocketError.HostNotFound || socketErrorCode == SocketError.NoData)
		{
			return WebExceptionStatus.NameResolutionFailure;
		}
		return WebExceptionStatus.UnknownError;
	}

	internal static WebExceptionStatus GetStatusFromException(HttpRequestException ex)
	{
		int hResult = ex.HResult;
		if (hResult == -2147012889)
		{
			return WebExceptionStatus.NameResolutionFailure;
		}
		return GetStatusFromExceptionHelper(ex);
	}
}
