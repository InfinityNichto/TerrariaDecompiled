using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class)]
public class RunInstallerAttribute : Attribute
{
	public static readonly RunInstallerAttribute Yes = new RunInstallerAttribute(runInstaller: true);

	public static readonly RunInstallerAttribute No = new RunInstallerAttribute(runInstaller: false);

	public static readonly RunInstallerAttribute Default = No;

	public bool RunInstaller { get; }

	public RunInstallerAttribute(bool runInstaller)
	{
		RunInstaller = runInstaller;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (obj is RunInstallerAttribute runInstallerAttribute)
		{
			return runInstallerAttribute.RunInstaller == RunInstaller;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool IsDefaultAttribute()
	{
		return Equals(Default);
	}
}
