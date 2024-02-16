using System.Runtime.InteropServices;

internal static class Interop
{
	internal static class User32
	{
		[DllImport("user32.dll", ExactSpelling = true)]
		[SuppressGCTransition]
		internal static extern uint GetSysColor(int nIndex);
	}
}
