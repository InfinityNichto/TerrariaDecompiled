namespace System.ComponentModel;

[Designer("System.Windows.Forms.Design.ComponentDocumentDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.ComponentModel.Design.IRootDesigner, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
[DesignerCategory("Component")]
[TypeConverter(typeof(ComponentConverter))]
public class MarshalByValueComponent : IComponent, IDisposable, IServiceProvider
{
	private static readonly object s_eventDisposed = new object();

	private ISite _site;

	private EventHandlerList _events;

	protected EventHandlerList Events => _events ?? (_events = new EventHandlerList());

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
	public virtual IContainer? Container => _site?.Container;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual bool DesignMode => _site?.DesignMode ?? false;

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

	~MarshalByValueComponent()
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
		if (disposing)
		{
			lock (this)
			{
				_site?.Container?.Remove(this);
				((EventHandler)(_events?[s_eventDisposed]))?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public virtual object? GetService(Type service)
	{
		return _site?.GetService(service);
	}

	public override string? ToString()
	{
		ISite site = _site;
		if (site != null)
		{
			return site.Name + " [" + GetType().FullName + "]";
		}
		return GetType().FullName;
	}
}
