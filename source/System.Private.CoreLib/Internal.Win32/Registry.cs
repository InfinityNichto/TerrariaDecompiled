using System;

namespace Internal.Win32;

internal static class Registry
{
	public static readonly RegistryKey CurrentUser = RegistryKey.OpenBaseKey((IntPtr)(-2147483647));

	public static readonly RegistryKey LocalMachine = RegistryKey.OpenBaseKey((IntPtr)(-2147483646));
}
