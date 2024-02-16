namespace System.Threading.Channels;

public sealed class BoundedChannelOptions : ChannelOptions
{
	private int _capacity;

	private BoundedChannelFullMode _mode;

	public int Capacity
	{
		get
		{
			return _capacity;
		}
		set
		{
			if (value < 1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_capacity = value;
		}
	}

	public BoundedChannelFullMode FullMode
	{
		get
		{
			return _mode;
		}
		set
		{
			if ((uint)value <= 3u)
			{
				_mode = value;
				return;
			}
			throw new ArgumentOutOfRangeException("value");
		}
	}

	public BoundedChannelOptions(int capacity)
	{
		if (capacity < 1)
		{
			throw new ArgumentOutOfRangeException("capacity");
		}
		Capacity = capacity;
	}
}
