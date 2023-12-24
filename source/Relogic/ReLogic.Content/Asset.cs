using System;
using System.Threading;
using ReLogic.Content.Sources;

namespace ReLogic.Content;

public sealed class Asset<T> : IAsset, IDisposable where T : class
{
	public static readonly Asset<T> Empty = new Asset<T>("");

	private T ownValue;

	public static T DefaultValue { get; set; }

	public string Name { get; private set; }

	public bool IsLoaded => State == AssetState.Loaded;

	public AssetState State { get; private set; }

	public bool IsDisposed { get; private set; }

	public IContentSource Source { get; private set; }

	public T Value
	{
		get
		{
			if (!IsLoaded)
			{
				return DefaultValue;
			}
			return ownValue;
		}
	}

	internal Action Continuation { get; set; }

	Action IAsset.Continuation
	{
		set
		{
			Continuation = value;
		}
	}

	public Action Wait { get; internal set; }

	internal Asset(string name)
	{
		State = AssetState.NotLoaded;
		Name = name;
	}

	public static explicit operator T(Asset<T> asset)
	{
		return asset.Value;
	}

	internal void Unload()
	{
		(ownValue as IDisposable)?.Dispose();
		State = AssetState.NotLoaded;
		ownValue = null;
		Source = null;
	}

	internal void SubmitLoadedContent(T value, IContentSource source)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		(ownValue as IDisposable)?.Dispose();
		ownValue = value;
		Source = source;
		Thread.MemoryBarrier();
		State = AssetState.Loaded;
	}

	internal void SetToLoadingState()
	{
		State = AssetState.Loading;
	}

	private void Dispose(bool disposing)
	{
		if (IsDisposed)
		{
			return;
		}
		if (disposing && ownValue != null)
		{
			IDisposable disposable = ownValue as IDisposable;
			if (IsLoaded)
			{
				disposable?.Dispose();
			}
			ownValue = null;
		}
		IsDisposed = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
