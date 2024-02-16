using System.IO;
using System.Runtime.Serialization;

namespace System.Net;

public abstract class WebResponse : MarshalByRefObject, ISerializable, IDisposable
{
	public virtual long ContentLength
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual string ContentType
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual bool IsFromCache => false;

	public virtual bool IsMutuallyAuthenticated => false;

	public virtual Uri ResponseUri
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual WebHeaderCollection Headers
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual bool SupportsHeaders => false;

	protected WebResponse()
	{
	}

	protected WebResponse(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	public virtual void Close()
	{
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
			try
			{
				Close();
			}
			catch
			{
			}
		}
	}

	public virtual Stream GetResponseStream()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}
}
