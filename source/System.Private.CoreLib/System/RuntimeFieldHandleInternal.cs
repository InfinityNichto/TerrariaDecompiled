namespace System;

internal struct RuntimeFieldHandleInternal
{
	internal IntPtr m_handle;

	internal IntPtr Value => m_handle;

	internal RuntimeFieldHandleInternal(IntPtr value)
	{
		m_handle = value;
	}
}
