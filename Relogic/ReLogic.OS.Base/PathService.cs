using System;
using System.IO;

namespace ReLogic.OS.Base;

internal abstract class PathService : IPathService
{
	public string ExpandPathVariables(string path)
	{
		return Environment.ExpandEnvironmentVariables(path);
	}

	public abstract string GetStoragePath();

	public string GetStoragePath(string subfolder)
	{
		return Path.Combine(GetStoragePath(), subfolder);
	}

	public abstract void OpenURL(string url);

	public abstract bool MoveToRecycleBin(string path);
}
