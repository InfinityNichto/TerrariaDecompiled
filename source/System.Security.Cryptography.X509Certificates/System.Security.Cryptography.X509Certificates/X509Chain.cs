using System.Runtime.Versioning;
using Internal.Cryptography.Pal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

public class X509Chain : IDisposable
{
	private X509ChainPolicy _chainPolicy;

	private volatile X509ChainStatus[] _lazyChainStatus;

	private X509ChainElementCollection _chainElements;

	private IChainPal _pal;

	private bool _useMachineContext;

	private readonly object _syncRoot = new object();

	public X509ChainElementCollection ChainElements
	{
		get
		{
			if (_chainElements == null)
			{
				_chainElements = new X509ChainElementCollection();
			}
			return _chainElements;
		}
	}

	public X509ChainPolicy ChainPolicy
	{
		get
		{
			if (_chainPolicy == null)
			{
				_chainPolicy = new X509ChainPolicy();
			}
			return _chainPolicy;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_chainPolicy = value;
		}
	}

	public X509ChainStatus[] ChainStatus
	{
		get
		{
			X509ChainStatus[] array = _lazyChainStatus;
			if (array == null)
			{
				array = (_lazyChainStatus = ((_pal == null) ? Array.Empty<X509ChainStatus>() : _pal.ChainStatus));
			}
			return array;
		}
	}

	public IntPtr ChainContext => SafeHandle?.DangerousGetHandle() ?? IntPtr.Zero;

	public SafeX509ChainHandle? SafeHandle
	{
		get
		{
			if (_pal == null)
			{
				return SafeX509ChainHandle.InvalidHandle;
			}
			return _pal.SafeHandle;
		}
	}

	public X509Chain()
	{
	}

	public X509Chain(bool useMachineContext)
	{
		_useMachineContext = useMachineContext;
	}

	[SupportedOSPlatform("windows")]
	public X509Chain(IntPtr chainContext)
	{
		_pal = ChainPal.FromHandle(chainContext);
		_chainElements = new X509ChainElementCollection(_pal.ChainElements);
	}

	public static X509Chain Create()
	{
		return new X509Chain();
	}

	public bool Build(X509Certificate2 certificate)
	{
		return Build(certificate, throwOnException: true);
	}

	internal bool Build(X509Certificate2 certificate, bool throwOnException)
	{
		lock (_syncRoot)
		{
			if (certificate == null || certificate.Pal == null)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidContextHandle, "certificate");
			}
			if (_chainPolicy != null && _chainPolicy.CustomTrustStore != null)
			{
				if (_chainPolicy.TrustMode == X509ChainTrustMode.System && _chainPolicy.CustomTrustStore.Count > 0)
				{
					throw new CryptographicException(System.SR.Cryptography_CustomTrustCertsInSystemMode, "TrustMode");
				}
				foreach (X509Certificate2 item in _chainPolicy.CustomTrustStore)
				{
					if (item == null || item.Handle == IntPtr.Zero)
					{
						throw new CryptographicException(System.SR.Cryptography_InvalidTrustCertificate, "CustomTrustStore");
					}
				}
			}
			Reset();
			X509ChainPolicy chainPolicy = ChainPolicy;
			_pal = ChainPal.BuildChain(_useMachineContext, certificate.Pal, chainPolicy._extraStore, chainPolicy._applicationPolicy, chainPolicy._certificatePolicy, chainPolicy.RevocationMode, chainPolicy.RevocationFlag, chainPolicy.CustomTrustStore, chainPolicy.TrustMode, chainPolicy.VerificationTime, chainPolicy.UrlRetrievalTimeout, chainPolicy.DisableCertificateDownloads);
			if (_pal == null)
			{
				return false;
			}
			_chainElements = new X509ChainElementCollection(_pal.ChainElements);
			Exception exception;
			bool? flag = _pal.Verify(chainPolicy.VerificationFlags, out exception);
			if (!flag.HasValue)
			{
				if (throwOnException)
				{
					throw exception;
				}
				flag = false;
			}
			return flag.Value;
		}
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
			Reset();
		}
	}

	public void Reset()
	{
		_lazyChainStatus = null;
		_chainElements = null;
		_useMachineContext = false;
		IChainPal pal = _pal;
		_pal = null;
		pal?.Dispose();
	}
}
