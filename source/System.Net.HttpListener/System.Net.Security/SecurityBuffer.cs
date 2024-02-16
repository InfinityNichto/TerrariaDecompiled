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

	public SecurityBuffer(byte[] data, System.Net.Security.SecurityBufferType tokentype)
	{
		offset = 0;
		size = ((data != null) ? data.Length : 0);
		type = tokentype;
		token = ((size == 0) ? null : data);
		unmanagedToken = null;
	}
}
