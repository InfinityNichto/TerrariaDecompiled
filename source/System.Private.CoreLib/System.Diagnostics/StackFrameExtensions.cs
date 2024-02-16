namespace System.Diagnostics;

public static class StackFrameExtensions
{
	public static bool HasNativeImage(this StackFrame stackFrame)
	{
		return stackFrame.GetNativeImageBase() != IntPtr.Zero;
	}

	public static bool HasMethod(this StackFrame stackFrame)
	{
		return stackFrame.GetMethod() != null;
	}

	public static bool HasILOffset(this StackFrame stackFrame)
	{
		return stackFrame.GetILOffset() != -1;
	}

	public static bool HasSource(this StackFrame stackFrame)
	{
		return stackFrame.GetFileName() != null;
	}

	public static IntPtr GetNativeIP(this StackFrame stackFrame)
	{
		return IntPtr.Zero;
	}

	public static IntPtr GetNativeImageBase(this StackFrame stackFrame)
	{
		return IntPtr.Zero;
	}
}
