using System.Runtime.CompilerServices;

namespace System;

internal static class LocalAppContextSwitches
{
	private static int s_enableUnsafeUTF7Encoding;

	private static int s_enforceJapaneseEraYearRanges;

	private static int s_formatJapaneseFirstYearAsANumber;

	private static int s_enforceLegacyJapaneseDateParsing;

	private static int s_preserveEventListnerObjectIdentity;

	private static int s_serializationGuard;

	private static int s_showILOffset;

	public static bool EnableUnsafeUTF7Encoding
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("System.Text.Encoding.EnableUnsafeUTF7Encoding", ref s_enableUnsafeUTF7Encoding);
		}
	}

	public static bool EnforceJapaneseEraYearRanges
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Globalization.EnforceJapaneseEraYearRanges", ref s_enforceJapaneseEraYearRanges);
		}
	}

	public static bool FormatJapaneseFirstYearAsANumber
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Globalization.FormatJapaneseFirstYearAsANumber", ref s_formatJapaneseFirstYearAsANumber);
		}
	}

	public static bool EnforceLegacyJapaneseDateParsing
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Globalization.EnforceLegacyJapaneseDateParsing", ref s_enforceLegacyJapaneseDateParsing);
		}
	}

	public static bool PreserveEventListnerObjectIdentity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Diagnostics.EventSource.PreserveEventListnerObjectIdentity", ref s_preserveEventListnerObjectIdentity);
		}
	}

	public static bool SerializationGuard
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetCachedSwitchValue("Switch.System.Runtime.Serialization.SerializationGuard", ref s_serializationGuard);
		}
	}

	public static bool ShowILOffsets
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return GetDefaultShowILOffsetSetting();
		}
	}

	private static bool GetDefaultShowILOffsetSetting()
	{
		if (s_showILOffset < 0)
		{
			return false;
		}
		if (s_showILOffset > 0)
		{
			return true;
		}
		bool booleanConfig = AppContextConfigHelper.GetBooleanConfig("Switch.System.Diagnostics.StackTrace.ShowILOffsets", defaultValue: false);
		s_showILOffset = (booleanConfig ? 1 : (-1));
		return booleanConfig;
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
