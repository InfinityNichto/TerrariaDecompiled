using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using Internal.Cryptography;
using Internal.Cryptography.Pal;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates;

public class X509Certificate : IDisposable, IDeserializationCallback, ISerializable
{
	private volatile byte[] _lazyCertHash;

	private volatile string _lazyIssuer;

	private volatile string _lazySubject;

	private volatile byte[] _lazySerialNumber;

	private volatile string _lazyKeyAlgorithm;

	private volatile byte[] _lazyKeyAlgorithmParameters;

	private volatile byte[] _lazyPublicKey;

	private DateTime _lazyNotBefore = DateTime.MinValue;

	private DateTime _lazyNotAfter = DateTime.MinValue;

	public IntPtr Handle
	{
		get
		{
			if (Pal == null)
			{
				return IntPtr.Zero;
			}
			return Pal.Handle;
		}
	}

	public string Issuer
	{
		get
		{
			ThrowIfInvalid();
			string text = _lazyIssuer;
			if (text == null)
			{
				text = (_lazyIssuer = Pal.Issuer);
			}
			return text;
		}
	}

	public string Subject
	{
		get
		{
			ThrowIfInvalid();
			string text = _lazySubject;
			if (text == null)
			{
				text = (_lazySubject = Pal.Subject);
			}
			return text;
		}
	}

	internal ICertificatePalCore? Pal { get; private set; }

	public virtual void Reset()
	{
		_lazyCertHash = null;
		_lazyIssuer = null;
		_lazySubject = null;
		_lazySerialNumber = null;
		_lazyKeyAlgorithm = null;
		_lazyKeyAlgorithmParameters = null;
		_lazyPublicKey = null;
		_lazyNotBefore = DateTime.MinValue;
		_lazyNotAfter = DateTime.MinValue;
		ICertificatePalCore pal = Pal;
		if (pal != null)
		{
			Pal = null;
			pal.Dispose();
		}
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public X509Certificate()
	{
	}

	public X509Certificate(byte[] data)
		: this(new ReadOnlySpan<byte>(data))
	{
	}

	private protected X509Certificate(ReadOnlySpan<byte> data)
	{
		if (!data.IsEmpty)
		{
			using (SafePasswordHandle password = new SafePasswordHandle((string)null))
			{
				Pal = CertificatePal.FromBlob(data, password, X509KeyStorageFlags.DefaultKeySet);
			}
		}
	}

	public X509Certificate(byte[] rawData, string? password)
		: this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	[CLSCompliant(false)]
	public X509Certificate(byte[] rawData, SecureString? password)
		: this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	public X509Certificate(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		if (rawData == null || rawData.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		Pal = CertificatePal.FromBlob(rawData, password2, keyStorageFlags);
	}

	[CLSCompliant(false)]
	public X509Certificate(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		if (rawData == null || rawData.Length == 0)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		Pal = CertificatePal.FromBlob(rawData, password2, keyStorageFlags);
	}

	private protected X509Certificate(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags)
	{
		if (rawData.IsEmpty)
		{
			throw new ArgumentException(System.SR.Arg_EmptyOrNullArray, "rawData");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		Pal = CertificatePal.FromBlob(rawData, password2, keyStorageFlags);
	}

	public X509Certificate(IntPtr handle)
	{
		Pal = CertificatePal.FromHandle(handle);
	}

	internal X509Certificate(ICertificatePalCore pal)
	{
		Pal = pal;
	}

	public X509Certificate(string fileName)
		: this(fileName, (string?)null, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	public X509Certificate(string fileName, string? password)
		: this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	[CLSCompliant(false)]
	public X509Certificate(string fileName, SecureString? password)
		: this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
	{
	}

	public X509Certificate(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		Pal = CertificatePal.FromFile(fileName, password2, keyStorageFlags);
	}

	private protected X509Certificate(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags)
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		Pal = CertificatePal.FromFile(fileName, password2, keyStorageFlags);
	}

	[CLSCompliant(false)]
	public X509Certificate(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
		: this()
	{
		if (fileName == null)
		{
			throw new ArgumentNullException("fileName");
		}
		ValidateKeyStorageFlags(keyStorageFlags);
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		Pal = CertificatePal.FromFile(fileName, password2, keyStorageFlags);
	}

	public X509Certificate(X509Certificate cert)
	{
		if (cert == null)
		{
			throw new ArgumentNullException("cert");
		}
		if (cert.Pal != null)
		{
			Pal = CertificatePal.FromOtherCert(cert);
		}
	}

	public X509Certificate(SerializationInfo info, StreamingContext context)
		: this()
	{
		throw new PlatformNotSupportedException();
	}

	public static X509Certificate CreateFromCertFile(string filename)
	{
		return new X509Certificate(filename);
	}

	public static X509Certificate CreateFromSignedFile(string filename)
	{
		return new X509Certificate(filename);
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		throw new PlatformNotSupportedException();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			Reset();
		}
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is X509Certificate other))
		{
			return false;
		}
		return Equals(other);
	}

	public virtual bool Equals([NotNullWhen(true)] X509Certificate? other)
	{
		if (other == null)
		{
			return false;
		}
		if (Pal == null)
		{
			return other.Pal == null;
		}
		if (!Issuer.Equals(other.Issuer))
		{
			return false;
		}
		byte[] rawSerialNumber = GetRawSerialNumber();
		byte[] rawSerialNumber2 = other.GetRawSerialNumber();
		if (rawSerialNumber.Length != rawSerialNumber2.Length)
		{
			return false;
		}
		for (int i = 0; i < rawSerialNumber.Length; i++)
		{
			if (rawSerialNumber[i] != rawSerialNumber2[i])
			{
				return false;
			}
		}
		return true;
	}

	public virtual byte[] Export(X509ContentType contentType)
	{
		return Export(contentType, (string?)null);
	}

	public virtual byte[] Export(X509ContentType contentType, string? password)
	{
		VerifyContentType(contentType);
		if (Pal == null)
		{
			throw new CryptographicException(-2147467261);
		}
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		return Pal.Export(contentType, password2);
	}

	[CLSCompliant(false)]
	public virtual byte[] Export(X509ContentType contentType, SecureString? password)
	{
		VerifyContentType(contentType);
		if (Pal == null)
		{
			throw new CryptographicException(-2147467261);
		}
		using SafePasswordHandle password2 = new SafePasswordHandle(password);
		return Pal.Export(contentType, password2);
	}

	public virtual string GetRawCertDataString()
	{
		ThrowIfInvalid();
		return GetRawCertData().ToHexStringUpper();
	}

	public virtual byte[] GetCertHash()
	{
		ThrowIfInvalid();
		return GetRawCertHash().CloneByteArray();
	}

	public virtual byte[] GetCertHash(HashAlgorithmName hashAlgorithm)
	{
		ThrowIfInvalid();
		return GetCertHash(hashAlgorithm, Pal);
	}

	private static byte[] GetCertHash(HashAlgorithmName hashAlgorithm, ICertificatePalCore certPal)
	{
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm);
		incrementalHash.AppendData(certPal.RawData);
		return incrementalHash.GetHashAndReset();
	}

