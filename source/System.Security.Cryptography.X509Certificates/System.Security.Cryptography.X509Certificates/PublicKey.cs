using System.Buffers;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates;

public sealed class PublicKey
{
	private readonly Oid _oid;

	private AsymmetricAlgorithm _key;

	public AsnEncodedData EncodedKeyValue { get; private set; }

	public AsnEncodedData EncodedParameters { get; private set; }

	[Obsolete("PublicKey.Key is obsolete. Use the appropriate method to get the public key, such as GetRSAPublicKey.", DiagnosticId = "SYSLIB0027", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public AsymmetricAlgorithm Key
	{
		get
		{
			if (_key == null)
			{
				string value = _oid.Value;
				if (!(value == "1.2.840.113549.1.1.1") && !(value == "1.2.840.10040.4.1"))
				{
					throw new NotSupportedException(System.SR.NotSupported_KeyAlgorithm);
				}
				_key = X509Pal.Instance.DecodePublicKey(_oid, EncodedKeyValue.RawData, EncodedParameters.RawData, null);
			}
			return _key;
		}
	}

	public Oid Oid => _oid;

	public PublicKey(Oid oid, AsnEncodedData parameters, AsnEncodedData keyValue)
	{
		_oid = oid;
		EncodedParameters = new AsnEncodedData(parameters);
		EncodedKeyValue = new AsnEncodedData(keyValue);
	}

	public PublicKey(AsymmetricAlgorithm key)
	{
		byte[] array = key.ExportSubjectPublicKeyInfo();
		DecodeSubjectPublicKeyInfo(array, out var oid, out var parameters, out var keyValue);
		_oid = oid;
		EncodedParameters = parameters;
		EncodedKeyValue = keyValue;
	}

	public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
	{
		return EncodeSubjectPublicKeyInfo().TryEncode(destination, out bytesWritten);
	}

	public byte[] ExportSubjectPublicKeyInfo()
	{
		return EncodeSubjectPublicKeyInfo().Encode();
	}

	public static PublicKey CreateFromSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
	{
		Oid oid;
		AsnEncodedData parameters;
		AsnEncodedData keyValue;
		int num = DecodeSubjectPublicKeyInfo(source, out oid, out parameters, out keyValue);
		bytesRead = num;
		return new PublicKey(oid, parameters, keyValue);
	}

	public RSA? GetRSAPublicKey()
	{
		if (_oid.Value != "1.2.840.113549.1.1.1")
		{
			return null;
		}
		RSA rSA = RSA.Create();
		try
		{
			rSA.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out var _);
			return rSA;
		}
		catch
		{
			rSA.Dispose();
			throw;
		}
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public DSA? GetDSAPublicKey()
	{
		if (_oid.Value != "1.2.840.10040.4.1")
		{
			return null;
		}
		DSA dSA = DSA.Create();
		try
		{
			dSA.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out var _);
			return dSA;
		}
		catch
		{
			dSA.Dispose();
			throw;
		}
	}

	public ECDsa? GetECDsaPublicKey()
	{
		if (_oid.Value != "1.2.840.10045.2.1")
		{
			return null;
		}
		ECDsa eCDsa = ECDsa.Create();
		try
		{
			eCDsa.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out var _);
			return eCDsa;
		}
		catch
		{
			eCDsa.Dispose();
			throw;
		}
	}

	public ECDiffieHellman? GetECDiffieHellmanPublicKey()
	{
		if (_oid.Value != "1.2.840.10045.2.1")
		{
			return null;
		}
		ECDiffieHellman eCDiffieHellman = ECDiffieHellman.Create();
		try
		{
			eCDiffieHellman.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out var _);
			return eCDiffieHellman;
		}
		catch
		{
			eCDiffieHellman.Dispose();
			throw;
		}
	}

	private AsnWriter EncodeSubjectPublicKeyInfo()
	{
		System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn subjectPublicKeyInfoAsn = default(System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn);
		subjectPublicKeyInfoAsn.Algorithm = new System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn
		{
			Algorithm = (_oid.Value ?? string.Empty),
			Parameters = EncodedParameters.RawData
		};
		subjectPublicKeyInfoAsn.SubjectPublicKey = EncodedKeyValue.RawData;
		System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn subjectPublicKeyInfoAsn2 = subjectPublicKeyInfoAsn;
		AsnWriter asnWriter = new AsnWriter(AsnEncodingRules.DER);
		subjectPublicKeyInfoAsn2.Encode(asnWriter);
		return asnWriter;
	}

	private unsafe static int DecodeSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out Oid oid, out AsnEncodedData parameters, out AsnEncodedData keyValue)
	{
		fixed (byte* pointer = &MemoryMarshal.GetReference(source))
		{
			using MemoryManager<byte> memoryManager = new System.Buffers.PointerMemoryManager<byte>(pointer, source.Length);
			System.Formats.Asn1.AsnValueReader reader = new System.Formats.Asn1.AsnValueReader(source, AsnEncodingRules.DER);
			int length;
			System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn decoded;
			try
			{
				length = reader.PeekEncodedValue().Length;
				System.Security.Cryptography.Asn1.SubjectPublicKeyInfoAsn.Decode(ref reader, memoryManager.Memory, out decoded);
			}
			catch (AsnContentException inner)
			{
				throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding, inner);
			}
			oid = new Oid(decoded.Algorithm.Algorithm, null);
			parameters = new AsnEncodedData(decoded.Algorithm.Parameters?.ToArray() ?? Array.Empty<byte>());
			keyValue = new AsnEncodedData(decoded.SubjectPublicKey.ToArray());
			return length;
		}
	}
}
