using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReLogic.Content.Sources;

public interface IContentSource
{
	IContentValidator ContentValidator { get; set; }

	RejectedAssetCollection Rejections { get; }

	IEnumerable<string> EnumerateAssets();

	/// <summary>
	/// Must be threadsafe! 
	/// </summary>
	/// <returns>null if the asset does not exist</returns>
	string GetExtension(string assetName);

	/// <summary>
	/// Must be threadsafe! 
	/// </summary>
	Stream OpenStream(string fullAssetName);

	/// <summary>
	/// Checks Rejections and GetExtension to determine if an asset exists
	/// </summary>
	/// <param name="assetName"></param>
	/// <returns></returns>
	bool HasAsset(string assetName)
	{
		if (!Rejections.IsRejected(assetName))
		{
			return GetExtension(assetName) != null;
		}
		return false;
	}

	IEnumerable<string> GetAllAssetsStartingWith(string assetNameStart)
	{
		return from s in EnumerateAssets()
			where s.StartsWith(assetNameStart)
			select s;
	}
}
