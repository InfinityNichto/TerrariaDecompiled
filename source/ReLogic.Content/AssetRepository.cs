using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReLogic.Content.Readers;
using ReLogic.Content.Sources;

namespace ReLogic.Content;

/// <summary>
/// Async loading has been fully integrated into AssetRepository
/// Assets which are asynchronously loaded will:
///             	- be deserialized on the thread pool
///             	- return to the main thread if the asset can only be created there (for assets requiring GraphicsDevice)
///             	- become loaded at a defined time:
///             		- at the end of a frame or
///             		- when content sources are changing or
///             		- when requested by ImmediateLoad on the main thread
///
/// Assets which require main thread creation, but are requested via ImmediateLoad on a worker thread will:
///             	- be deserialized immediately on the worker thread
///             	- transition to asynchronous loading for creation
///
/// </summary>
public class AssetRepository : IAssetRepository, IDisposable
{
	internal struct ContinuationScheduler
	{
		public readonly IAsset asset;

		public readonly AssetRepository repository;

		internal ContinuationScheduler(IAsset asset, AssetRepository repository)
		{
			this.asset = asset;
			this.repository = repository;
		}

		public void OnCompleted(Action continuation)
		{
			if (asset == null)
			{
				throw new Exception("Main thread transition requested without an asset");
			}
			continuation = continuation.OnlyRunnableOnce();
			repository._assetTransferQueue.Enqueue(continuation);
			asset.Continuation = continuation;
		}
	}

	private static Thread _mainThread;

	protected readonly Dictionary<string, IAsset> _assets = new Dictionary<string, IAsset>();

	private readonly Dictionary<Type, Action<IAsset, AssetRequestMode>> _typeSpecificReloadActions = new Dictionary<Type, Action<IAsset, AssetRequestMode>>();

	protected readonly AssetReaderCollection _readers;

	private readonly object _requestLock = new object();

	internal readonly ConcurrentQueue<Action> _assetTransferQueue = new ConcurrentQueue<Action>();

	private bool _isDisposed;

	private int _Remaining;

	public static bool IsMainThread => Thread.CurrentThread == _mainThread;

	protected IContentSource[] _sources { get; private set; }

	public bool IsDisposed => _isDisposed;

	public int PendingAssets => _Remaining;

	public int TotalAssets { get; private set; }

	public int LoadedAssets { get; private set; }

	public bool IsAsyncLoadingEnabled => true;

	public FailedToLoadAssetCustomAction AssetLoadFailHandler { get; set; }

	public static void SetMainThread()
	{
		if (_mainThread != null)
		{
			throw new InvalidOperationException("Main thread already set");
		}
		_mainThread = Thread.CurrentThread;
	}

	public static void ThrowIfNotMainThread()
	{
		if (!IsMainThread)
		{
			throw new Exception("Must be on main thread");
		}
	}

	private void Invoke(Action action)
	{
		if (_readers == null)
		{
			_assetTransferQueue.Clear();
			return;
		}
		ManualResetEvent evt = new ManualResetEvent(initialState: false);
		_assetTransferQueue.Enqueue(delegate
		{
			action();
			evt.Set();
		});
		evt.WaitOne();
	}

	public IAsset[] GetLoadedAssets()
	{
		lock (_requestLock)
		{
			return _assets.Values.ToArray();
		}
	}

	public AssetRepository(AssetReaderCollection readers, IEnumerable<IContentSource> sources = null)
	{
		_readers = readers;
		_sources = sources?.ToArray() ?? Array.Empty<IContentSource>();
	}

	public virtual void SetSources(IEnumerable<IContentSource> sources, AssetRequestMode mode = AssetRequestMode.ImmediateLoad)
	{
		ThrowIfDisposed();
		ThrowIfNotMainThread();
		lock (_requestLock)
		{
			TransferAllAssets();
			_sources = sources.ToArray();
			ReloadAssetsIfSourceChanged(mode);
			if (mode == AssetRequestMode.ImmediateLoad && _Remaining > 0)
			{
				throw new Exception("Some assets loaded asynchronously, despite AssetRequestMode.ImmediateLoad on main thread");
			}
		}
	}

