using System.Runtime.InteropServices;

namespace System.Net.Http.Headers;

internal static class KnownHeaders
{
	private interface IHeaderNameAccessor
	{
		int Length { get; }

		char this[int index] { get; }
	}

	private readonly struct StringAccessor : IHeaderNameAccessor
	{
		private readonly string _string;

		public int Length => _string.Length;

		public char this[int index] => _string[index];

		public StringAccessor(string s)
		{
			_string = s;
		}
	}

	private readonly struct BytePtrAccessor : IHeaderNameAccessor
	{
		private unsafe readonly byte* _p;

		private readonly int _length;

		public int Length => _length;

		public unsafe char this[int index] => (char)_p[index];

		public unsafe BytePtrAccessor(byte* p, int length)
		{
			_p = p;
			_length = length;
		}
	}

	public static readonly KnownHeader PseudoStatus = new KnownHeader(":status", HttpHeaderType.Response, null);

	public static readonly KnownHeader Accept = new KnownHeader("Accept", HttpHeaderType.Request, MediaTypeHeaderParser.MultipleValuesParser, null, 19, 29);

	public static readonly KnownHeader AcceptCharset = new KnownHeader("Accept-Charset", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, 15);

	public static readonly KnownHeader AcceptEncoding = new KnownHeader("Accept-Encoding", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, 16, 31);

	public static readonly KnownHeader AcceptLanguage = new KnownHeader("Accept-Language", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, 17, 72);

	public static readonly KnownHeader AcceptPatch = new KnownHeader("Accept-Patch");

	public static readonly KnownHeader AcceptRanges = new KnownHeader("Accept-Ranges", HttpHeaderType.Response, GenericHeaderParser.TokenListParser, null, 18, 32);

	public static readonly KnownHeader AccessControlAllowCredentials;

	public static readonly KnownHeader AccessControlAllowHeaders;

	public static readonly KnownHeader AccessControlAllowMethods;

	public static readonly KnownHeader AccessControlAllowOrigin;

	public static readonly KnownHeader AccessControlExposeHeaders;

	public static readonly KnownHeader AccessControlMaxAge;

	public static readonly KnownHeader Age;

	public static readonly KnownHeader Allow;

	public static readonly KnownHeader AltSvc;

	public static readonly KnownHeader AltUsed;

	public static readonly KnownHeader Authorization;

	public static readonly KnownHeader CacheControl;

	public static readonly KnownHeader Connection;

	public static readonly KnownHeader ContentDisposition;

	public static readonly KnownHeader ContentEncoding;

	public static readonly KnownHeader ContentLanguage;

	public static readonly KnownHeader ContentLength;

	public static readonly KnownHeader ContentLocation;

	public static readonly KnownHeader ContentMD5;

	public static readonly KnownHeader ContentRange;

	public static readonly KnownHeader ContentSecurityPolicy;

	public static readonly KnownHeader ContentType;

	public static readonly KnownHeader Cookie;

	public static readonly KnownHeader Cookie2;

	public static readonly KnownHeader Date;

	public static readonly KnownHeader ETag;

	public static readonly KnownHeader Expect;

	public static readonly KnownHeader ExpectCT;

	public static readonly KnownHeader Expires;

	public static readonly KnownHeader From;

	public static readonly KnownHeader GrpcEncoding;

	public static readonly KnownHeader GrpcMessage;

	public static readonly KnownHeader GrpcStatus;

	public static readonly KnownHeader Host;

	public static readonly KnownHeader IfMatch;

	public static readonly KnownHeader IfModifiedSince;

	public static readonly KnownHeader IfNoneMatch;

	public static readonly KnownHeader IfRange;

	public static readonly KnownHeader IfUnmodifiedSince;

	public static readonly KnownHeader KeepAlive;

	public static readonly KnownHeader LastModified;

	public static readonly KnownHeader Link;

	public static readonly KnownHeader Location;

	public static readonly KnownHeader MaxForwards;

	public static readonly KnownHeader Origin;

	public static readonly KnownHeader P3P;

	public static readonly KnownHeader Pragma;

	public static readonly KnownHeader ProxyAuthenticate;

	public static readonly KnownHeader ProxyAuthorization;

