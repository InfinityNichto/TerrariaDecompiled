namespace System.Net;

internal static class ExceptionCheck
{
	internal static bool IsFatal(Exception exception)
	{
		return exception is OutOfMemoryException;
	}
}
