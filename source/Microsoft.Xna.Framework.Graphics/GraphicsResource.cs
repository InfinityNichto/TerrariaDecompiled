using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public abstract class GraphicsResource : IDisposable
{
	private string _localName;

	private object _localTag;

	private protected GraphicsDevice _parent;

	internal ulong _internalHandle;

	internal bool isDisposed;

	private EventHandler<EventArgs> _003Cbacking_store_003EDisposing;

	public bool IsDisposed
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return isDisposed;
		}
	}

	public object Tag
	{
		get
		{
			ulong internalHandle = _internalHandle;
			if (internalHandle != 0)
			{
				return _parent.Resources.GetCachedTag(internalHandle);
			}
			return _localTag;
		}
		set
		{
			ulong internalHandle = _internalHandle;
			if (internalHandle != 0)
			{
				_parent.Resources.SetCachedTag(internalHandle, value);
			}
			else
			{
				_localTag = value;
			}
		}
	}

	public string Name
	{
		get
		{
			ulong internalHandle = _internalHandle;
			if (internalHandle != 0)
			{
				return _parent.Resources.GetCachedName(internalHandle);
			}
			return _localName;
		}
		set
		{
			ulong internalHandle = _internalHandle;
			if (internalHandle != 0)
			{
				_parent.Resources.SetCachedName(internalHandle, value);
			}
			else
			{
				_localName = value;
			}
		}
	}

	public GraphicsDevice GraphicsDevice => _parent;

	[SpecialName]
	public event EventHandler<EventArgs> Disposing
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EDisposing = (EventHandler<EventArgs>)Delegate.Combine(_003Cbacking_store_003EDisposing, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EDisposing = (EventHandler<EventArgs>)Delegate.Remove(_003Cbacking_store_003EDisposing, value);
		}
	}

	internal GraphicsResource()
	{
	}

	public override string ToString()
	{
		ulong internalHandle = _internalHandle;
		string text = ((internalHandle == 0) ? _localName : _parent.Resources.GetCachedName(internalHandle));
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return base.ToString();
	}

	[SpecialName]
	protected void raise_Disposing(object value0, EventArgs value1)
	{
		_003Cbacking_store_003EDisposing?.Invoke(value0, value1);
	}

	private void _0021GraphicsResource()
	{
		isDisposed = true;
	}

	private void _007EGraphicsResource()
	{
		if (!isDisposed)
		{
			_0021GraphicsResource();
			EventArgs empty = EventArgs.Empty;
			_003Cbacking_store_003EDisposing?.Invoke(this, empty);
		}
	}

	[HandleProcessCorruptedStateExceptions]
	protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			_007EGraphicsResource();
			return;
		}
		try
		{
			_0021GraphicsResource();
		}
		finally
		{
			base.Finalize();
		}
	}

	public virtual sealed void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~GraphicsResource()
	{
		Dispose(false);
	}
}
