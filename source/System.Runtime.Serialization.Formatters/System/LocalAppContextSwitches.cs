using System.Runtime.CompilerServices;

namespace System;

internal static class LocalAppContextSwitches
{
	private static int s_binaryFormatterEnabled;

	public static bool BinaryFormatterEnabled
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", ref s_binaryFormatterEnabled);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool GetCachedSwitchValue(string switchName, ref int cachedSwitchValue)
	{
		if (cachedSwitchValue < 0)
		{
			return false;
		}
		if (cachedSwitchValue > 0)
		{
			return true;
		}
		return GetCachedSwitchValueInternal(switchName, ref cachedSwitchValue);
	}

	private static bool GetCachedSwitchValueInternal(string switchName, ref int cachedSwitchValue)
	{
		if (!AppContext.TryGetSwitch(switchName, out var isEnabled))
		{
			isEnabled = GetSwitchDefaultValue(switchName);
		}
		AppContext.TryGetSwitch("TestSwitch.LocalAppContext.DisableCaching", out var isEnabled2);
		if (!isEnabled2)
		{
			cachedSwitchValue = (isEnabled ? 1 : (-1));
		}
		return isEnabled;
	}

	private static bool GetSwitchDefaultValue(string switchName)
	{
		if (switchName == "Switch.System.Runtime.Serialization.SerializationGuard")
		{
			return true;
		}
		if (switchName == "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization")
		{
			return true;
		}
		return false;
	}
}
