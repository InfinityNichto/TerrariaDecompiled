using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net.NetworkInformation;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class NetworkInformationException : Win32Exception
{
	public override int ErrorCode => base.NativeErrorCode;

	public NetworkInformationException()
		: base(Marshal.GetLastWin32Error())
	{
	}

	public NetworkInformationException(int errorCode)
		: base(errorCode)
	{
	}

	protected NetworkInformationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}

	internal NetworkInformationException(SocketError socketError)
		: base((int)socketError)
	{
	}
}
