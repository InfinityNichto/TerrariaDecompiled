using System.ComponentModel;
using System.ComponentModel.Design;
using System.Threading;

namespace System.Timers;

[DefaultProperty("Interval")]
[DefaultEvent("Elapsed")]
public class Timer : Component, ISupportInitialize
{
	private double _interval;

	private bool _enabled;

	private bool _initializing;

	private bool _delayedEnable;

	private ElapsedEventHandler _onIntervalElapsed;

	private bool _autoReset;

	private ISynchronizeInvoke _synchronizingObject;

	private bool _disposed;

	private System.Threading.Timer _timer;

	private readonly TimerCallback _callback;

	private object _cookie;

	[TimersDescription("TimerAutoReset", null)]
	[DefaultValue(true)]
	public bool AutoReset
	{
		get
		{
			return _autoReset;
		}
		set
		{
			if (base.DesignMode)
			{
				_autoReset = value;
			}
			else if (_autoReset != value)
			{
				_autoReset = value;
				if (_timer != null)
				{
					UpdateTimer();
				}
			}
		}
	}

	[TimersDescription("TimerEnabled", null)]
	[DefaultValue(false)]
	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			if (base.DesignMode)
			{
				_delayedEnable = value;
				_enabled = value;
			}
			else if (_initializing)
			{
				_delayedEnable = value;
			}
			else
			{
				if (_enabled == value)
				{
					return;
				}
				if (!value)
				{
					if (_timer != null)
					{
						_cookie = null;
						_timer.Dispose();
						_timer = null;
					}
					_enabled = value;
					return;
				}
				_enabled = value;
				if (_timer == null)
				{
					if (_disposed)
					{
						throw new ObjectDisposedException(GetType().Name);
					}
					int num = (int)Math.Ceiling(_interval);
					_cookie = new object();
					_timer = new System.Threading.Timer(_callback, _cookie, -1, -1);
					_timer.Change(num, _autoReset ? num : (-1));
				}
				else
				{
					UpdateTimer();
				}
			}
		}
	}

	[TimersDescription("TimerInterval", null)]
	[DefaultValue(100.0)]
	public double Interval
	{
		get
		{
			return _interval;
		}
		set
		{
			if (value <= 0.0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.TimerInvalidInterval, value, 0));
			}
			_interval = value;
			if (_timer != null)
			{
				UpdateTimer();
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
			if (base.DesignMode)
			{
				_enabled = true;
			}
		}
	}

	[DefaultValue(null)]
	[TimersDescription("TimerSynchronizingObject", null)]
	public ISynchronizeInvoke? SynchronizingObject
	{
		get
		{
			if (_synchronizingObject == null && base.DesignMode)
			{
				object obj = ((IDesignerHost)GetService(typeof(IDesignerHost)))?.RootComponent;
				if (obj != null && obj is ISynchronizeInvoke)
				{
					_synchronizingObject = (ISynchronizeInvoke)obj;
				}
			}
			return _synchronizingObject;
		}
		set
		{
			_synchronizingObject = value;
		}
	}

	[TimersDescription("TimerIntervalElapsed", null)]
	public event ElapsedEventHandler Elapsed
	{
		add
		{
			_onIntervalElapsed = (ElapsedEventHandler)Delegate.Combine(_onIntervalElapsed, value);
		}
		remove
		{
			_onIntervalElapsed = (ElapsedEventHandler)Delegate.Remove(_onIntervalElapsed, value);
		}
	}

	public Timer()
	{
		_interval = 100.0;
		_enabled = false;
		_autoReset = true;
		_initializing = false;
		_delayedEnable = false;
		_callback = MyTimerCallback;
	}

	public Timer(double interval)
		: this()
	{
		if (interval <= 0.0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidParameter, "interval", interval));
		}
		double num = Math.Ceiling(interval);
		if (num > 2147483647.0 || num <= 0.0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidParameter, "interval", interval));
		}
		_interval = (int)num;
	}

	private void UpdateTimer()
	{
		int num = (int)Math.Ceiling(_interval);
		_timer.Change(num, _autoReset ? num : (-1));
	}

	public void BeginInit()
	{
		Close();
		_initializing = true;
	}

	public void Close()
	{
		_initializing = false;
		_delayedEnable = false;
		_enabled = false;
		if (_timer != null)
		{
			_timer.Dispose();
			_timer = null;
		}
	}

	protected override void Dispose(bool disposing)
	{
		Close();
		_disposed = true;
		base.Dispose(disposing);
	}

	public void EndInit()
	{
		_initializing = false;
		Enabled = _delayedEnable;
	}

	public void Start()
	{
		Enabled = true;
	}

	public void Stop()
	{
		Enabled = false;
	}

	private void MyTimerCallback(object state)
	{
		if (state != _cookie)
		{
			return;
		}
		if (!_autoReset)
		{
			_enabled = false;
		}
		ElapsedEventArgs elapsedEventArgs = new ElapsedEventArgs(DateTime.Now);
		try
		{
			ElapsedEventHandler onIntervalElapsed = _onIntervalElapsed;
			if (onIntervalElapsed != null)
			{
				if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
				{
					SynchronizingObject.BeginInvoke(onIntervalElapsed, new object[2] { this, elapsedEventArgs });
				}
				else
				{
					onIntervalElapsed(this, elapsedEventArgs);
				}
			}
		}
		catch
		{
		}
	}
}
