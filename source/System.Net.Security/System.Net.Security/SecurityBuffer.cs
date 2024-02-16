using System.Runtime.InteropServices;

namespace System.Net.Security;

[StructLayout(LayoutKind.Auto)]
internal struct SecurityBuffer
{
	public int offset;

	public int size;

	public SecurityBufferType type;

	public byte[] token;

	public SafeHandle unmanagedToken;

	public SecurityBuffer(byte[] data, int offset, int size, SecurityBufferType tokentype)
	{
		this.offset = ((data != null && offset >= 0) ? Math.Min(offset, data.Length) : 0);
		this.size = ((data != null && size >= 0) ? Math.Min(size, data.Length - this.offset) : 0);
		type = tokentype;
		token = ((size == 0) ? null : data);
		unmanagedToken = null;
	}

	public SecurityBuffer(byte[] data, SecurityBufferType tokentype)
	{
		offset = 0;
		size = ((data != null) ? data.Length : 0);
		type = tokentype;
		token = ((size == 0) ? null : data);
		unmanagedToken = null;
	}

	public SecurityBuffer(int size, SecurityBufferType tokentype)
	{
		offset = 0;
		this.size = size;
		type = tokentype;
		token = ((size == 0) ? null : new byte[size]);
		unmanagedToken = null;
	}
}
