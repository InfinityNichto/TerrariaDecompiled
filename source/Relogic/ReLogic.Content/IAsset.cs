using System;
using ReLogic.Content.Sources;

namespace ReLogic.Content;

public interface IAsset : IDisposable
{
	AssetState State { get; }

	IContentSource Source { get; }

	string Name { get; }

	bool IsLoaded { get; }

	bool IsDisposed { get; }

	internal Action Continuation { set; }
}
