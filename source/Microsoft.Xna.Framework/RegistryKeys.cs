namespace Microsoft.Xna.Framework;

internal static class RegistryKeys
{
	internal const string ProductVersion = "v4.0";

	internal const string FrameworkVersion = "v4.0";

	internal const string FrameworkKeyBase = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\XNA\\Framework\\";

	internal const string FrameworkKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\XNA\\Framework\\v4.0";

	internal const string FrameworkNativeLibraryPath = "NativeLibraryPath";

	internal const string GameStudioKeyBase = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\XNA\\Game Studio\\";

	internal const string GameStudioKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\XNA\\Game Studio\\v4.0";

	internal const string GameStudioSubkey = "SOFTWARE\\Microsoft\\XNA\\Game Studio\\v4.0";

	internal const string GameStudioInstalled = "Installed";

	internal const string GameStudioInstallPath = "InstallPath";

	internal const string SharedKeyRoot = "HKEY_LOCAL_MACHINE\\";

	internal const string SharedKeyPart = "SOFTWARE\\Microsoft\\XNA\\Game Studio\\";

	internal const string SharedKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\XNA\\Game Studio\\";

	internal const string SharedKeyPath = "SharedComponentsPath";

	internal const string SharedDeployableRuntimes = "DeployableRuntimes";

	internal const string GS4r1BlockKey = "BlockOutOfOrderInstallGs4r1";

	internal static bool DontRemoveMePlease;
}
