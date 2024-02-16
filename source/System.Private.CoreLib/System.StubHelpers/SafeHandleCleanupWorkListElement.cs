using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal sealed class SafeHandleCleanupWorkListElement : CleanupWorkListElement
{
	private SafeHandle m_handle;

	private bool m_owned;

	public SafeHandleCleanupWorkListElement(SafeHandle handle)
	{
		m_handle = handle;
	}

	protected override void DestroyCore()
	{
		if (m_owned)
		{
			StubHelpers.SafeHandleRelease(m_handle);
		}
	}

	public IntPtr AddRef()
	{
		return StubHelpers.SafeHandleAddRef(m_handle, ref m_owned);
	}
}
