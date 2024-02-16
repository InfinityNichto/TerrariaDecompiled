using System.Runtime.InteropServices;

namespace System;

[StructLayout(LayoutKind.Sequential)]
internal sealed class RuntimeFieldInfoStub : IRuntimeFieldInfo
{
	private object m_keepalive;

	private object m_c;

	private object m_d;

	private int m_b;

	private object m_e;

	private RuntimeFieldHandleInternal m_fieldHandle;

	RuntimeFieldHandleInternal IRuntimeFieldInfo.Value => m_fieldHandle;
}