	public static readonly KnownHeader ProxyConnection;

	public static readonly KnownHeader ProxySupport;

	public static readonly KnownHeader PublicKeyPins;

	public static readonly KnownHeader Range;

	public static readonly KnownHeader Referer;

	public static readonly KnownHeader ReferrerPolicy;

	public static readonly KnownHeader Refresh;

	public static readonly KnownHeader RetryAfter;

	public static readonly KnownHeader SecWebSocketAccept;

	public static readonly KnownHeader SecWebSocketExtensions;

	public static readonly KnownHeader SecWebSocketKey;

	public static readonly KnownHeader SecWebSocketProtocol;

	public static readonly KnownHeader SecWebSocketVersion;

	public static readonly KnownHeader Server;

	public static readonly KnownHeader ServerTiming;

	public static readonly KnownHeader SetCookie;

	public static readonly KnownHeader SetCookie2;

	public static readonly KnownHeader StrictTransportSecurity;

	public static readonly KnownHeader TE;

	public static readonly KnownHeader TSV;

	public static readonly KnownHeader Trailer;

	public static readonly KnownHeader TransferEncoding;

	public static readonly KnownHeader Upgrade;

	public static readonly KnownHeader UpgradeInsecureRequests;

	public static readonly KnownHeader UserAgent;

	public static readonly KnownHeader Vary;

	public static readonly KnownHeader Via;

	public static readonly KnownHeader WWWAuthenticate;

	public static readonly KnownHeader Warning;

	public static readonly KnownHeader XAspNetVersion;

	public static readonly KnownHeader XCache;

	public static readonly KnownHeader XContentDuration;

	public static readonly KnownHeader XContentTypeOptions;

	public static readonly KnownHeader XFrameOptions;

	public static readonly KnownHeader XMSEdgeRef;

	public static readonly KnownHeader XPoweredBy;

	public static readonly KnownHeader XRequestID;

	public static readonly KnownHeader XUACompatible;

	public static readonly KnownHeader XXssProtection;

	private static HttpHeaderParser GetAltSvcHeaderParser()
	{
		return AltSvcHeaderParser.Parser;
	}

