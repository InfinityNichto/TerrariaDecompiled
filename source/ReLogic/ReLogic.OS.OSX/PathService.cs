using System;
using System.Diagnostics;
using System.IO;
using ReLogic.OS.Base;

namespace ReLogic.OS.OSX;

internal class PathService : ReLogic.OS.Base.PathService
{
	public override string GetStoragePath()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("HOME");
		if (string.IsNullOrEmpty(environmentVariable))
		{
			return ".";
		}
		return environmentVariable + "/Library/Application Support";
	}

	public override void OpenURL(string url)
	{
		Process.Start("open", "\"" + url + "\"");
	}

	public override bool MoveToRecycleBin(string path)
	{
		File.Delete(path);
		return true;
	}
}
