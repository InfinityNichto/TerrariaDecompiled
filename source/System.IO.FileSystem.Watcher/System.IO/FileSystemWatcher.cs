using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

public class FileSystemWatcher : Component, ISupportInitialize
{
	private sealed class NormalizedFilterCollection : Collection<string>
	{
		private sealed class ImmutableStringList : IList<string>, ICollection<string>, IEnumerable<string>, IEnumerable
		{
			public string[] Items = Array.Empty<string>();

			public string this[int index]
			{
				get
				{
					string[] items = Items;
					if ((uint)index >= (uint)items.Length)
					{
						throw new ArgumentOutOfRangeException("index");
					}
					return items[index];
				}
				set
				{
					string[] array = (string[])Items.Clone();
					array[index] = value;
					Items = array;
				}
			}

			public int Count => Items.Length;

			public bool IsReadOnly => false;

			public void Add(string item)
			{
				throw new NotSupportedException();
			}

			public void Clear()
			{
				Items = Array.Empty<string>();
			}

			public bool Contains(string item)
			{
				return Array.IndexOf(Items, item) != -1;
			}

			public void CopyTo(string[] array, int arrayIndex)
			{
				Items.CopyTo(array, arrayIndex);
			}

			public IEnumerator<string> GetEnumerator()
			{
				return ((IEnumerable<string>)Items).GetEnumerator();
			}

			public int IndexOf(string item)
			{
				return Array.IndexOf(Items, item);
			}

			public void Insert(int index, string item)
			{
				string[] items = Items;
				string[] array = new string[items.Length + 1];
				items.AsSpan(0, index).CopyTo(array);
				items.AsSpan(index).CopyTo(array.AsSpan(index + 1));
				array[index] = item;
				Items = array;
			}

			public bool Remove(string item)
			{
				throw new NotSupportedException();
			}

			public void RemoveAt(int index)
			{
				string[] items = Items;
				string[] array = new string[items.Length - 1];
				items.AsSpan(0, index).CopyTo(array);
				items.AsSpan(index + 1).CopyTo(array.AsSpan(index));
				Items = array;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		internal NormalizedFilterCollection()
			: base((IList<string>)new ImmutableStringList())
		{
		}

		protected override void InsertItem(int index, string item)
		{
			base.InsertItem(index, (string.IsNullOrEmpty(item) || item == "*.*") ? "*" : item);
		}

		protected override void SetItem(int index, string item)
		{
			base.SetItem(index, (string.IsNullOrEmpty(item) || item == "*.*") ? "*" : item);
		}

		internal string[] GetFilters()
		{
			return ((ImmutableStringList)base.Items).Items;
		}
	}

	private sealed class AsyncReadState
	{
		internal int Session { get; }

		internal byte[] Buffer { get; }

		internal SafeFileHandle DirectoryHandle { get; }

		internal ThreadPoolBoundHandle ThreadPoolBinding { get; }

		internal PreAllocatedOverlapped PreAllocatedOverlapped { get; set; }

		internal WeakReference<FileSystemWatcher> WeakWatcher { get; }

		internal AsyncReadState(int session, byte[] buffer, SafeFileHandle handle, ThreadPoolBoundHandle binding, FileSystemWatcher parent)
		{
			Session = session;
			Buffer = buffer;
			DirectoryHandle = handle;
			ThreadPoolBinding = binding;
			WeakWatcher = new WeakReference<FileSystemWatcher>(parent);
		}
	}

	private readonly NormalizedFilterCollection _filters = new NormalizedFilterCollection();

	private string _directory;

	private NotifyFilters _notifyFilters = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

	private bool _includeSubdirectories;

	private bool _enabled;

	private bool _initializing;

	private uint _internalBufferSize = 8192u;

	private bool _disposed;

	private FileSystemEventHandler _onChangedHandler;

	private FileSystemEventHandler _onCreatedHandler;

	private FileSystemEventHandler _onDeletedHandler;

	private RenamedEventHandler _onRenamedHandler;

