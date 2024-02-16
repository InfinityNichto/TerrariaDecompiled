namespace System.IO;

internal static class Error
{
	internal static Exception GetEndOfFile()
	{
		return new EndOfStreamException(System.SR.IO_EOF_ReadBeyondEOF);
	}

	internal static Exception GetPipeNotOpen()
	{
		return new ObjectDisposedException(null, System.SR.ObjectDisposed_PipeClosed);
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

	internal static Exception GetOperationAborted()
	{
		return new IOException(System.SR.IO_OperationAborted_Unexpected);
	}
}
