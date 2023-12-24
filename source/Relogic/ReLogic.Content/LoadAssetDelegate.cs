namespace ReLogic.Content;

public delegate void LoadAssetDelegate<T>(bool loadedSuccessfully, T theAsset) where T : class;
