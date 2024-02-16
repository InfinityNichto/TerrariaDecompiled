namespace System.Transactions.Configuration;

internal static class AppSettings
{
	private static volatile bool s_settingsInitalized;

	private static readonly object s_appSettingsLock = new object();

	private static bool s_includeDistributedTxIdInExceptionMessage;

	internal static bool IncludeDistributedTxIdInExceptionMessage
	{
		get
		{
			EnsureSettingsLoaded();
			return s_includeDistributedTxIdInExceptionMessage;
		}
	}

	private static void EnsureSettingsLoaded()
	{
		if (s_settingsInitalized)
		{
			return;
		}
		lock (s_appSettingsLock)
		{
			if (!s_settingsInitalized)
			{
				s_includeDistributedTxIdInExceptionMessage = false;
				s_settingsInitalized = true;
			}
		}
	}
}
