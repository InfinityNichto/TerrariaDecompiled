using System;

namespace ReLogic.Peripherals.RGB;

[Serializable]
public class DeviceInitializationException : Exception
{
	public DeviceInitializationException(string text)
		: base(text)
	{
	}
}
