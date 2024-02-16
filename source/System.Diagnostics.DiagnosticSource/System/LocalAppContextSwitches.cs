using System.Runtime.CompilerServices;

namespace System;

internal static class LocalAppContextSwitches
{
	public static bool DefaultActivityIdFormatIsHierarchial { get; } = InitializeDefaultActivityIdFormat();


	private static bool InitializeDefaultActivityIdFormat()
	{
		bool switchValue = false;
		if (!GetSwitchValue("System.Diagnostics.DefaultActivityIdFormatIsHierarchial", ref switchValue))
		{
			string environmentVariable = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_DIAGNOSTICS_DEFAULTACTIVITYIDFORMATISHIERARCHIAL");
			if (environmentVariable != null)
			{
				switchValue = IsTrueStringIgnoreCase(environmentVariable) || environmentVariable.Equals("1");
			}
		}
		return switchValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsTrueStringIgnoreCase(string value)
	{
		if (value.Length == 4 && (value[0] == 't' || value[0] == 'T') && (value[1] == 'r' || value[1] == 'R') && (value[2] == 'u' || value[2] == 'U'))
		{
			if (value[3] != 'e')
			{
				return value[3] == 'E';
			}
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool GetSwitchValue(string switchName, ref bool switchValue)
	{
		return AppContext.TryGetSwitch(switchName, out switchValue);
	}
}
