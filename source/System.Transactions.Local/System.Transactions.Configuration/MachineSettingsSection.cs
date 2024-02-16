namespace System.Transactions.Configuration;

internal sealed class MachineSettingsSection
{
	private static readonly MachineSettingsSection s_section = new MachineSettingsSection();

	private static TimeSpan s_maxTimeout = TimeSpan.Parse("00:10:00");

	public TimeSpan MaxTimeout => s_maxTimeout;

	internal static MachineSettingsSection GetSection()
	{
		return s_section;
	}
}
