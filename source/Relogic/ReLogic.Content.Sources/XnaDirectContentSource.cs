using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReLogic.Content.Sources;

public class XnaDirectContentSource : ContentSource
{
	private readonly string[] _rootDirectories;

	public XnaDirectContentSource(IEnumerable<string> rootDirectories)
	{
		_rootDirectories = rootDirectories.Select(AssetPathHelper.CleanPath).ToArray();
		SetAssetNames(_rootDirectories.SelectMany((string rootDir) => from path in Directory.GetFiles(rootDir, "*.xnb", SearchOption.AllDirectories)
			select path.Substring(rootDir.Length + 1)).ToHashSet());
	}

	public override Stream OpenStream(string assetName)
	{
		try
		{
			return File.OpenRead(_rootDirectories.Select((string rootDir) => Path.Combine(rootDir, assetName)).First(File.Exists));
		}
		catch (Exception innerException)
		{
			throw AssetLoadException.FromMissingAsset(assetName, innerException);
		}
	}
}