	internal Asset<T> Request<T>(string assetName) where T : class
	{
		return Request<T>(assetName, AssetRequestMode.ImmediateLoad);
	}

	public virtual Asset<T> Request<T>(string assetName, AssetRequestMode mode = AssetRequestMode.AsyncLoad) where T : class
	{
		if (_readers == null)
		{
			mode = AssetRequestMode.DoNotLoad;
		}
		ThrowIfDisposed();
		assetName = AssetPathHelper.CleanPath(assetName);
		Asset<T> asset = null;
		lock (_requestLock)
		{
			if (_assets.TryGetValue(assetName, out var value))
			{
				asset = value as Asset<T>;
			}
			if (asset == null)
			{
				asset = new Asset<T>(assetName);
				_assets[assetName] = asset;
			}
			if (asset.State == AssetState.NotLoaded)
			{
				EnsureReloadActionExistsForType<T>();
				LoadAsset(asset, mode);
			}
		}
		if (mode == AssetRequestMode.ImmediateLoad)
		{
			asset.Wait();
		}
		return asset;
	}

	public void TransferAllAssets()
	{
		if (!IsMainThread)
		{
			Invoke(TransferAllAssets);
			return;
		}
		while (_Remaining > 0)
		{
			TransferCompletedAssets();
		}
	}

	public void TransferCompletedAssets()
	{
		ThrowIfDisposed();
		ThrowIfNotMainThread();
		lock (_requestLock)
		{
			Action action;
			while (_assetTransferQueue.TryDequeue(out action))
			{
				action();
			}
		}
	}

	private void ReloadAssetsIfSourceChanged(AssetRequestMode mode)
	{
		foreach (IAsset item in _assets.Values.Where((IAsset asset) => asset.IsLoaded))
		{
			IContentSource contentSource = FindSourceForAsset(item.Name);
			if (contentSource == null)
			{
				ForceReloadAsset(item, AssetRequestMode.DoNotLoad);
			}
			else if (item.Source != contentSource)
			{
				ForceReloadAsset(item, mode);
			}
		}
	}

	private void LoadAsset<T>(Asset<T> asset, AssetRequestMode mode) where T : class
	{
		if (mode != 0)
		{
			Task loadTask = LoadAssetWithPotentialAsync(asset, mode);
			asset.Wait = delegate
			{
				SafelyWaitForLoad(asset, loadTask, tracked: true);
			};
		}
	}

	private async Task LoadAssetWithPotentialAsync<T>(Asset<T> asset, AssetRequestMode mode) where T : class
	{
		_ = 3;
		try
		{
			if (!Monitor.IsEntered(_requestLock))
			{
				throw new Exception("Asset load started without holding _requestLock");
			}
			TotalAssets++;
			asset.SetToLoadingState();
			Interlocked.Increment(ref _Remaining);
			new List<string>();
			MainThreadCreationContext mainThreadCtx = new MainThreadCreationContext(new ContinuationScheduler(asset, this));
			IContentSource[] sources = _sources;
			foreach (IContentSource source in sources)
			{
				if (source.Rejections.IsRejected(asset.Name))
				{
					continue;
				}
				string extension = source.GetExtension(asset.Name);
				if (extension == null)
				{
					continue;
				}
				if (!_readers.TryGetReader(extension, out var reader))
				{
					source.Rejections.Reject(asset.Name, new ContentRejectionNoCompatibleReader(extension, _readers.GetSupportedExtensions()));
					continue;
				}
				if (mode == AssetRequestMode.AsyncLoad)
				{
					await Task.Yield();
				}
				if (Monitor.IsEntered(_requestLock) && !IsMainThread)
				{
					await Task.Yield();
				}
				T resultAsset;
				using (Stream stream = source.OpenStream(asset.Name + extension))
				{
					_ = 2;
					try
					{
						resultAsset = await reader.FromStream<T>(stream, mainThreadCtx);
					}
					catch (Exception e2)
					{
						source.Rejections.Reject(asset.Name, new ContentRejectionAssetReaderException(e2));
						continue;
					}
				}
				if (source.ContentValidator != null && !source.ContentValidator.AssetIsValid(resultAsset, asset.Name, out var rejectionReason))
				{
					source.Rejections.Reject(asset.Name, rejectionReason);
					continue;
				}
				await mainThreadCtx;
				if (!Monitor.IsEntered(_requestLock))
				{
					throw new Exception("Asset transfer started without holding _requestLock");
				}
				asset.SubmitLoadedContent(resultAsset, source);
				LoadedAssets++;
				return;
			}
			throw AssetLoadException.FromMissingAsset(asset.Name);
		}
		catch (Exception e)
		{
			AssetLoadFailHandler?.Invoke(asset.Name, e);
			if (mode == AssetRequestMode.ImmediateLoad)
			{
				throw;
			}
		}
		finally
		{
			Interlocked.Decrement(ref _Remaining);
		}
	}