	public virtual bool TryGetCertHash(HashAlgorithmName hashAlgorithm, Span<byte> destination, out int bytesWritten)
	{
		ThrowIfInvalid();
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithm);
		incrementalHash.AppendData(Pal.RawData);
		return incrementalHash.TryGetHashAndReset(destination, out bytesWritten);
	}

	public virtual string GetCertHashString()
	{
		ThrowIfInvalid();
		return GetRawCertHash().ToHexStringUpper();
	}

	public virtual string GetCertHashString(HashAlgorithmName hashAlgorithm)
	{
		ThrowIfInvalid();
		return GetCertHashString(hashAlgorithm, Pal);
	}

	internal static string GetCertHashString(HashAlgorithmName hashAlgorithm, ICertificatePalCore certPal)
	{
		return GetCertHash(hashAlgorithm, certPal).ToHexStringUpper();
	}

	private byte[] GetRawCertHash()
	{
		return _lazyCertHash ?? (_lazyCertHash = Pal.Thumbprint);
	}

	public virtual string GetEffectiveDateString()
	{
		return GetNotBefore().ToString();
	}

	public virtual string GetExpirationDateString()
	{
		return GetNotAfter().ToString();
	}

	public virtual string GetFormat()
	{
		return "X509";
	}

	public virtual string GetPublicKeyString()
	{
		return GetPublicKey().ToHexStringUpper();
	}

	public virtual byte[] GetRawCertData()
	{
		ThrowIfInvalid();
		return Pal.RawData.CloneByteArray();
	}

	public override int GetHashCode()
	{
		if (Pal == null)
		{
			return 0;
		}
		byte[] rawCertHash = GetRawCertHash();
		int num = 0;
		for (int i = 0; i < rawCertHash.Length && i < 4; i++)
		{
			num = (num << 8) | rawCertHash[i];
		}
		return num;
	}

	public virtual string GetKeyAlgorithm()
	{
		ThrowIfInvalid();
		string text = _lazyKeyAlgorithm;
		if (text == null)
		{
			text = (_lazyKeyAlgorithm = Pal.KeyAlgorithm);
		}
		return text;
	}

	public virtual byte[] GetKeyAlgorithmParameters()
	{
		ThrowIfInvalid();
		byte[] array = _lazyKeyAlgorithmParameters;
		if (array == null)
		{
			array = (_lazyKeyAlgorithmParameters = Pal.KeyAlgorithmParameters);
		}
		return array.CloneByteArray();
	}

	public virtual string GetKeyAlgorithmParametersString()
	{
		ThrowIfInvalid();
		byte[] keyAlgorithmParameters = GetKeyAlgorithmParameters();
		return keyAlgorithmParameters.ToHexStringUpper();
	}

	public virtual byte[] GetPublicKey()
	{
		ThrowIfInvalid();
		byte[] array = _lazyPublicKey;
		if (array == null)
		{
			array = (_lazyPublicKey = Pal.PublicKeyValue);
		}
		return array.CloneByteArray();
	}

	public virtual byte[] GetSerialNumber()
	{
		ThrowIfInvalid();
		byte[] array = GetRawSerialNumber().CloneByteArray();
		Array.Reverse(array);
		return array;
	}

	public virtual string GetSerialNumberString()
	{
		ThrowIfInvalid();
		return GetRawSerialNumber().ToHexStringUpper();
	}

	private byte[] GetRawSerialNumber()
	{
		return _lazySerialNumber ?? (_lazySerialNumber = Pal.SerialNumber);
	}

	[Obsolete("X509Certificate.GetName has been deprecated. Use the Subject property instead.")]
	public virtual string GetName()
	{
		ThrowIfInvalid();
		return Pal.LegacySubject;
	}

	[Obsolete("X509Certificate.GetIssuerName has been deprecated. Use the Issuer property instead.")]
	public virtual string GetIssuerName()
	{
		ThrowIfInvalid();
		return Pal.LegacyIssuer;
	}

	public override string ToString()
	{
		return ToString(fVerbose: false);
	}

	public virtual string ToString(bool fVerbose)
	{
		if (!fVerbose || Pal == null)
		{
			return GetType().ToString();
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Subject]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(Subject);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Issuer]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(Issuer);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Serial Number]");
		stringBuilder.Append("  ");
		byte[] serialNumber = GetSerialNumber();
		Array.Reverse(serialNumber);
		stringBuilder.Append(serialNumber.ToHexArrayUpper());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Not Before]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(FormatDate(GetNotBefore()));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Not After]");
		stringBuilder.Append("  ");
		stringBuilder.AppendLine(FormatDate(GetNotAfter()));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[Thumbprint]");
		stringBuilder.Append("  ");
		stringBuilder.Append(GetRawCertHash().ToHexArrayUpper());
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(byte[] rawData)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[CLSCompliant(false)]
	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(string fileName)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	[CLSCompliant(false)]
	[Obsolete("X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate.", DiagnosticId = "SYSLIB0026", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public virtual void Import(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
	{
		throw new PlatformNotSupportedException(System.SR.NotSupported_ImmutableX509Certificate);
	}

	internal DateTime GetNotAfter()
	{
		ThrowIfInvalid();
		DateTime dateTime = _lazyNotAfter;
		if (dateTime == DateTime.MinValue)
		{
			dateTime = (_lazyNotAfter = Pal.NotAfter);
		}
		return dateTime;
	}

	internal DateTime GetNotBefore()
	{
		ThrowIfInvalid();
		DateTime dateTime = _lazyNotBefore;
		if (dateTime == DateTime.MinValue)
		{
			dateTime = (_lazyNotBefore = Pal.NotBefore);
		}
		return dateTime;
	}

	internal void ThrowIfInvalid()
	{
		if (Pal == null)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidHandle, "m_safeCertContext"));
		}
	}

	protected static string FormatDate(DateTime date)
	{
		CultureInfo cultureInfo = CultureInfo.CurrentCulture;
		if (!cultureInfo.DateTimeFormat.Calendar.IsValidDay(date.Year, date.Month, date.Day, 0))
		{
			if (cultureInfo.DateTimeFormat.Calendar is UmAlQuraCalendar)
			{
				cultureInfo = cultureInfo.Clone() as CultureInfo;
				cultureInfo.DateTimeFormat.Calendar = new HijriCalendar();
			}
			else
			{
				cultureInfo = CultureInfo.InvariantCulture;
			}
		}
		return date.ToString(cultureInfo);
	}

	internal static void ValidateKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
	{
		if (((uint)keyStorageFlags & 0xFFFFFFC0u) != 0)
		{
			throw new ArgumentException(System.SR.Argument_InvalidFlag, "keyStorageFlags");
		}
		X509KeyStorageFlags x509KeyStorageFlags = keyStorageFlags & (X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet);
		if (x509KeyStorageFlags == (X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_X509_InvalidFlagCombination, x509KeyStorageFlags), "keyStorageFlags");
		}
	}

	private void VerifyContentType(X509ContentType contentType)
	{
		if (contentType != X509ContentType.Cert && contentType != X509ContentType.SerializedCert && contentType != X509ContentType.Pfx)
		{
			throw new CryptographicException(System.SR.Cryptography_X509_InvalidContentType);
		}
	}
}
