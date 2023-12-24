using ReLogic.Content.Sources;

namespace ReLogic.Content;

public interface IAssetLoader
{
	bool TryLoad<T>(string assetName, IContentSource source, out T resultAsset) where T : class;
}
