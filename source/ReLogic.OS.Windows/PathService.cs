using System;
using System.Diagnostics;
using System.IO;
using ReLogic.OS.Base;

namespace ReLogic.OS.Windows;

internal class PathService : ReLogic.OS.Base.PathService
{
	public override string GetStoragePath()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "My Games");
	}

	public override void OpenURL(string url)
	{
		Process.Start("explorer.exe", "\"" + url + "\"");
	}

	public override bool MoveToRecycleBin(string path)
	{
		return NativeMethods.MoveToRecycleBin(path);
	}
}
