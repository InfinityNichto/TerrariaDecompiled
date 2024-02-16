namespace System.Net.Sockets;

public class LingerOption
{
	private bool _enabled;

	private int _lingerTime;

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
		}
	}

	public int LingerTime
	{
		get
		{
			return _lingerTime;
		}
		set
		{
			_lingerTime = value;
		}
	}

	public LingerOption(bool enable, int seconds)
	{
		Enabled = enable;
		LingerTime = seconds;
	}
}
