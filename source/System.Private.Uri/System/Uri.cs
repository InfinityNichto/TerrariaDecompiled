using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class Uri : ISerializable
{
	[Flags]
	internal enum Flags : ulong
	{
		Zero = 0uL,
		SchemeNotCanonical = 1uL,
		UserNotCanonical = 2uL,
		HostNotCanonical = 4uL,
		PortNotCanonical = 8uL,
		PathNotCanonical = 0x10uL,
		QueryNotCanonical = 0x20uL,
		FragmentNotCanonical = 0x40uL,
		CannotDisplayCanonical = 0x7FuL,
		E_UserNotCanonical = 0x80uL,
		E_HostNotCanonical = 0x100uL,
		E_PortNotCanonical = 0x200uL,
		E_PathNotCanonical = 0x400uL,
		E_QueryNotCanonical = 0x800uL,
		E_FragmentNotCanonical = 0x1000uL,
		E_CannotDisplayCanonical = 0x1F80uL,
		ShouldBeCompressed = 0x2000uL,
		FirstSlashAbsent = 0x4000uL,
		BackslashInPath = 0x8000uL,
		IndexMask = 0xFFFFuL,
		HostTypeMask = 0x70000uL,
		HostNotParsed = 0uL,
		IPv6HostType = 0x10000uL,
		IPv4HostType = 0x20000uL,
		DnsHostType = 0x30000uL,
		UncHostType = 0x40000uL,
		BasicHostType = 0x50000uL,
		UnusedHostType = 0x60000uL,
		UnknownHostType = 0x70000uL,
		UserEscaped = 0x80000uL,
		AuthorityFound = 0x100000uL,
		HasUserInfo = 0x200000uL,
		LoopbackHost = 0x400000uL,
		NotDefaultPort = 0x800000uL,
		UserDrivenParsing = 0x1000000uL,
		CanonicalDnsHost = 0x2000000uL,
		ErrorOrParsingRecursion = 0x4000000uL,
		DosPath = 0x8000000uL,
		UncPath = 0x10000000uL,
		ImplicitFile = 0x20000000uL,
		MinimalUriInfoSet = 0x40000000uL,
		AllUriInfoSet = 0x80000000uL,
		IdnHost = 0x100000000uL,
		HasUnicode = 0x200000000uL,
		HostUnicodeNormalized = 0x400000000uL,
		RestUnicodeNormalized = 0x800000000uL,
		UnicodeHost = 0x1000000000uL,
		IntranetUri = 0x2000000000uL,
		UserIriCanonical = 0x8000000000uL,
		PathIriCanonical = 0x10000000000uL,
		QueryIriCanonical = 0x20000000000uL,
		FragmentIriCanonical = 0x40000000000uL,
		IriCanonical = 0x78000000000uL,
		UnixPath = 0x100000000000uL,
		DisablePathAndQueryCanonicalization = 0x200000000000uL,
		CustomParser_ParseMinimalAlreadyCalled = 0x4000000000000000uL,
		Debug_LeftConstructor = 9223372036854775808uL
	}

	private sealed class UriInfo
	{
		public Offset Offset;

		public string String;

		public string Host;

		public string IdnHost;

		public string PathAndQuery;

		public string ScopeId;

		private MoreInfo _moreInfo;

		public MoreInfo MoreInfo
		{
			get
			{
				if (_moreInfo == null)
				{
					Interlocked.CompareExchange(ref _moreInfo, new MoreInfo(), null);
				}
				return _moreInfo;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct Offset
	{
		public ushort Scheme;

		public ushort User;

		public ushort Host;

		public ushort PortValue;

		public ushort Path;

		public ushort Query;

		public ushort Fragment;

		public ushort End;
	}

	private sealed class MoreInfo
	{
		public string Path;

		public string Query;

		public string Fragment;

		public string AbsoluteUri;

		public string RemoteUrl;
	}

	[Flags]
	private enum Check
	{
		None = 0,
		EscapedCanonical = 1,
		DisplayCanonical = 2,
		DotSlashAttn = 4,
		DotSlashEscaped = 0x80,
		BackslashInPath = 0x10,
		ReservedFound = 0x20,
		NotIriCanonical = 0x40,
		FoundNonAscii = 8
	}

	public static readonly string UriSchemeFile = UriParser.FileUri.SchemeName;

	public static readonly string UriSchemeFtp = UriParser.FtpUri.SchemeName;

	public static readonly string UriSchemeSftp = "sftp";

	public static readonly string UriSchemeFtps = "ftps";

	public static readonly string UriSchemeGopher = UriParser.GopherUri.SchemeName;

	public static readonly string UriSchemeHttp = UriParser.HttpUri.SchemeName;

	public static readonly string UriSchemeHttps = UriParser.HttpsUri.SchemeName;

	public static readonly string UriSchemeWs = UriParser.WsUri.SchemeName;

	public static readonly string UriSchemeWss = UriParser.WssUri.SchemeName;

	public static readonly string UriSchemeMailto = UriParser.MailToUri.SchemeName;

	public static readonly string UriSchemeNews = UriParser.NewsUri.SchemeName;

	public static readonly string UriSchemeNntp = UriParser.NntpUri.SchemeName;

	public static readonly string UriSchemeSsh = "ssh";

	public static readonly string UriSchemeTelnet = UriParser.TelnetUri.SchemeName;

	public static readonly string UriSchemeNetTcp = UriParser.NetTcpUri.SchemeName;

	public static readonly string UriSchemeNetPipe = UriParser.NetPipeUri.SchemeName;

	public static readonly string SchemeDelimiter = "://";

	private string _string;

	private string _originalUnicodeString;

	internal UriParser _syntax;

	internal Flags _flags;

	private UriInfo _info;

	private static readonly char[] s_pathDelims = new char[5] { ':', '\\', '/', '?', '#' };

	private bool IsImplicitFile => (_flags & Flags.ImplicitFile) != 0;

	private bool IsUncOrDosPath => (_flags & (Flags.DosPath | Flags.UncPath)) != 0;

	private bool IsDosPath => (_flags & Flags.DosPath) != 0;

	private bool IsUncPath => (_flags & Flags.UncPath) != 0;

	private Flags HostType => _flags & Flags.HostTypeMask;

	private UriParser Syntax => _syntax;

	private bool IsNotAbsoluteUri => _syntax == null;

	private bool IriParsing => IriParsingStatic(_syntax);

	internal bool DisablePathAndQueryCanonicalization => (_flags & Flags.DisablePathAndQueryCanonicalization) != 0;

	internal bool UserDrivenParsing => (_flags & Flags.UserDrivenParsing) != 0;

	private int SecuredPathIndex
	{
		get
		{
			if (IsDosPath)
			{
				char c = _string[_info.Offset.Path];
				if (c != '/' && c != '\\')
				{
					return 2;
				}
				return 3;
			}
			return 0;
		}
	}

	public string AbsolutePath
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			string text = PrivateAbsolutePath;
			if (IsDosPath && text[0] == '/')
			{
				text = text.Substring(1);
			}
			return text;
		}
	}

	private string PrivateAbsolutePath
	{
		get
		{
			MoreInfo moreInfo = EnsureUriInfo().MoreInfo;
			MoreInfo moreInfo2 = moreInfo;
			return moreInfo2.Path ?? (moreInfo2.Path = GetParts(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.UriEscaped));
		}
	}

	public string AbsoluteUri
	{
		get
		{
			if (_syntax == null)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			MoreInfo moreInfo = EnsureUriInfo().MoreInfo;
			MoreInfo moreInfo2 = moreInfo;
			return moreInfo2.AbsoluteUri ?? (moreInfo2.AbsoluteUri = GetParts(UriComponents.AbsoluteUri, UriFormat.UriEscaped));
		}
	}

	public string LocalPath
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			return GetLocalPath();
		}
	}

	public string Authority
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			return GetParts(UriComponents.Host | UriComponents.Port, UriFormat.UriEscaped);
		}
	}

	public UriHostNameType HostNameType
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			if (_syntax.IsSimple)
			{
				EnsureUriInfo();
			}
			else
			{
				EnsureHostString(allowDnsOptimization: false);
			}
			return HostType switch
			{
				Flags.DnsHostType => UriHostNameType.Dns, 
				Flags.IPv4HostType => UriHostNameType.IPv4, 
				Flags.IPv6HostType => UriHostNameType.IPv6, 
				Flags.BasicHostType => UriHostNameType.Basic, 
				Flags.UncHostType => UriHostNameType.Basic, 
				Flags.HostTypeMask => UriHostNameType.Unknown, 
				_ => UriHostNameType.Unknown, 
			};
		}
	}

	public bool IsDefaultPort
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			if (_syntax.IsSimple)
			{
				EnsureUriInfo();
			}
			else
			{
				EnsureHostString(allowDnsOptimization: false);
			}
			return NotAny(Flags.NotDefaultPort);
		}
	}

	public bool IsFile
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			return (object)_syntax.SchemeName == UriSchemeFile;
		}
	}

	public bool IsLoopback
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			EnsureHostString(allowDnsOptimization: false);
			return InFact(Flags.LoopbackHost);
		}
	}

	public string PathAndQuery
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			UriInfo uriInfo = EnsureUriInfo();
			if (uriInfo.PathAndQuery == null)
			{
				string text = GetParts(UriComponents.PathAndQuery, UriFormat.UriEscaped);
				if (IsDosPath && text[0] == '/')
				{
					text = text.Substring(1);
				}
				uriInfo.PathAndQuery = text;
			}
			return uriInfo.PathAndQuery;
		}
	}

	public string[] Segments
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			string privateAbsolutePath = PrivateAbsolutePath;
			if (privateAbsolutePath.Length == 0)
			{
				return Array.Empty<string>();
			}
			ArrayBuilder<string> arrayBuilder = default(ArrayBuilder<string>);
			int num = 0;
			while (num < privateAbsolutePath.Length)
			{
				int num2 = privateAbsolutePath.IndexOf('/', num);
				if (num2 == -1)
				{
					num2 = privateAbsolutePath.Length - 1;
				}
				arrayBuilder.Add(privateAbsolutePath.Substring(num, num2 - num + 1));
				num = num2 + 1;
			}
			return arrayBuilder.ToArray();
		}
	}

	public bool IsUnc
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			return IsUncPath;
		}
	}

	public string Host
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			return GetParts(UriComponents.Host, UriFormat.UriEscaped);
		}
	}

	public int Port
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			if (_syntax.IsSimple)
			{
				EnsureUriInfo();
			}
			else
			{
				EnsureHostString(allowDnsOptimization: false);
			}
			if (InFact(Flags.NotDefaultPort))
			{
				return _info.Offset.PortValue;
			}
			return _syntax.DefaultPort;
		}
	}

	public string Query
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			MoreInfo moreInfo = EnsureUriInfo().MoreInfo;
			MoreInfo moreInfo2 = moreInfo;
			return moreInfo2.Query ?? (moreInfo2.Query = GetParts(UriComponents.Query | UriComponents.KeepDelimiter, UriFormat.UriEscaped));
		}
	}

	public string Fragment
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			MoreInfo moreInfo = EnsureUriInfo().MoreInfo;
			MoreInfo moreInfo2 = moreInfo;
			return moreInfo2.Fragment ?? (moreInfo2.Fragment = GetParts(UriComponents.Fragment | UriComponents.KeepDelimiter, UriFormat.UriEscaped));
		}
	}

	public string Scheme
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			return _syntax.SchemeName;
		}
	}

	public string OriginalString => _originalUnicodeString ?? _string;

	public string DnsSafeHost
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			EnsureHostString(allowDnsOptimization: false);
			Flags hostType = HostType;
			if (hostType == Flags.IPv6HostType || (hostType == Flags.BasicHostType && InFact(Flags.HostNotCanonical | Flags.E_HostNotCanonical)))
			{
				return IdnHost;
			}
			return _info.Host;
		}
	}

	public string IdnHost
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			if (_info?.IdnHost == null)
			{
				EnsureHostString(allowDnsOptimization: false);
				string text = _info.Host;
				switch (HostType)
				{
				case Flags.DnsHostType:
					text = DomainNameHelper.IdnEquivalent(text);
					break;
				case Flags.IPv6HostType:
					text = ((_info.ScopeId != null) ? string.Concat(text.AsSpan(1, text.Length - 2), _info.ScopeId) : text.Substring(1, text.Length - 2));
					break;
				case Flags.BasicHostType:
					if (InFact(Flags.HostNotCanonical | Flags.E_HostNotCanonical))
					{
						Span<char> initialBuffer = stackalloc char[512];
						System.Text.ValueStringBuilder dest = new System.Text.ValueStringBuilder(initialBuffer);
						UriHelper.UnescapeString(text, 0, text.Length, ref dest, '\uffff', '\uffff', '\uffff', UnescapeMode.Unescape | UnescapeMode.UnescapeAll, _syntax, isQuery: false);
						text = dest.ToString();
					}
					break;
				}
				_info.IdnHost = text;
			}
			return _info.IdnHost;
		}
	}

	public bool IsAbsoluteUri => _syntax != null;

	public bool UserEscaped => InFact(Flags.UserEscaped);

	public string UserInfo
	{
		get
		{
			if (IsNotAbsoluteUri)
			{
				throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
			}
			return GetParts(UriComponents.UserInfo, UriFormat.UriEscaped);
		}
	}

	private void InterlockedSetFlags(Flags flags)
	{
		if (_syntax.IsSimple)
		{
			Interlocked.Or(ref Internal.Runtime.CompilerServices.Unsafe.As<Flags, ulong>(ref _flags), (ulong)flags);
			return;
		}
		lock (_info)
		{
			_flags |= flags;
		}
	}

	internal static bool IriParsingStatic(UriParser syntax)
	{
		return syntax?.InFact(UriSyntaxFlags.AllowIriParsing) ?? true;
	}

	private bool NotAny(Flags flags)
	{
		return (_flags & flags) == 0;
	}

	private bool InFact(Flags flags)
	{
		return (_flags & flags) != 0;
	}

	private static bool StaticNotAny(Flags allFlags, Flags checkFlags)
	{
		return (allFlags & checkFlags) == 0;
	}

	private static bool StaticInFact(Flags allFlags, Flags checkFlags)
	{
		return (allFlags & checkFlags) != 0;
	}

	private UriInfo EnsureUriInfo()
	{
		Flags flags = _flags;
		if ((flags & Flags.MinimalUriInfoSet) == Flags.Zero)
		{
			CreateUriInfo(flags);
		}
		return _info;
	}

	private void EnsureParseRemaining()
	{
		if ((_flags & Flags.AllUriInfoSet) == Flags.Zero)
		{
			ParseRemaining();
		}
	}

	private void EnsureHostString(bool allowDnsOptimization)
	{
		UriInfo uriInfo = EnsureUriInfo();
		if (uriInfo.Host == null && (!allowDnsOptimization || !InFact(Flags.CanonicalDnsHost)))
		{
			CreateHostString();
		}
	}

	public Uri(string uriString)
	{
		if (uriString == null)
		{
			throw new ArgumentNullException("uriString");
		}
		UriCreationOptions creationOptions = default(UriCreationOptions);
		CreateThis(uriString, dontEscape: false, UriKind.Absolute, in creationOptions);
	}

	[Obsolete("This constructor has been deprecated; the dontEscape parameter is always false. Use Uri(string) instead.")]
	public Uri(string uriString, bool dontEscape)
	{
		if (uriString == null)
		{
			throw new ArgumentNullException("uriString");
		}
		UriCreationOptions creationOptions = default(UriCreationOptions);
		CreateThis(uriString, dontEscape, UriKind.Absolute, in creationOptions);
	}

	[Obsolete("This constructor has been deprecated; the dontEscape parameter is always false. Use Uri(Uri, string) instead.")]
	public Uri(Uri baseUri, string? relativeUri, bool dontEscape)
	{
		if ((object)baseUri == null)
		{
			throw new ArgumentNullException("baseUri");
		}
		if (!baseUri.IsAbsoluteUri)
		{
			throw new ArgumentOutOfRangeException("baseUri");
		}
		CreateUri(baseUri, relativeUri, dontEscape);
	}

	public Uri(string uriString, UriKind uriKind)
	{
		if (uriString == null)
		{
			throw new ArgumentNullException("uriString");
		}
		UriCreationOptions creationOptions = default(UriCreationOptions);
		CreateThis(uriString, dontEscape: false, uriKind, in creationOptions);
	}

	public Uri(string uriString, in UriCreationOptions creationOptions)
	{
		if (uriString == null)
		{
			throw new ArgumentNullException("uriString");
		}
		CreateThis(uriString, dontEscape: false, UriKind.Absolute, in creationOptions);
	}

	public Uri(Uri baseUri, string? relativeUri)
	{
		if ((object)baseUri == null)
		{
			throw new ArgumentNullException("baseUri");
		}
		if (!baseUri.IsAbsoluteUri)
		{
			throw new ArgumentOutOfRangeException("baseUri");
		}
		CreateUri(baseUri, relativeUri, dontEscape: false);
	}

	protected Uri(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		string @string = serializationInfo.GetString("AbsoluteUri");
		UriCreationOptions creationOptions;
		if (@string.Length != 0)
		{
			string uri = @string;
			creationOptions = default(UriCreationOptions);
			CreateThis(uri, dontEscape: false, UriKind.Absolute, in creationOptions);
			return;
		}
		@string = serializationInfo.GetString("RelativeUri");
		if (@string == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "RelativeUri"), "serializationInfo");
		}
		string uri2 = @string;
		creationOptions = default(UriCreationOptions);
		CreateThis(uri2, dontEscape: false, UriKind.Relative, in creationOptions);
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		GetObjectData(serializationInfo, streamingContext);
	}

	protected void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		if (IsAbsoluteUri)
		{
			serializationInfo.AddValue("AbsoluteUri", GetParts(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
			return;
		}
		serializationInfo.AddValue("AbsoluteUri", string.Empty);
		serializationInfo.AddValue("RelativeUri", GetParts(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
	}

	private void CreateUri(Uri baseUri, string relativeUri, bool dontEscape)
	{
		string uri = relativeUri;
		bool dontEscape2 = dontEscape;
		UriCreationOptions creationOptions = default(UriCreationOptions);
		CreateThis(uri, dontEscape2, UriKind.RelativeOrAbsolute, in creationOptions);
		if (baseUri.Syntax.IsSimple)
		{
			Uri uri2 = ResolveHelper(baseUri, this, ref relativeUri, ref dontEscape);
			if (uri2 != null)
			{
				if ((object)this != uri2)
				{
					CreateThisFromUri(uri2);
				}
				return;
			}
		}
		else
		{
			dontEscape = false;
			relativeUri = baseUri.Syntax.InternalResolve(baseUri, this, out var parsingError);
			if (parsingError != null)
			{
				throw parsingError;
			}
		}
		_flags = Flags.Zero;
		_info = null;
		_syntax = null;
		_originalUnicodeString = null;
		string uri3 = relativeUri;
		bool dontEscape3 = dontEscape;
		creationOptions = default(UriCreationOptions);
		CreateThis(uri3, dontEscape3, UriKind.Absolute, in creationOptions);
	}

	public Uri(Uri baseUri, Uri relativeUri)
	{
		if ((object)baseUri == null)
		{
			throw new ArgumentNullException("baseUri");
		}
		if (!baseUri.IsAbsoluteUri)
		{
			throw new ArgumentOutOfRangeException("baseUri");
		}
		CreateThisFromUri(relativeUri);
		string newUriString = null;
		bool userEscaped;
		if (baseUri.Syntax.IsSimple)
		{
			userEscaped = InFact(Flags.UserEscaped);
			Uri uri = ResolveHelper(baseUri, this, ref newUriString, ref userEscaped);
			if (uri != null)
			{
				if ((object)this != uri)
				{
					CreateThisFromUri(uri);
				}
				return;
			}
		}
		else
		{
			userEscaped = false;
			newUriString = baseUri.Syntax.InternalResolve(baseUri, this, out var parsingError);
			if (parsingError != null)
			{
				throw parsingError;
			}
		}
		_flags = Flags.Zero;
		_info = null;
		_syntax = null;
		_originalUnicodeString = null;
		string uri2 = newUriString;
		bool dontEscape = userEscaped;
		UriCreationOptions creationOptions = default(UriCreationOptions);
		CreateThis(uri2, dontEscape, UriKind.Absolute, in creationOptions);
	}

	private static void GetCombinedString(Uri baseUri, string relativeStr, bool dontEscape, ref string result)
	{
		for (int i = 0; i < relativeStr.Length && relativeStr[i] != '/' && relativeStr[i] != '\\' && relativeStr[i] != '?' && relativeStr[i] != '#'; i++)
		{
			if (relativeStr[i] == ':')
			{
				if (i < 2)
				{
					break;
				}
				UriParser syntax = null;
				if (CheckSchemeSyntax(relativeStr.AsSpan(0, i), ref syntax) != 0)
				{
					break;
				}
				if (baseUri.Syntax == syntax)
				{
					relativeStr = ((i + 1 >= relativeStr.Length) ? string.Empty : relativeStr.Substring(i + 1));
					break;
				}
				result = relativeStr;
				return;
			}
		}
		if (relativeStr.Length == 0)
		{
			result = baseUri.OriginalString;
		}
		else
		{
			result = CombineUri(baseUri, relativeStr, dontEscape ? UriFormat.UriEscaped : UriFormat.SafeUnescaped);
		}
	}

	private static UriFormatException GetException(ParsingError err)
	{
		return err switch
		{
			ParsingError.None => null, 
			ParsingError.BadFormat => new UriFormatException(System.SR.net_uri_BadFormat), 
			ParsingError.BadScheme => new UriFormatException(System.SR.net_uri_BadScheme), 
			ParsingError.BadAuthority => new UriFormatException(System.SR.net_uri_BadAuthority), 
			ParsingError.EmptyUriString => new UriFormatException(System.SR.net_uri_EmptyUri), 
			ParsingError.SchemeLimit => new UriFormatException(System.SR.net_uri_SchemeLimit), 
			ParsingError.SizeLimit => new UriFormatException(System.SR.net_uri_SizeLimit), 
			ParsingError.MustRootedPath => new UriFormatException(System.SR.net_uri_MustRootedPath), 
			ParsingError.BadHostName => new UriFormatException(System.SR.net_uri_BadHostName), 
			ParsingError.NonEmptyHost => new UriFormatException(System.SR.net_uri_BadFormat), 
			ParsingError.BadPort => new UriFormatException(System.SR.net_uri_BadPort), 
			ParsingError.BadAuthorityTerminator => new UriFormatException(System.SR.net_uri_BadAuthorityTerminator), 
			ParsingError.CannotCreateRelative => new UriFormatException(System.SR.net_uri_CannotCreateRelative), 
			_ => new UriFormatException(System.SR.net_uri_BadFormat), 
		};
	}

	private static bool StaticIsFile(UriParser syntax)
	{
		return syntax.InFact(UriSyntaxFlags.FileLikeUri);
	}

	private string GetLocalPath()
	{
		EnsureParseRemaining();
		if (IsUncOrDosPath)
		{
			EnsureHostString(allowDnsOptimization: false);
			int num;
			if (NotAny(Flags.HostNotCanonical | Flags.PathNotCanonical | Flags.ShouldBeCompressed))
			{
				num = (IsUncPath ? (_info.Offset.Host - 2) : _info.Offset.Path);
				string text = ((IsImplicitFile && _info.Offset.Host == ((!IsDosPath) ? 2 : 0) && _info.Offset.Query == _info.Offset.End) ? _string : ((IsDosPath && (_string[num] == '/' || _string[num] == '\\')) ? _string.Substring(num + 1, _info.Offset.Query - num - 1) : _string.Substring(num, _info.Offset.Query - num)));
				if (IsDosPath && text[1] == '|')
				{
					text = text.Remove(1, 1);
					text = text.Insert(1, ":");
				}
				for (int i = 0; i < text.Length; i++)
				{
					if (text[i] == '/')
					{
						text = text.Replace('/', '\\');
						break;
					}
				}
				return text;
			}
			int destPosition = 0;
			num = _info.Offset.Path;
			string host = _info.Host;
			char[] array = new char[host.Length + 3 + _info.Offset.Fragment - _info.Offset.Path];
			if (IsUncPath)
			{
				array[0] = '\\';
				array[1] = '\\';
				destPosition = 2;
				UriHelper.UnescapeString(host, 0, host.Length, array, ref destPosition, '\uffff', '\uffff', '\uffff', UnescapeMode.CopyOnly, _syntax, isQuery: false);
			}
			else if (_string[num] == '/' || _string[num] == '\\')
			{
				num++;
			}
			ushort num2 = (ushort)destPosition;
			UnescapeMode unescapeMode = ((InFact(Flags.PathNotCanonical) && !IsImplicitFile) ? (UnescapeMode.Unescape | UnescapeMode.UnescapeAll) : UnescapeMode.CopyOnly);
			UriHelper.UnescapeString(_string, num, _info.Offset.Query, array, ref destPosition, '\uffff', '\uffff', '\uffff', unescapeMode, _syntax, isQuery: true);
			if (array[1] == '|')
			{
				array[1] = ':';
			}
			if (InFact(Flags.ShouldBeCompressed))
			{
				Compress(array, IsDosPath ? (num2 + 2) : num2, ref destPosition, _syntax);
			}
			for (ushort num3 = 0; num3 < (ushort)destPosition; num3++)
			{
				if (array[num3] == '/')
				{
					array[num3] = '\\';
				}
			}
			return new string(array, 0, destPosition);
		}
		return GetUnescapedParts(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
	}

	public unsafe static UriHostNameType CheckHostName(string? name)
	{
		if (string.IsNullOrEmpty(name) || name.Length > 32767)
		{
			return UriHostNameType.Unknown;
		}
		int end = name.Length;
		fixed (char* name2 = name)
		{
			if (name[0] == '[' && name[name.Length - 1] == ']' && IPv6AddressHelper.IsValid(name2, 1, ref end) && end == name.Length)
			{
				return UriHostNameType.IPv6;
			}
			end = name.Length;
			if (IPv4AddressHelper.IsValid(name2, 0, ref end, allowIPv6: false, notImplicitFile: false, unknownScheme: false) && end == name.Length)
			{
				return UriHostNameType.IPv4;
			}
			end = name.Length;
			bool notCanonical = false;
			if (DomainNameHelper.IsValid(name2, 0, ref end, ref notCanonical, notImplicitFile: false) && end == name.Length)
			{
				return UriHostNameType.Dns;
			}
			end = name.Length;
			notCanonical = false;
			if (DomainNameHelper.IsValidByIri(name2, 0, ref end, ref notCanonical, notImplicitFile: false) && end == name.Length)
			{
				return UriHostNameType.Dns;
			}
		}
		end = name.Length + 2;
		name = "[" + name + "]";
		fixed (char* name3 = name)
		{
			if (IPv6AddressHelper.IsValid(name3, 1, ref end) && end == name.Length)
			{
				return UriHostNameType.IPv6;
			}
		}
		return UriHostNameType.Unknown;
	}

	public string GetLeftPart(UriPartial part)
	{
		if (IsNotAbsoluteUri)
		{
			throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
		}
		EnsureUriInfo();
		switch (part)
		{
		case UriPartial.Scheme:
			return GetParts(UriComponents.Scheme | UriComponents.KeepDelimiter, UriFormat.UriEscaped);
		case UriPartial.Authority:
			if (NotAny(Flags.AuthorityFound) || IsDosPath)
			{
				return string.Empty;
			}
			return GetParts(UriComponents.SchemeAndServer | UriComponents.UserInfo, UriFormat.UriEscaped);
		case UriPartial.Path:
			return GetParts(UriComponents.SchemeAndServer | UriComponents.UserInfo | UriComponents.Path, UriFormat.UriEscaped);
		case UriPartial.Query:
			return GetParts(UriComponents.HttpRequestUrl | UriComponents.UserInfo, UriFormat.UriEscaped);
		default:
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidUriSubcomponent, part), "part");
		}
	}

	public static string HexEscape(char character)
	{
		if (character > 'ÿ')
		{
			throw new ArgumentOutOfRangeException("character");
		}
		return string.Create(3, (byte)character, delegate(Span<char> chars, byte b)
		{
			chars[0] = '%';
			System.HexConverter.ToCharsBuffer(b, chars, 1);
		});
	}

	public static char HexUnescape(string pattern, ref int index)
	{
		if (index < 0 || index >= pattern.Length)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (pattern[index] == '%' && pattern.Length - index >= 3)
		{
			char c = UriHelper.DecodeHexChars(pattern[index + 1], pattern[index + 2]);
			if (c != '\uffff')
			{
				index += 3;
				return c;
			}
		}
		return pattern[index++];
	}

	public static bool IsHexEncoding(string pattern, int index)
	{
		if (pattern.Length - index >= 3 && pattern[index] == '%' && IsHexDigit(pattern[index + 1]))
		{
			return IsHexDigit(pattern[index + 2]);
		}
		return false;
	}

	public static bool CheckSchemeName([NotNullWhen(true)] string? schemeName)
	{
		if (string.IsNullOrEmpty(schemeName) || !UriHelper.IsAsciiLetter(schemeName[0]))
		{
			return false;
		}
		for (int num = schemeName.Length - 1; num > 0; num--)
		{
			if (!UriHelper.IsAsciiLetterOrDigit(schemeName[num]) && schemeName[num] != '+' && schemeName[num] != '-' && schemeName[num] != '.')
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsHexDigit(char character)
	{
		return System.HexConverter.IsHexChar(character);
	}

	public static int FromHex(char digit)
	{
		int num = System.HexConverter.FromChar(digit);
		if (num == 255)
		{
			throw new ArgumentException(null, "digit");
		}
		return num;
	}

	public override int GetHashCode()
	{
		if (IsNotAbsoluteUri)
		{
			return OriginalString.GetHashCode();
		}
		MoreInfo moreInfo = EnsureUriInfo().MoreInfo;
		MoreInfo moreInfo2 = moreInfo;
		string text = moreInfo2.RemoteUrl ?? (moreInfo2.RemoteUrl = GetParts(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped));
		if (IsUncOrDosPath)
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(text);
		}
		return text.GetHashCode();
	}

	public override string ToString()
	{
		if (_syntax == null)
		{
			return _string;
		}
		EnsureUriInfo();
		if (_info.String == null)
		{
			if (_syntax.IsSimple)
			{
				_info.String = GetComponentsHelper(UriComponents.AbsoluteUri, (UriFormat)32767);
			}
			else
			{
				_info.String = GetParts(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
			}
		}
		return _info.String;
	}

	public static bool operator ==(Uri? uri1, Uri? uri2)
	{
		if ((object)uri1 == uri2)
		{
			return true;
		}
		if ((object)uri1 == null || (object)uri2 == null)
		{
			return false;
		}
		return uri1.Equals(uri2);
	}

	public static bool operator !=(Uri? uri1, Uri? uri2)
	{
		if ((object)uri1 == uri2)
		{
			return false;
		}
		if ((object)uri1 == null || (object)uri2 == null)
		{
			return true;
		}
		return !uri1.Equals(uri2);
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (comparand == null)
		{
			return false;
		}
		if (this == comparand)
		{
			return true;
		}
		Uri result = comparand as Uri;
		if ((object)result == null)
		{
			if (DisablePathAndQueryCanonicalization)
			{
				return false;
			}
			if (!(comparand is string text))
			{
				return false;
			}
			if ((object)text == OriginalString)
			{
				return true;
			}
			if (!TryCreate(text, UriKind.RelativeOrAbsolute, out result))
			{
				return false;
			}
		}
		if (DisablePathAndQueryCanonicalization != result.DisablePathAndQueryCanonicalization)
		{
			return false;
		}
		if ((object)OriginalString == result.OriginalString)
		{
			return true;
		}
		if (IsAbsoluteUri != result.IsAbsoluteUri)
		{
			return false;
		}
		if (IsNotAbsoluteUri)
		{
			return OriginalString.Equals(result.OriginalString);
		}
		if ((NotAny(Flags.AllUriInfoSet) || result.NotAny(Flags.AllUriInfoSet)) && string.Equals(_string, result._string, IsUncOrDosPath ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
		{
			return true;
		}
		EnsureUriInfo();
		result.EnsureUriInfo();
		if (!UserDrivenParsing && !result.UserDrivenParsing && Syntax.IsSimple && result.Syntax.IsSimple)
		{
			if (InFact(Flags.CanonicalDnsHost) && result.InFact(Flags.CanonicalDnsHost))
			{
				int num = _info.Offset.Host;
				int num2 = _info.Offset.Path;
				int num3 = result._info.Offset.Host;
				int path = result._info.Offset.Path;
				string @string = result._string;
				if (num2 - num > path - num3)
				{
					num2 = num + path - num3;
				}
				while (num < num2)
				{
					if (_string[num] != @string[num3])
					{
						return false;
					}
					if (@string[num3] == ':')
					{
						break;
					}
					num++;
					num3++;
				}
				if (num < _info.Offset.Path && _string[num] != ':')
				{
					return false;
				}
				if (num3 < path && @string[num3] != ':')
				{
					return false;
				}
			}
			else
			{
				EnsureHostString(allowDnsOptimization: false);
				result.EnsureHostString(allowDnsOptimization: false);
				if (!_info.Host.Equals(result._info.Host))
				{
					return false;
				}
			}
			if (Port != result.Port)
			{
				return false;
			}
		}
		MoreInfo moreInfo = _info.MoreInfo;
		MoreInfo moreInfo2 = result._info.MoreInfo;
		MoreInfo moreInfo3 = moreInfo;
		string a = moreInfo3.RemoteUrl ?? (moreInfo3.RemoteUrl = GetParts(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped));
		moreInfo3 = moreInfo2;
		string b = moreInfo3.RemoteUrl ?? (moreInfo3.RemoteUrl = result.GetParts(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped));
		return string.Equals(a, b, IsUncOrDosPath ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	public Uri MakeRelativeUri(Uri uri)
	{
		if ((object)uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (IsNotAbsoluteUri || uri.IsNotAbsoluteUri)
		{
			throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
		}
		if (Scheme == uri.Scheme && Host == uri.Host && Port == uri.Port)
		{
			string absolutePath = uri.AbsolutePath;
			string text = PathDifference(AbsolutePath, absolutePath, !IsUncOrDosPath);
			if (CheckForColonInFirstPathSegment(text) && (!uri.IsDosPath || !absolutePath.Equals(text, StringComparison.Ordinal)))
			{
				text = "./" + text;
			}
			text += uri.GetParts(UriComponents.Query | UriComponents.Fragment, UriFormat.UriEscaped);
			return new Uri(text, UriKind.Relative);
		}
		return uri;
	}

	private static bool CheckForColonInFirstPathSegment(string uriString)
	{
		int num = uriString.IndexOfAny(s_pathDelims);
		if (num >= 0)
		{
			return uriString[num] == ':';
		}
		return false;
	}

	internal static string InternalEscapeString(string rawString)
	{
		if (rawString != null)
		{
			return UriHelper.EscapeString(rawString, checkExistingEscaped: true, UriHelper.UnreservedReservedTable, '?', '#');
		}
		return string.Empty;
	}

	private unsafe static ParsingError ParseScheme(string uriString, ref Flags flags, ref UriParser syntax)
	{
		int length = uriString.Length;
		if (length == 0)
		{
			return ParsingError.EmptyUriString;
		}
		if (length >= 65520)
		{
			return ParsingError.SizeLimit;
		}
		fixed (char* uriString2 = uriString)
		{
			ParsingError err = ParsingError.None;
			int num = ParseSchemeCheckImplicitFile(uriString2, length, ref err, ref flags, ref syntax);
			if (err != 0)
			{
				return err;
			}
			flags |= (Flags)num;
		}
		return ParsingError.None;
	}

	internal UriFormatException ParseMinimal()
	{
		ParsingError parsingError = PrivateParseMinimal();
		if (parsingError == ParsingError.None)
		{
			return null;
		}
		_flags |= Flags.ErrorOrParsingRecursion;
		return GetException(parsingError);
	}

	private unsafe ParsingError PrivateParseMinimal()
	{
		int num = (int)(_flags & Flags.IndexMask);
		int num2 = _string.Length;
		string newHost = null;
		_flags &= ~(Flags.IndexMask | Flags.UserDrivenParsing);
		fixed (char* ptr = (((_flags & Flags.HostUnicodeNormalized) == Flags.Zero) ? OriginalString : _string))
		{
			if (num2 > num && UriHelper.IsLWS(ptr[num2 - 1]))
			{
				num2--;
				while (num2 != num && UriHelper.IsLWS(ptr[--num2]))
				{
				}
				num2++;
			}
			if (_syntax.IsAllSet(UriSyntaxFlags.AllowEmptyHost | UriSyntaxFlags.AllowDOSPath) && NotAny(Flags.ImplicitFile) && num + 1 < num2)
			{
				int i;
				for (i = num; i < num2; i++)
				{
					char c;
					if ((c = ptr[i]) != '\\' && c != '/')
					{
						break;
					}
				}
				if (_syntax.InFact(UriSyntaxFlags.FileLikeUri) || i - num <= 3)
				{
					if (i - num >= 2)
					{
						_flags |= Flags.AuthorityFound;
					}
					char c;
					if (i + 1 < num2 && ((c = ptr[i + 1]) == ':' || c == '|') && UriHelper.IsAsciiLetter(ptr[i]))
					{
						if (i + 2 >= num2 || ((c = ptr[i + 2]) != '\\' && c != '/'))
						{
							if (_syntax.InFact(UriSyntaxFlags.FileLikeUri))
							{
								return ParsingError.MustRootedPath;
							}
						}
						else
						{
							_flags |= Flags.DosPath;
							if (_syntax.InFact(UriSyntaxFlags.MustHaveAuthority))
							{
								_flags |= Flags.AuthorityFound;
							}
							num = ((i == num || i - num == 2) ? i : (i - 1));
						}
					}
					else if (_syntax.InFact(UriSyntaxFlags.FileLikeUri) && i - num >= 2 && i - num != 3 && i < num2 && ptr[i] != '?' && ptr[i] != '#')
					{
						_flags |= Flags.UncPath;
						num = i;
					}
				}
			}
			if ((_flags & (Flags.DosPath | Flags.UncPath | Flags.UnixPath)) == Flags.Zero)
			{
				if (num + 2 <= num2)
				{
					char c2 = ptr[num];
					char c3 = ptr[num + 1];
					if (_syntax.InFact(UriSyntaxFlags.MustHaveAuthority))
					{
						if ((c2 != '/' && c2 != '\\') || (c3 != '/' && c3 != '\\'))
						{
							return ParsingError.BadAuthority;
						}
						_flags |= Flags.AuthorityFound;
						num += 2;
					}
					else if (_syntax.InFact(UriSyntaxFlags.OptionalAuthority) && (InFact(Flags.AuthorityFound) || (c2 == '/' && c3 == '/')))
					{
						_flags |= Flags.AuthorityFound;
						num += 2;
					}
					else if (_syntax.NotAny(UriSyntaxFlags.MailToLikeUri))
					{
						if ((_flags & (Flags.HasUnicode | Flags.HostUnicodeNormalized)) == Flags.HasUnicode)
						{
							_string = _string.Substring(0, num);
						}
						_flags |= (Flags)((ulong)num | 0x70000uL);
						return ParsingError.None;
					}
				}
				else
				{
					if (_syntax.InFact(UriSyntaxFlags.MustHaveAuthority))
					{
						return ParsingError.BadAuthority;
					}
					if (_syntax.NotAny(UriSyntaxFlags.MailToLikeUri))
					{
						if ((_flags & (Flags.HasUnicode | Flags.HostUnicodeNormalized)) == Flags.HasUnicode)
						{
							_string = _string.Substring(0, num);
						}
						_flags |= (Flags)((ulong)num | 0x70000uL);
						return ParsingError.None;
					}
				}
			}
			if (InFact(Flags.DosPath))
			{
				_flags |= (Flags)(((_flags & Flags.AuthorityFound) != Flags.Zero) ? 327680 : 458752);
				_flags |= (Flags)num;
				return ParsingError.None;
			}
			ParsingError err = ParsingError.None;
			num = CheckAuthorityHelper(ptr, num, num2, ref err, ref _flags, _syntax, ref newHost);
			if (err != 0)
			{
				return err;
			}
			if (num < num2)
			{
				char c4 = ptr[num];
				if (c4 == '\\' && NotAny(Flags.ImplicitFile) && _syntax.NotAny(UriSyntaxFlags.AllowDOSPath))
				{
					return ParsingError.BadAuthorityTerminator;
				}
			}
			_flags |= (Flags)num;
		}
		if (IriParsing && newHost != null)
		{
			_string = newHost;
		}
		return ParsingError.None;
	}

	private unsafe void CreateUriInfo(Flags cF)
	{
		UriInfo uriInfo = new UriInfo();
		uriInfo.Offset.End = (ushort)_string.Length;
		if (!UserDrivenParsing)
		{
			bool flag = false;
			int i;
			if ((cF & Flags.ImplicitFile) != Flags.Zero)
			{
				i = 0;
				while (UriHelper.IsLWS(_string[i]))
				{
					i++;
					uriInfo.Offset.Scheme++;
				}
				if (StaticInFact(cF, Flags.UncPath))
				{
					i += 2;
					for (int num = (int)(cF & Flags.IndexMask); i < num && (_string[i] == '/' || _string[i] == '\\'); i++)
					{
					}
				}
			}
			else
			{
				i = _syntax.SchemeName.Length;
				while (_string[i++] != ':')
				{
					uriInfo.Offset.Scheme++;
				}
				if ((cF & Flags.AuthorityFound) != Flags.Zero)
				{
					if (_string[i] == '\\' || _string[i + 1] == '\\')
					{
						flag = true;
					}
					i += 2;
					if ((cF & (Flags.DosPath | Flags.UncPath)) != Flags.Zero)
					{
						for (int num2 = (int)(cF & Flags.IndexMask); i < num2 && (_string[i] == '/' || _string[i] == '\\'); i++)
						{
							flag = true;
						}
					}
				}
			}
			if (_syntax.DefaultPort != -1)
			{
				uriInfo.Offset.PortValue = (ushort)_syntax.DefaultPort;
			}
			if ((cF & Flags.HostTypeMask) == Flags.HostTypeMask || StaticInFact(cF, Flags.DosPath))
			{
				uriInfo.Offset.User = (ushort)(cF & Flags.IndexMask);
				uriInfo.Offset.Host = uriInfo.Offset.User;
				uriInfo.Offset.Path = uriInfo.Offset.User;
				cF = (Flags)((ulong)cF & 0xFFFFFFFFFFFF0000uL);
				if (flag)
				{
					cF |= Flags.SchemeNotCanonical;
				}
			}
			else
			{
				uriInfo.Offset.User = (ushort)i;
				if (HostType == Flags.BasicHostType)
				{
					uriInfo.Offset.Host = (ushort)i;
					uriInfo.Offset.Path = (ushort)(cF & Flags.IndexMask);
					cF = (Flags)((ulong)cF & 0xFFFFFFFFFFFF0000uL);
				}
				else
				{
					if ((cF & Flags.HasUserInfo) != Flags.Zero)
					{
						for (; _string[i] != '@'; i++)
						{
						}
						i++;
						uriInfo.Offset.Host = (ushort)i;
					}
					else
					{
						uriInfo.Offset.Host = (ushort)i;
					}
					i = (int)(cF & Flags.IndexMask);
					cF = (Flags)((ulong)cF & 0xFFFFFFFFFFFF0000uL);
					if (flag)
					{
						cF |= Flags.SchemeNotCanonical;
					}
					uriInfo.Offset.Path = (ushort)i;
					bool flag2 = false;
					if ((cF & Flags.HasUnicode) != Flags.Zero)
					{
						uriInfo.Offset.End = (ushort)_originalUnicodeString.Length;
					}
					if (i < uriInfo.Offset.End)
					{
						fixed (char* ptr = OriginalString)
						{
							if (ptr[i] == ':')
							{
								int num3 = 0;
								if (++i < uriInfo.Offset.End)
								{
									num3 = ptr[i] - 48;
									if ((uint)num3 <= 9u)
									{
										flag2 = true;
										if (num3 == 0)
										{
											cF |= Flags.PortNotCanonical | Flags.E_PortNotCanonical;
										}
										for (i++; i < uriInfo.Offset.End; i++)
										{
											int num4 = ptr[i] - 48;
											if ((uint)num4 > 9u)
											{
												break;
											}
											num3 = num3 * 10 + num4;
										}
									}
								}
								if (flag2 && _syntax.DefaultPort != num3)
								{
									uriInfo.Offset.PortValue = (ushort)num3;
									cF |= Flags.NotDefaultPort;
								}
								else
								{
									cF |= Flags.PortNotCanonical | Flags.E_PortNotCanonical;
								}
								uriInfo.Offset.Path = (ushort)i;
							}
						}
					}
				}
			}
		}
		cF |= Flags.MinimalUriInfoSet;
		Interlocked.CompareExchange(ref _info, uriInfo, null);
		Flags flags = _flags;
		while ((flags & Flags.MinimalUriInfoSet) == Flags.Zero)
		{
			Flags value = (Flags)(((ulong)flags & 0xFFFFFFFFFFFF0000uL) | (ulong)cF);
			ulong num5 = Interlocked.CompareExchange(ref Internal.Runtime.CompilerServices.Unsafe.As<Flags, ulong>(ref _flags), (ulong)value, (ulong)flags);
			if (num5 == (ulong)flags)
			{
				break;
			}
			flags = (Flags)num5;
		}
	}

	private unsafe void CreateHostString()
	{
		if (!_syntax.IsSimple)
		{
			lock (_info)
			{
				if (NotAny(Flags.ErrorOrParsingRecursion))
				{
					_flags |= Flags.ErrorOrParsingRecursion;
					GetHostViaCustomSyntax();
					_flags &= ~Flags.ErrorOrParsingRecursion;
					return;
				}
			}
		}
		Flags flags = _flags;
		string text = CreateHostStringHelper(_string, _info.Offset.Host, _info.Offset.Path, ref flags, ref _info.ScopeId);
		if (text.Length != 0)
		{
			if (HostType == Flags.BasicHostType)
			{
				int idx = 0;
				Check check;
				fixed (char* str = text)
				{
					check = CheckCanonical(str, ref idx, text.Length, '\uffff');
				}
				if ((check & Check.DisplayCanonical) == 0 && (NotAny(Flags.ImplicitFile) || (check & Check.ReservedFound) != 0))
				{
					flags |= Flags.HostNotCanonical;
				}
				if (InFact(Flags.ImplicitFile) && (check & (Check.EscapedCanonical | Check.ReservedFound)) != 0)
				{
					check &= ~Check.EscapedCanonical;
				}
				if ((check & (Check.EscapedCanonical | Check.BackslashInPath)) != Check.EscapedCanonical)
				{
					flags |= Flags.E_HostNotCanonical;
					if (NotAny(Flags.UserEscaped))
					{
						text = UriHelper.EscapeString(text, !IsImplicitFile, UriHelper.UnreservedReservedTable, '?', '#');
					}
				}
			}
			else if (NotAny(Flags.CanonicalDnsHost))
			{
				if (_info.ScopeId != null)
				{
					flags |= Flags.HostNotCanonical | Flags.E_HostNotCanonical;
				}
				else
				{
					for (int i = 0; i < text.Length; i++)
					{
						if (_info.Offset.Host + i >= _info.Offset.End || text[i] != _string[_info.Offset.Host + i])
						{
							flags |= Flags.HostNotCanonical | Flags.E_HostNotCanonical;
							break;
						}
					}
				}
			}
		}
		_info.Host = text;
		InterlockedSetFlags(flags);
	}

	private static string CreateHostStringHelper(string str, int idx, int end, ref Flags flags, ref string scopeId)
	{
		bool loopback = false;
		string text;
		switch (flags & Flags.HostTypeMask)
		{
		case Flags.DnsHostType:
			text = DomainNameHelper.ParseCanonicalName(str, idx, end, ref loopback);
			break;
		case Flags.IPv6HostType:
			text = IPv6AddressHelper.ParseCanonicalName(str, idx, ref loopback, ref scopeId);
			break;
		case Flags.IPv4HostType:
			text = IPv4AddressHelper.ParseCanonicalName(str, idx, end, ref loopback);
			break;
		case Flags.UncHostType:
			text = UncNameHelper.ParseCanonicalName(str, idx, end, ref loopback);
			break;
		case Flags.BasicHostType:
			text = ((!StaticInFact(flags, Flags.DosPath)) ? str.Substring(idx, end - idx) : string.Empty);
			if (text.Length == 0)
			{
				loopback = true;
			}
			break;
		case Flags.HostTypeMask:
			text = string.Empty;
			break;
		default:
			throw GetException(ParsingError.BadHostName);
		}
		if (loopback)
		{
			flags |= Flags.LoopbackHost;
		}
		return text;
	}

	private unsafe void GetHostViaCustomSyntax()
	{
		if (_info.Host != null)
		{
			return;
		}
		string text = _syntax.InternalGetComponents(this, UriComponents.Host, UriFormat.UriEscaped);
		if (_info.Host == null)
		{
			if (text.Length >= 65520)
			{
				throw GetException(ParsingError.SizeLimit);
			}
			ParsingError err = ParsingError.None;
			Flags flags = (Flags)((ulong)_flags & 0xFFFFFFFFFFF8FFFFuL);
			fixed (char* pString = text)
			{
				string newHost = null;
				if (CheckAuthorityHelper(pString, 0, text.Length, ref err, ref flags, _syntax, ref newHost) != text.Length)
				{
					flags = (Flags)((ulong)flags & 0xFFFFFFFFFFF8FFFFuL);
					flags |= Flags.HostTypeMask;
				}
			}
			if (err != 0 || (flags & Flags.HostTypeMask) == Flags.HostTypeMask)
			{
				_flags = (Flags)(((ulong)_flags & 0xFFFFFFFFFFF8FFFFuL) | 0x50000);
			}
			else
			{
				text = CreateHostStringHelper(text, 0, text.Length, ref flags, ref _info.ScopeId);
				for (int i = 0; i < text.Length; i++)
				{
					if (_info.Offset.Host + i >= _info.Offset.End || text[i] != _string[_info.Offset.Host + i])
					{
						_flags |= Flags.HostNotCanonical | Flags.E_HostNotCanonical;
						break;
					}
				}
				_flags = (Flags)(((ulong)_flags & 0xFFFFFFFFFFF8FFFFuL) | (ulong)(flags & Flags.HostTypeMask));
			}
		}
		string text2 = _syntax.InternalGetComponents(this, UriComponents.StrongPort, UriFormat.UriEscaped);
		int num = 0;
		if (string.IsNullOrEmpty(text2))
		{
			_flags &= ~Flags.NotDefaultPort;
			_flags |= Flags.PortNotCanonical | Flags.E_PortNotCanonical;
			_info.Offset.PortValue = 0;
		}
		else
		{
			for (int j = 0; j < text2.Length; j++)
			{
				int num2 = text2[j] - 48;
				if (num2 < 0 || num2 > 9 || (num = num * 10 + num2) > 65535)
				{
					throw new UriFormatException(System.SR.Format(System.SR.net_uri_PortOutOfRange, _syntax.GetType(), text2));
				}
			}
			if (num != _info.Offset.PortValue)
			{
				if (num == _syntax.DefaultPort)
				{
					_flags &= ~Flags.NotDefaultPort;
				}
				else
				{
					_flags |= Flags.NotDefaultPort;
				}
				_flags |= Flags.PortNotCanonical | Flags.E_PortNotCanonical;
				_info.Offset.PortValue = (ushort)num;
			}
		}
		_info.Host = text;
	}

	internal string GetParts(UriComponents uriParts, UriFormat formatAs)
	{
		return InternalGetComponents(uriParts, formatAs);
	}

	private string GetEscapedParts(UriComponents uriParts)
	{
		ushort num = (ushort)(((ushort)_flags & 0x3F80) >> 6);
		if (InFact(Flags.SchemeNotCanonical))
		{
			num = (ushort)(num | 1u);
		}
		if ((uriParts & UriComponents.Path) != 0)
		{
			if (InFact(Flags.ShouldBeCompressed | Flags.FirstSlashAbsent | Flags.BackslashInPath))
			{
				num = (ushort)(num | 0x10u);
			}
			else if (IsDosPath && _string[_info.Offset.Path + SecuredPathIndex - 1] == '|')
			{
				num = (ushort)(num | 0x10u);
			}
		}
		if (((ushort)uriParts & num) == 0)
		{
			string uriPartsFromUserString = GetUriPartsFromUserString(uriParts);
			if (uriPartsFromUserString != null)
			{
				return uriPartsFromUserString;
			}
		}
		return ReCreateParts(uriParts, num, UriFormat.UriEscaped);
	}

	private string GetUnescapedParts(UriComponents uriParts, UriFormat formatAs)
	{
		ushort num = (ushort)((ushort)_flags & 0x7Fu);
		if ((uriParts & UriComponents.Path) != 0)
		{
			if ((_flags & (Flags.ShouldBeCompressed | Flags.FirstSlashAbsent | Flags.BackslashInPath)) != Flags.Zero)
			{
				num = (ushort)(num | 0x10u);
			}
			else if (IsDosPath && _string[_info.Offset.Path + SecuredPathIndex - 1] == '|')
			{
				num = (ushort)(num | 0x10u);
			}
		}
		if (((ushort)uriParts & num) == 0)
		{
			string uriPartsFromUserString = GetUriPartsFromUserString(uriParts);
			if (uriPartsFromUserString != null)
			{
				return uriPartsFromUserString;
			}
		}
		return ReCreateParts(uriParts, num, formatAs);
	}

	private string ReCreateParts(UriComponents parts, ushort nonCanonical, UriFormat formatAs)
	{
		EnsureHostString(allowDnsOptimization: false);
		string @string = _string;
		System.Text.ValueStringBuilder valueStringBuilder;
		if (@string.Length <= 512)
		{
			Span<char> initialBuffer = stackalloc char[512];
			valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new System.Text.ValueStringBuilder(@string.Length);
		}
		System.Text.ValueStringBuilder dest = valueStringBuilder;
		if ((parts & UriComponents.Scheme) != 0)
		{
			dest.Append(_syntax.SchemeName);
			if (parts != UriComponents.Scheme)
			{
				dest.Append(':');
				if (InFact(Flags.AuthorityFound))
				{
					dest.Append('/');
					dest.Append('/');
				}
			}
		}
		ReadOnlySpan<char> format;
		if ((parts & UriComponents.UserInfo) != 0 && InFact(Flags.HasUserInfo))
		{
			ReadOnlySpan<char> readOnlySpan = @string.AsSpan(_info.Offset.User, _info.Offset.Host - _info.Offset.User);
			if ((nonCanonical & 2u) != 0)
			{
				switch (formatAs)
				{
				case UriFormat.UriEscaped:
					if (NotAny(Flags.UserEscaped))
					{
						UriHelper.EscapeString(readOnlySpan, ref dest, checkExistingEscaped: true, '?', '#');
						break;
					}
					InFact(Flags.E_UserNotCanonical);
					dest.Append(readOnlySpan);
					break;
				case UriFormat.SafeUnescaped:
					format = readOnlySpan;
					UriHelper.UnescapeString(format[..^1], ref dest, '@', '/', '\\', InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape, _syntax, isQuery: false);
					dest.Append('@');
					break;
				case UriFormat.Unescaped:
					UriHelper.UnescapeString(readOnlySpan, ref dest, '\uffff', '\uffff', '\uffff', UnescapeMode.Unescape | UnescapeMode.UnescapeAll, _syntax, isQuery: false);
					break;
				default:
					dest.Append(readOnlySpan);
					break;
				}
			}
			else
			{
				dest.Append(readOnlySpan);
			}
			if (parts == UriComponents.UserInfo)
			{
				dest.Length--;
			}
		}
		if ((parts & UriComponents.Host) != 0)
		{
			string text = _info.Host;
			if (text.Length != 0)
			{
				UnescapeMode unescapeMode = ((formatAs != UriFormat.UriEscaped && HostType == Flags.BasicHostType && (nonCanonical & 4u) != 0) ? ((formatAs == UriFormat.Unescaped) ? (UnescapeMode.Unescape | UnescapeMode.UnescapeAll) : (InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape)) : UnescapeMode.CopyOnly);
				Span<char> initialBuffer = stackalloc char[512];
				System.Text.ValueStringBuilder dest2 = new System.Text.ValueStringBuilder(initialBuffer);
				if ((parts & UriComponents.NormalizedHost) != 0)
				{
					text = UriHelper.StripBidiControlCharacters(text, text);
					if (!DomainNameHelper.TryGetUnicodeEquivalent(text, ref dest2))
					{
						dest2.Length = 0;
					}
				}
				UriHelper.UnescapeString((dest2.Length == 0) ? ((ReadOnlySpan<char>)text) : dest2.AsSpan(), ref dest, '/', '?', '#', unescapeMode, _syntax, isQuery: false);
				dest2.Dispose();
				if (((uint)parts & 0x80000000u) != 0 && HostType == Flags.IPv6HostType && _info.ScopeId != null)
				{
					dest.Length--;
					dest.Append(_info.ScopeId);
					dest.Append(']');
				}
			}
		}
		if ((parts & UriComponents.Port) != 0 && (InFact(Flags.NotDefaultPort) || ((parts & UriComponents.StrongPort) != 0 && _syntax.DefaultPort != -1)))
		{
			dest.Append(':');
			ref ushort portValue = ref _info.Offset.PortValue;
			Span<char> destination = dest.AppendSpan(5);
			format = default(ReadOnlySpan<char>);
			int charsWritten;
			bool flag = portValue.TryFormat(destination, out charsWritten, format);
			dest.Length -= 5 - charsWritten;
		}
		if ((parts & UriComponents.Path) != 0)
		{
			GetCanonicalPath(ref dest, formatAs);
			if (parts == UriComponents.Path)
			{
				int start = ((InFact(Flags.AuthorityFound) && dest.Length != 0 && dest[0] == '/') ? 1 : 0);
				format = dest.AsSpan(start);
				string result = format.ToString();
				dest.Dispose();
				return result;
			}
		}
		if ((parts & UriComponents.Query) != 0 && _info.Offset.Query < _info.Offset.Fragment)
		{
			int num = _info.Offset.Query + 1;
			if (parts != UriComponents.Query)
			{
				dest.Append('?');
			}
			UnescapeMode unescapeMode2 = UnescapeMode.CopyOnly;
			if ((nonCanonical & 0x20u) != 0)
			{
				if (formatAs == UriFormat.UriEscaped)
				{
					if (NotAny(Flags.UserEscaped))
					{
						UriHelper.EscapeString(@string.AsSpan(num, _info.Offset.Fragment - num), ref dest, checkExistingEscaped: true, '#');
						goto IL_04d5;
					}
				}
				else
				{
					unescapeMode2 = formatAs switch
					{
						(UriFormat)32767 => (InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape) | UnescapeMode.V1ToStringFlag, 
						UriFormat.Unescaped => UnescapeMode.Unescape | UnescapeMode.UnescapeAll, 
						_ => InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape, 
					};
				}
			}
			UriHelper.UnescapeString(@string, num, _info.Offset.Fragment, ref dest, '#', '\uffff', '\uffff', unescapeMode2, _syntax, isQuery: true);
		}
		goto IL_04d5;
		IL_04d5:
		if ((parts & UriComponents.Fragment) != 0 && _info.Offset.Fragment < _info.Offset.End)
		{
			int num2 = _info.Offset.Fragment + 1;
			if (parts != UriComponents.Fragment)
			{
				dest.Append('#');
			}
			UnescapeMode unescapeMode3 = UnescapeMode.CopyOnly;
			if ((nonCanonical & 0x40u) != 0)
			{
				if (formatAs == UriFormat.UriEscaped)
				{
					if (NotAny(Flags.UserEscaped))
					{
						UriHelper.EscapeString(@string.AsSpan(num2, _info.Offset.End - num2), ref dest, checkExistingEscaped: true);
						goto IL_05d8;
					}
				}
				else
				{
					unescapeMode3 = formatAs switch
					{
						(UriFormat)32767 => (InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape) | UnescapeMode.V1ToStringFlag, 
						UriFormat.Unescaped => UnescapeMode.Unescape | UnescapeMode.UnescapeAll, 
						_ => InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape, 
					};
				}
			}
			UriHelper.UnescapeString(@string, num2, _info.Offset.End, ref dest, '#', '\uffff', '\uffff', unescapeMode3, _syntax, isQuery: false);
		}
		goto IL_05d8;
		IL_05d8:
		return dest.ToString();
	}

	private string GetUriPartsFromUserString(UriComponents uriParts)
	{
		switch (uriParts & ~UriComponents.KeepDelimiter)
		{
		case UriComponents.SchemeAndServer:
			if (!InFact(Flags.HasUserInfo))
			{
				return _string.Substring(_info.Offset.Scheme, _info.Offset.Path - _info.Offset.Scheme);
			}
			return string.Concat(_string.AsSpan(_info.Offset.Scheme, _info.Offset.User - _info.Offset.Scheme), _string.AsSpan(_info.Offset.Host, _info.Offset.Path - _info.Offset.Host));
		case UriComponents.HostAndPort:
			if (InFact(Flags.HasUserInfo))
			{
				if (InFact(Flags.NotDefaultPort) || _syntax.DefaultPort == -1)
				{
					return _string.Substring(_info.Offset.Host, _info.Offset.Path - _info.Offset.Host);
				}
				return string.Concat(_string.AsSpan(_info.Offset.Host, _info.Offset.Path - _info.Offset.Host), ":", _info.Offset.PortValue.ToString(CultureInfo.InvariantCulture));
			}
			goto case UriComponents.StrongAuthority;
		case UriComponents.AbsoluteUri:
			if (_info.Offset.Scheme == 0 && _info.Offset.End == _string.Length)
			{
				return _string;
			}
			return _string.Substring(_info.Offset.Scheme, _info.Offset.End - _info.Offset.Scheme);
		case UriComponents.HttpRequestUrl:
			if (InFact(Flags.HasUserInfo))
			{
				return string.Concat(_string.AsSpan(_info.Offset.Scheme, _info.Offset.User - _info.Offset.Scheme), _string.AsSpan(_info.Offset.Host, _info.Offset.Fragment - _info.Offset.Host));
			}
			if (_info.Offset.Scheme == 0 && _info.Offset.Fragment == _string.Length)
			{
				return _string;
			}
			return _string.Substring(_info.Offset.Scheme, _info.Offset.Fragment - _info.Offset.Scheme);
		case UriComponents.SchemeAndServer | UriComponents.UserInfo:
			return _string.Substring(_info.Offset.Scheme, _info.Offset.Path - _info.Offset.Scheme);
		case UriComponents.HttpRequestUrl | UriComponents.UserInfo:
			if (_info.Offset.Scheme == 0 && _info.Offset.Fragment == _string.Length)
			{
				return _string;
			}
			return _string.Substring(_info.Offset.Scheme, _info.Offset.Fragment - _info.Offset.Scheme);
		case UriComponents.Scheme:
			if (uriParts != UriComponents.Scheme)
			{
				return _string.Substring(_info.Offset.Scheme, _info.Offset.User - _info.Offset.Scheme);
			}
			return _syntax.SchemeName;
		case UriComponents.Host:
		{
			int num2 = _info.Offset.Path;
			if (InFact(Flags.PortNotCanonical | Flags.NotDefaultPort))
			{
				while (_string[--num2] != ':')
				{
				}
			}
			if (num2 - _info.Offset.Host != 0)
			{
				return _string.Substring(_info.Offset.Host, num2 - _info.Offset.Host);
			}
			return string.Empty;
		}
		case UriComponents.Path:
		{
			int num = ((uriParts != UriComponents.Path || !InFact(Flags.AuthorityFound) || _info.Offset.End <= _info.Offset.Path || _string[_info.Offset.Path] != '/') ? _info.Offset.Path : (_info.Offset.Path + 1));
			if (num >= _info.Offset.Query)
			{
				return string.Empty;
			}
			return _string.Substring(num, _info.Offset.Query - num);
		}
		case UriComponents.Query:
		{
			int num = ((uriParts != UriComponents.Query) ? _info.Offset.Query : (_info.Offset.Query + 1));
			if (num >= _info.Offset.Fragment)
			{
				return string.Empty;
			}
			return _string.Substring(num, _info.Offset.Fragment - num);
		}
		case UriComponents.Fragment:
		{
			int num = ((uriParts != UriComponents.Fragment) ? _info.Offset.Fragment : (_info.Offset.Fragment + 1));
			if (num >= _info.Offset.End)
			{
				return string.Empty;
			}
			return _string.Substring(num, _info.Offset.End - num);
		}
		case UriComponents.UserInfo | UriComponents.Host | UriComponents.Port:
			if (_info.Offset.Path - _info.Offset.User != 0)
			{
				return _string.Substring(_info.Offset.User, _info.Offset.Path - _info.Offset.User);
			}
			return string.Empty;
		case UriComponents.StrongAuthority:
			if (!InFact(Flags.NotDefaultPort) && _syntax.DefaultPort != -1)
			{
				return string.Concat(_string.AsSpan(_info.Offset.User, _info.Offset.Path - _info.Offset.User), ":", _info.Offset.PortValue.ToString(CultureInfo.InvariantCulture));
			}
			goto case UriComponents.UserInfo | UriComponents.Host | UriComponents.Port;
		case UriComponents.PathAndQuery:
			return _string.Substring(_info.Offset.Path, _info.Offset.Fragment - _info.Offset.Path);
		case UriComponents.HttpRequestUrl | UriComponents.Fragment:
			if (InFact(Flags.HasUserInfo))
			{
				return string.Concat(_string.AsSpan(_info.Offset.Scheme, _info.Offset.User - _info.Offset.Scheme), _string.AsSpan(_info.Offset.Host, _info.Offset.End - _info.Offset.Host));
			}
			if (_info.Offset.Scheme == 0 && _info.Offset.End == _string.Length)
			{
				return _string;
			}
			return _string.Substring(_info.Offset.Scheme, _info.Offset.End - _info.Offset.Scheme);
		case UriComponents.PathAndQuery | UriComponents.Fragment:
			return _string.Substring(_info.Offset.Path, _info.Offset.End - _info.Offset.Path);
		case UriComponents.UserInfo:
		{
			if (NotAny(Flags.HasUserInfo))
			{
				return string.Empty;
			}
			int num = ((uriParts != UriComponents.UserInfo) ? _info.Offset.Host : (_info.Offset.Host - 1));
			if (_info.Offset.User >= num)
			{
				return string.Empty;
			}
			return _string.Substring(_info.Offset.User, num - _info.Offset.User);
		}
		default:
			return null;
		}
	}

	private void GetLengthWithoutTrailingSpaces(string str, ref int length, int idx)
	{
		int num = length;
		while (num > idx && UriHelper.IsLWS(str[num - 1]))
		{
			num--;
		}
		length = num;
	}

	private unsafe void ParseRemaining()
	{
		EnsureUriInfo();
		Flags flags = Flags.Zero;
		if (!UserDrivenParsing)
		{
			bool flag = (_flags & (Flags.HasUnicode | Flags.RestUnicodeNormalized)) == Flags.HasUnicode;
			int scheme = _info.Offset.Scheme;
			int length = _string.Length;
			Check check = Check.None;
			UriSyntaxFlags flags2 = _syntax.Flags;
			fixed (char* ptr = _string)
			{
				GetLengthWithoutTrailingSpaces(_string, ref length, scheme);
				if (IsImplicitFile)
				{
					flags |= Flags.SchemeNotCanonical;
				}
				else
				{
					string schemeName = _syntax.SchemeName;
					int i;
					for (i = 0; i < schemeName.Length; i++)
					{
						if (schemeName[i] != ptr[scheme + i])
						{
							flags |= Flags.SchemeNotCanonical;
						}
					}
					if ((_flags & Flags.AuthorityFound) != Flags.Zero && (scheme + i + 3 >= length || ptr[scheme + i + 1] != '/' || ptr[scheme + i + 2] != '/'))
					{
						flags |= Flags.SchemeNotCanonical;
					}
				}
				if ((_flags & Flags.HasUserInfo) != Flags.Zero)
				{
					scheme = _info.Offset.User;
					check = CheckCanonical(ptr, ref scheme, _info.Offset.Host, '@');
					if ((check & Check.DisplayCanonical) == 0)
					{
						flags |= Flags.UserNotCanonical;
					}
					if ((check & (Check.EscapedCanonical | Check.BackslashInPath)) != Check.EscapedCanonical)
					{
						flags |= Flags.E_UserNotCanonical;
					}
					if (IriParsing && (check & (Check.EscapedCanonical | Check.DisplayCanonical | Check.BackslashInPath | Check.NotIriCanonical | Check.FoundNonAscii)) == (Check.DisplayCanonical | Check.FoundNonAscii))
					{
						flags |= Flags.UserIriCanonical;
					}
				}
			}
			scheme = _info.Offset.Path;
			int num = _info.Offset.Path;
			if (flag)
			{
				if (IsFile && !IsUncPath)
				{
					if (IsImplicitFile)
					{
						_string = string.Empty;
					}
					else
					{
						_string = _syntax.SchemeName + SchemeDelimiter;
					}
				}
				_info.Offset.Path = (ushort)_string.Length;
				scheme = _info.Offset.Path;
			}
			if (DisablePathAndQueryCanonicalization)
			{
				if (flag)
				{
					_string += _originalUnicodeString.Substring(num);
				}
				string @string = _string;
				if (IsImplicitFile || (flags2 & UriSyntaxFlags.MayHaveQuery) == 0)
				{
					scheme = @string.Length;
				}
				else
				{
					scheme = @string.IndexOf('?');
					if (scheme == -1)
					{
						scheme = @string.Length;
					}
				}
				_info.Offset.Query = (ushort)scheme;
				_info.Offset.Fragment = (ushort)@string.Length;
				_info.Offset.End = (ushort)@string.Length;
			}
			else
			{
				if (flag)
				{
					int start = num;
					if (IsImplicitFile || (flags2 & (UriSyntaxFlags.MayHaveQuery | UriSyntaxFlags.MayHaveFragment)) == 0)
					{
						num = _originalUnicodeString.Length;
					}
					else
					{
						ReadOnlySpan<char> span = _originalUnicodeString.AsSpan(num);
						int num2 = ((!_syntax.InFact(UriSyntaxFlags.MayHaveQuery)) ? span.IndexOf('#') : ((!_syntax.InFact(UriSyntaxFlags.MayHaveFragment)) ? span.IndexOf('?') : span.IndexOfAny('?', '#')));
						num = ((num2 == -1) ? _originalUnicodeString.Length : (num2 + num));
					}
					_string += EscapeUnescapeIri(_originalUnicodeString, start, num, UriComponents.Path);
					if (_string.Length > 65535)
					{
						UriFormatException exception = GetException(ParsingError.SizeLimit);
						throw exception;
					}
					length = _string.Length;
					if (_string == _originalUnicodeString)
					{
						GetLengthWithoutTrailingSpaces(_string, ref length, scheme);
					}
				}
				fixed (char* ptr2 = _string)
				{
					check = ((!IsImplicitFile && (flags2 & (UriSyntaxFlags.MayHaveQuery | UriSyntaxFlags.MayHaveFragment)) != 0) ? CheckCanonical(ptr2, ref scheme, length, ((flags2 & UriSyntaxFlags.MayHaveQuery) != 0) ? '?' : (_syntax.InFact(UriSyntaxFlags.MayHaveFragment) ? '#' : '\ufffe')) : CheckCanonical(ptr2, ref scheme, length, '\uffff'));
					if ((_flags & Flags.AuthorityFound) != Flags.Zero && (flags2 & UriSyntaxFlags.PathIsRooted) != 0 && (_info.Offset.Path == length || (ptr2[(int)_info.Offset.Path] != '/' && ptr2[(int)_info.Offset.Path] != '\\')))
					{
						flags |= Flags.FirstSlashAbsent;
					}
				}
				bool flag2 = false;
				if (IsDosPath || ((_flags & Flags.AuthorityFound) != Flags.Zero && ((flags2 & (UriSyntaxFlags.ConvertPathSlashes | UriSyntaxFlags.CompressPath)) != 0 || _syntax.InFact(UriSyntaxFlags.UnEscapeDotsAndSlashes))))
				{
					if ((check & Check.DotSlashEscaped) != 0 && _syntax.InFact(UriSyntaxFlags.UnEscapeDotsAndSlashes))
					{
						flags |= Flags.PathNotCanonical | Flags.E_PathNotCanonical;
						flag2 = true;
					}
					if ((flags2 & UriSyntaxFlags.ConvertPathSlashes) != 0 && (check & Check.BackslashInPath) != 0)
					{
						flags |= Flags.PathNotCanonical | Flags.E_PathNotCanonical;
						flag2 = true;
					}
					if ((flags2 & UriSyntaxFlags.CompressPath) != 0 && ((flags & Flags.E_PathNotCanonical) != Flags.Zero || (check & Check.DotSlashAttn) != 0))
					{
						flags |= Flags.ShouldBeCompressed;
					}
					if ((check & Check.BackslashInPath) != 0)
					{
						flags |= Flags.BackslashInPath;
					}
				}
				else if ((check & Check.BackslashInPath) != 0)
				{
					flags |= Flags.E_PathNotCanonical;
					flag2 = true;
				}
				if ((check & Check.DisplayCanonical) == 0 && ((_flags & Flags.ImplicitFile) == Flags.Zero || (_flags & Flags.UserEscaped) != Flags.Zero || (check & Check.ReservedFound) != 0))
				{
					flags |= Flags.PathNotCanonical;
					flag2 = true;
				}
				if ((_flags & Flags.ImplicitFile) != Flags.Zero && (check & (Check.EscapedCanonical | Check.ReservedFound)) != 0)
				{
					check &= ~Check.EscapedCanonical;
				}
				if ((check & Check.EscapedCanonical) == 0)
				{
					flags |= Flags.E_PathNotCanonical;
				}
				if (IriParsing && !flag2 && (check & (Check.EscapedCanonical | Check.DisplayCanonical | Check.NotIriCanonical | Check.FoundNonAscii)) == (Check.DisplayCanonical | Check.FoundNonAscii))
				{
					flags |= Flags.PathIriCanonical;
				}
				if (flag)
				{
					int start2 = num;
					if (num < _originalUnicodeString.Length && _originalUnicodeString[num] == '?')
					{
						if ((flags2 & UriSyntaxFlags.MayHaveFragment) != 0)
						{
							num++;
							int num3 = _originalUnicodeString.AsSpan(num).IndexOf('#');
							num = ((num3 == -1) ? _originalUnicodeString.Length : (num3 + num));
						}
						else
						{
							num = _originalUnicodeString.Length;
						}
						_string += EscapeUnescapeIri(_originalUnicodeString, start2, num, UriComponents.Query);
						if (_string.Length > 65535)
						{
							UriFormatException exception2 = GetException(ParsingError.SizeLimit);
							throw exception2;
						}
						length = _string.Length;
						if (_string == _originalUnicodeString)
						{
							GetLengthWithoutTrailingSpaces(_string, ref length, scheme);
						}
					}
				}
				_info.Offset.Query = (ushort)scheme;
				fixed (char* ptr3 = _string)
				{
					if (scheme < length && ptr3[scheme] == '?')
					{
						scheme++;
						check = CheckCanonical(ptr3, ref scheme, length, ((flags2 & UriSyntaxFlags.MayHaveFragment) != 0) ? '#' : '\ufffe');
						if ((check & Check.DisplayCanonical) == 0)
						{
							flags |= Flags.QueryNotCanonical;
						}
						if ((check & (Check.EscapedCanonical | Check.BackslashInPath)) != Check.EscapedCanonical)
						{
							flags |= Flags.E_QueryNotCanonical;
						}
						if (IriParsing && (check & (Check.EscapedCanonical | Check.DisplayCanonical | Check.BackslashInPath | Check.NotIriCanonical | Check.FoundNonAscii)) == (Check.DisplayCanonical | Check.FoundNonAscii))
						{
							flags |= Flags.QueryIriCanonical;
						}
					}
				}
				if (flag)
				{
					int start3 = num;
					if (num < _originalUnicodeString.Length && _originalUnicodeString[num] == '#')
					{
						num = _originalUnicodeString.Length;
						_string += EscapeUnescapeIri(_originalUnicodeString, start3, num, UriComponents.Fragment);
						if (_string.Length > 65535)
						{
							UriFormatException exception3 = GetException(ParsingError.SizeLimit);
							throw exception3;
						}
						length = _string.Length;
						GetLengthWithoutTrailingSpaces(_string, ref length, scheme);
					}
				}
				_info.Offset.Fragment = (ushort)scheme;
				fixed (char* ptr4 = _string)
				{
					if (scheme < length && ptr4[scheme] == '#')
					{
						scheme++;
						check = CheckCanonical(ptr4, ref scheme, length, '\ufffe');
						if ((check & Check.DisplayCanonical) == 0)
						{
							flags |= Flags.FragmentNotCanonical;
						}
						if ((check & (Check.EscapedCanonical | Check.BackslashInPath)) != Check.EscapedCanonical)
						{
							flags |= Flags.E_FragmentNotCanonical;
						}
						if (IriParsing && (check & (Check.EscapedCanonical | Check.DisplayCanonical | Check.BackslashInPath | Check.NotIriCanonical | Check.FoundNonAscii)) == (Check.DisplayCanonical | Check.FoundNonAscii))
						{
							flags |= Flags.FragmentIriCanonical;
						}
					}
				}
				_info.Offset.End = (ushort)scheme;
			}
		}
		flags |= Flags.AllUriInfoSet | Flags.RestUnicodeNormalized;
		InterlockedSetFlags(flags);
	}

	private unsafe static int ParseSchemeCheckImplicitFile(char* uriString, int length, ref ParsingError err, ref Flags flags, ref UriParser syntax)
	{
		int i;
		for (i = 0; i < length && UriHelper.IsLWS(uriString[i]); i++)
		{
		}
		int j;
		for (j = i; j < length && uriString[j] != ':'; j++)
		{
		}
		if (IntPtr.Size == 4)
		{
		}
		if (i + 2 >= length || j == i)
		{
			err = ParsingError.BadFormat;
			return 0;
		}
		char c;
		if ((c = uriString[i + 1]) == ':' || c == '|')
		{
			if (UriHelper.IsAsciiLetter(uriString[i]))
			{
				if ((c = uriString[i + 2]) == '\\' || c == '/')
				{
					flags |= Flags.AuthorityFound | Flags.DosPath | Flags.ImplicitFile;
					syntax = UriParser.FileUri;
					return i;
				}
				err = ParsingError.MustRootedPath;
				return 0;
			}
			if (c == ':')
			{
				err = ParsingError.BadScheme;
			}
			else
			{
				err = ParsingError.BadFormat;
			}
			return 0;
		}
		if ((c = uriString[i]) == '/' || c == '\\')
		{
			if ((c = uriString[i + 1]) == '\\' || c == '/')
			{
				flags |= Flags.AuthorityFound | Flags.UncPath | Flags.ImplicitFile;
				syntax = UriParser.FileUri;
				for (i += 2; i < length; i++)
				{
					if ((c = uriString[i]) != '/' && c != '\\')
					{
						break;
					}
				}
				return i;
			}
			err = ParsingError.BadFormat;
			return 0;
		}
		if (j == length)
		{
			err = ParsingError.BadFormat;
			return 0;
		}
		err = CheckSchemeSyntax(new ReadOnlySpan<char>(uriString + i, j - i), ref syntax);
		if (err != 0)
		{
			return 0;
		}
		return j + 1;
	}

	private unsafe static ParsingError CheckSchemeSyntax(ReadOnlySpan<char> span, ref UriParser syntax)
	{
		if (span.Length == 0)
		{
			return ParsingError.BadScheme;
		}
		char c2 = span[0];
		switch (c2)
		{
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
		case 'G':
		case 'H':
		case 'I':
		case 'J':
		case 'K':
		case 'L':
		case 'M':
		case 'N':
		case 'O':
		case 'P':
		case 'Q':
		case 'R':
		case 'S':
		case 'T':
		case 'U':
		case 'V':
		case 'W':
		case 'X':
		case 'Y':
		case 'Z':
			c2 = (char)(c2 | 0x20u);
			break;
		default:
			return ParsingError.BadScheme;
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
		case 'g':
		case 'h':
		case 'i':
		case 'j':
		case 'k':
		case 'l':
		case 'm':
		case 'n':
		case 'o':
		case 'p':
		case 'q':
		case 'r':
		case 's':
		case 't':
		case 'u':
		case 'v':
		case 'w':
		case 'x':
		case 'y':
		case 'z':
			break;
		}
		switch (span.Length)
		{
		case 2:
			if (30579 == (((uint)c2 << 8) | ToLowerCaseAscii(span[1])))
			{
				syntax = UriParser.WsUri;
				return ParsingError.None;
			}
			break;
		case 3:
			switch ((int)(((uint)c2 << 16) | ((uint)ToLowerCaseAscii(span[1]) << 8) | ToLowerCaseAscii(span[2])))
			{
			case 6714480:
				syntax = UriParser.FtpUri;
				return ParsingError.None;
			case 7828339:
				syntax = UriParser.WssUri;
				return ParsingError.None;
			}
			break;
		case 4:
			switch ((int)(((uint)c2 << 24) | ((uint)ToLowerCaseAscii(span[1]) << 16) | ((uint)ToLowerCaseAscii(span[2]) << 8) | ToLowerCaseAscii(span[3])))
			{
			case 1752462448:
				syntax = UriParser.HttpUri;
				return ParsingError.None;
			case 1718185061:
				syntax = UriParser.FileUri;
				return ParsingError.None;
			}
			break;
		case 5:
			if (1752462448 == (((uint)c2 << 24) | ((uint)ToLowerCaseAscii(span[1]) << 16) | ((uint)ToLowerCaseAscii(span[2]) << 8) | ToLowerCaseAscii(span[3])) && ToLowerCaseAscii(span[4]) == 's')
			{
				syntax = UriParser.HttpsUri;
				return ParsingError.None;
			}
			break;
		case 6:
			if (1835100524 == (((uint)c2 << 24) | ((uint)ToLowerCaseAscii(span[1]) << 16) | ((uint)ToLowerCaseAscii(span[2]) << 8) | ToLowerCaseAscii(span[3])) && ToLowerCaseAscii(span[4]) == 't' && ToLowerCaseAscii(span[5]) == 'o')
			{
				syntax = UriParser.MailToUri;
				return ParsingError.None;
			}
			break;
		}
		for (int i = 1; i < span.Length; i++)
		{
			char c3 = span[i];
			if ((uint)(c3 - 97) > 25u && (uint)(c3 - 65) > 25u && (uint)(c3 - 48) > 9u && c3 != '+' && c3 != '-' && c3 != '.')
			{
				return ParsingError.BadScheme;
			}
		}
		if (span.Length > 1024)
		{
			return ParsingError.SchemeLimit;
		}
		string lwrCaseScheme;
		fixed (char* ptr = span)
		{
			lwrCaseScheme = string.Create(span.Length, ((IntPtr)ptr, span.Length), delegate(Span<char> buffer, (IntPtr ip, int length) state)
			{
				int num = new ReadOnlySpan<char>((void*)state.ip, state.length).ToLowerInvariant(buffer);
			});
		}
		syntax = UriParser.FindOrFetchAsUnknownV1Syntax(lwrCaseScheme);
		return ParsingError.None;
		static char ToLowerCaseAscii(char c)
		{
			if ((uint)(c - 65) > 25u)
			{
				return c;
			}
			return (char)(c | 0x20u);
		}
	}

	private unsafe int CheckAuthorityHelper(char* pString, int idx, int length, ref ParsingError err, ref Flags flags, UriParser syntax, ref string newHost)
	{
		int i = length;
		int num = idx;
		int j = idx;
		newHost = null;
		bool justNormalized = false;
		bool flag = IriParsingStatic(syntax);
		bool flag2 = (flags & Flags.HasUnicode) != 0;
		bool flag3 = flag2 && (flags & Flags.HostUnicodeNormalized) == 0;
		UriSyntaxFlags flags2 = syntax.Flags;
		char c;
		if (idx == length || (c = pString[idx]) == '/' || (c == '\\' && StaticIsFile(syntax)) || c == '#' || c == '?')
		{
			if (syntax.InFact(UriSyntaxFlags.AllowEmptyHost))
			{
				flags &= ~Flags.UncPath;
				if (StaticInFact(flags, Flags.ImplicitFile))
				{
					err = ParsingError.BadHostName;
				}
				else
				{
					flags |= Flags.BasicHostType;
				}
			}
			else
			{
				err = ParsingError.BadHostName;
			}
			if (flag3)
			{
				flags |= Flags.HostUnicodeNormalized;
			}
			return idx;
		}
		if (flag3)
		{
			newHost = _originalUnicodeString.Substring(0, num);
		}
		string text = null;
		if ((flags2 & UriSyntaxFlags.MayHaveUserInfo) != 0)
		{
			for (; j < i; j++)
			{
				if (j == i - 1 || pString[j] == '?' || pString[j] == '#' || pString[j] == '\\' || pString[j] == '/')
				{
					j = idx;
					break;
				}
				if (pString[j] != '@')
				{
					continue;
				}
				flags |= Flags.HasUserInfo;
				if (flag)
				{
					if (flag3)
					{
						text = IriHelper.EscapeUnescapeIri(pString, num, j + 1, UriComponents.UserInfo);
						newHost += text;
						if (newHost.Length > 65535)
						{
							err = ParsingError.SizeLimit;
							return idx;
						}
					}
					else
					{
						text = new string(pString, num, j - num + 1);
					}
				}
				j++;
				c = pString[j];
				break;
			}
		}
		bool notCanonical = (flags2 & UriSyntaxFlags.SimpleUserSyntax) == 0;
		if (c == '[' && syntax.InFact(UriSyntaxFlags.AllowIPv6Host) && IPv6AddressHelper.IsValid(pString, j + 1, ref i))
		{
			flags |= Flags.IPv6HostType;
			if (flag3)
			{
				newHost += new string(pString, j, i - j);
				flags |= Flags.HostUnicodeNormalized;
				justNormalized = true;
			}
		}
		else if (c <= '9' && c >= '0' && syntax.InFact(UriSyntaxFlags.AllowIPv4Host) && IPv4AddressHelper.IsValid(pString, j, ref i, allowIPv6: false, StaticNotAny(flags, Flags.ImplicitFile), syntax.InFact(UriSyntaxFlags.V1_UnknownUri)))
		{
			flags |= Flags.IPv4HostType;
			if (flag3)
			{
				newHost += new string(pString, j, i - j);
				flags |= Flags.HostUnicodeNormalized;
				justNormalized = true;
			}
		}
		else if ((flags2 & UriSyntaxFlags.AllowDnsHost) != 0 && !flag && DomainNameHelper.IsValid(pString, j, ref i, ref notCanonical, StaticNotAny(flags, Flags.ImplicitFile)))
		{
			flags |= Flags.DnsHostType;
			if (!notCanonical)
			{
				flags |= Flags.CanonicalDnsHost;
			}
		}
		else if ((flags2 & UriSyntaxFlags.AllowDnsHost) != 0 && (flag3 || syntax.InFact(UriSyntaxFlags.AllowIdn)) && DomainNameHelper.IsValidByIri(pString, j, ref i, ref notCanonical, StaticNotAny(flags, Flags.ImplicitFile)))
		{
			CheckAuthorityHelperHandleDnsIri(pString, j, i, flag2, ref flags, ref justNormalized, ref newHost, ref err);
		}
		else if ((flags2 & UriSyntaxFlags.AllowUncHost) != 0 && UncNameHelper.IsValid(pString, j, ref i, StaticNotAny(flags, Flags.ImplicitFile)) && i - j <= 256)
		{
			flags |= Flags.UncHostType;
			if (flag3)
			{
				newHost += new string(pString, j, i - j);
				flags |= Flags.HostUnicodeNormalized;
				justNormalized = true;
			}
		}
		if (i < length && pString[i] == '\\' && (flags & Flags.HostTypeMask) != Flags.Zero && !StaticIsFile(syntax))
		{
			if (syntax.InFact(UriSyntaxFlags.V1_UnknownUri))
			{
				err = ParsingError.BadHostName;
				flags |= Flags.HostTypeMask;
				return i;
			}
			flags &= ~Flags.HostTypeMask;
		}
		else if (i < length && pString[i] == ':')
		{
			if (syntax.InFact(UriSyntaxFlags.MayHavePort))
			{
				int num2 = 0;
				int num3 = i;
				for (idx = i + 1; idx < length; idx++)
				{
					int num4 = pString[idx] - 48;
					switch (num4)
					{
					case 0:
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
					case 7:
					case 8:
					case 9:
						if ((num2 = num2 * 10 + num4) <= 65535)
						{
							continue;
						}
						break;
					default:
						if (syntax.InFact(UriSyntaxFlags.AllowAnyOtherHost) && syntax.NotAny(UriSyntaxFlags.V1_UnknownUri))
						{
							flags &= ~Flags.HostTypeMask;
							break;
						}
						err = ParsingError.BadPort;
						return idx;
					case -13:
					case -1:
					case 15:
						break;
					}
					break;
				}
				if (num2 > 65535)
				{
					if (!syntax.InFact(UriSyntaxFlags.AllowAnyOtherHost))
					{
						err = ParsingError.BadPort;
						return idx;
					}
					flags &= ~Flags.HostTypeMask;
				}
				if (flag2 && justNormalized)
				{
					newHost += new string(pString, num3, idx - num3);
				}
			}
			else
			{
				flags &= ~Flags.HostTypeMask;
			}
		}
		if ((flags & Flags.HostTypeMask) == Flags.Zero)
		{
			flags &= ~Flags.HasUserInfo;
			if (syntax.InFact(UriSyntaxFlags.AllowAnyOtherHost))
			{
				flags |= Flags.BasicHostType;
				for (i = idx; i < length && pString[i] != '/' && pString[i] != '?' && pString[i] != '#'; i++)
				{
				}
				if (flag3)
				{
					string text2 = new string(pString, num, i - num);
					try
					{
						newHost += text2.Normalize(NormalizationForm.FormC);
					}
					catch (ArgumentException)
					{
						err = ParsingError.BadHostName;
					}
					flags |= Flags.HostUnicodeNormalized;
				}
			}
			else if (syntax.InFact(UriSyntaxFlags.V1_UnknownUri))
			{
				bool flag4 = false;
				int num5 = idx;
				for (i = idx; i < length && (!flag4 || (pString[i] != '/' && pString[i] != '?' && pString[i] != '#')); i++)
				{
					if (i < idx + 2 && pString[i] == '.')
					{
						flag4 = true;
						continue;
					}
					err = ParsingError.BadHostName;
					flags |= Flags.HostTypeMask;
					return idx;
				}
				flags |= Flags.BasicHostType;
				if (flag3)
				{
					string text3 = new string(pString, num5, i - num5);
					try
					{
						newHost += text3.Normalize(NormalizationForm.FormC);
					}
					catch (ArgumentException)
					{
						err = ParsingError.BadFormat;
						return idx;
					}
					flags |= Flags.HostUnicodeNormalized;
				}
			}
			else if (syntax.InFact(UriSyntaxFlags.MustHaveAuthority) || syntax.InFact(UriSyntaxFlags.MailToLikeUri))
			{
				err = ParsingError.BadHostName;
				flags |= Flags.HostTypeMask;
				return idx;
			}
		}
		return i;
	}

	private unsafe void CheckAuthorityHelperHandleDnsIri(char* pString, int start, int end, bool hasUnicode, ref Flags flags, ref bool justNormalized, ref string newHost, ref ParsingError err)
	{
		flags |= Flags.DnsHostType;
		if (hasUnicode)
		{
			string text = UriHelper.StripBidiControlCharacters(new ReadOnlySpan<char>(pString + start, end - start));
			try
			{
				newHost += text.Normalize(NormalizationForm.FormC);
			}
			catch (ArgumentException)
			{
				err = ParsingError.BadHostName;
			}
			justNormalized = true;
		}
		flags |= Flags.HostUnicodeNormalized;
	}

	private unsafe Check CheckCanonical(char* str, ref int idx, int end, char delim)
	{
		Check check = Check.None;
		bool flag = false;
		bool flag2 = false;
		bool iriParsing = IriParsing;
		int i;
		for (i = idx; i < end; i++)
		{
			char c = str[i];
			if (c <= '\u001f' || (c >= '\u007f' && c <= '\u009f'))
			{
				flag = true;
				flag2 = true;
				check |= Check.ReservedFound;
				continue;
			}
			if (c > '~')
			{
				if (iriParsing)
				{
					bool flag3 = false;
					check |= Check.FoundNonAscii;
					if (char.IsHighSurrogate(c))
					{
						if (i + 1 < end)
						{
							flag3 = IriHelper.CheckIriUnicodeRange(c, str[i + 1], out var _, isQuery: true);
						}
					}
					else
					{
						flag3 = IriHelper.CheckIriUnicodeRange(c, isQuery: true);
					}
					if (!flag3)
					{
						check |= Check.NotIriCanonical;
					}
				}
				if (!flag)
				{
					flag = true;
				}
				continue;
			}
			if (c == delim || (delim == '?' && c == '#' && _syntax != null && _syntax.InFact(UriSyntaxFlags.MayHaveFragment)))
			{
				break;
			}
			if (c == '?')
			{
				if (IsImplicitFile || (_syntax != null && !_syntax.InFact(UriSyntaxFlags.MayHaveQuery) && delim != '\ufffe'))
				{
					check |= Check.ReservedFound;
					flag2 = true;
					flag = true;
				}
				continue;
			}
			if (c == '#')
			{
				flag = true;
				if (IsImplicitFile || (_syntax != null && !_syntax.InFact(UriSyntaxFlags.MayHaveFragment)))
				{
					check |= Check.ReservedFound;
					flag2 = true;
				}
				continue;
			}
			if (c == '/' || c == '\\')
			{
				if ((check & Check.BackslashInPath) == 0 && c == '\\')
				{
					check |= Check.BackslashInPath;
				}
				if ((check & Check.DotSlashAttn) == 0 && i + 1 != end && (str[i + 1] == '/' || str[i + 1] == '\\'))
				{
					check |= Check.DotSlashAttn;
				}
				continue;
			}
			if (c == '.')
			{
				if (((check & Check.DotSlashAttn) == 0 && i + 1 == end) || str[i + 1] == '.' || str[i + 1] == '/' || str[i + 1] == '\\' || str[i + 1] == '?' || str[i + 1] == '#')
				{
					check |= Check.DotSlashAttn;
				}
				continue;
			}
			if ((c > '"' || c == '!') && (c < '[' || c > '^'))
			{
				switch (c)
				{
				case '<':
				case '>':
				case '`':
					break;
				case '{':
				case '|':
				case '}':
					flag = true;
					continue;
				default:
					if (c != '%')
					{
						continue;
					}
					if (!flag2)
					{
						flag2 = true;
					}
					if (i + 2 < end && (c = UriHelper.DecodeHexChars(str[i + 1], str[i + 2])) != '\uffff')
					{
						if (c == '.' || c == '/' || c == '\\')
						{
							check |= Check.DotSlashEscaped;
						}
						i += 2;
					}
					else if (!flag)
					{
						flag = true;
					}
					continue;
				}
			}
			if (!flag)
			{
				flag = true;
			}
			if ((_flags & Flags.HasUnicode) != Flags.Zero)
			{
				check |= Check.NotIriCanonical;
			}
		}
		if (flag2)
		{
			if (!flag)
			{
				check |= Check.EscapedCanonical;
			}
		}
		else
		{
			check |= Check.DisplayCanonical;
			if (!flag)
			{
				check |= Check.EscapedCanonical;
			}
		}
		idx = i;
		return check;
	}

	private unsafe void GetCanonicalPath(ref System.Text.ValueStringBuilder dest, UriFormat formatAs)
	{
		if (InFact(Flags.FirstSlashAbsent))
		{
			dest.Append('/');
		}
		if (_info.Offset.Path == _info.Offset.Query)
		{
			return;
		}
		int length = dest.Length;
		int securedPathIndex = SecuredPathIndex;
		if (formatAs == UriFormat.UriEscaped)
		{
			if (InFact(Flags.ShouldBeCompressed))
			{
				dest.Append(_string.AsSpan(_info.Offset.Path, _info.Offset.Query - _info.Offset.Path));
				if (_syntax.InFact(UriSyntaxFlags.UnEscapeDotsAndSlashes) && InFact(Flags.PathNotCanonical) && !IsImplicitFile)
				{
					fixed (char* pch = dest)
					{
						int end = dest.Length;
						UnescapeOnly(pch, length, ref end, '.', '/', _syntax.InFact(UriSyntaxFlags.ConvertPathSlashes) ? '\\' : '\uffff');
						dest.Length = end;
					}
				}
			}
			else if (InFact(Flags.E_PathNotCanonical) && NotAny(Flags.UserEscaped))
			{
				ReadOnlySpan<char> readOnlySpan = _string;
				if (securedPathIndex != 0 && readOnlySpan[securedPathIndex + _info.Offset.Path - 1] == '|')
				{
					char[] array = readOnlySpan.ToArray();
					array[securedPathIndex + _info.Offset.Path - 1] = ':';
					readOnlySpan = array;
				}
				UriHelper.EscapeString(readOnlySpan.Slice(_info.Offset.Path, _info.Offset.Query - _info.Offset.Path), ref dest, !IsImplicitFile, '?', '#');
			}
			else
			{
				dest.Append(_string.AsSpan(_info.Offset.Path, _info.Offset.Query - _info.Offset.Path));
			}
		}
		else
		{
			dest.Append(_string.AsSpan(_info.Offset.Path, _info.Offset.Query - _info.Offset.Path));
			if (InFact(Flags.ShouldBeCompressed) && _syntax.InFact(UriSyntaxFlags.UnEscapeDotsAndSlashes) && InFact(Flags.PathNotCanonical) && !IsImplicitFile)
			{
				fixed (char* pch2 = dest)
				{
					int end2 = dest.Length;
					UnescapeOnly(pch2, length, ref end2, '.', '/', _syntax.InFact(UriSyntaxFlags.ConvertPathSlashes) ? '\\' : '\uffff');
					dest.Length = end2;
				}
			}
		}
		int num = length + securedPathIndex;
		if (securedPathIndex != 0 && dest[num - 1] == '|')
		{
			dest[num - 1] = ':';
		}
		if (InFact(Flags.ShouldBeCompressed) && dest.Length - num > 0)
		{
			dest.Length = num + Compress(dest.RawChars.Slice(num, dest.Length - num), _syntax);
			if (dest[length] == '\\')
			{
				dest[length] = '/';
			}
			if (formatAs == UriFormat.UriEscaped && NotAny(Flags.UserEscaped) && InFact(Flags.E_PathNotCanonical))
			{
				Span<char> initialBuffer = stackalloc char[512];
				System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
				valueStringBuilder.Append(dest.AsSpan(length, dest.Length - length));
				dest.Length = length;
				ReadOnlySpan<char> stringToEscape = MemoryMarshal.CreateReadOnlySpan(ref valueStringBuilder.GetPinnableReference(), valueStringBuilder.Length);
				UriHelper.EscapeString(stringToEscape, ref dest, !IsImplicitFile, '?', '#');
				length = dest.Length;
				valueStringBuilder.Dispose();
			}
		}
		if (formatAs == UriFormat.UriEscaped || !InFact(Flags.PathNotCanonical))
		{
			return;
		}
		UnescapeMode unescapeMode;
		switch (formatAs)
		{
		case (UriFormat)32767:
			unescapeMode = (InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape) | UnescapeMode.V1ToStringFlag;
			if (IsImplicitFile)
			{
				unescapeMode &= ~UnescapeMode.Unescape;
			}
			break;
		case UriFormat.Unescaped:
			unescapeMode = ((!IsImplicitFile) ? (UnescapeMode.Unescape | UnescapeMode.UnescapeAll) : UnescapeMode.CopyOnly);
			break;
		default:
			unescapeMode = (InFact(Flags.UserEscaped) ? UnescapeMode.Unescape : UnescapeMode.EscapeUnescape);
			if (IsImplicitFile)
			{
				unescapeMode &= ~UnescapeMode.Unescape;
			}
			break;
		}
		if (unescapeMode != 0)
		{
			Span<char> initialBuffer = stackalloc char[512];
			System.Text.ValueStringBuilder valueStringBuilder2 = new System.Text.ValueStringBuilder(initialBuffer);
			valueStringBuilder2.Append(dest.AsSpan(length, dest.Length - length));
			dest.Length = length;
			fixed (char* pStr = valueStringBuilder2)
			{
				UriHelper.UnescapeString(pStr, 0, valueStringBuilder2.Length, ref dest, '?', '#', '\uffff', unescapeMode, _syntax, isQuery: false);
			}
			valueStringBuilder2.Dispose();
		}
	}

	private unsafe static void UnescapeOnly(char* pch, int start, ref int end, char ch1, char ch2, char ch3)
	{
		if (end - start < 3)
		{
			return;
		}
		char* ptr = pch + end - 2;
		pch += start;
		char* ptr2 = null;
		while (pch < ptr)
		{
			if (*(pch++) != '%')
			{
				continue;
			}
			char c = UriHelper.DecodeHexChars(*(pch++), *(pch++));
			if (c != ch1 && c != ch2 && c != ch3)
			{
				continue;
			}
			ptr2 = pch - 2;
			*(ptr2 - 1) = c;
			while (pch < ptr)
			{
				if ((*(ptr2++) = *(pch++)) == '%')
				{
					c = UriHelper.DecodeHexChars(*(ptr2++) = *(pch++), *(ptr2++) = *(pch++));
					if (c == ch1 || c == ch2 || c == ch3)
					{
						ptr2 -= 2;
						*(ptr2 - 1) = c;
					}
				}
			}
			break;
		}
		ptr += 2;
		if (ptr2 == null)
		{
			return;
		}
		if (pch == ptr)
		{
			end -= (int)(pch - ptr2);
			return;
		}
		*(ptr2++) = *(pch++);
		if (pch == ptr)
		{
			end -= (int)(pch - ptr2);
			return;
		}
		*(ptr2++) = *(pch++);
		end -= (int)(pch - ptr2);
	}

	private static void Compress(char[] dest, int start, ref int destLength, UriParser syntax)
	{
		destLength = start + Compress(dest.AsSpan(start, destLength - start), syntax);
	}

	private static int Compress(Span<char> span, UriParser syntax)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int num5 = span.Length - 1; num5 >= 0; num5--)
		{
			char c = span[num5];
			if (c == '\\' && syntax.InFact(UriSyntaxFlags.ConvertPathSlashes))
			{
				c = (span[num5] = '/');
			}
			if (c == '/')
			{
				num++;
			}
			else
			{
				if (num > 1)
				{
					num2 = num5 + 1;
				}
				num = 0;
			}
			if (c == '.')
			{
				num3++;
				continue;
			}
			if (num3 != 0)
			{
				if ((!syntax.NotAny(UriSyntaxFlags.CanonicalizeAsFilePath) || (num3 <= 2 && c == '/')) && c == '/' && (num2 == num5 + num3 + 1 || (num2 == 0 && num5 + num3 + 1 == span.Length)) && num3 <= 2)
				{
					num2 = num5 + 1 + num3 + ((num2 != 0) ? 1 : 0);
					span.Slice(num2).CopyTo(span.Slice(num5 + 1));
					span = span.Slice(0, span.Length - (num2 - num5 - 1));
					num2 = num5;
					if (num3 == 2)
					{
						num4++;
					}
					num3 = 0;
					continue;
				}
				num3 = 0;
			}
			if (c == '/')
			{
				if (num4 != 0)
				{
					num4--;
					span.Slice(num2 + 1).CopyTo(span.Slice(num5 + 1));
					span = span.Slice(0, span.Length - (num2 - num5));
				}
				num2 = num5;
			}
		}
		if (span.Length != 0 && syntax.InFact(UriSyntaxFlags.CanonicalizeAsFilePath) && num <= 1)
		{
			if (num4 != 0 && span[0] != '/')
			{
				num2++;
				span.Slice(num2).CopyTo(span);
				return span.Length - num2;
			}
			if (num3 != 0 && (num2 == num3 || (num2 == 0 && num3 == span.Length)))
			{
				num3 += ((num2 != 0) ? 1 : 0);
				span.Slice(num3).CopyTo(span);
				return span.Length - num3;
			}
		}
		return span.Length;
	}

	private static string CombineUri(Uri basePart, string relativePart, UriFormat uriFormat)
	{
		char c = relativePart[0];
		if (basePart.IsDosPath && (c == '/' || c == '\\') && (relativePart.Length == 1 || (relativePart[1] != '/' && relativePart[1] != '\\')))
		{
			int num = basePart.OriginalString.IndexOf(':');
			if (basePart.IsImplicitFile)
			{
				return string.Concat(basePart.OriginalString.AsSpan(0, num + 1), relativePart);
			}
			num = basePart.OriginalString.IndexOf(':', num + 1);
			return string.Concat(basePart.OriginalString.AsSpan(0, num + 1), relativePart);
		}
		if (StaticIsFile(basePart.Syntax) && (c == '\\' || c == '/'))
		{
			if (relativePart.Length >= 2 && (relativePart[1] == '\\' || relativePart[1] == '/'))
			{
				if (!basePart.IsImplicitFile)
				{
					return "file:" + relativePart;
				}
				return relativePart;
			}
			if (basePart.IsUnc)
			{
				ReadOnlySpan<char> readOnlySpan = basePart.GetParts(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
				for (int i = 1; i < readOnlySpan.Length; i++)
				{
					if (readOnlySpan[i] == '/')
					{
						readOnlySpan = readOnlySpan.Slice(0, i);
						break;
					}
				}
				if (basePart.IsImplicitFile)
				{
					return string.Concat("\\\\", basePart.GetParts(UriComponents.Host, UriFormat.Unescaped), readOnlySpan, relativePart);
				}
				return string.Concat("file://", basePart.GetParts(UriComponents.Host, uriFormat), readOnlySpan, relativePart);
			}
			return "file://" + relativePart;
		}
		bool flag = basePart.Syntax.InFact(UriSyntaxFlags.ConvertPathSlashes);
		string text = null;
		if (c == '/' || (c == '\\' && flag))
		{
			if (relativePart.Length >= 2 && relativePart[1] == '/')
			{
				return basePart.Scheme + ":" + relativePart;
			}
			text = ((basePart.HostType != Flags.IPv6HostType) ? basePart.GetParts(UriComponents.SchemeAndServer | UriComponents.UserInfo, uriFormat) : $"{basePart.GetParts(UriComponents.Scheme | UriComponents.UserInfo, uriFormat)}[{basePart.DnsSafeHost}]{basePart.GetParts(UriComponents.Port | UriComponents.KeepDelimiter, uriFormat)}");
			if (!flag || c != '\\')
			{
				return text + relativePart;
			}
			return text + "/" + relativePart.AsSpan(1);
		}
		text = basePart.GetParts(UriComponents.Path | UriComponents.KeepDelimiter, basePart.IsImplicitFile ? UriFormat.Unescaped : uriFormat);
		int num2 = text.Length;
		char[] array = new char[num2 + relativePart.Length];
		if (num2 > 0)
		{
			text.CopyTo(0, array, 0, num2);
			while (num2 > 0)
			{
				if (array[--num2] == '/')
				{
					num2++;
					break;
				}
			}
		}
		relativePart.CopyTo(0, array, num2, relativePart.Length);
		c = (basePart.Syntax.InFact(UriSyntaxFlags.MayHaveQuery) ? '?' : '\uffff');
		char c2 = ((!basePart.IsImplicitFile && basePart.Syntax.InFact(UriSyntaxFlags.MayHaveFragment)) ? '#' : '\uffff');
		ReadOnlySpan<char> readOnlySpan2 = string.Empty;
		if (c != '\uffff' || c2 != '\uffff')
		{
			int j;
			for (j = 0; j < relativePart.Length && array[num2 + j] != c && array[num2 + j] != c2; j++)
			{
			}
			if (j == 0)
			{
				readOnlySpan2 = relativePart;
			}
			else if (j < relativePart.Length)
			{
				readOnlySpan2 = relativePart.AsSpan(j);
			}
			num2 += j;
		}
		else
		{
			num2 += relativePart.Length;
		}
		if (basePart.HostType == Flags.IPv6HostType)
		{
			text = ((!basePart.IsImplicitFile) ? (basePart.GetParts(UriComponents.Scheme | UriComponents.UserInfo, uriFormat) + "[" + basePart.DnsSafeHost + "]" + basePart.GetParts(UriComponents.Port | UriComponents.KeepDelimiter, uriFormat)) : ("\\\\[" + basePart.DnsSafeHost + "]"));
		}
		else if (basePart.IsImplicitFile)
		{
			if (basePart.IsDosPath)
			{
				Compress(array, 3, ref num2, basePart.Syntax);
				return string.Concat(array.AsSpan(1, num2 - 1), readOnlySpan2);
			}
			text = "\\\\" + basePart.GetParts(UriComponents.Host, UriFormat.Unescaped);
		}
		else
		{
			text = basePart.GetParts(UriComponents.SchemeAndServer | UriComponents.UserInfo, uriFormat);
		}
		Compress(array, basePart.SecuredPathIndex, ref num2, basePart.Syntax);
		return string.Concat(text, array.AsSpan(0, num2), readOnlySpan2);
	}

	private static string PathDifference(string path1, string path2, bool compareCase)
	{
		int num = -1;
		int i;
		for (i = 0; i < path1.Length && i < path2.Length && (path1[i] == path2[i] || (!compareCase && char.ToLowerInvariant(path1[i]) == char.ToLowerInvariant(path2[i]))); i++)
		{
			if (path1[i] == '/')
			{
				num = i;
			}
		}
		if (i == 0)
		{
			return path2;
		}
		if (i == path1.Length && i == path2.Length)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (; i < path1.Length; i++)
		{
			if (path1[i] == '/')
			{
				stringBuilder.Append("../");
			}
		}
		if (stringBuilder.Length == 0 && path2.Length - 1 == num)
		{
			return "./";
		}
		return stringBuilder.Append(path2.AsSpan(num + 1)).ToString();
	}

	[Obsolete("Uri.MakeRelative has been deprecated. Use MakeRelativeUri(Uri uri) instead.")]
	public string MakeRelative(Uri toUri)
	{
		if (toUri == null)
		{
			throw new ArgumentNullException("toUri");
		}
		if (IsNotAbsoluteUri || toUri.IsNotAbsoluteUri)
		{
			throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
		}
		if (Scheme == toUri.Scheme && Host == toUri.Host && Port == toUri.Port)
		{
			return PathDifference(AbsolutePath, toUri.AbsolutePath, !IsUncOrDosPath);
		}
		return toUri.ToString();
	}

	[Obsolete("Uri.Canonicalize has been deprecated and is not supported.")]
	protected virtual void Canonicalize()
	{
	}

	[Obsolete("Uri.Parse has been deprecated and is not supported.")]
	protected virtual void Parse()
	{
	}

	[Obsolete("Uri.Escape has been deprecated and is not supported.")]
	protected virtual void Escape()
	{
	}

	[Obsolete("Uri.Unescape has been deprecated. Use GetComponents() or static UnescapeDataString() to unescape a Uri component or a string.")]
	protected virtual string Unescape(string path)
	{
		char[] dest = new char[path.Length];
		int destPosition = 0;
		dest = UriHelper.UnescapeString(path, 0, path.Length, dest, ref destPosition, '\uffff', '\uffff', '\uffff', UnescapeMode.Unescape | UnescapeMode.UnescapeAll, null, isQuery: false);
		return new string(dest, 0, destPosition);
	}

	[Obsolete("Uri.EscapeString has been deprecated. Use GetComponents() or Uri.EscapeDataString to escape a Uri component or a string.")]
	protected static string EscapeString(string? str)
	{
		if (str != null)
		{
			return UriHelper.EscapeString(str, checkExistingEscaped: true, UriHelper.UnreservedReservedTable, '?', '#');
		}
		return string.Empty;
	}

	[Obsolete("Uri.CheckSecurity has been deprecated and is not supported.")]
	protected virtual void CheckSecurity()
	{
	}

	[Obsolete("Uri.IsReservedCharacter has been deprecated and is not supported.")]
	protected virtual bool IsReservedCharacter(char character)
	{
		if (character != ';' && character != '/' && character != ':' && character != '@' && character != '&' && character != '=' && character != '+' && character != '$')
		{
			return character == ',';
		}
		return true;
	}

	[Obsolete("Uri.IsExcludedCharacter has been deprecated and is not supported.")]
	protected static bool IsExcludedCharacter(char character)
	{
		if (character > ' ' && character < '\u007f' && character != '<' && character != '>' && character != '#' && character != '%' && character != '"' && character != '{' && character != '}' && character != '|' && character != '\\' && character != '^' && character != '[' && character != ']')
		{
			return character == '`';
		}
		return true;
	}

	[Obsolete("Uri.IsBadFileSystemCharacter has been deprecated and is not supported.")]
	protected virtual bool IsBadFileSystemCharacter(char character)
	{
		if (character >= ' ' && character != ';' && character != '/' && character != '?' && character != ':' && character != '&' && character != '=' && character != ',' && character != '*' && character != '<' && character != '>' && character != '"' && character != '|' && character != '\\')
		{
			return character == '^';
		}
		return true;
	}

	private void CreateThis(string uri, bool dontEscape, UriKind uriKind, in UriCreationOptions creationOptions = default(UriCreationOptions))
	{
		if (uriKind < UriKind.RelativeOrAbsolute || uriKind > UriKind.Relative)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_uri_InvalidUriKind, uriKind));
		}
		_string = uri ?? string.Empty;
		if (dontEscape)
		{
			_flags |= Flags.UserEscaped;
		}
		if (creationOptions.DangerousDisablePathAndQueryCanonicalization)
		{
			_flags |= Flags.DisablePathAndQueryCanonicalization;
		}
		ParsingError err = ParseScheme(_string, ref _flags, ref _syntax);
		InitializeUri(err, uriKind, out var e);
		if (e != null)
		{
			throw e;
		}
	}

	private void InitializeUri(ParsingError err, UriKind uriKind, out UriFormatException e)
	{
		if (err == ParsingError.None)
		{
			if (IsImplicitFile)
			{
				if (NotAny(Flags.DosPath) && uriKind != UriKind.Absolute && (uriKind == UriKind.Relative || (_string.Length >= 2 && (_string[0] != '\\' || _string[1] != '\\'))))
				{
					_syntax = null;
					_flags &= Flags.UserEscaped;
					e = null;
					return;
				}
				if (uriKind == UriKind.Relative && InFact(Flags.DosPath))
				{
					_syntax = null;
					_flags &= Flags.UserEscaped;
					e = null;
					return;
				}
			}
		}
		else if (err > ParsingError.EmptyUriString)
		{
			_string = null;
			e = GetException(err);
			return;
		}
		bool flag = false;
		if (IriParsing && CheckForUnicodeOrEscapedUnreserved(_string))
		{
			_flags |= Flags.HasUnicode;
			flag = true;
			_originalUnicodeString = _string;
		}
		if (_syntax != null)
		{
			if (_syntax.IsSimple)
			{
				if ((err = PrivateParseMinimal()) != 0)
				{
					if (uriKind != UriKind.Absolute && err <= ParsingError.EmptyUriString)
					{
						_syntax = null;
						e = null;
						_flags &= Flags.UserEscaped;
						return;
					}
					e = GetException(err);
				}
				else if (uriKind == UriKind.Relative)
				{
					e = GetException(ParsingError.CannotCreateRelative);
				}
				else
				{
					e = null;
				}
				if (flag)
				{
					try
					{
						EnsureParseRemaining();
						return;
					}
					catch (UriFormatException ex)
					{
						e = ex;
						return;
					}
				}
				return;
			}
			_syntax = _syntax.InternalOnNewUri();
			_flags |= Flags.UserDrivenParsing;
			_syntax.InternalValidate(this, out e);
			if (e != null)
			{
				if (uriKind != UriKind.Absolute && err != 0 && err <= ParsingError.EmptyUriString)
				{
					_syntax = null;
					e = null;
					_flags &= Flags.UserEscaped;
				}
				return;
			}
			if (err != 0 || InFact(Flags.ErrorOrParsingRecursion))
			{
				_flags = Flags.UserDrivenParsing | (_flags & Flags.UserEscaped);
			}
			else if (uriKind == UriKind.Relative)
			{
				e = GetException(ParsingError.CannotCreateRelative);
			}
			if (flag)
			{
				try
				{
					EnsureParseRemaining();
				}
				catch (UriFormatException ex2)
				{
					e = ex2;
				}
			}
		}
		else if (err != 0 && uriKind != UriKind.Absolute && err <= ParsingError.EmptyUriString)
		{
			e = null;
			_flags &= Flags.UserEscaped | Flags.HasUnicode;
			if (flag)
			{
				_string = EscapeUnescapeIri(_originalUnicodeString, 0, _originalUnicodeString.Length, (UriComponents)0);
				if (_string.Length > 65535)
				{
					err = ParsingError.SizeLimit;
				}
			}
		}
		else
		{
			_string = null;
			e = GetException(err);
		}
	}

	private static bool CheckForUnicodeOrEscapedUnreserved(string data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			char c = data[i];
			if (c == '%')
			{
				if ((uint)(i + 2) < (uint)data.Length)
				{
					char c2 = UriHelper.DecodeHexChars(data[i + 1], data[i + 2]);
					if (c2 >= UriHelper.UnreservedTable.Length || UriHelper.UnreservedTable[c2])
					{
						return true;
					}
					i += 2;
				}
			}
			else if (c > '\u007f')
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryCreate([NotNullWhen(true)] string? uriString, UriKind uriKind, [NotNullWhen(true)] out Uri? result)
	{
		if (uriString == null)
		{
			result = null;
			return false;
		}
		UriFormatException e = null;
		UriCreationOptions creationOptions = default(UriCreationOptions);
		result = CreateHelper(uriString, dontEscape: false, uriKind, ref e, in creationOptions);
		if (e == null)
		{
			return result != null;
		}
		return false;
	}

	public static bool TryCreate([NotNullWhen(true)] string? uriString, in UriCreationOptions creationOptions, [NotNullWhen(true)] out Uri? result)
	{
		if (uriString == null)
		{
			result = null;
			return false;
		}
		UriFormatException e = null;
		result = CreateHelper(uriString, dontEscape: false, UriKind.Absolute, ref e, in creationOptions);
		if (e == null)
		{
			return result != null;
		}
		return false;
	}

	public static bool TryCreate(Uri? baseUri, string? relativeUri, [NotNullWhen(true)] out Uri? result)
	{
		if (TryCreate(relativeUri, UriKind.RelativeOrAbsolute, out Uri result2))
		{
			if (!result2.IsAbsoluteUri)
			{
				return TryCreate(baseUri, result2, out result);
			}
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	public static bool TryCreate(Uri? baseUri, Uri? relativeUri, [NotNullWhen(true)] out Uri? result)
	{
		result = null;
		if ((object)baseUri == null || (object)relativeUri == null)
		{
			return false;
		}
		if (baseUri.IsNotAbsoluteUri)
		{
			return false;
		}
		UriFormatException parsingError = null;
		string newUriString = null;
		bool userEscaped;
		if (baseUri.Syntax.IsSimple)
		{
			userEscaped = relativeUri.UserEscaped;
			result = ResolveHelper(baseUri, relativeUri, ref newUriString, ref userEscaped);
		}
		else
		{
			userEscaped = false;
			newUriString = baseUri.Syntax.InternalResolve(baseUri, relativeUri, out parsingError);
			if (parsingError != null)
			{
				return false;
			}
		}
		if ((object)result == null)
		{
			string uriString = newUriString;
			bool dontEscape = userEscaped;
			UriCreationOptions creationOptions = default(UriCreationOptions);
			result = CreateHelper(uriString, dontEscape, UriKind.Absolute, ref parsingError, in creationOptions);
		}
		if (parsingError == null && result != null)
		{
			return result.IsAbsoluteUri;
		}
		return false;
	}

	public string GetComponents(UriComponents components, UriFormat format)
	{
		if (DisablePathAndQueryCanonicalization && (components & UriComponents.PathAndQuery) != 0)
		{
			throw new InvalidOperationException(System.SR.net_uri_GetComponentsCalledWhenCanonicalizationDisabled);
		}
		return InternalGetComponents(components, format);
	}

	private string InternalGetComponents(UriComponents components, UriFormat format)
	{
		if (((uint)components & 0x80000000u) != 0 && components != UriComponents.SerializationInfoString)
		{
			throw new ArgumentOutOfRangeException("components", components, System.SR.net_uri_NotJustSerialization);
		}
		if (((uint)format & 0xFFFFFFFCu) != 0)
		{
			throw new ArgumentOutOfRangeException("format");
		}
		if (IsNotAbsoluteUri)
		{
			if (components == UriComponents.SerializationInfoString)
			{
				return GetRelativeSerializationString(format);
			}
			throw new InvalidOperationException(System.SR.net_uri_NotAbsolute);
		}
		if (Syntax.IsSimple)
		{
			return GetComponentsHelper(components, format);
		}
		return Syntax.InternalGetComponents(this, components, format);
	}

	public static int Compare(Uri? uri1, Uri? uri2, UriComponents partsToCompare, UriFormat compareFormat, StringComparison comparisonType)
	{
		if ((object)uri1 == null)
		{
			if ((object)uri2 == null)
			{
				return 0;
			}
			return -1;
		}
		if ((object)uri2 == null)
		{
			return 1;
		}
		if (!uri1.IsAbsoluteUri || !uri2.IsAbsoluteUri)
		{
			if (!uri1.IsAbsoluteUri)
			{
				if (!uri2.IsAbsoluteUri)
				{
					return string.Compare(uri1.OriginalString, uri2.OriginalString, comparisonType);
				}
				return -1;
			}
			return 1;
		}
		return string.Compare(uri1.GetParts(partsToCompare, compareFormat), uri2.GetParts(partsToCompare, compareFormat), comparisonType);
	}

	public bool IsWellFormedOriginalString()
	{
		if (IsNotAbsoluteUri || Syntax.IsSimple)
		{
			return InternalIsWellFormedOriginalString();
		}
		return Syntax.InternalIsWellFormedOriginalString(this);
	}

	public static bool IsWellFormedUriString([NotNullWhen(true)] string? uriString, UriKind uriKind)
	{
		if (!TryCreate(uriString, uriKind, out Uri result))
		{
			return false;
		}
		return result.IsWellFormedOriginalString();
	}

	internal unsafe bool InternalIsWellFormedOriginalString()
	{
		if (UserDrivenParsing)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_uri_UserDrivenParsing, GetType()));
		}
		fixed (char* ptr = _string)
		{
			int idx = 0;
			if (!IsAbsoluteUri)
			{
				if (CheckForColonInFirstPathSegment(_string))
				{
					return false;
				}
				return (CheckCanonical(ptr, ref idx, _string.Length, '\ufffe') & (Check.EscapedCanonical | Check.BackslashInPath)) == Check.EscapedCanonical;
			}
			if (IsImplicitFile)
			{
				return false;
			}
			EnsureParseRemaining();
			Flags flags = _flags & (Flags.E_CannotDisplayCanonical | Flags.IriCanonical);
			if ((flags & Flags.IriCanonical) != Flags.Zero)
			{
				if ((flags & (Flags.E_UserNotCanonical | Flags.UserIriCanonical)) == (Flags.E_UserNotCanonical | Flags.UserIriCanonical))
				{
					flags &= ~(Flags.E_UserNotCanonical | Flags.UserIriCanonical);
				}
				if ((flags & (Flags.E_PathNotCanonical | Flags.PathIriCanonical)) == (Flags.E_PathNotCanonical | Flags.PathIriCanonical))
				{
					flags &= ~(Flags.E_PathNotCanonical | Flags.PathIriCanonical);
				}
				if ((flags & (Flags.E_QueryNotCanonical | Flags.QueryIriCanonical)) == (Flags.E_QueryNotCanonical | Flags.QueryIriCanonical))
				{
					flags &= ~(Flags.E_QueryNotCanonical | Flags.QueryIriCanonical);
				}
				if ((flags & (Flags.E_FragmentNotCanonical | Flags.FragmentIriCanonical)) == (Flags.E_FragmentNotCanonical | Flags.FragmentIriCanonical))
				{
					flags &= ~(Flags.E_FragmentNotCanonical | Flags.FragmentIriCanonical);
				}
			}
			if ((flags & Flags.E_CannotDisplayCanonical & (Flags.E_UserNotCanonical | Flags.E_PathNotCanonical | Flags.E_QueryNotCanonical | Flags.E_FragmentNotCanonical)) != Flags.Zero)
			{
				return false;
			}
			if (InFact(Flags.AuthorityFound))
			{
				idx = _info.Offset.Scheme + _syntax.SchemeName.Length + 2;
				if (idx >= _info.Offset.User || _string[idx - 1] == '\\' || _string[idx] == '\\')
				{
					return false;
				}
				if (InFact(Flags.DosPath | Flags.UncPath) && ++idx < _info.Offset.User && (_string[idx] == '/' || _string[idx] == '\\'))
				{
					return false;
				}
			}
			if (InFact(Flags.FirstSlashAbsent) && _info.Offset.Query > _info.Offset.Path)
			{
				return false;
			}
			if (InFact(Flags.BackslashInPath))
			{
				return false;
			}
			if (IsDosPath && _string[_info.Offset.Path + SecuredPathIndex - 1] == '|')
			{
				return false;
			}
			if ((_flags & Flags.CanonicalDnsHost) == Flags.Zero && HostType != Flags.IPv6HostType)
			{
				idx = _info.Offset.User;
				Check check = CheckCanonical(ptr, ref idx, _info.Offset.Path, '/');
				if ((check & (Check.EscapedCanonical | Check.BackslashInPath | Check.ReservedFound)) != Check.EscapedCanonical && (!IriParsing || (check & (Check.DisplayCanonical | Check.NotIriCanonical | Check.FoundNonAscii)) != (Check.DisplayCanonical | Check.FoundNonAscii)))
				{
					return false;
				}
			}
			if ((_flags & (Flags.SchemeNotCanonical | Flags.AuthorityFound)) == (Flags.SchemeNotCanonical | Flags.AuthorityFound))
			{
				idx = _syntax.SchemeName.Length;
				while (ptr[idx++] != ':')
				{
				}
				if (idx + 1 >= _string.Length || ptr[idx] != '/' || ptr[idx + 1] != '/')
				{
					return false;
				}
			}
		}
		return true;
	}

	public static string UnescapeDataString(string stringToUnescape)
	{
		if (stringToUnescape == null)
		{
			throw new ArgumentNullException("stringToUnescape");
		}
		if (stringToUnescape.Length == 0)
		{
			return string.Empty;
		}
		int num = stringToUnescape.IndexOf('%');
		if (num == -1)
		{
			return stringToUnescape;
		}
		Span<char> initialBuffer = stackalloc char[512];
		System.Text.ValueStringBuilder dest = new System.Text.ValueStringBuilder(initialBuffer);
		dest.EnsureCapacity(stringToUnescape.Length);
		dest.Append(stringToUnescape.AsSpan(0, num));
		UriHelper.UnescapeString(stringToUnescape, num, stringToUnescape.Length, ref dest, '\uffff', '\uffff', '\uffff', UnescapeMode.Unescape | UnescapeMode.UnescapeAll, null, isQuery: false);
		return dest.ToString();
	}

	[Obsolete("Uri.EscapeUriString can corrupt the Uri string in some cases. Consider using Uri.EscapeDataString for query string components instead.", DiagnosticId = "SYSLIB0013", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static string EscapeUriString(string stringToEscape)
	{
		return UriHelper.EscapeString(stringToEscape, checkExistingEscaped: false, UriHelper.UnreservedReservedTable);
	}

	public static string EscapeDataString(string stringToEscape)
	{
		return UriHelper.EscapeString(stringToEscape, checkExistingEscaped: false, UriHelper.UnreservedTable);
	}

	internal unsafe string EscapeUnescapeIri(string input, int start, int end, UriComponents component)
	{
		fixed (char* pInput = input)
		{
			return IriHelper.EscapeUnescapeIri(pInput, start, end, component);
		}
	}

	private Uri(Flags flags, UriParser uriParser, string uri)
	{
		_flags = flags;
		_syntax = uriParser;
		_string = uri;
	}

	internal static Uri CreateHelper(string uriString, bool dontEscape, UriKind uriKind, ref UriFormatException e, in UriCreationOptions creationOptions = default(UriCreationOptions))
	{
		if (uriKind < UriKind.RelativeOrAbsolute || uriKind > UriKind.Relative)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_uri_InvalidUriKind, uriKind));
		}
		UriParser syntax = null;
		Flags flags = Flags.Zero;
		ParsingError parsingError = ParseScheme(uriString, ref flags, ref syntax);
		if (dontEscape)
		{
			flags |= Flags.UserEscaped;
		}
		if (creationOptions.DangerousDisablePathAndQueryCanonicalization)
		{
			flags |= Flags.DisablePathAndQueryCanonicalization;
		}
		if (parsingError != 0)
		{
			if (uriKind != UriKind.Absolute && parsingError <= ParsingError.EmptyUriString)
			{
				return new Uri(flags & Flags.UserEscaped, null, uriString);
			}
			return null;
		}
		Uri uri = new Uri(flags, syntax, uriString);
		try
		{
			uri.InitializeUri(parsingError, uriKind, out e);
			if (e == null)
			{
				return uri;
			}
			return null;
		}
		catch (UriFormatException ex)
		{
			e = ex;
			return null;
		}
	}

	internal static Uri ResolveHelper(Uri baseUri, Uri relativeUri, ref string newUriString, ref bool userEscaped)
	{
		string text;
		if ((object)relativeUri != null)
		{
			if (relativeUri.IsAbsoluteUri)
			{
				return relativeUri;
			}
			text = relativeUri.OriginalString;
			userEscaped = relativeUri.UserEscaped;
		}
		else
		{
			text = string.Empty;
		}
		if (text.Length > 0 && (UriHelper.IsLWS(text[0]) || UriHelper.IsLWS(text[text.Length - 1])))
		{
			text = text.Trim(UriHelper.s_WSchars);
		}
		if (text.Length == 0)
		{
			newUriString = baseUri.GetParts(UriComponents.AbsoluteUri, baseUri.UserEscaped ? UriFormat.UriEscaped : UriFormat.SafeUnescaped);
			return null;
		}
		if (text[0] == '#' && !baseUri.IsImplicitFile && baseUri.Syntax.InFact(UriSyntaxFlags.MayHaveFragment))
		{
			newUriString = baseUri.GetParts(UriComponents.HttpRequestUrl | UriComponents.UserInfo, UriFormat.UriEscaped) + text;
			return null;
		}
		if (text[0] == '?' && !baseUri.IsImplicitFile && baseUri.Syntax.InFact(UriSyntaxFlags.MayHaveQuery))
		{
			newUriString = baseUri.GetParts(UriComponents.SchemeAndServer | UriComponents.UserInfo | UriComponents.Path, UriFormat.UriEscaped) + text;
			return null;
		}
		if (text.Length >= 3 && (text[1] == ':' || text[1] == '|') && UriHelper.IsAsciiLetter(text[0]) && (text[2] == '\\' || text[2] == '/'))
		{
			if (baseUri.IsImplicitFile)
			{
				newUriString = text;
				return null;
			}
			if (baseUri.Syntax.InFact(UriSyntaxFlags.AllowDOSPath))
			{
				newUriString = string.Concat(str1: (!baseUri.InFact(Flags.AuthorityFound)) ? (baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":/" : ":") : (baseUri.Syntax.InFact(UriSyntaxFlags.PathIsRooted) ? ":///" : "://"), str0: baseUri.Scheme, str2: text);
				return null;
			}
		}
		GetCombinedString(baseUri, text, userEscaped, ref newUriString);
		if ((object)newUriString == baseUri._string)
		{
			return baseUri;
		}
		return null;
	}

	private string GetRelativeSerializationString(UriFormat format)
	{
		switch (format)
		{
		case UriFormat.UriEscaped:
			return UriHelper.EscapeString(_string, checkExistingEscaped: true, UriHelper.UnreservedReservedTable);
		case UriFormat.Unescaped:
			return UnescapeDataString(_string);
		case UriFormat.SafeUnescaped:
		{
			if (_string.Length == 0)
			{
				return string.Empty;
			}
			Span<char> initialBuffer = stackalloc char[512];
			System.Text.ValueStringBuilder dest = new System.Text.ValueStringBuilder(initialBuffer);
			UriHelper.UnescapeString(_string, ref dest, '\uffff', '\uffff', '\uffff', UnescapeMode.EscapeUnescape, null, isQuery: false);
			return dest.ToString();
		}
		default:
			throw new ArgumentOutOfRangeException("format");
		}
	}

	internal string GetComponentsHelper(UriComponents uriComponents, UriFormat uriFormat)
	{
		if (uriComponents == UriComponents.Scheme)
		{
			return _syntax.SchemeName;
		}
		if (((uint)uriComponents & 0x80000000u) != 0)
		{
			uriComponents |= UriComponents.AbsoluteUri;
		}
		EnsureParseRemaining();
		if ((uriComponents & UriComponents.NormalizedHost) != 0)
		{
			uriComponents |= UriComponents.Host;
		}
		if ((uriComponents & UriComponents.Host) != 0)
		{
			EnsureHostString(allowDnsOptimization: true);
		}
		if (uriComponents == UriComponents.Port || uriComponents == UriComponents.StrongPort)
		{
			if ((_flags & Flags.NotDefaultPort) != Flags.Zero || (uriComponents == UriComponents.StrongPort && _syntax.DefaultPort != -1))
			{
				return _info.Offset.PortValue.ToString(CultureInfo.InvariantCulture);
			}
			return string.Empty;
		}
		if ((uriComponents & UriComponents.StrongPort) != 0)
		{
			uriComponents |= UriComponents.Port;
		}
		if (uriComponents == UriComponents.Host && (uriFormat == UriFormat.UriEscaped || (_flags & (Flags.HostNotCanonical | Flags.E_HostNotCanonical)) == Flags.Zero))
		{
			EnsureHostString(allowDnsOptimization: false);
			return _info.Host;
		}
		switch (uriFormat)
		{
		case UriFormat.UriEscaped:
			return GetEscapedParts(uriComponents);
		case UriFormat.Unescaped:
		case UriFormat.SafeUnescaped:
		case (UriFormat)32767:
			return GetUnescapedParts(uriComponents, uriFormat);
		default:
			throw new ArgumentOutOfRangeException("uriFormat");
		}
	}

	public bool IsBaseOf(Uri uri)
	{
		if ((object)uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (!IsAbsoluteUri)
		{
			return false;
		}
		if (Syntax.IsSimple)
		{
			return IsBaseOfHelper(uri);
		}
		return Syntax.InternalIsBaseOf(this, uri);
	}

	internal unsafe bool IsBaseOfHelper(Uri uriLink)
	{
		//The blocks IL_00a5, IL_00bf, IL_00c7, IL_00c8 are reachable both inside and outside the pinned region starting at IL_00a0. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!IsAbsoluteUri || UserDrivenParsing)
		{
			return false;
		}
		if (!uriLink.IsAbsoluteUri)
		{
			string newUriString = null;
			bool userEscaped = false;
			uriLink = ResolveHelper(this, uriLink, ref newUriString, ref userEscaped);
			if ((object)uriLink == null)
			{
				UriFormatException e = null;
				string uriString = newUriString;
				bool dontEscape = userEscaped;
				UriCreationOptions creationOptions = default(UriCreationOptions);
				uriLink = CreateHelper(uriString, dontEscape, UriKind.Absolute, ref e, in creationOptions);
				if (e != null)
				{
					return false;
				}
			}
		}
		if (Syntax.SchemeName != uriLink.Syntax.SchemeName)
		{
			return false;
		}
		string parts = GetParts(UriComponents.HttpRequestUrl | UriComponents.UserInfo, UriFormat.SafeUnescaped);
		string parts2 = uriLink.GetParts(UriComponents.HttpRequestUrl | UriComponents.UserInfo, UriFormat.SafeUnescaped);
		fixed (char* ptr3 = parts)
		{
			char* intPtr;
			char* selfPtr;
			int length;
			char* otherPtr;
			int length2;
			int ignoreCase;
			char* ptr2;
			if (parts2 != null)
			{
				fixed (char* ptr = &parts2.GetPinnableReference())
				{
					intPtr = (ptr2 = ptr);
					selfPtr = ptr3;
					length = parts.Length;
					otherPtr = ptr2;
					length2 = parts2.Length;
					ignoreCase = ((IsUncOrDosPath || uriLink.IsUncOrDosPath) ? 1 : 0);
					return UriHelper.TestForSubPath(selfPtr, length, otherPtr, length2, (byte)ignoreCase != 0);
				}
			}
			intPtr = (ptr2 = null);
			selfPtr = ptr3;
			length = parts.Length;
			otherPtr = ptr2;
			length2 = parts2.Length;
			ignoreCase = ((IsUncOrDosPath || uriLink.IsUncOrDosPath) ? 1 : 0);
			return UriHelper.TestForSubPath(selfPtr, length, otherPtr, length2, (byte)ignoreCase != 0);
		}
	}

	private void CreateThisFromUri(Uri otherUri)
	{
		_info = null;
		_flags = otherUri._flags;
		if (InFact(Flags.MinimalUriInfoSet))
		{
			_flags &= ~(Flags.IndexMask | Flags.MinimalUriInfoSet | Flags.AllUriInfoSet);
			int num = otherUri._info.Offset.Path;
			if (InFact(Flags.NotDefaultPort))
			{
				while (otherUri._string[num] != ':' && num > otherUri._info.Offset.Host)
				{
					num--;
				}
				if (otherUri._string[num] != ':')
				{
					num = otherUri._info.Offset.Path;
				}
			}
			_flags |= (Flags)num;
		}
		_syntax = otherUri._syntax;
		_string = otherUri._string;
		_originalUnicodeString = otherUri._originalUnicodeString;
	}
}
