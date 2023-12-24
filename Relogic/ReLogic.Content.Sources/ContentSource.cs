using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReLogic.Content.Sources;

public abstract class ContentSource : IContentSource
{
	protected string[] assetPaths;

	protected Dictionary<string, string> assetExtensions = new Dictionary<string, string>();

	public IContentValidator ContentValidator { get; set; }

	public RejectedAssetCollection Rejections { get; } = new RejectedAssetCollection();


	protected void SetAssetNames(IEnumerable<string> paths)
	{
		assetPaths = paths.ToArray();
		assetExtensions.Clear();
		string[] array = assetPaths;
		foreach (string obj in array)
		{
			string ext = Path.GetExtension(obj);
			string text = obj;
			int length = ext.Length;
			string name = AssetPathHelper.CleanPath(text.Substring(0, text.Length - length));
			if (assetExtensions.TryGetValue(name, out var ext2))
			{
				throw new Exception($"Multiple extensions for asset {name}, ({ext}, {ext2})");
			}
			assetExtensions[name] = ext;
		}
	}

	public IEnumerable<string> EnumerateAssets()
	{
		return assetPaths;
	}

	public string GetExtension(string assetName)
	{
		if (!assetExtensions.TryGetValue(AssetPathHelper.CleanPath(assetName), out var ext))
		{
			return null;
		}
		return ext;
	}

	public abstract Stream OpenStream(string fullAssetName);
}
