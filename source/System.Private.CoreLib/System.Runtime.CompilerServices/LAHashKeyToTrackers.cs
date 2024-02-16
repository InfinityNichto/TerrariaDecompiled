using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Sequential)]
internal sealed class LAHashKeyToTrackers
{
	private object _trackerOrTrackerSet;

	private object _laLocalKeyValueStore;
}
