using System;
using System.Collections.Generic;
using System.IO;

namespace ReLogic.Content.Sources;

public class FileSystemContentSource : ContentSource
{
	private readonly string _basePath;

	private readonly Dictionary<string, string> _nameToAbsolutePath = new Dictionary<string, string>();

	public int FileCount => _nameToAbsolutePath.Count;

	public FileSystemContentSource(string basePath)
	{
		_basePath = Path.GetFullPath(basePath);
		if (!_basePath.EndsWith("/") && !_basePath.EndsWith("\\"))
		{
			_basePath += Path.DirectorySeparatorChar;
		}
		BuildNameToAbsolutePathDictionary();
		SetAssetNames(_nameToAbsolutePath.Keys);
	}

	public override Stream OpenStream(string assetName)
	{
		if (!_nameToAbsolutePath.TryGetValue(assetName, out var value))
		{
			throw AssetLoadException.FromMissingAsset(assetName);
		}
		if (!File.Exists(value))
		{
			throw AssetLoadException.FromMissingAsset(assetName);
		}
		try
		{
			return File.OpenRead(value);
		}
		catch (Exception innerException)
		{
			throw AssetLoadException.FromMissingAsset(assetName, innerException);
		}
	}

	private void BuildNameToAbsolutePathDictionary()
	{
		if (Directory.Exists(_basePath))
		{
			string[] files = Directory.GetFiles(_basePath, "*", SearchOption.AllDirectories);
			for (int i = 0; i < files.Length; i++)
			{
				string fullPath = Path.GetFullPath(files[i]);
				string path = fullPath.Substring(_basePath.Length);
				path = AssetPathHelper.CleanPath(path);
				_nameToAbsolutePath[path] = fullPath;
			}
		}
	}
}
