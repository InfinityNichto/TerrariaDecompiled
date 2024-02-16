using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography;

public class AsnEncodedData
{
	private Oid _oid;

	private byte[] _rawData;

	public Oid? Oid
	{
		get
		{
			return _oid;
		}
		set
		{
			_oid = value;
		}
	}

	public byte[] RawData
	{
		get
		{
			return _rawData;
		}
		[MemberNotNull("_rawData")]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_rawData = value.CloneByteArray();
		}
	}

	protected AsnEncodedData()
	{
		_rawData = Array.Empty<byte>();
	}

	public AsnEncodedData(byte[] rawData)
	{
		Reset(null, rawData);
	}

	public AsnEncodedData(ReadOnlySpan<byte> rawData)
	{
		Reset(null, rawData);
	}

	public AsnEncodedData(AsnEncodedData asnEncodedData)
	{
		if (asnEncodedData == null)
		{
			throw new ArgumentNullException("asnEncodedData");
		}
		Reset(asnEncodedData._oid, asnEncodedData._rawData);
	}

	public AsnEncodedData(Oid? oid, byte[] rawData)
	{
		Reset(oid, rawData);
	}

	public AsnEncodedData(string oid, byte[] rawData)
	{
		Reset(new Oid(oid), rawData);
	}

	public AsnEncodedData(Oid? oid, ReadOnlySpan<byte> rawData)
	{
		Reset(oid, rawData);
	}

	public AsnEncodedData(string oid, ReadOnlySpan<byte> rawData)
	{
		Reset(new Oid(oid), rawData);
	}

	public virtual void CopyFrom(AsnEncodedData asnEncodedData)
	{
		if (asnEncodedData == null)
		{
			throw new ArgumentNullException("asnEncodedData");
		}
		Reset(asnEncodedData._oid, asnEncodedData._rawData);
	}

	public virtual string Format(bool multiLine)
	{
		if (_rawData == null || _rawData.Length == 0)
		{
			return string.Empty;
		}
		return AsnFormatter.Instance.Format(_oid, _rawData, multiLine);
	}

	[MemberNotNull("_rawData")]
	private void Reset(Oid oid, byte[] rawData)
	{
		Oid = oid;
		RawData = rawData;
	}

	[MemberNotNull("_rawData")]
	private void Reset(Oid oid, ReadOnlySpan<byte> rawData)
	{
		Oid = oid;
		_rawData = rawData.ToArray();
	}
}
