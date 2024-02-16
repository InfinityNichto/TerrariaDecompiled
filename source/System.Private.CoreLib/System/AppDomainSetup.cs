namespace System;

public sealed class AppDomainSetup
{
	public string? ApplicationBase => AppContext.BaseDirectory;

	public string? TargetFrameworkName => AppContext.TargetFrameworkName;

	internal AppDomainSetup()
	{
	}
}
