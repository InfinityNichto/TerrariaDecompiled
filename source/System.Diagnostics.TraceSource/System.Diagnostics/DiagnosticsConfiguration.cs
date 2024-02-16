namespace System.Diagnostics;

internal static class DiagnosticsConfiguration
{
	internal static bool AutoFlush => false;

	internal static bool UseGlobalLock => true;

	internal static int IndentSize => 4;

	internal static bool AssertUIEnabled => true;

	internal static string LogFileName => string.Empty;
}
