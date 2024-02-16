using System.Runtime.InteropServices;

namespace System.Net.Security;

[StructLayout(LayoutKind.Auto)]
internal struct SecurityBuffer
{
	public int offset;

	public int size;

	public System.Net.Security.SecurityBufferType type;

	public byte[] token;

	public SafeHandle unmanagedToken;

	public SecurityBuffer(byte[] data, int offset, int size, System.Net.Security.SecurityBufferType tokentype)
	{
		this.offset = ((data != null && offset >= 0) ? Math.Min(offset, data.Length) : 0);
		this.size = ((data != null && size >= 0) ? Math.Min(size, data.Length - this.offset) : 0);
		type = tokentype;
		token = ((size == 0) ? null : data);
		unmanagedToken = null;
	}

	public SecurityBuffer(byte[] data, System.Net.Security.SecurityBufferType tokentype)
	{
		offset = 0;
		size = ((data != null) ? data.Length : 0);
		type = tokentype;
		token = ((size == 0) ? null : data);
		unmanagedToken = null;
	}

	public SecurityBuffer(int size, System.Net.Security.SecurityBufferType tokentype)
	{
		offset = 0;
		this.size = size;
		type = tokentype;
		token = ((size == 0) ? null : new byte[size]);
		unmanagedToken = null;
	}
}
