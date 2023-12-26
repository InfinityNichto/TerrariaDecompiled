using System;
using System.Diagnostics;
using System.IO;
using ReLogic.OS.Base;

namespace ReLogic.OS.Linux;

internal class PathService : ReLogic.OS.Base.PathService
{
	public override string GetStoragePath()
	{
		string text = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
		if (string.IsNullOrEmpty(text))
		{
			text = Environment.GetEnvironmentVariable("HOME");
			if (string.IsNullOrEmpty(text))
			{
				return ".";
			}
			text += "/.local/share";
		}
		return text;
	}

	public override void OpenURL(string url)
	{
		Process.Start("xdg-open", "\"" + url + "\"");
	}

	public override bool MoveToRecycleBin(string path)
	{
		File.Delete(path);
		return true;
	}
}