	private static KnownHeader GetCandidate<T>(T key) where T : struct, IHeaderNameAccessor
	{
		switch (key.Length)
		{
		case 2:
			return TE;
		case 3:
			switch (key[0] | 0x20)
			{
			case 97:
				return Age;
			case 112:
				return P3P;
			case 116:
				return TSV;
			case 118:
				return Via;
			}
			break;
		case 4:
			switch (key[0] | 0x20)
			{
			case 100:
				return Date;
			case 101:
				return ETag;
			case 102:
				return From;
			case 104:
				return Host;
			case 108:
				return Link;
			case 118:
				return Vary;
			}
			break;
		case 5:
			switch (key[0] | 0x20)
			{
			case 97:
				return Allow;
			case 114:
				return Range;
			}
			break;
		case 6:
			switch (key[0] | 0x20)
			{
			case 97:
				return Accept;
			case 99:
				return Cookie;
			case 101:
				return Expect;
			case 111:
				return Origin;
			case 112:
				return Pragma;
			case 115:
				return Server;
			}
			break;
		case 7:
			switch (key[0] | 0x20)
			{
			case 58:
				return PseudoStatus;
			case 97:
				return AltSvc;
			case 99:
				return Cookie2;
			case 101:
				return Expires;
			case 114:
				switch (key[3] | 0x20)
				{
				case 101:
					return Referer;
				case 114:
					return Refresh;
				}
				break;
			case 116:
				return Trailer;
			case 117:
				return Upgrade;
			case 119:
				return Warning;
			case 120:
				return XCache;
			}
			break;
		case 8:
			switch (key[3] | 0x20)
			{
			case 45:
				return AltUsed;
			case 97:
				return Location;
			case 109:
				return IfMatch;
			case 114:
				return IfRange;
			}
			break;
		case 9:
			return ExpectCT;
		case 10:
			switch (key[0] | 0x20)
			{
			case 99:
				return Connection;
			case 107:
				return KeepAlive;
			case 115:
				return SetCookie;
			case 117:
				return UserAgent;
			}
			break;
		case 11:
			switch (key[0] | 0x20)
			{
			case 99:
				return ContentMD5;
			case 103:
				return GrpcStatus;
			case 114:
				return RetryAfter;
			case 115:
				return SetCookie2;
			}
			break;
		case 12:
			switch (key[5] | 0x20)
			{
			case 100:
				return XMSEdgeRef;
			case 101:
				return XPoweredBy;
			case 109:
				return GrpcMessage;
			case 110:
				return ContentType;
			case 111:
				return MaxForwards;
			case 116:
				return AcceptPatch;
			case 117:
				return XRequestID;
			}
			break;
		case 13:
			switch (key[12] | 0x20)
			{
			case 100:
				return LastModified;
			case 101:
				return ContentRange;
			case 103:
				switch (key[0] | 0x20)
				{
				case 115:
					return ServerTiming;
				case 103:
					return GrpcEncoding;
				}
				break;
			case 104:
				return IfNoneMatch;
			case 108:
				return CacheControl;
			case 110:
				return Authorization;
			case 115:
				return AcceptRanges;
			case 116:
				return ProxySupport;
			}
			break;
		case 14:
			switch (key[0] | 0x20)
			{
			case 97:
				return AcceptCharset;
			case 99:
				return ContentLength;
			}
			break;
		case 15:
			switch (key[7] | 0x20)
			{
			case 45:
				return XFrameOptions;
			case 101:
				return AcceptEncoding;
			case 107:
				return PublicKeyPins;
			case 108:
				return AcceptLanguage;
			case 109:
				return XUACompatible;
			case 114:
				return ReferrerPolicy;
			}
			break;
		case 16:
			switch (key[11] | 0x20)
			{
			case 97:
				return ContentLocation;
			case 99:
				switch (key[0] | 0x20)
				{
				case 112:
					return ProxyConnection;
				case 120:
					return XXssProtection;
				}
				break;
			case 103:
				return ContentLanguage;
			case 105:
				return WWWAuthenticate;
			case 111:
				return ContentEncoding;
			case 114:
				return XAspNetVersion;
			}
			break;
		case 17:
			switch (key[0] | 0x20)
			{
			case 105:
				return IfModifiedSince;
			case 115:
				return SecWebSocketKey;
			case 116:
				return TransferEncoding;
			}
			break;
		case 18:
			switch (key[0] | 0x20)
			{
			case 112:
				return ProxyAuthenticate;
			case 120:
				return XContentDuration;
			}
			break;
		case 19:
			switch (key[0] | 0x20)
			{
			case 99:
				return ContentDisposition;
			case 105:
				return IfUnmodifiedSince;
			case 112:
				return ProxyAuthorization;
			}
			break;
		case 20:
			return SecWebSocketAccept;
		case 21:
			return SecWebSocketVersion;
		case 22:
			switch (key[0] | 0x20)
			{
			case 97:
				return AccessControlMaxAge;
			case 115:
				return SecWebSocketProtocol;
			case 120:
				return XContentTypeOptions;
			}
			break;
		case 23:
			return ContentSecurityPolicy;
		case 24:
			return SecWebSocketExtensions;
		case 25:
			switch (key[0] | 0x20)
			{
			case 115:
				return StrictTransportSecurity;
			case 117:
				return UpgradeInsecureRequests;
			}
			break;
		case 27:
			return AccessControlAllowOrigin;
		case 28:
			switch (key[21] | 0x20)
			{
			case 104:
				return AccessControlAllowHeaders;
			case 109:
				return AccessControlAllowMethods;
			}
			break;
		case 29:
			return AccessControlExposeHeaders;
		case 32:
			return AccessControlAllowCredentials;
		}
		return null;
	}

	internal static KnownHeader TryGetKnownHeader(string name)
	{
		KnownHeader candidate = GetCandidate(new StringAccessor(name));
		if (candidate != null && StringComparer.OrdinalIgnoreCase.Equals(name, candidate.Name))
		{
			return candidate;
		}
		return null;
	}

