using System;
using System.Collections.Generic;
using System.IO;
using ReLogic.Content.Sources;

namespace ReLogic.Content;

public interface IAssetRepository : IDisposable
{
	int PendingAssets { get; }

	int TotalAssets { get; }

	int LoadedAssets { get; }

	FailedToLoadAssetCustomAction AssetLoadFailHandler { get; set; }

	void SetSources(IEnumerable<IContentSource> sources, AssetRequestMode mode = AssetRequestMode.ImmediateLoad);

	Asset<T> Request<T>(string assetName, AssetRequestMode mode = AssetRequestMode.AsyncLoad) where T : class;

	Asset<T> CreateUntracked<T>(Stream stream, string name, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) where T : class;

	void TransferCompletedAssets();

	internal Asset<T> Request<T>(string assetName) where T : class
	{
		return Request<T>(assetName, AssetRequestMode.ImmediateLoad);
	}
}
