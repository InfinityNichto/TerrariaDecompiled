using System.Runtime.CompilerServices;

namespace System;

internal static class LocalAppContextSwitches
{
	private static int s_dontThrowOnInvalidSurrogatePairs;

	private static int s_ignoreEmptyKeySequences;

	private static int s_ignoreKindInUtcTimeSerialization;

	private static int s_limitXPathComplexity;

	private static int s_allowDefaultResolver;

	public static bool DontThrowOnInvalidSurrogatePairs
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Xml.DontThrowOnInvalidSurrogatePairs", ref s_dontThrowOnInvalidSurrogatePairs);
		}
	}

	public static bool IgnoreEmptyKeySequences
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Xml.IgnoreEmptyKeySequencess", ref s_ignoreEmptyKeySequences);
		}
	}

	public static bool IgnoreKindInUtcTimeSerialization
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Xml.IgnoreKindInUtcTimeSerialization", ref s_ignoreKindInUtcTimeSerialization);
		}
	}

	public static bool LimitXPathComplexity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Xml.LimitXPathComplexity", ref s_limitXPathComplexity);
		}
	}

	public static bool AllowDefaultResolver
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Xml.AllowDefaultResolver", ref s_allowDefaultResolver);
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
