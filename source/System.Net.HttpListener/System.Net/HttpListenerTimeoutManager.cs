using System.Runtime.Versioning;

namespace System.Net;

public class HttpListenerTimeoutManager
{
	private readonly HttpListener _listener;

	private readonly int[] _timeouts;

	private uint _minSendBytesPerSecond;

	public TimeSpan EntityBody
	{
		get
		{
			return GetTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.EntityBody);
		}
		[SupportedOSPlatform("windows")]
		set
		{
			SetTimespanTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.EntityBody, value);
		}
	}

	public TimeSpan DrainEntityBody
	{
		get
		{
			return GetTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody);
		}
		set
		{
			SetTimespanTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody, value);
		}
	}

	public TimeSpan RequestQueue
	{
		get
		{
			return GetTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue);
		}
		[SupportedOSPlatform("windows")]
		set
		{
			SetTimespanTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue, value);
		}
	}

	public TimeSpan IdleConnection
	{
		get
		{
			return GetTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection);
		}
		set
		{
			SetTimespanTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection, value);
		}
	}

	public TimeSpan HeaderWait
	{
		get
		{
			return GetTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait);
		}
		[SupportedOSPlatform("windows")]
		set
		{
			SetTimespanTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait, value);
		}
	}

	public long MinSendBytesPerSecond
	{
		get
		{
			return _minSendBytesPerSecond;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			if (value < 0 || value > uint.MaxValue)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_listener.SetServerTimeout(_timeouts, (uint)value);
			_minSendBytesPerSecond = (uint)value;
		}
	}

	internal HttpListenerTimeoutManager(HttpListener context)
	{
		_listener = context;
		_timeouts = new int[5];
	}

	private TimeSpan GetTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE type)
	{
		return new TimeSpan(0, 0, _timeouts[(int)type]);
	}

	private void SetTimespanTimeout(global::Interop.HttpApi.HTTP_TIMEOUT_TYPE type, TimeSpan value)
	{
		long num = Convert.ToInt64(value.TotalSeconds);
		if (num < 0 || num > 65535)
		{
			throw new ArgumentOutOfRangeException("value");
		}
		int[] timeouts = _timeouts;
		timeouts[(int)type] = (int)num;
		_listener.SetServerTimeout(timeouts, _minSendBytesPerSecond);
		_timeouts[(int)type] = (int)num;
	}
}
