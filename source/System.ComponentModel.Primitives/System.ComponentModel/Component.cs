namespace System.ComponentModel;

[DesignerCategory("Component")]
public class Component : MarshalByRefObject, IComponent, IDisposable
{
	private static readonly object s_eventDisposed = new object();

	private ISite _site;

	private EventHandlerList _events;

	protected virtual bool CanRaiseEvents => true;

	internal bool CanRaiseEventsInternal => CanRaiseEvents;

	protected EventHandlerList Events => _events ?? (_events = new EventHandlerList(this));

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual ISite? Site
	{
		get
		{
			return _site;
		}
		set
		{
			_site = value;
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IContainer? Container => _site?.Container;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected bool DesignMode => _site?.DesignMode ?? false;

	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public event EventHandler? Disposed
	{
		add
		{
			Events.AddHandler(s_eventDisposed, value);
		}
		remove
		{
			Events.RemoveHandler(s_eventDisposed, value);
		}
	}

	~Component()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}
		lock (this)
		{
			_site?.Container?.Remove(this);
			if (_events != null)
			{
				((EventHandler)_events[s_eventDisposed])?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	protected virtual object? GetService(Type service)
	{
		return _site?.GetService(service);
	}

	public override string ToString()
	{
		ISite site = _site;
		if (site == null)
		{
			return GetType().FullName;
		}
		return site.Name + " [" + GetType().FullName + "]";
	}
}
