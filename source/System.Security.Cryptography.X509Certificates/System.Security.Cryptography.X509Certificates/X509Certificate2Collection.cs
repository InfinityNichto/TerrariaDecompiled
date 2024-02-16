using System.Collections;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography.X509Certificates.Asn1;
using Internal.Cryptography;
using Internal.Cryptography.Pal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

public class X509Certificate2Collection : X509CertificateCollection, IEnumerable<X509Certificate2>, IEnumerable
{
	public new X509Certificate2 this[int index]
	{
		get
		{
			return (X509Certificate2)base[index];
		}
		set
		{
			base[index] = value;
		}
	}

	public X509Certificate2Collection()
	{
	}

	public X509Certificate2Collection(X509Certificate2 certificate)
	{
		Add(certificate);
	}

	public X509Certificate2Collection(X509Certificate2[] certificates)
	{
		AddRange(certificates);
	}

	public X509Certificate2Collection(X509Certificate2Collection certificates)
	{
		AddRange(certificates);
	}

	public int Add(X509Certificate2 certificate)
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		return Add((X509Certificate)certificate);
	}

	public void AddRange(X509Certificate2[] certificates)
	{
		if (certificates == null)
		{
			throw new ArgumentNullException("certificates");
		}
		int i = 0;
		try
		{
			for (; i < certificates.Length; i++)
			{
				Add(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Remove(certificates[j]);
			}
			throw;
		}
	}

	public void AddRange(X509Certificate2Collection certificates)
	{
		if (certificates == null)
		{
			throw new ArgumentNullException("certificates");
		}
		int i = 0;
		try
		{
			for (; i < certificates.Count; i++)
			{
				Add(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Remove(certificates[j]);
			}
			throw;
		}
	}

	public bool Contains(X509Certificate2 certificate)
	{
		return Contains((X509Certificate)certificate);
	}

	public byte[]? Export(X509ContentType contentType)
	{
		return Export(contentType, null);
	}

	public byte[]? Export(X509ContentType contentType, string? password)
	{
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		using IExportPal exportPal = StorePal.LinkFromCertificateCollection(this);
		return exportPal.Export(contentType, password2);
	}

	public X509Certificate2Collection Find(X509FindType findType, object findValue, bool validOnly)
	{
		if (findValue == null)
		{
			throw new ArgumentNullException("findValue");
		}
		return FindPal.FindFromCollection(this, findType, findValue, validOnly);
	}

	public new X509Certificate2Enumerator GetEnumerator()
	{
		return new X509Certificate2Enumerator(this);
	}

	IEnumerator<X509Certificate2> IEnumerable<X509Certificate2>.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Import(byte[] rawData)
	{
		if (rawData == null)
		{
			throw new ArgumentNullException("rawData");
		}
		Import(rawData.AsSpan());
	}

	public void Import(ReadOnlySpan<byte> rawData)
	{
		Import(rawData, null);
	}

	public void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		if (rawData == null)
		{
			throw new ArgumentNullException("rawData");
		}
		Import(rawData.AsSpan(), password.AsSpan(), keyStorageFlags);
	}

	public void Import(ReadOnlySpan<byte> rawData, string? password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		Import(rawData, password.AsSpan(), keyStorageFlags);
	}

	public void Import(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		if (rawData == null)
		{
			throw new ArgumentNullException("rawData");
		}
		X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		using ILoaderPal loaderPal = StorePal.FromBlob(rawData, password2, keyStorageFlags);
		loaderPal.MoveTo(this);
	}

	public void Import(string fileName)
	{
		Import(fileName, null);
	}

	public void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		using ILoaderPal loaderPal = StorePal.FromFile(fileName, password2, keyStorageFlags);
		loaderPal.MoveTo(this);
	}

	public void Import(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		X509Certificate.ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		using ILoaderPal loaderPal = StorePal.FromFile(fileName, password2, keyStorageFlags);
		loaderPal.MoveTo(this);
	}

	public void Insert(int index, X509Certificate2 certificate)
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		Insert(index, (X509Certificate)certificate);
	}

	public void Remove(X509Certificate2 certificate)
	{
		if (certificate == null)
		{
			throw new ArgumentNullException("certificate");
		}
		Remove((X509Certificate)certificate);
	}

	public void RemoveRange(X509Certificate2[] certificates)
	{
		if (certificates == null)
		{
			throw new ArgumentNullException("certificates");
		}
		int i = 0;
		try
		{
			for (; i < certificates.Length; i++)
			{
				Remove(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Add(certificates[j]);
			}
			throw;
		}
	}

	public void RemoveRange(X509Certificate2Collection certificates)
	{
		if (certificates == null)
		{
			throw new ArgumentNullException("certificates");
		}
		int i = 0;
		try
		{
			for (; i < certificates.Count; i++)
			{
				Remove(certificates[i]);
			}
		}
		catch
		{
			for (int j = 0; j < i; j++)
			{
				Add(certificates[j]);
			}
			throw;
		}
	}

	public void ImportFromPemFile(string certPemFilePath)
	{
		if (certPemFilePath == null)
		{
			throw new ArgumentNullException("certPemFilePath");
		}
		ReadOnlySpan<char> certPem = File.ReadAllText(certPemFilePath);
		ImportFromPem(certPem);
	}

	public void ImportFromPem(ReadOnlySpan<char> certPem)
	{
		int num = 0;
		try
		{
			PemEnumerator.Enumerator enumerator = new PemEnumerator(certPem).GetEnumerator();
			while (enumerator.MoveNext())
			{
				enumerator.Current.Deconstruct(out var contents, out var pemFields);
				ReadOnlySpan<char> readOnlySpan = contents;
				PemFields pemFields2 = pemFields;
				contents = readOnlySpan;
				ReadOnlySpan<char> span = contents[pemFields2.Label];
				if (span.SequenceEqual("CERTIFICATE"))
				{
					byte[] array = GC.AllocateUninitializedArray<byte>(pemFields2.DecodedDataLength);
					contents = readOnlySpan;
					if (!Convert.TryFromBase64Chars(contents[pemFields2.Base64Data], array, out var bytesWritten) || bytesWritten != pemFields2.DecodedDataLength)
					{
						throw new CryptographicException(System.SR.Cryptography_X509_NoPemCertificate);
					}
					try
					{
						CertificateAsn.Decode(array, AsnEncodingRules.DER);
					}
					catch (CryptographicException)
					{
						throw new CryptographicException(System.SR.Cryptography_X509_NoPemCertificate);
					}
					Import(array);
					num++;
				}
			}
		}
		catch
		{
			for (int i = 0; i < num; i++)
			{
				RemoveAt(base.Count - 1);
			}
			throw;
		}
	}
}
