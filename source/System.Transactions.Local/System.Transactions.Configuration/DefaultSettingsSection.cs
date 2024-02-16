using System.Runtime.CompilerServices;

namespace System.Transactions.Configuration;

internal sealed class DefaultSettingsSection
{
	private static readonly DefaultSettingsSection s_section = new DefaultSettingsSection();

	private static TimeSpan s_timeout = TimeSpan.Parse("00:01:00");

	[CompilerGenerated]
	private string _003CDistributedTransactionManagerName_003Ek__BackingField = "";

	public TimeSpan Timeout => s_timeout;

	internal static DefaultSettingsSection GetSection()
	{
		return s_section;
	}
}