	private void SafelyWaitForLoad<T>(Asset<T> asset, Task loadTask, bool tracked) where T : class
	{
		if (asset.State == AssetState.Loaded)
		{
			return;
		}
		if (!loadTask.IsCompleted && IsMainThread)
		{
			while (asset.Continuation == null)
			{
				Thread.Yield();
			}
			if (tracked)
			{
				lock (_requestLock)
				{
					asset.Continuation();
				}
			}
			else
			{
				asset.Continuation();
			}
			if (!loadTask.IsCompleted)
			{
				throw new Exception("Load task not completed after running continuations on main thread?");
			}
		}
		loadTask.GetAwaiter().GetResult();
		if (asset.State == AssetState.Loaded)
		{
			return;
		}
		throw new Exception("How did you get here?");
	}

	public Asset<T> CreateUntracked<T>(Stream stream, string name, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) where T : class
	{
		string ext = Path.GetExtension(name);
		if (!_readers.TryGetReader(ext, out var reader))
		{
			throw AssetLoadException.FromMissingReader(ext);
		}
		int length = ext.Length;
		Asset<T> asset = new Asset<T>(name.Substring(0, name.Length - length));
		Task loadTask = LoadUntracked(stream, reader, asset, mode);
		asset.Wait = delegate
		{
			SafelyWaitForLoad(asset, loadTask, tracked: false);
		};
		if (mode == AssetRequestMode.ImmediateLoad)
		{
			asset.Wait();
		}
		return asset;
	}

	private async Task LoadUntracked<T>(Stream stream, IAssetReader reader, Asset<T> asset, AssetRequestMode mode) where T : class
	{
		if (mode == AssetRequestMode.AsyncLoad)
		{
			await Task.Yield();
		}
		MainThreadCreationContext mainThreadCtx = new MainThreadCreationContext(new ContinuationScheduler(asset, this));
		asset.SubmitLoadedContent(await reader.FromStream<T>(stream, mainThreadCtx), null);
	}

	private void ForceReloadAsset(IAsset asset, AssetRequestMode mode)
	{
		_typeSpecificReloadActions[asset.GetType()](asset, mode);
	}

	private void EnsureReloadActionExistsForType<T>() where T : class
	{
		_typeSpecificReloadActions[typeof(Asset<T>)] = ForceReloadAsset<T>;
	}

	private void ForceReloadAsset<T>(IAsset asset, AssetRequestMode mode) where T : class
	{
		Asset<T> asset2 = (Asset<T>)asset;
		if (asset.IsLoaded)
		{
			LoadedAssets--;
		}
		if (asset.State != 0)
		{
			TotalAssets--;
		}
		asset2.Unload();
		LoadAsset(asset2, mode);
	}

	protected IContentSource FindSourceForAsset(string assetName)
	{
		return _sources.FirstOrDefault((IContentSource source) => source.HasAsset(assetName)) ?? throw AssetLoadException.FromMissingAsset(assetName);
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException("AssetRepository is disposed.");
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed)
		{
			return;
		}
		if (disposing)
		{
			foreach (KeyValuePair<string, IAsset> asset in _assets)
			{
				asset.Value.Dispose();
			}
		}
		_isDisposed = true;
	}

	public void Dispose()
	{
		if (!IsMainThread)
		{
			Invoke(Dispose);
		}
		else
		{
			Dispose(disposing: true);
		}
	}
}
