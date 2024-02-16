namespace System;

public ref struct RuntimeArgumentHandle
{
	private IntPtr m_ptr;

	internal IntPtr Value => m_ptr;
}
