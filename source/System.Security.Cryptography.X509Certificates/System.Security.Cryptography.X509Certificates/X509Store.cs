using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public sealed class X509Store : IDisposable
{
	private IStorePal _storePal;

	public IntPtr StoreHandle
	{
		get
		{
			if (_storePal == null)
			{
				throw new CryptographicException(System.SR.Cryptography_X509_StoreNotOpen);
			}
			if (_storePal.SafeHandle == null)
			{
				return IntPtr.Zero;
			}
			return _storePal.SafeHandle.DangerousGetHandle();
		}
	}

	public StoreLocation Location { get; private set; }

	public string? Name { get; private set; }

	public X509Certificate2Collection Certificates
	{
		get
		{
			X509Certificate2Collection x509Certificate2Collection = new X509Certificate2Collection();
			if (_storePal != null)
			{
				_storePal.CloneTo(x509Certificate2Collection);
			}
			return x509Certificate2Collection;
		}
	}

	public bool IsOpen => _storePal != null;

	public X509Store()
		: this("MY", StoreLocation.CurrentUser)
	{
	}

	public X509Store(string storeName)
		: this(storeName, StoreLocation.CurrentUser)
	{
	}

	public X509Store(StoreName storeName)
		: this(storeName, StoreLocation.CurrentUser)
	{
	}

	public X509Store(StoreLocation storeLocation)
		: this("MY", storeLocation)
	{
	}

	public X509Store(StoreName storeName, StoreLocation storeLocation)
	{
		if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "storeLocation"));
		}
		Name = storeName switch
		{
			StoreName.AddressBook => "AddressBook", 
			StoreName.AuthRoot => "AuthRoot", 
			StoreName.CertificateAuthority => "CA", 
			StoreName.Disallowed => "Disallowed", 
			StoreName.My => "My", 
			StoreName.Root => "Root", 
			StoreName.TrustedPeople => "TrustedPeople", 
			StoreName.TrustedPublisher => "TrustedPublisher", 
			_ => throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "storeName")), 
		};
		Location = storeLocation;
	}

	public X509Store(StoreName storeName, StoreLocation storeLocation, OpenFlags flags)
		: this(storeName, storeLocation)
	{
		Open(flags);
	}

	public X509Store(string storeName, StoreLocation storeLocation)
	{
		if (storeLocation != StoreLocation.CurrentUser && storeLocation != StoreLocation.LocalMachine)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_EnumIllegalVal, "storeLocation"));
		}
		Location = storeLocation;
		Name = storeName;
	}

	public X509Store(string storeName, StoreLocation storeLocation, OpenFlags flags)
		: this(storeName, storeLocation)
	{
		Open(flags);
	}

	public X509Store(IntPtr storeHandle)
	{
		_storePal = StorePal.FromHandle(storeHandle);
	}

	public void Open(OpenFlags flags)
	{
		Close();
		_storePal = StorePal.FromSystemStore(Name, Location, flags);
	}

	public void Add(X509Certificate2 certificate)
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		if (_storePal == null)
		{
			throw new CryptographicException(System.SR.Cryptography_X509_StoreNotOpen);
		}
		if (certificate.Pal == null)
		{
			throw new CryptographicException(System.SR.Cryptography_InvalidHandle, "pCertContext");
		}
		_storePal.Add(certificate.Pal);
	}

	public void AddRange(X509Certificate2Collection certificates)
	{
		if (certificates == null)
		{
			throw new ArgumentNullException("certificates");
		}
		int num = 0;
		try
		{
			foreach (X509Certificate2 certificate in certificates)
			{
				Add(certificate);
				num++;
			}
		}
		catch
		{
			for (int i = 0; i < num; i++)
			{
				Remove(certificates[i]);
			}
			throw;
		}
	}

	public void Remove(X509Certificate2 certificate)
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		if (_storePal == null)
		{
			throw new CryptographicException(System.SR.Cryptography_X509_StoreNotOpen);
		}
		if (certificate.Pal != null)
		{
			_storePal.Remove(certificate.Pal);
		}
	}

	public void RemoveRange(X509Certificate2Collection certificates)
	{
		if (certificates == null)
		{
			throw new ArgumentNullException("certificates");
		}
		int num = 0;
		try
		{
			foreach (X509Certificate2 certificate in certificates)
			{
				Remove(certificate);
				num++;
			}
		}
		catch
		{
			for (int i = 0; i < num; i++)
			{
				Add(certificates[i]);
			}
			throw;
		}
	}

	public void Dispose()
	{
		Close();
	}

	public void Close()
	{
		IStorePal storePal = _storePal;
		_storePal = null;
		storePal?.Dispose();
	}
}