	private ErrorEventHandler _onErrorHandler;

	private int _currentSession;

	private SafeFileHandle _directoryHandle;

	public NotifyFilters NotifyFilter
	{
		get
		{
			return _notifyFilters;
		}
		set
		{
			if (((uint)value & 0xFFFFFE80u) != 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidEnumArgument, "value", (int)value, "NotifyFilters"));
			}
			if (_notifyFilters != value)
			{
				_notifyFilters = value;
				Restart();
			}
		}
	}

	public Collection<string> Filters => _filters;

	public bool EnableRaisingEvents
	{
		get
		{
			return _enabled;
		}
		set
		{
			if (_enabled != value)
			{
				if (IsSuspended())
				{
					_enabled = value;
				}
				else if (value)
				{
					StartRaisingEventsIfNotDisposed();
				}
				else
				{
					StopRaisingEvents();
				}
			}
		}
	}

	public string Filter
	{
		get
		{
			if (Filters.Count != 0)
			{
				return Filters[0];
			}
			return "*";
		}
		set
		{
			Filters.Clear();
			Filters.Add(value);
		}
	}

	public bool IncludeSubdirectories
	{
		get
		{
			return _includeSubdirectories;
		}
		set
		{
			if (_includeSubdirectories != value)
			{
				_includeSubdirectories = value;
				Restart();
			}
		}
	}

	public int InternalBufferSize
	{
		get
		{
			return (int)_internalBufferSize;
		}
		set
		{
			if (_internalBufferSize != value)
			{
				if (value < 4096)
				{
					_internalBufferSize = 4096u;
				}
				else
				{
					_internalBufferSize = (uint)value;
				}
				Restart();
			}
		}
	}

