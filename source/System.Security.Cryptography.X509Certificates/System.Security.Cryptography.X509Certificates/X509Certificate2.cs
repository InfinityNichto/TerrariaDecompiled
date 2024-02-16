using System.Formats.Asn1;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates.Asn1;
using System.Text;
using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public class X509Certificate2 : X509Certificate
{
	private volatile byte[] _lazyRawData;

	private volatile Oid _lazySignatureAlgorithm;

	private volatile int _lazyVersion;

	private volatile X500DistinguishedName _lazySubjectName;

	private volatile X500DistinguishedName _lazyIssuerName;

	private volatile PublicKey _lazyPublicKey;

	private volatile AsymmetricAlgorithm _lazyPrivateKey;

	private volatile X509ExtensionCollection _lazyExtensions;

	private static readonly string[] s_EcPublicKeyPrivateKeyLabels = new string[2] { "EC PRIVATE KEY", "PRIVATE KEY" };

	private static readonly string[] s_RsaPublicKeyPrivateKeyLabels = new string[2] { "RSA PRIVATE KEY", "PRIVATE KEY" };

	private static readonly string[] s_DsaPublicKeyPrivateKeyLabels = new string[1] { "PRIVATE KEY" };

	internal new ICertificatePal Pal => (ICertificatePal)base.Pal;

	public bool Archived
	{
		get
		{
			ThrowIfInvalid();
			return Pal.Archived;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ThrowIfInvalid();
			Pal.Archived = value;
		}
	}

	public X509ExtensionCollection Extensions
	{
		get
		{
			ThrowIfInvalid();
			X509ExtensionCollection x509ExtensionCollection = _lazyExtensions;
			if (x509ExtensionCollection == null)
			{
				x509ExtensionCollection = new X509ExtensionCollection();
				foreach (X509Extension extension in Pal.Extensions)
				{
					X509Extension x509Extension = CreateCustomExtensionIfAny(extension.Oid);
					if (x509Extension == null)
					{
						x509ExtensionCollection.Add(extension);
						continue;
					}
					x509Extension.CopyFrom(extension);
					x509ExtensionCollection.Add(x509Extension);
				}
				_lazyExtensions = x509ExtensionCollection;
			}
			return x509ExtensionCollection;
		}
	}

	public string FriendlyName
	{
		get
		{
			ThrowIfInvalid();
			return Pal.FriendlyName;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ThrowIfInvalid();
			Pal.FriendlyName = value;
		}
	}

	public bool HasPrivateKey
	{
		get
		{
			ThrowIfInvalid();
			return Pal.HasPrivateKey;
		}
	}

	[Obsolete("X509Certificate2.PrivateKey is obsolete. Use the appropriate method to get the private key, such as GetRSAPrivateKey, or use the CopyWithPrivateKey method to create a new instance with a private key.", DiagnosticId = "SYSLIB0028", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public AsymmetricAlgorithm? PrivateKey
	{
		get
		{
			ThrowIfInvalid();
			if (!HasPrivateKey)
			{
				return null;
			}
			if (_lazyPrivateKey == null)
			{
				string keyAlgorithm = GetKeyAlgorithm();
				AsymmetricAlgorithm lazyPrivateKey;
				if (!(keyAlgorithm == "1.2.840.113549.1.1.1"))
				{
					if (!(keyAlgorithm == "1.2.840.10040.4.1"))
					{
						throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
					}
					lazyPrivateKey = Pal.GetDSAPrivateKey();
				}
				else
				{
					lazyPrivateKey = Pal.GetRSAPrivateKey();
				}
				_lazyPrivateKey = lazyPrivateKey;
			}
			return _lazyPrivateKey;
		}
		set
		{
			throw new PlatformNotSupportedException();
		}
	}

	public X500DistinguishedName IssuerName
	{
		get
		{
			ThrowIfInvalid();
			X500DistinguishedName x500DistinguishedName = _lazyIssuerName;
			if (x500DistinguishedName == null)
			{
				x500DistinguishedName = (_lazyIssuerName = Pal.IssuerName);
			}
			return x500DistinguishedName;
		}
	}

	public DateTime NotAfter => GetNotAfter();

	public DateTime NotBefore => GetNotBefore();

	public PublicKey PublicKey
	{
		get
		{
			ThrowIfInvalid();
			PublicKey publicKey = _lazyPublicKey;
			if (publicKey == null)
			{
				string keyAlgorithm = GetKeyAlgorithm();
				byte[] keyAlgorithmParameters = GetKeyAlgorithmParameters();
				byte[] publicKey2 = GetPublicKey();
				Oid oid = new Oid(keyAlgorithm);
				publicKey = (_lazyPublicKey = new PublicKey(oid, new AsnEncodedData(oid, keyAlgorithmParameters), new AsnEncodedData(oid, publicKey2)));
			}
			return publicKey;
		}
	}

	public byte[] RawData
	{
		get
		{
			ThrowIfInvalid();
			byte[] array = _lazyRawData;
			if (array == null)
			{
				array = (_lazyRawData = Pal.RawData);
			}
			return array.CloneByteArray();
		}
	}

	public string SerialNumber => GetSerialNumberString();

	public Oid SignatureAlgorithm
	{
		get
		{
			ThrowIfInvalid();
			Oid oid = _lazySignatureAlgorithm;
			if (oid == null)
			{
				string signatureAlgorithm = Pal.SignatureAlgorithm;
				oid = (_lazySignatureAlgorithm = new Oid(signatureAlgorithm, null));
			}
			return oid;
		}
	}

	public X500DistinguishedName SubjectName
	{
		get
		{
			ThrowIfInvalid();
			X500DistinguishedName x500DistinguishedName = _lazySubjectName;
			if (x500DistinguishedName == null)
			{
				x500DistinguishedName = (_lazySubjectName = Pal.SubjectName);
			}
			return x500DistinguishedName;
		}
	}

	public string Thumbprint => GetCertHashString();

	public int Version
	{
		get
		{
			ThrowIfInvalid();
			int num = _lazyVersion;
			if (num == 0)
			{
				num = (_lazyVersion = Pal.Version);
			}
			return num;
		}
	}

	public override void Reset()
	{
		_lazyRawData = null;
		_lazySignatureAlgorithm = null;
		_lazyVersion = 0;
		_lazySubjectName = null;
		_lazyIssuerName = null;
		_lazyPublicKey = null;
		_lazyPrivateKey = null;
		_lazyExtensions = null;
		base.Reset();
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public X509Certificate2()
	{
	}

	public X509Certificate2(byte[] rawData)
		: base(rawData)
	{
	}

	public X509Certificate2(byte[] rawData, string? password)
		: base(rawData, password)
	{
	}

	[CLSCompliant(false)]
	public X509Certificate2(byte[] rawData, SecureString? password)
		: base(rawData, password)
	{
	}

	public X509Certificate2(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
		: base(rawData, password, keyStorageFlags)
	{
	}

	[CLSCompliant(false)]
	public X509Certificate2(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
		: base(rawData, password, keyStorageFlags)
	{
	}

	public X509Certificate2(ReadOnlySpan<byte> rawData)
		: base(rawData)
	{
	}

	public X509Certificate2(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
		: base(rawData, password, keyStorageFlags)
	{
	}

	public X509Certificate2(IntPtr handle)
		: base(handle)
	{
	}

	internal X509Certificate2(ICertificatePal pal)
		: base(pal)
	{
	}

	public X509Certificate2(string fileName)
		: base(fileName)
	{
	}

	public X509Certificate2(string fileName, string? password)
		: base(fileName, password)
	{
	}

	[CLSCompliant(false)]
	public X509Certificate2(string fileName, SecureString? password)
		: base(fileName, password)
	{
	}

	public X509Certificate2(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
		: base(fileName, password, keyStorageFlags)
	{
	}

	[CLSCompliant(false)]
	public X509Certificate2(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
		: base(fileName, password, keyStorageFlags)
	{
	}

	public X509Certificate2(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet)
		: base(fileName, password, keyStorageFlags)
	{
	}

	public X509Certificate2(X509Certificate certificate)
		: base(certificate)
	{
	}

	protected X509Certificate2(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		throw new PlatformNotSupportedException();
	}

	public static X509ContentType GetCertContentType(byte[] rawData)
	{
		if (rawData == null || rawData.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		return X509Pal.Instance.GetCertContentType(rawData);
	}

	public static X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
	{
		if (rawData.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		return X509Pal.Instance.GetCertContentType(rawData);
	}

	public static X509ContentType GetCertContentType(string fileName)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		Path.GetFullPath(fileName);
		return X509Pal.Instance.GetCertContentType(fileName);
	}

	public string GetNameInfo(X509NameType nameType, bool forIssuer)
	{
		return Pal.GetNameInfo(nameType, forIssuer);
	}

	public override string ToString()
	{
		return base.ToString(fVerbose: true);
	}

	public override string ToString(bool verbose)
	{
		if (!verbose || Pal == null)
		{
			return ToString();
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Version]");
		stringBuilder.Append("  V");
		stringBuilder.Append(Version);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Subject]");
		stringBuilder.Append("  ");
		stringBuilder.Append(SubjectName.Name);
		string nameInfo = GetNameInfo(X509NameType.SimpleName, forIssuer: false);
		if (nameInfo.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("Simple Name: ");
			stringBuilder.Append(nameInfo);
		}
		string nameInfo2 = GetNameInfo(X509NameType.EmailName, forIssuer: false);
		if (nameInfo2.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("Email Name: ");
			stringBuilder.Append(nameInfo2);
		}
		string nameInfo3 = GetNameInfo(X509NameType.UpnName, forIssuer: false);
		if (nameInfo3.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("UPN Name: ");
			stringBuilder.Append(nameInfo3);
		}
		string nameInfo4 = GetNameInfo(X509NameType.DnsName, forIssuer: false);
		if (nameInfo4.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("DNS Name: ");
			stringBuilder.Append(nameInfo4);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Issuer]");
		stringBuilder.Append("  ");
		stringBuilder.Append(IssuerName.Name);
		nameInfo = GetNameInfo(X509NameType.SimpleName, forIssuer: true);
		if (nameInfo.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("Simple Name: ");
			stringBuilder.Append(nameInfo);
		}
		nameInfo2 = GetNameInfo(X509NameType.EmailName, forIssuer: true);
		if (nameInfo2.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("Email Name: ");
			stringBuilder.Append(nameInfo2);
		}
		nameInfo3 = GetNameInfo(X509NameType.UpnName, forIssuer: true);
		if (nameInfo3.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("UPN Name: ");
			stringBuilder.Append(nameInfo3);
		}
		nameInfo4 = GetNameInfo(X509NameType.DnsName, forIssuer: true);
		if (nameInfo4.Length > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("DNS Name: ");
			stringBuilder.Append(nameInfo4);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Serial Number]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(SerialNumber);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Not Before]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(X509Certificate.FormatDate(NotBefore));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Not After]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(X509Certificate.FormatDate(NotAfter));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Thumbprint]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(Thumbprint);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Signature Algorithm]");
		stringBuilder.Append("  ");
		stringBuilder.Append(SignatureAlgorithm.FriendlyName);
		stringBuilder.Append('(');
		stringBuilder.Append(SignatureAlgorithm.Value);
		stringBuilder.AppendLine(")");
		stringBuilder.AppendLine();
		stringBuilder.Append("[Public Key]");
		try
		{
			PublicKey publicKey = PublicKey;
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("Algorithm: ");
			stringBuilder.Append(publicKey.Oid.FriendlyName);
			try
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("  ");
				stringBuilder.Append("Length: ");
				using RSA rSA = this.GetRSAPublicKey();
				if (rSA != null)
				{
					stringBuilder.Append(rSA.KeySize);
				}
			}
			catch (NotSupportedException)
			{
			}
			stringBuilder.AppendLine();
			stringBuilder.Append("  ");
			stringBuilder.Append("Key Blob: ");
			stringBuilder.AppendLine(publicKey.EncodedKeyValue.Format(multiLine: true));
			stringBuilder.Append("  ");
			stringBuilder.Append("Parameters: ");
			stringBuilder.Append(publicKey.EncodedParameters.Format(multiLine: true));
		}
		catch (CryptographicException)
		{
		}
		Pal.AppendPrivateKeyInfo(stringBuilder);
		X509ExtensionCollection extensions = Extensions;
		if (extensions.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.Append("[Extensions]");
			foreach (X509Extension item in extensions)
			{
				try
				{
					stringBuilder.AppendLine();
					stringBuilder.Append("* ");
					stringBuilder.Append(item.Oid.FriendlyName);
					stringBuilder.Append('(');
					stringBuilder.Append(item.Oid.Value);
					stringBuilder.Append("):");
					stringBuilder.AppendLine();
					stringBuilder.Append("  ");
					stringBuilder.Append(item.Format(multiLine: true));
				}
				catch (CryptographicException)
				{
				}
			}
		}
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override void Import(byte[] rawData)
	{
		base.Import(rawData);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		base.Import(rawData, password, keyStorageFlags);
	}

	[CLSCompliant(false)]
	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override void Import(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		base.Import(rawData, password, keyStorageFlags);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override void Import(string fileName)
	{
		base.Import(fileName);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		base.Import(fileName, password, keyStorageFlags);
	}

	[CLSCompliant(false)]
	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public override void Import(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		base.Import(fileName, password, keyStorageFlags);
	}

	public bool Verify()
	{
		ThrowIfInvalid();
		using X509Chain x509Chain = new X509Chain();
		bool result = x509Chain.Build(this, throwOnException: false);
		for (int i = 0; i < x509Chain.ChainElements.Count; i++)
		{
			x509Chain.ChainElements[i].Certificate.Dispose();
		}
		return result;
	}

	public ECDiffieHellman? GetECDiffieHellmanPublicKey()
	{
		return this.GetPublicKey<ECDiffieHellman>((X509Certificate2 cert) => HasECDiffieHellmanKeyUsage(cert));
	}

	public ECDiffieHellman? GetECDiffieHellmanPrivateKey()
	{
		return this.GetPrivateKey<ECDiffieHellman>((X509Certificate2 cert) => HasECDiffieHellmanKeyUsage(cert));
	}

	public X509Certificate2 CopyWithPrivateKey(ECDiffieHellman privateKey)
	{
		if (privateKey == null)
		{
			throw new ArgumentNullException("privateKey");
		}
		if (HasPrivateKey)
		{
			throw new InvalidOperationException(System.SR.Cryptography_Cert_AlreadyHasPrivateKey);
		}
		using (ECDiffieHellman eCDiffieHellman = GetECDiffieHellmanPublicKey())
		{
			if (eCDiffieHellman == null)
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_WrongAlgorithm);
			}
			if (!Internal.Cryptography.Helpers.AreSamePublicECParameters(eCDiffieHellman.ExportParameters(includePrivateParameters: false), privateKey.ExportParameters(includePrivateParameters: false)))
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_DoesNotMatch, "privateKey");
			}
		}
		ICertificatePal pal = Pal.CopyWithPrivateKey(privateKey);
		return new X509Certificate2(pal);
	}

	public static X509Certificate2 CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = null)
	{
		if (certPemFilePath == null)
		{
			throw new ArgumentNullException("certPemFilePath");
		}
		ReadOnlySpan<char> readOnlySpan = File.ReadAllText(certPemFilePath);
		ReadOnlySpan<char> keyPem = ((keyPemFilePath == null) ? readOnlySpan : ((ReadOnlySpan<char>)File.ReadAllText(keyPemFilePath)));
		return CreateFromPem(readOnlySpan, keyPem);
	}

	public static X509Certificate2 CreateFromEncryptedPemFile(string certPemFilePath, ReadOnlySpan<char> password, string? keyPemFilePath = null)
	{
		if (certPemFilePath == null)
		{
			throw new ArgumentNullException("certPemFilePath");
		}
		ReadOnlySpan<char> readOnlySpan = File.ReadAllText(certPemFilePath);
		ReadOnlySpan<char> keyPem = ((keyPemFilePath == null) ? readOnlySpan : ((ReadOnlySpan<char>)File.ReadAllText(keyPemFilePath)));
		return CreateFromEncryptedPem(readOnlySpan, keyPem, password);
	}

	public static X509Certificate2 CreateFromPem(ReadOnlySpan<char> certPem, ReadOnlySpan<char> keyPem)
	{
		using X509Certificate2 x509Certificate = CreateFromPem(certPem);
		string keyAlgorithm = x509Certificate.GetKeyAlgorithm();
		switch (keyAlgorithm)
		{
		case "1.2.840.113549.1.1.1":
			return ExtractKeyFromPem(keyPem, s_RsaPublicKeyPrivateKeyLabels, RSA.Create, x509Certificate.CopyWithPrivateKey);
		case "1.2.840.10040.4.1":
			if (Internal.Cryptography.Helpers.IsDSASupported)
			{
				return ExtractKeyFromPem(keyPem, s_DsaPublicKeyPrivateKeyLabels, DSA.Create, x509Certificate.CopyWithPrivateKey);
			}
			break;
		case "1.2.840.10045.2.1":
			if (IsECDsa(x509Certificate))
			{
				return ExtractKeyFromPem(keyPem, s_EcPublicKeyPrivateKeyLabels, ECDsa.Create, x509Certificate.CopyWithPrivateKey);
			}
			if (IsECDiffieHellman(x509Certificate))
			{
				return ExtractKeyFromPem(keyPem, s_EcPublicKeyPrivateKeyLabels, ECDiffieHellman.Create, x509Certificate.CopyWithPrivateKey);
			}
			break;
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownKeyAlgorithm, keyAlgorithm));
	}

	public static X509Certificate2 CreateFromEncryptedPem(ReadOnlySpan<char> certPem, ReadOnlySpan<char> keyPem, ReadOnlySpan<char> password)
	{
		using X509Certificate2 x509Certificate = CreateFromPem(certPem);
		string keyAlgorithm = x509Certificate.GetKeyAlgorithm();
		switch (keyAlgorithm)
		{
		case "1.2.840.113549.1.1.1":
			return ExtractKeyFromEncryptedPem(keyPem, password, RSA.Create, x509Certificate.CopyWithPrivateKey);
		case "1.2.840.10040.4.1":
			if (Internal.Cryptography.Helpers.IsDSASupported)
			{
				return ExtractKeyFromEncryptedPem(keyPem, password, DSA.Create, x509Certificate.CopyWithPrivateKey);
			}
			break;
		case "1.2.840.10045.2.1":
			if (IsECDsa(x509Certificate))
			{
				return ExtractKeyFromEncryptedPem(keyPem, password, ECDsa.Create, x509Certificate.CopyWithPrivateKey);
			}
			if (IsECDiffieHellman(x509Certificate))
			{
				return ExtractKeyFromEncryptedPem(keyPem, password, ECDiffieHellman.Create, x509Certificate.CopyWithPrivateKey);
			}
			break;
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownKeyAlgorithm, keyAlgorithm));
	}

	private static bool IsECDsa(X509Certificate2 certificate)
	{
		using ECDsa eCDsa = certificate.GetECDsaPublicKey();
		return eCDsa != null;
	}

	private static bool IsECDiffieHellman(X509Certificate2 certificate)
	{
		using ECDiffieHellman eCDiffieHellman = certificate.GetECDiffieHellmanPublicKey();
		return eCDiffieHellman != null;
	}

	public static X509Certificate2 CreateFromPem(ReadOnlySpan<char> certPem)
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
				byte[] array = System.Security.Cryptography.CryptoPool.Rent(pemFields2.DecodedDataLength);
				contents = readOnlySpan;
				if (!Convert.TryFromBase64Chars(contents[pemFields2.Base64Data], array, out var bytesWritten) || bytesWritten != pemFields2.DecodedDataLength)
				{
					throw new CryptographicException(System.SR.Cryptography_X509_NoPemCertificate);
				}
				ReadOnlyMemory<byte> encoded = new ReadOnlyMemory<byte>(array, 0, bytesWritten);
				try
				{
					CertificateAsn.Decode(encoded, AsnEncodingRules.DER);
				}
				catch (CryptographicException)
				{
					throw new CryptographicException(System.SR.Cryptography_X509_NoPemCertificate);
				}
				X509Certificate2 result = new X509Certificate2(encoded.Span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
				return result;
			}
		}
		throw new CryptographicException(System.SR.Cryptography_X509_NoPemCertificate);
	}

	private static X509Certificate2 ExtractKeyFromPem<TAlg>(ReadOnlySpan<char> keyPem, string[] labels, Func<TAlg> factory, Func<TAlg, X509Certificate2> import) where TAlg : AsymmetricAlgorithm
	{
		PemEnumerator.Enumerator enumerator = new PemEnumerator(keyPem).GetEnumerator();
		while (enumerator.MoveNext())
		{
			enumerator.Current.Deconstruct(out var contents, out var pemFields);
			ReadOnlySpan<char> readOnlySpan = contents;
			PemFields pemFields2 = pemFields;
			contents = readOnlySpan;
			ReadOnlySpan<char> span = contents[pemFields2.Label];
			foreach (string text in labels)
			{
				if (span.SequenceEqual(text))
				{
					TAlg val = factory();
					contents = readOnlySpan;
					val.ImportFromPem(contents[pemFields2.Location]);
					try
					{
						return import(val);
					}
					catch (ArgumentException inner)
					{
						throw new CryptographicException(System.SR.Cryptography_X509_NoOrMismatchedPemKey, inner);
					}
				}
			}
		}
		throw new CryptographicException(System.SR.Cryptography_X509_NoOrMismatchedPemKey);
	}

	private static X509Certificate2 ExtractKeyFromEncryptedPem<TAlg>(ReadOnlySpan<char> keyPem, ReadOnlySpan<char> password, Func<TAlg> factory, Func<TAlg, X509Certificate2> import) where TAlg : AsymmetricAlgorithm
	{
		PemEnumerator.Enumerator enumerator = new PemEnumerator(keyPem).GetEnumerator();
		while (enumerator.MoveNext())
		{
			enumerator.Current.Deconstruct(out var contents, out var pemFields);
			ReadOnlySpan<char> readOnlySpan = contents;
			PemFields pemFields2 = pemFields;
			contents = readOnlySpan;
			ReadOnlySpan<char> span = contents[pemFields2.Label];
			if (span.SequenceEqual("ENCRYPTED PRIVATE KEY"))
			{
				TAlg val = factory();
				contents = readOnlySpan;
				val.ImportFromEncryptedPem(contents[pemFields2.Location], password);
				try
				{
					return import(val);
				}
				catch (ArgumentException inner)
				{
					throw new CryptographicException(System.SR.Cryptography_X509_NoOrMismatchedPemKey, inner);
				}
			}
		}
		throw new CryptographicException(System.SR.Cryptography_X509_NoOrMismatchedPemKey);
	}

	private static X509Extension CreateCustomExtensionIfAny(Oid oid)
	{
		return oid.Value switch
		{
			"2.5.29.10" => X509Pal.Instance.SupportsLegacyBasicConstraintsExtension ? new X509BasicConstraintsExtension() : null, 
			"2.5.29.19" => new X509BasicConstraintsExtension(), 
			"2.5.29.15" => new X509KeyUsageExtension(), 
			"2.5.29.37" => new X509EnhancedKeyUsageExtension(), 
			"2.5.29.14" => new X509SubjectKeyIdentifierExtension(), 
			_ => null, 
		};
	}

	private static bool HasECDiffieHellmanKeyUsage(X509Certificate2 certificate)
	{
		foreach (X509Extension extension in certificate.Extensions)
		{
			if (extension.Oid?.Value == "2.5.29.15" && extension is X509KeyUsageExtension x509KeyUsageExtension)
			{
				return (x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.KeyAgreement) != 0;
			}
		}
		return true;
	}
}
