using System.Diagnostics.CodeAnalysis;
using System.Net.Http.HPack;
using System.Net.Http.QPack;
using System.Text;

namespace System.Net.Http.Headers;

internal sealed class KnownHeader
{
	public string Name { get; }

	public HttpHeaderParser Parser { get; }

	public HttpHeaderType HeaderType { get; }

	public string[] KnownValues { get; }

	public byte[] AsciiBytesWithColonSpace { get; }

	public HeaderDescriptor Descriptor => new HeaderDescriptor(this);

	public byte[] Http2EncodedName { get; private set; }

	public byte[] Http3EncodedName { get; private set; }

	public KnownHeader(string name, int? http2StaticTableIndex = null, int? http3StaticTableIndex = null)
		: this(name, HttpHeaderType.Custom, null, null, http2StaticTableIndex, http3StaticTableIndex)
	{
	}

	public KnownHeader(string name, HttpHeaderType headerType, HttpHeaderParser parser, string[] knownValues = null, int? http2StaticTableIndex = null, int? http3StaticTableIndex = null)
	{
		Name = name;
		HeaderType = headerType;
		Parser = parser;
		KnownValues = knownValues;
		Initialize(http2StaticTableIndex, http3StaticTableIndex);
		byte[] array = new byte[name.Length + 2];
		int bytes = Encoding.ASCII.GetBytes(name, array);
		array[^2] = 58;
		array[^1] = 32;
		AsciiBytesWithColonSpace = array;
	}

	[MemberNotNull("Http2EncodedName")]
	[MemberNotNull("Http3EncodedName")]
	private void Initialize(int? http2StaticTableIndex, int? http3StaticTableIndex)
	{
		Http2EncodedName = (http2StaticTableIndex.HasValue ? HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(http2StaticTableIndex.GetValueOrDefault()) : HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingNewNameToAllocatedArray(Name));
		Http3EncodedName = (http3StaticTableIndex.HasValue ? QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(http3StaticTableIndex.GetValueOrDefault()) : QPackEncoder.EncodeLiteralHeaderFieldWithoutNameReferenceToArray(Name));
	}
}
