using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security;

[StructLayout(LayoutKind.Auto)]
internal readonly ref struct InputSecurityBuffer
{
	public readonly System.Net.Security.SecurityBufferType Type;

	public readonly ReadOnlySpan<byte> Token;

	public readonly SafeHandle UnmanagedToken;

	public InputSecurityBuffer(ReadOnlySpan<byte> data, System.Net.Security.SecurityBufferType tokentype)
	{
		Token = data;
		Type = tokentype;
		UnmanagedToken = null;
	}

	public InputSecurityBuffer(ChannelBinding binding)
	{
		Type = System.Net.Security.SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS;
		Token = default(ReadOnlySpan<byte>);
		UnmanagedToken = binding;
	}
}
