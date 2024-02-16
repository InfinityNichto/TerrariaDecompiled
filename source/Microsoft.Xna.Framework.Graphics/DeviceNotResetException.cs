using System;
using System.Runtime.Serialization;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public sealed class DeviceNotResetException : Exception
{
	public DeviceNotResetException()
	{
	}

	public DeviceNotResetException(string message)
		: base(message)
	{
	}

	public DeviceNotResetException(string message, Exception inner)
		: base(message, inner)
	{
	}

	private DeviceNotResetException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
