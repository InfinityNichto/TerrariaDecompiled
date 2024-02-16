namespace System;

internal struct RuntimeMethodHandleInternal
{
	internal IntPtr m_handle;

	internal static RuntimeMethodHandleInternal EmptyHandle => default(RuntimeMethodHandleInternal);

	internal IntPtr Value => m_handle;

	internal bool IsNullHandle()
	{
		return m_handle == IntPtr.Zero;
	}

	internal RuntimeMethodHandleInternal(IntPtr value)
	{
		m_handle = value;
	}
}