	internal unsafe static KnownHeader TryGetKnownHeader(ReadOnlySpan<byte> name)
	{
		fixed (byte* p = &MemoryMarshal.GetReference(name))
		{
			KnownHeader candidate = GetCandidate(new BytePtrAccessor(p, name.Length));
			if (candidate != null && ByteArrayHelpers.EqualsOrdinalAsciiIgnoreCase(candidate.Name, name))
			{
				return candidate;
			}
		}
		return null;
	}

	static KnownHeaders()
	{
		string[] knownValues = new string[1] { "true" };
		int? http3StaticTableIndex = 73;
		AccessControlAllowCredentials = new KnownHeader("Access-Control-Allow-Credentials", HttpHeaderType.Response, null, knownValues, null, http3StaticTableIndex);
		string[] knownValues2 = new string[1] { "*" };
		http3StaticTableIndex = 33;
		AccessControlAllowHeaders = new KnownHeader("Access-Control-Allow-Headers", HttpHeaderType.Response, null, knownValues2, null, http3StaticTableIndex);
		string[] knownValues3 = new string[1] { "*" };
		http3StaticTableIndex = 76;
		AccessControlAllowMethods = new KnownHeader("Access-Control-Allow-Methods", HttpHeaderType.Response, null, knownValues3, null, http3StaticTableIndex);
		AccessControlAllowOrigin = new KnownHeader("Access-Control-Allow-Origin", HttpHeaderType.Response, null, new string[2] { "*", "null" }, 20, 35);
		AccessControlExposeHeaders = new KnownHeader("Access-Control-Expose-Headers", HttpHeaderType.Response, null, new string[1] { "*" }, 79);
		AccessControlMaxAge = new KnownHeader("Access-Control-Max-Age");
		Age = new KnownHeader("Age", HttpHeaderType.Response | HttpHeaderType.NonTrailing, TimeSpanHeaderParser.Parser, null, 21, 2);
		Allow = new KnownHeader("Allow", HttpHeaderType.Content, GenericHeaderParser.TokenListParser, null, 22);
		HttpHeaderParser altSvcHeaderParser = GetAltSvcHeaderParser();
		http3StaticTableIndex = 83;
		AltSvc = new KnownHeader("Alt-Svc", HttpHeaderType.Response, altSvcHeaderParser, null, null, http3StaticTableIndex);
		AltUsed = new KnownHeader("Alt-Used", HttpHeaderType.Request, null);
		Authorization = new KnownHeader("Authorization", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.SingleValueAuthenticationParser, null, 23, 84);
		CacheControl = new KnownHeader("Cache-Control", HttpHeaderType.General | HttpHeaderType.NonTrailing, CacheControlHeaderParser.Parser, new string[7] { "must-revalidate", "no-cache", "no-store", "no-transform", "private", "proxy-revalidate", "public" }, 24, 36);
		Connection = new KnownHeader("Connection", HttpHeaderType.General, GenericHeaderParser.TokenListParser, new string[1] { "close" });
		ContentDisposition = new KnownHeader("Content-Disposition", HttpHeaderType.Content | HttpHeaderType.NonTrailing, GenericHeaderParser.ContentDispositionParser, new string[2] { "inline", "attachment" }, 25, 3);
		ContentEncoding = new KnownHeader("Content-Encoding", HttpHeaderType.Content | HttpHeaderType.NonTrailing, GenericHeaderParser.TokenListParser, new string[5] { "gzip", "deflate", "br", "compress", "identity" }, 26, 42);
		ContentLanguage = new KnownHeader("Content-Language", HttpHeaderType.Content, GenericHeaderParser.TokenListParser, null, 27);
		ContentLength = new KnownHeader("Content-Length", HttpHeaderType.Content | HttpHeaderType.NonTrailing, Int64NumberHeaderParser.Parser, null, 28, 4);
		ContentLocation = new KnownHeader("Content-Location", HttpHeaderType.Content | HttpHeaderType.NonTrailing, UriHeaderParser.RelativeOrAbsoluteUriParser, null, 29);
		ContentMD5 = new KnownHeader("Content-MD5", HttpHeaderType.Content, ByteArrayHeaderParser.Parser);
		ContentRange = new KnownHeader("Content-Range", HttpHeaderType.Content | HttpHeaderType.NonTrailing, GenericHeaderParser.ContentRangeParser, null, 30);
		http3StaticTableIndex = 85;
		ContentSecurityPolicy = new KnownHeader("Content-Security-Policy", null, http3StaticTableIndex);
		ContentType = new KnownHeader("Content-Type", HttpHeaderType.Content | HttpHeaderType.NonTrailing, MediaTypeHeaderParser.SingleValueParser, null, 31, 44);
		Cookie = new KnownHeader("Cookie", 32, 5);
		Cookie2 = new KnownHeader("Cookie2");
		Date = new KnownHeader("Date", HttpHeaderType.General | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, null, 33, 6);
		ETag = new KnownHeader("ETag", HttpHeaderType.Response, GenericHeaderParser.SingleValueEntityTagParser, null, 34, 7);
		Expect = new KnownHeader("Expect", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueNameValueWithParametersParser, new string[1] { "100-continue" }, 35);
		ExpectCT = new KnownHeader("Expect-CT");
		Expires = new KnownHeader("Expires", HttpHeaderType.Content | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, null, 36);
		From = new KnownHeader("From", HttpHeaderType.Request, GenericHeaderParser.SingleValueParserWithoutValidation, null, 37);
		GrpcEncoding = new KnownHeader("grpc-encoding", HttpHeaderType.Custom, null, new string[3] { "identity", "gzip", "deflate" });
		GrpcMessage = new KnownHeader("grpc-message");
		GrpcStatus = new KnownHeader("grpc-status", HttpHeaderType.Custom, null, new string[1] { "0" });
		Host = new KnownHeader("Host", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.HostParser, null, 38);
		IfMatch = new KnownHeader("If-Match", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueEntityTagParser, null, 39);
		IfModifiedSince = new KnownHeader("If-Modified-Since", HttpHeaderType.Request | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, null, 40, 8);
		IfNoneMatch = new KnownHeader("If-None-Match", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueEntityTagParser, null, 41, 9);
		IfRange = new KnownHeader("If-Range", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.RangeConditionParser, null, 42, 89);
		IfUnmodifiedSince = new KnownHeader("If-Unmodified-Since", HttpHeaderType.Request | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, null, 43);
		KeepAlive = new KnownHeader("Keep-Alive");
		LastModified = new KnownHeader("Last-Modified", HttpHeaderType.Content, DateHeaderParser.Parser, null, 44, 10);
		Link = new KnownHeader("Link", 45, 11);
		Location = new KnownHeader("Location", HttpHeaderType.Response | HttpHeaderType.NonTrailing, UriHeaderParser.RelativeOrAbsoluteUriParser, null, 46, 12);
		MaxForwards = new KnownHeader("Max-Forwards", HttpHeaderType.Request | HttpHeaderType.NonTrailing, Int32NumberHeaderParser.Parser, null, 47);
		http3StaticTableIndex = 90;
		Origin = new KnownHeader("Origin", null, http3StaticTableIndex);
		P3P = new KnownHeader("P3P");
		Pragma = new KnownHeader("Pragma", HttpHeaderType.General | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueNameValueParser, new string[1] { "no-cache" });
		ProxyAuthenticate = new KnownHeader("Proxy-Authenticate", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueAuthenticationParser, null, 48);
		ProxyAuthorization = new KnownHeader("Proxy-Authorization", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.SingleValueAuthenticationParser, null, 49);
		ProxyConnection = new KnownHeader("Proxy-Connection");
		ProxySupport = new KnownHeader("Proxy-Support");
		PublicKeyPins = new KnownHeader("Public-Key-Pins");
		Range = new KnownHeader("Range", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.RangeParser, null, 50, 55);
		Referer = new KnownHeader("Referer", HttpHeaderType.Request, UriHeaderParser.RelativeOrAbsoluteUriParser, null, 51, 13);
		ReferrerPolicy = new KnownHeader("Referrer-Policy", HttpHeaderType.Custom, null, new string[8] { "strict-origin-when-cross-origin", "origin-when-cross-origin", "strict-origin", "origin", "same-origin", "no-referrer-when-downgrade", "no-referrer", "unsafe-url" });
		Refresh = new KnownHeader("Refresh", 52);
		RetryAfter = new KnownHeader("Retry-After", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.RetryConditionParser, null, 53);
		SecWebSocketAccept = new KnownHeader("Sec-WebSocket-Accept");
		SecWebSocketExtensions = new KnownHeader("Sec-WebSocket-Extensions");
		SecWebSocketKey = new KnownHeader("Sec-WebSocket-Key");
		SecWebSocketProtocol = new KnownHeader("Sec-WebSocket-Protocol");
		SecWebSocketVersion = new KnownHeader("Sec-WebSocket-Version");
		Server = new KnownHeader("Server", HttpHeaderType.Response, ProductInfoHeaderParser.MultipleValueParser, null, 54, 92);
		ServerTiming = new KnownHeader("Server-Timing");
		SetCookie = new KnownHeader("Set-Cookie", HttpHeaderType.Custom | HttpHeaderType.NonTrailing, null, null, 55, 14);
		SetCookie2 = new KnownHeader("Set-Cookie2", HttpHeaderType.Custom | HttpHeaderType.NonTrailing, null);
		StrictTransportSecurity = new KnownHeader("Strict-Transport-Security", 56, 56);
		TE = new KnownHeader("TE", HttpHeaderType.Request | HttpHeaderType.NonTrailing, TransferCodingHeaderParser.MultipleValueWithQualityParser, new string[4] { "trailers", "compress", "deflate", "gzip" });
		TSV = new KnownHeader("TSV");
		Trailer = new KnownHeader("Trailer", HttpHeaderType.General | HttpHeaderType.NonTrailing, GenericHeaderParser.TokenListParser);
		TransferEncoding = new KnownHeader("Transfer-Encoding", HttpHeaderType.General | HttpHeaderType.NonTrailing, TransferCodingHeaderParser.MultipleValueParser, new string[5] { "chunked", "compress", "deflate", "gzip", "identity" }, 57);
		Upgrade = new KnownHeader("Upgrade", HttpHeaderType.General, GenericHeaderParser.MultipleValueProductParser);
		string[] knownValues4 = new string[1] { "1" };
		http3StaticTableIndex = 94;
		UpgradeInsecureRequests = new KnownHeader("Upgrade-Insecure-Requests", HttpHeaderType.Custom, null, knownValues4, null, http3StaticTableIndex);
		UserAgent = new KnownHeader("User-Agent", HttpHeaderType.Request, ProductInfoHeaderParser.MultipleValueParser, null, 58, 95);
		Vary = new KnownHeader("Vary", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.TokenListParser, new string[1] { "*" }, 59, 59);
		Via = new KnownHeader("Via", HttpHeaderType.General, GenericHeaderParser.MultipleValueViaParser, null, 60);
		WWWAuthenticate = new KnownHeader("WWW-Authenticate", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueAuthenticationParser, null, 61);
		Warning = new KnownHeader("Warning", HttpHeaderType.General | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueWarningParser);
		XAspNetVersion = new KnownHeader("X-AspNet-Version");
		XCache = new KnownHeader("X-Cache");
		XContentDuration = new KnownHeader("X-Content-Duration");
		string[] knownValues5 = new string[1] { "nosniff" };
		http3StaticTableIndex = 61;
		XContentTypeOptions = new KnownHeader("X-Content-Type-Options", HttpHeaderType.Custom, null, knownValues5, null, http3StaticTableIndex);
		string[] knownValues6 = new string[2] { "DENY", "SAMEORIGIN" };
		http3StaticTableIndex = 97;
		XFrameOptions = new KnownHeader("X-Frame-Options", HttpHeaderType.Custom, null, knownValues6, null, http3StaticTableIndex);
		XMSEdgeRef = new KnownHeader("X-MSEdge-Ref");
		XPoweredBy = new KnownHeader("X-Powered-By");
		XRequestID = new KnownHeader("X-Request-ID");
		XUACompatible = new KnownHeader("X-UA-Compatible");
		XXssProtection = new KnownHeader("X-XSS-Protection", HttpHeaderType.Custom, null, new string[3] { "0", "1", "1; mode=block" });
	}
}
