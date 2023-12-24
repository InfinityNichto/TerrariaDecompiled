using System;

namespace ReLogic.Content;

[Serializable]
public class AssetLoadException : Exception
{
	private AssetLoadException(string text, Exception innerException)
		: base(text, innerException)
	{
	}

	public static AssetLoadException FromMissingAsset(string assetName, Exception innerException = null)
	{
		return new AssetLoadException("Asset could not be found: \"" + assetName + "\"", innerException);
	}

	public static AssetLoadException FromInvalidReader<TReaderType, TAssetType>()
	{
		return new AssetLoadException("Asset Reader " + typeof(TReaderType).Name + " is unable to read " + typeof(TAssetType).Name, null);
	}

	public static AssetLoadException FromMissingReader(string extension)
	{
		return new AssetLoadException("Unable to find asset reader for type " + extension + ".", null);
	}
}
