using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net.Sockets;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SocketException : Win32Exception
{
	private readonly SocketError _errorCode;

	public override string Message => base.Message;

	public SocketError SocketErrorCode => _errorCode;

	public override int ErrorCode => base.NativeErrorCode;

	public SocketException(int errorCode)
		: this((SocketError)errorCode)
	{
	}

	internal SocketException(SocketError socketError)
		: base(GetNativeErrorForSocketError(socketError))
	{
		_errorCode = socketError;
	}

	protected SocketException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		if (NetEventSource.Log.IsEnabled())
		{
			NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	public SocketException()
		: this(Marshal.GetLastPInvokeError())
	{
	}

	private static int GetNativeErrorForSocketError(SocketError error)
	{
		return (int)error;
	}
}
