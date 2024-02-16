namespace System.Net;

internal static class ExceptionHelper
{
	internal static NotSupportedException PropertyNotSupportedException => new NotSupportedException(System.SR.net_PropertyNotSupportedException);

	internal static WebException RequestAbortedException => new WebException(System.SR.net_reqaborted);

	internal static WebException TimeoutException => new WebException(System.SR.net_timeout);
}
