using System.Security.Cryptography.X509Certificates;

namespace System.Net;

public class ServicePoint
{
	private int _connectionLeaseTimeout = -1;

	private int _maxIdleTime = 100000;

	private int _receiveBufferSize = -1;

	private int _connectionLimit;

	public BindIPEndPoint? BindIPEndPointDelegate { get; set; }

	public int ConnectionLeaseTimeout
	{
		get
		{
			return _connectionLeaseTimeout;
		}
		set
		{
			if (value < -1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_connectionLeaseTimeout = value;
		}
	}

	public Uri Address { get; }

	public int MaxIdleTime
	{
		get
		{
			return _maxIdleTime;
		}
		set
		{
			if (value < -1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_maxIdleTime = value;
		}
	}

	public bool UseNagleAlgorithm { get; set; }

	public int ReceiveBufferSize
	{
		get
		{
			return _receiveBufferSize;
		}
		set
		{
			if (value < -1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_receiveBufferSize = value;
		}
	}

	public bool Expect100Continue { get; set; }

	public DateTime IdleSince { get; internal set; }

	public virtual Version ProtocolVersion { get; } = new Version(1, 1);


	public string ConnectionName { get; }

	public int ConnectionLimit
	{
		get
		{
			return _connectionLimit;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_connectionLimit = value;
		}
	}

	public int CurrentConnections => 0;

	public X509Certificate? Certificate { get; }

	public X509Certificate? ClientCertificate { get; }

	public bool SupportsPipelining { get; } = true;


	internal ServicePoint(Uri address)
	{
		Address = address;
		ConnectionName = address.Scheme;
	}

	public bool CloseConnectionGroup(string connectionGroupName)
	{
		return true;
	}

	public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval)
	{
		if (enabled)
		{
			if (keepAliveTime <= 0)
			{
				throw new ArgumentOutOfRangeException("keepAliveTime");
			}
			if (keepAliveInterval <= 0)
			{
				throw new ArgumentOutOfRangeException("keepAliveInterval");
			}
		}
	}
}
