using System.Collections;
using System.Collections.Specialized;

namespace System.ComponentModel.Design;

public class MenuCommand
{
	private readonly EventHandler _execHandler;

	private int _status;

	private IDictionary _properties;

	public virtual bool Checked
	{
		get
		{
			return (_status & 4) != 0;
		}
		set
		{
			SetStatus(4, value);
		}
	}

	public virtual bool Enabled
	{
		get
		{
			return (_status & 2) != 0;
		}
		set
		{
			SetStatus(2, value);
		}
	}

	public virtual IDictionary Properties => _properties ?? (_properties = new HybridDictionary());

	public virtual bool Supported
	{
		get
		{
			return (_status & 1) != 0;
		}
		set
		{
			SetStatus(1, value);
		}
	}

	public virtual bool Visible
	{
		get
		{
			return (_status & 0x10) == 0;
		}
		set
		{
			SetStatus(16, !value);
		}
	}

	public virtual CommandID? CommandID { get; }

	public virtual int OleStatus => _status;

	public event EventHandler? CommandChanged;

	public MenuCommand(EventHandler? handler, CommandID? command)
	{
		_execHandler = handler;
		CommandID = command;
		_status = 3;
	}

	private void SetStatus(int mask, bool value)
	{
		int status = _status;
		status = ((!value) ? (status & ~mask) : (status | mask));
		if (status != _status)
		{
			_status = status;
			OnCommandChanged(EventArgs.Empty);
		}
	}

	public virtual void Invoke()
	{
		if (_execHandler == null)
		{
			return;
		}
		try
		{
			_execHandler(this, EventArgs.Empty);
		}
		catch (CheckoutException ex)
		{
			if (ex == CheckoutException.Canceled)
			{
				return;
			}
			throw;
		}
	}

	public virtual void Invoke(object arg)
	{
		Invoke();
	}

	protected virtual void OnCommandChanged(EventArgs e)
	{
		this.CommandChanged?.Invoke(this, e);
	}

	public override string ToString()
	{
		string text = CommandID?.ToString() + " : ";
		if (((uint)_status & (true ? 1u : 0u)) != 0)
		{
			text += "Supported";
		}
		if (((uint)_status & 2u) != 0)
		{
			text += "|Enabled";
		}
		if ((_status & 0x10) == 0)
		{
			text += "|Visible";
		}
		if (((uint)_status & 4u) != 0)
		{
			text += "|Checked";
		}
		return text;
	}
}
