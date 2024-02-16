using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

public enum PosixSignal
{
	SIGHUP = -1,
	SIGINT = -2,
	SIGQUIT = -3,
	SIGTERM = -4,
	[UnsupportedOSPlatform("windows")]
	SIGCHLD = -5,
	[UnsupportedOSPlatform("windows")]
	SIGCONT = -6,
	[UnsupportedOSPlatform("windows")]
	SIGWINCH = -7,
	[UnsupportedOSPlatform("windows")]
	SIGTTIN = -8,
	[UnsupportedOSPlatform("windows")]
	SIGTTOU = -9,
	[UnsupportedOSPlatform("windows")]
	SIGTSTP = -10
}
