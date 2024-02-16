using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class HttpListenerException : Win32Exception
{
	public override int ErrorCode => base.NativeErrorCode;

	public HttpListenerException()
		: base(Marshal.GetLastPInvokeError())
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	public HttpListenerException(int errorCode)
		: base(errorCode)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	public HttpListenerException(int errorCode, string message)
		: base(errorCode, message)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}

	protected HttpListenerException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"{base.NativeErrorCode}:{Message}", ".ctor");
		}
	}
}
