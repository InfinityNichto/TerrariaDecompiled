using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC;

[SupportedOSPlatform("macos")]
[CLSCompliant(false)]
public static class ObjectiveCMarshal
{
	public unsafe delegate delegate* unmanaged<IntPtr, void> UnhandledExceptionPropagationHandler(Exception exception, RuntimeMethodHandle lastMethod, out IntPtr context);

	public enum MessageSendFunction
	{
		MsgSend,
		MsgSendFpret,
		MsgSendStret,
		MsgSendSuper,
		MsgSendSuperStret
	}

	public unsafe static void Initialize(delegate* unmanaged<void> beginEndCallback, delegate* unmanaged<IntPtr, int> isReferencedCallback, delegate* unmanaged<IntPtr, void> trackedObjectEnteredFinalization, UnhandledExceptionPropagationHandler unhandledExceptionPropagationHandler)
	{
		throw new PlatformNotSupportedException();
	}

	public static GCHandle CreateReferenceTrackingHandle(object obj, out Span<IntPtr> taggedMemory)
	{
		throw new PlatformNotSupportedException();
	}

	public static void SetMessageSendCallback(MessageSendFunction msgSendFunction, IntPtr func)
	{
		throw new PlatformNotSupportedException();
	}

	public static void SetMessageSendPendingException(Exception? exception)
	{
		throw new PlatformNotSupportedException();
	}
}
