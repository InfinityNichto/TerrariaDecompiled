using System;

namespace Microsoft.Xna.Framework;

internal static class FrameworkCallbackLinker
{
	internal static event EventHandler StorageDeviceChanged;

	internal static event EventHandler DownloadCompleted;

	internal static event EventHandler AvatarChanged;

	internal static void OnStorageDeviceChanged(EventArgs args)
	{
		FrameworkCallbackLinker.StorageDeviceChanged?.Invoke(null, args);
	}

	internal static void OnDownloadCompleted(EventArgs args)
	{
		FrameworkCallbackLinker.DownloadCompleted?.Invoke(null, args);
	}

	internal static void OnAvatarChanged(object sender, EventArgs args)
	{
		FrameworkCallbackLinker.AvatarChanged?.Invoke(sender, args);
	}
}
