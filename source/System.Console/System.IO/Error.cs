namespace System.IO;

internal static class Error
{
	internal static Exception GetFileNotOpen()
	{
		return new ObjectDisposedException(null, System.SR.ObjectDisposed_FileClosed);
	}

	internal static Exception GetReadNotSupported()
	{
		return new NotSupportedException(System.SR.NotSupported_UnreadableStream);
	}

	internal static Exception GetSeekNotSupported()
	{
		return new NotSupportedException(System.SR.NotSupported_UnseekableStream);
	}

	internal static Exception GetWriteNotSupported()
	{
		return new NotSupportedException(System.SR.NotSupported_UnwritableStream);
	}
}
