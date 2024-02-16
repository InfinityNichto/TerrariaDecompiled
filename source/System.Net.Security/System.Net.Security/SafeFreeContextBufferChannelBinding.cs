using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security;

internal abstract class SafeFreeContextBufferChannelBinding : ChannelBinding
{
	private int _size;

	public override int Size => _size;

	public override bool IsInvalid
	{
		get
		{
			if (!(handle == new IntPtr(0)))
			{
				return handle == new IntPtr(-1);
			}
			return true;
		}
	}

	internal void Set(IntPtr value)
	{
		handle = value;
	}

	internal static SafeFreeContextBufferChannelBinding CreateEmptyHandle()
	{
		return new SafeFreeContextBufferChannelBinding_SECURITY();
	}

	public unsafe static int QueryContextChannelBinding(SafeDeleteContext phContext, global::Interop.SspiCli.ContextAttribute contextAttribute, SecPkgContext_Bindings* buffer, SafeFreeContextBufferChannelBinding refHandle)
	{
		int num = -2146893055;
		if (contextAttribute != global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_ENDPOINT_BINDINGS && contextAttribute != global::Interop.SspiCli.ContextAttribute.SECPKG_ATTR_UNIQUE_BINDINGS)
		{
			return num;
		}
		try
		{
			bool success = false;
			phContext.DangerousAddRef(ref success);
			num = global::Interop.SspiCli.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
		}
		finally
		{
			phContext.DangerousRelease();
		}
		if (num == 0 && refHandle != null)
		{
			refHandle.Set(buffer->Bindings);
			refHandle._size = buffer->BindingsLength;
		}
		if (num != 0)
		{
			refHandle?.SetHandleAsInvalid();
		}
		return num;
	}

	public override string ToString()
	{
		if (IsInvalid)
		{
			return null;
		}
		byte[] array = new byte[_size];
		Marshal.Copy(handle, array, 0, array.Length);
		return BitConverter.ToString(array).Replace('-', ' ');
	}
}
