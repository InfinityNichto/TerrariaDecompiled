using System.Collections.Generic;
using System.Linq;
using ReLogic.Content.Readers;

namespace ReLogic.Content;

public class AssetReaderCollection
{
	private readonly Dictionary<string, IAssetReader> _readersByExtension = new Dictionary<string, IAssetReader>();

	private string[] _extensions;

	public void RegisterReader(IAssetReader reader, params string[] extensions)
	{
		foreach (string text in extensions)
		{
			_readersByExtension[text.ToLower()] = reader;
		}
		_extensions = _readersByExtension.Keys.ToArray();
	}

	public bool TryGetReader(string extension, out IAssetReader reader)
	{
		return _readersByExtension.TryGetValue(extension.ToLower(), out reader);
	}

	public string[] GetSupportedExtensions()
	{
		return _extensions;
	}
}
