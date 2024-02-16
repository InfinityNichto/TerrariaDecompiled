using System.Globalization;

namespace System.Net.Http;

internal static class RuntimeSettingParser
{
	public static bool QueryRuntimeSettingSwitch(string appCtxSettingName, string environmentVariableSettingName, bool defaultValue)
	{
		if (AppContext.TryGetSwitch(appCtxSettingName, out var isEnabled))
		{
			return isEnabled;
		}
		string environmentVariable = Environment.GetEnvironmentVariable(environmentVariableSettingName);
		if (bool.TryParse(environmentVariable, out isEnabled))
		{
			return isEnabled;
		}
		if (uint.TryParse(environmentVariable, out var result))
		{
			return result != 0;
		}
		return defaultValue;
	}

	public static int ParseInt32EnvironmentVariableValue(string environmentVariableSettingName, int defaultValue)
	{
		string environmentVariable = Environment.GetEnvironmentVariable(environmentVariableSettingName);
		if (int.TryParse(environmentVariable, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public static double ParseDoubleEnvironmentVariableValue(string environmentVariableSettingName, double defaultValue)
	{
		string environmentVariable = Environment.GetEnvironmentVariable(environmentVariableSettingName);
		if (double.TryParse(environmentVariable, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		return defaultValue;
	}
}
