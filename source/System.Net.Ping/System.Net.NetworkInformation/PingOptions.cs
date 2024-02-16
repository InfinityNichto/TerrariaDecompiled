namespace System.Net.NetworkInformation;

public class PingOptions
{
	private int _ttl;

	private bool _dontFragment;

	public int Ttl
	{
		get
		{
			return _ttl;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_ttl = value;
		}
	}

	public bool DontFragment
	{
		get
		{
			return _dontFragment;
		}
		set
		{
			_dontFragment = value;
		}
	}

	public PingOptions()
	{
		_ttl = 128;
	}

	public PingOptions(int ttl, bool dontFragment)
	{
		if (ttl <= 0)
		{
			throw new ArgumentOutOfRangeException("ttl");
		}
		_ttl = ttl;
		_dontFragment = dontFragment;
	}
}