	[Editor("System.Diagnostics.Design.FSWPathEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
	public string Path
	{
		get
		{
			return _directory;
		}
		set
		{
			value = ((value == null) ? string.Empty : value);
			if (!string.Equals(_directory, value, System.IO.PathInternal.StringComparison))
			{
				if (value.Length == 0)
				{
					throw new ArgumentException(System.SR.Format(System.SR.InvalidDirName, value), "Path");
				}
				if (!Directory.Exists(value))
				{
					throw new ArgumentException(System.SR.Format(System.SR.InvalidDirName_NotExists, value), "Path");
				}
				_directory = value;
				Restart();
			}
		}
	}

	public override ISite? Site
	{
		get
		{
			return base.Site;
		}
		set
		{
			base.Site = value;
			if (Site != null && Site.DesignMode)
			{
				EnableRaisingEvents = true;
			}
		}
	}

	public ISynchronizeInvoke? SynchronizingObject { get; set; }

	public event FileSystemEventHandler? Changed
	{
		add
		{
			_onChangedHandler = (FileSystemEventHandler)Delegate.Combine(_onChangedHandler, value);
		}
		remove
		{
			_onChangedHandler = (FileSystemEventHandler)Delegate.Remove(_onChangedHandler, value);
		}
	}

	public event FileSystemEventHandler? Created
	{
		add
		{
			_onCreatedHandler = (FileSystemEventHandler)Delegate.Combine(_onCreatedHandler, value);
		}
		remove
		{
			_onCreatedHandler = (FileSystemEventHandler)Delegate.Remove(_onCreatedHandler, value);
		}
	}

	public event FileSystemEventHandler? Deleted
	{
		add
		{
			_onDeletedHandler = (FileSystemEventHandler)Delegate.Combine(_onDeletedHandler, value);
		}
		remove
		{
			_onDeletedHandler = (FileSystemEventHandler)Delegate.Remove(_onDeletedHandler, value);
		}
	}

	public event ErrorEventHandler? Error
	{
		add
		{
			_onErrorHandler = (ErrorEventHandler)Delegate.Combine(_onErrorHandler, value);
		}
		remove
		{
			_onErrorHandler = (ErrorEventHandler)Delegate.Remove(_onErrorHandler, value);
		}
	}

	public event RenamedEventHandler? Renamed
	{
		add
		{
			_onRenamedHandler = (RenamedEventHandler)Delegate.Combine(_onRenamedHandler, value);
		}
		remove
		{
			_onRenamedHandler = (RenamedEventHandler)Delegate.Remove(_onRenamedHandler, value);
		}
	}

	public FileSystemWatcher()
	{
		_directory = string.Empty;
	}

	public FileSystemWatcher(string path)
	{
		CheckPathValidity(path);
		_directory = path;
	}

	public FileSystemWatcher(string path, string filter)
	{
		CheckPathValidity(path);
		_directory = path;
		Filter = filter ?? throw new ArgumentNullException("filter");
	}

	private byte[] AllocateBuffer()
	{
		try
		{
			return new byte[_internalBufferSize];
		}
		catch (OutOfMemoryException)
		{
			throw new OutOfMemoryException(System.SR.Format(System.SR.BufferSizeTooLarge, _internalBufferSize));
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				StopRaisingEvents();
				_onChangedHandler = null;
				_onCreatedHandler = null;
				_onDeletedHandler = null;
				_onRenamedHandler = null;
				_onErrorHandler = null;
			}
			else
			{
				FinalizeDispose();
			}
		}
		finally
		{
			_disposed = true;
			base.Dispose(disposing);
		}
	}

	private static void CheckPathValidity(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidDirName, path), "path");
		}
		if (!Directory.Exists(path))
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidDirName_NotExists, path), "path");
		}
	}

	private bool MatchPattern(ReadOnlySpan<char> relativePath)
	{
		ReadOnlySpan<char> fileName = System.IO.Path.GetFileName(relativePath);
		if (fileName.Length == 0)
		{
			return false;
		}
		string[] filters = _filters.GetFilters();
		if (filters.Length == 0)
		{
			return true;
		}
		string[] array = filters;
		foreach (string text in array)
		{
			if (FileSystemName.MatchesSimpleExpression(text, fileName, !System.IO.PathInternal.IsCaseSensitive))
			{
				return true;
			}
		}
		return false;
	}

	private void NotifyInternalBufferOverflowEvent()
	{
		if (_onErrorHandler != null)
		{
			OnError(new ErrorEventArgs(new InternalBufferOverflowException(System.SR.Format(System.SR.FSW_BufferOverflow, _directory))));
		}
	}

	private void NotifyRenameEventArgs(WatcherChangeTypes action, ReadOnlySpan<char> name, ReadOnlySpan<char> oldName)
	{
		if (_onRenamedHandler != null && (MatchPattern(name) || MatchPattern(oldName)))
		{
			OnRenamed(new RenamedEventArgs(action, _directory, name.IsEmpty ? null : name.ToString(), oldName.IsEmpty ? null : oldName.ToString()));
		}
	}

	private FileSystemEventHandler GetHandler(WatcherChangeTypes changeType)
	{
		return changeType switch
		{
			WatcherChangeTypes.Created => _onCreatedHandler, 
			WatcherChangeTypes.Deleted => _onDeletedHandler, 
			WatcherChangeTypes.Changed => _onChangedHandler, 
			_ => null, 
		};
	}

	private void NotifyFileSystemEventArgs(WatcherChangeTypes changeType, ReadOnlySpan<char> name)
	{
		FileSystemEventHandler handler = GetHandler(changeType);
		if (handler != null && MatchPattern(name.IsEmpty ? ((ReadOnlySpan<char>)_directory) : name))
		{
			InvokeOn(new FileSystemEventArgs(changeType, _directory, name.IsEmpty ? null : name.ToString()), handler);
		}
	}

	protected void OnChanged(FileSystemEventArgs e)
	{
		InvokeOn(e, _onChangedHandler);
	}

	protected void OnCreated(FileSystemEventArgs e)
	{
		InvokeOn(e, _onCreatedHandler);
	}

	protected void OnDeleted(FileSystemEventArgs e)
	{
		InvokeOn(e, _onDeletedHandler);
	}

	private void InvokeOn(FileSystemEventArgs e, FileSystemEventHandler handler)
	{
		if (handler != null)
		{
			ISynchronizeInvoke synchronizingObject = SynchronizingObject;
			if (synchronizingObject != null && synchronizingObject.InvokeRequired)
			{
				synchronizingObject.BeginInvoke(handler, new object[2] { this, e });
			}
			else
			{
				handler(this, e);
			}
		}
	}

	protected void OnError(ErrorEventArgs e)
	{
		ErrorEventHandler onErrorHandler = _onErrorHandler;
		if (onErrorHandler != null)
		{
			ISynchronizeInvoke synchronizingObject = SynchronizingObject;
			if (synchronizingObject != null && synchronizingObject.InvokeRequired)
			{
				synchronizingObject.BeginInvoke(onErrorHandler, new object[2] { this, e });
			}
			else
			{
				onErrorHandler(this, e);
			}
		}
	}

	protected void OnRenamed(RenamedEventArgs e)
	{
		RenamedEventHandler onRenamedHandler = _onRenamedHandler;
		if (onRenamedHandler != null)
		{
			ISynchronizeInvoke synchronizingObject = SynchronizingObject;
			if (synchronizingObject != null && synchronizingObject.InvokeRequired)
			{
				synchronizingObject.BeginInvoke(onRenamedHandler, new object[2] { this, e });
			}
			else
			{
				onRenamedHandler(this, e);
			}
		}
	}

	public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType)
	{
		return WaitForChanged(changeType, -1);
	}

	public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout)
	{
		TaskCompletionSource<WaitForChangedResult> tcs = new TaskCompletionSource<WaitForChangedResult>();
		FileSystemEventHandler fileSystemEventHandler = null;
		RenamedEventHandler renamedEventHandler = null;
		if ((changeType & (WatcherChangeTypes.Created | WatcherChangeTypes.Deleted | WatcherChangeTypes.Changed)) != 0)
		{
			fileSystemEventHandler = delegate(object s, FileSystemEventArgs e)
			{
				if ((e.ChangeType & changeType) != 0)
				{
					tcs.TrySetResult(new WaitForChangedResult(e.ChangeType, e.Name, null, timedOut: false));
				}
			};
			if ((changeType & WatcherChangeTypes.Created) != 0)
			{
				Created += fileSystemEventHandler;
			}
			if ((changeType & WatcherChangeTypes.Deleted) != 0)
			{
				Deleted += fileSystemEventHandler;
			}
			if ((changeType & WatcherChangeTypes.Changed) != 0)
			{
				Changed += fileSystemEventHandler;
			}
		}
		if ((changeType & WatcherChangeTypes.Renamed) != 0)
		{
			renamedEventHandler = delegate(object s, RenamedEventArgs e)
			{
				if ((e.ChangeType & changeType) != 0)
				{
					tcs.TrySetResult(new WaitForChangedResult(e.ChangeType, e.Name, e.OldName, timedOut: false));
				}
			};
			Renamed += renamedEventHandler;
		}
		try
		{
			bool enableRaisingEvents = EnableRaisingEvents;
			if (!enableRaisingEvents)
			{
				EnableRaisingEvents = true;
			}
			tcs.Task.Wait(timeout);
			EnableRaisingEvents = enableRaisingEvents;
		}
		finally
		{
			if (renamedEventHandler != null)
			{
				Renamed -= renamedEventHandler;
			}
			if (fileSystemEventHandler != null)
			{
				if ((changeType & WatcherChangeTypes.Changed) != 0)
				{
					Changed -= fileSystemEventHandler;
				}
				if ((changeType & WatcherChangeTypes.Deleted) != 0)
				{
					Deleted -= fileSystemEventHandler;
				}
				if ((changeType & WatcherChangeTypes.Created) != 0)
				{
					Created -= fileSystemEventHandler;
				}
			}
		}
		if (!tcs.Task.IsCompletedSuccessfully)
		{
			return WaitForChangedResult.TimedOutResult;
		}
		return tcs.Task.Result;
	}

	private void Restart()
	{
		if (!IsSuspended() && _enabled)
		{
			StopRaisingEvents();
			StartRaisingEventsIfNotDisposed();
		}
	}

	private void StartRaisingEventsIfNotDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().Name);
		}
		StartRaisingEvents();
	}

	public void BeginInit()
	{
		bool enabled = _enabled;
		StopRaisingEvents();
		_enabled = enabled;
		_initializing = true;
	}

	public void EndInit()
	{
		_initializing = false;
		if (_directory.Length != 0 && _enabled)
		{
			StartRaisingEvents();
		}
	}

	private bool IsSuspended()
	{
		if (!_initializing)
		{
			return base.DesignMode;
		}
		return true;
	}

	private unsafe void StartRaisingEvents()
	{
		if (IsSuspended())
		{
			_enabled = true;
		}
		else
		{
			if (!IsHandleInvalid(_directoryHandle))
			{
				return;
			}
			_directoryHandle = global::Interop.Kernel32.CreateFile(_directory, 1, FileShare.ReadWrite | FileShare.Delete, FileMode.Open, 1107296256);
			if (IsHandleInvalid(_directoryHandle))
			{
				_directoryHandle = null;
				throw new FileNotFoundException(System.SR.Format(System.SR.FSW_IOError, _directory));
			}
			AsyncReadState asyncReadState;
			try
			{
				int session = Interlocked.Increment(ref _currentSession);
				byte[] array = AllocateBuffer();
				asyncReadState = new AsyncReadState(session, array, _directoryHandle, ThreadPoolBoundHandle.BindHandle(_directoryHandle), this);
				asyncReadState.PreAllocatedOverlapped = new PreAllocatedOverlapped(delegate(uint errorCode, uint numBytes, NativeOverlapped* overlappedPointer)
				{
					AsyncReadState asyncReadState2 = (AsyncReadState)ThreadPoolBoundHandle.GetNativeOverlappedState(overlappedPointer);
					asyncReadState2.ThreadPoolBinding.FreeNativeOverlapped(overlappedPointer);
					if (asyncReadState2.WeakWatcher.TryGetTarget(out var target))
					{
						target.ReadDirectoryChangesCallback(errorCode, numBytes, asyncReadState2);
					}
				}, asyncReadState, array);
			}
			catch
			{
				_directoryHandle.Dispose();
				_directoryHandle = null;
				throw;
			}
			_enabled = true;
			Monitor(asyncReadState);
		}
	}

	private void StopRaisingEvents()
	{
		_enabled = false;
		if (!IsSuspended() && !IsHandleInvalid(_directoryHandle))
		{
			Interlocked.Increment(ref _currentSession);
			_directoryHandle.Dispose();
			_directoryHandle = null;
		}
	}

	private void FinalizeDispose()
	{
		if (!IsHandleInvalid(_directoryHandle))
		{
			_directoryHandle.Dispose();
		}
	}

	private static bool IsHandleInvalid([NotNullWhen(false)] SafeFileHandle handle)
	{
		if (handle != null && !handle.IsInvalid)
		{
			return handle.IsClosed;
		}
		return true;
	}

	private unsafe void Monitor(AsyncReadState state)
	{
		NativeOverlapped* ptr = null;
		bool flag = false;
		try
		{
			if (_enabled && !IsHandleInvalid(state.DirectoryHandle))
			{
				ptr = state.ThreadPoolBinding.AllocateNativeOverlapped(state.PreAllocatedOverlapped);
				flag = global::Interop.Kernel32.ReadDirectoryChangesW(state.DirectoryHandle, state.Buffer, _internalBufferSize, _includeSubdirectories, (uint)_notifyFilters, null, ptr, null);
			}
		}
		catch (ObjectDisposedException)
		{
		}
		catch (ArgumentNullException)
		{
		}
		finally
		{
			if (!flag)
			{
				if (ptr != null)
				{
					state.ThreadPoolBinding.FreeNativeOverlapped(ptr);
				}
				state.PreAllocatedOverlapped.Dispose();
				state.ThreadPoolBinding.Dispose();
				if (!IsHandleInvalid(state.DirectoryHandle))
				{
					OnError(new ErrorEventArgs(new Win32Exception()));
				}
			}
		}
	}

	private void ReadDirectoryChangesCallback(uint errorCode, uint numBytes, AsyncReadState state)
	{
		try
		{
			if (IsHandleInvalid(state.DirectoryHandle))
			{
				return;
			}
			switch (errorCode)
			{
			default:
				OnError(new ErrorEventArgs(new Win32Exception((int)errorCode)));
				EnableRaisingEvents = false;
				break;
			case 995u:
				break;
			case 0u:
				if (state.Session == Volatile.Read(ref _currentSession))
				{
					if (numBytes == 0)
					{
						NotifyInternalBufferOverflowEvent();
					}
					else
					{
						ParseEventBufferAndNotifyForEach(new ReadOnlySpan<byte>(state.Buffer, 0, (int)numBytes));
					}
				}
				break;
			}
		}
		finally
		{
			Monitor(state);
		}
	}

	private unsafe void ParseEventBufferAndNotifyForEach(ReadOnlySpan<byte> buffer)
	{
		ReadOnlySpan<char> oldName = ReadOnlySpan<char>.Empty;
		while (sizeof(global::Interop.Kernel32.FILE_NOTIFY_INFORMATION) <= (uint)buffer.Length)
		{
			ref readonly global::Interop.Kernel32.FILE_NOTIFY_INFORMATION reference = ref MemoryMarshal.AsRef<global::Interop.Kernel32.FILE_NOTIFY_INFORMATION>(buffer);
			if (reference.FileNameLength > (uint)buffer.Length - sizeof(global::Interop.Kernel32.FILE_NOTIFY_INFORMATION))
			{
				break;
			}
			ReadOnlySpan<char> readOnlySpan = MemoryMarshal.Cast<byte, char>(buffer.Slice(sizeof(global::Interop.Kernel32.FILE_NOTIFY_INFORMATION), (int)reference.FileNameLength));
			switch (reference.Action)
			{
			case global::Interop.Kernel32.FileAction.FILE_ACTION_RENAMED_OLD_NAME:
				oldName = readOnlySpan;
				break;
			case global::Interop.Kernel32.FileAction.FILE_ACTION_RENAMED_NEW_NAME:
				NotifyRenameEventArgs(WatcherChangeTypes.Renamed, readOnlySpan, oldName);
				oldName = ReadOnlySpan<char>.Empty;
				break;
			default:
				if (!oldName.IsEmpty)
				{
					NotifyRenameEventArgs(WatcherChangeTypes.Renamed, ReadOnlySpan<char>.Empty, oldName);
					oldName = ReadOnlySpan<char>.Empty;
				}
				switch (reference.Action)
				{
				case global::Interop.Kernel32.FileAction.FILE_ACTION_ADDED:
					NotifyFileSystemEventArgs(WatcherChangeTypes.Created, readOnlySpan);
					break;
				case global::Interop.Kernel32.FileAction.FILE_ACTION_REMOVED:
					NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, readOnlySpan);
					break;
				case global::Interop.Kernel32.FileAction.FILE_ACTION_MODIFIED:
					NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, readOnlySpan);
					break;
				}
				break;
			}
			if (reference.NextEntryOffset == 0 || reference.NextEntryOffset > (uint)buffer.Length)
			{
				break;
			}
			buffer = buffer.Slice((int)reference.NextEntryOffset);
		}
		if (!oldName.IsEmpty)
		{
			NotifyRenameEventArgs(WatcherChangeTypes.Renamed, ReadOnlySpan<char>.Empty, oldName);
		}
	}
}
