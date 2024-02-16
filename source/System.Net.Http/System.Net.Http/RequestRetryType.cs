namespace System.Net.Http;

internal enum RequestRetryType
{
	NoRetry,
	RetryOnConnectionFailure,
	RetryOnLowerHttpVersion,
	RetryOnNextProxy,
	RetryOnStreamLimitReached
}
