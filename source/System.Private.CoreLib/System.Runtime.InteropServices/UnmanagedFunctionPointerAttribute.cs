namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
public sealed class UnmanagedFunctionPointerAttribute : Attribute
{
	public bool BestFitMapping;

	public bool SetLastError;

	public bool ThrowOnUnmappableChar;

	public CharSet CharSet;

	public CallingConvention CallingConvention { get; }

	public UnmanagedFunctionPointerAttribute()
	{
		CallingConvention = CallingConvention.Winapi;
	}

	public UnmanagedFunctionPointerAttribute(CallingConvention callingConvention)
	{
		CallingConvention = callingConvention;
	}
}
