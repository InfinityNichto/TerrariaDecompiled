using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes;

public sealed class AnonymousPipeClientStream : PipeStream
{
	public override PipeTransmissionMode TransmissionMode => PipeTransmissionMode.Byte;

	public override PipeTransmissionMode ReadMode
	{
		set
		{
			CheckPipePropertyOperations();
			switch (value)
			{
			default:
				throw new ArgumentOutOfRangeException("value", System.SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
			case PipeTransmissionMode.Message:
				throw new NotSupportedException(System.SR.NotSupported_AnonymousPipeMessagesNotSupported);
			case PipeTransmissionMode.Byte:
				break;
			}
		}
	}

	public AnonymousPipeClientStream(string pipeHandleAsString)
		: this(PipeDirection.In, pipeHandleAsString)
	{
	}

	public AnonymousPipeClientStream(PipeDirection direction, string pipeHandleAsString)
		: base(direction, 0)
	{
		if (direction == PipeDirection.InOut)
		{
			throw new NotSupportedException(System.SR.NotSupported_AnonymousPipeUnidirectional);
		}
		if (pipeHandleAsString == null)
		{
			throw new ArgumentNullException("pipeHandleAsString");
		}
		long result = 0L;
		if (!long.TryParse(pipeHandleAsString, out result))
		{
			throw new ArgumentException(System.SR.Argument_InvalidHandle, "pipeHandleAsString");
		}
		SafePipeHandle safePipeHandle = new SafePipeHandle((IntPtr)result, ownsHandle: true);
		if (safePipeHandle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Argument_InvalidHandle, "pipeHandleAsString");
		}
		Init(direction, safePipeHandle);
	}

	public AnonymousPipeClientStream(PipeDirection direction, SafePipeHandle safePipeHandle)
		: base(direction, 0)
	{
		if (direction == PipeDirection.InOut)
		{
			throw new NotSupportedException(System.SR.NotSupported_AnonymousPipeUnidirectional);
		}
		if (safePipeHandle == null)
		{
			throw new ArgumentNullException("safePipeHandle");
		}
		if (safePipeHandle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Argument_InvalidHandle, "safePipeHandle");
		}
		Init(direction, safePipeHandle);
	}

	private void Init(PipeDirection direction, SafePipeHandle safePipeHandle)
	{
		ValidateHandleIsPipe(safePipeHandle);
		InitializeHandle(safePipeHandle, isExposed: true, isAsync: false);
		base.State = PipeState.Connected;
	}

	~AnonymousPipeClientStream()
	{
		Dispose(disposing: false);
	}
}
