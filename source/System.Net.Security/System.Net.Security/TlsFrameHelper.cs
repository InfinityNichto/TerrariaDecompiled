using System.Buffers.Binary;
using System.Globalization;
using System.Security.Authentication;
using System.Text;

namespace System.Net.Security;

internal static class TlsFrameHelper
{
	[Flags]
	public enum ProcessingOptions
	{
		All = 0,
		ServerName = 1,
		ApplicationProtocol = 2,
		Versions = 4
	}

	[Flags]
	public enum ApplicationProtocolInfo
	{
		None = 0,
		Http11 = 1,
		Http2 = 2,
		Other = 0x80
	}

	public struct TlsFrameInfo
	{
		public TlsFrameHeader Header;

		public TlsHandshakeType HandshakeType;

		public SslProtocols SupportedVersions;

		public string TargetName;

		public ApplicationProtocolInfo ApplicationProtocols;

		public TlsAlertDescription AlertDescription;

		public override string ToString()
		{
			if (Header.Type == TlsContentType.Handshake)
			{
				if (HandshakeType != TlsHandshakeType.ClientHello)
				{
					if (HandshakeType != TlsHandshakeType.ServerHello)
					{
						return $"{Header.Version}:{HandshakeType}[{Header.Length}] SupportedVersion='{SupportedVersions}'";
					}
					return $"{Header.Version}:{HandshakeType}[{Header.Length}] SupportedVersion='{SupportedVersions}' ApplicationProtocols='{ApplicationProtocols}'";
				}
				return $"{Header.Version}:{HandshakeType}[{Header.Length}] TargetName='{TargetName}' SupportedVersion='{SupportedVersions}' ApplicationProtocols='{ApplicationProtocols}'";
			}
			return $"{Header.Version}:{Header.Type}[{Header.Length}]";
		}
	}

	public delegate bool HelloExtensionCallback(ref TlsFrameInfo info, ExtensionType type, ReadOnlySpan<byte> extensionsData);

	private enum NameType : byte
	{
		HostName
	}

	private static byte[] s_protocolMismatch13 = new byte[7] { 21, 3, 4, 0, 2, 2, 70 };

	private static byte[] s_protocolMismatch12 = new byte[7] { 21, 3, 3, 0, 2, 2, 70 };

	private static byte[] s_protocolMismatch11 = new byte[7] { 21, 3, 2, 0, 2, 2, 70 };

	private static byte[] s_protocolMismatch10 = new byte[7] { 21, 3, 1, 0, 2, 2, 70 };

	private static byte[] s_protocolMismatch30 = new byte[7] { 21, 3, 0, 0, 2, 2, 40 };

	private static readonly IdnMapping s_idnMapping = new IdnMapping
	{
		AllowUnassigned = true
	};

	private static readonly Encoding s_encoding = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());

	public static bool TryGetFrameHeader(ReadOnlySpan<byte> frame, ref TlsFrameHeader header)
	{
		bool result = frame.Length > 4;
		if (frame.Length >= 1)
		{
			header.Type = (TlsContentType)frame[0];
			if (frame.Length >= 3)
			{
				if (frame[1] == 3)
				{
					if (frame.Length > 4)
					{
						header.Length = (frame[3] << 8) | frame[4];
					}
					header.Version = TlsMinorVersionToProtocol(frame[2]);
				}
				else
				{
					header.Length = -1;
					header.Version = SslProtocols.None;
				}
			}
		}
		return result;
	}

	public static bool TryGetFrameInfo(ReadOnlySpan<byte> frame, ref TlsFrameInfo info, ProcessingOptions options = ProcessingOptions.All, HelloExtensionCallback callback = null)
	{
		if (frame.Length < 5)
		{
			return false;
		}
		bool flag = TryGetFrameHeader(frame, ref info.Header);
		info.SupportedVersions = info.Header.Version;
		if (info.Header.Type == TlsContentType.Alert)
		{
			TlsAlertLevel level = (TlsAlertLevel)0;
			TlsAlertDescription description = TlsAlertDescription.CloseNotify;
			if (TryGetAlertInfo(frame, ref level, ref description))
			{
				info.AlertDescription = description;
				return true;
			}
			return false;
		}
		if (info.Header.Type != TlsContentType.Handshake || frame.Length <= 5)
		{
			return false;
		}
		info.HandshakeType = (TlsHandshakeType)frame[5];
		bool result = frame.Length >= 5 + info.Header.Length;
		if (info.Header.Version >= SslProtocols.Tls && (info.HandshakeType == TlsHandshakeType.ClientHello || info.HandshakeType == TlsHandshakeType.ServerHello) && !TryParseHelloFrame(frame.Slice(5), ref info, options, callback))
		{
			result = false;
		}
		return result;
	}

	public static bool TryGetAlertInfo(ReadOnlySpan<byte> frame, ref TlsAlertLevel level, ref TlsAlertDescription description)
	{
		if (frame.Length < 7 || frame[0] != 21)
		{
			return false;
		}
		level = (TlsAlertLevel)frame[5];
		description = (TlsAlertDescription)frame[6];
		return true;
	}

	private static byte[] CreateProtocolVersionAlert(SslProtocols version)
	{
		return version switch
		{
			SslProtocols.Tls13 => s_protocolMismatch13, 
			SslProtocols.Tls12 => s_protocolMismatch12, 
			SslProtocols.Tls11 => s_protocolMismatch11, 
			SslProtocols.Tls => s_protocolMismatch10, 
			SslProtocols.Ssl3 => s_protocolMismatch30, 
			_ => Array.Empty<byte>(), 
		};
	}

	public static byte[] CreateAlertFrame(SslProtocols version, TlsAlertDescription reason)
	{
		if (reason == TlsAlertDescription.ProtocolVersion)
		{
			return CreateProtocolVersionAlert(version);
		}
		if (version > SslProtocols.Tls)
		{
			byte[] obj = new byte[7] { 21, 3, 3, 0, 2, 2, 0 };
			obj[6] = (byte)reason;
			byte[] array = obj;
			switch (version)
			{
			case SslProtocols.Tls13:
				array[2] = 4;
				break;
			case SslProtocols.Tls11:
				array[2] = 2;
				break;
			case SslProtocols.Tls:
				array[2] = 1;
				break;
			}
			return array;
		}
		return Array.Empty<byte>();
	}

	private static bool TryParseHelloFrame(ReadOnlySpan<byte> sslHandshake, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback callback)
	{
		if (sslHandshake.Length < 4 || (sslHandshake[0] != 1 && sslHandshake[0] != 2))
		{
			return false;
		}
		int num = ReadUInt24BigEndian(sslHandshake.Slice(1));
		ReadOnlySpan<byte> readOnlySpan = sslHandshake.Slice(4);
		if (readOnlySpan.Length < num)
		{
			return false;
		}
		if (readOnlySpan[0] == 3)
		{
			info.SupportedVersions |= TlsMinorVersionToProtocol(readOnlySpan[1]);
		}
		if (sslHandshake[0] != 1)
		{
			return TryParseServerHello(readOnlySpan.Slice(0, num), ref info, options, callback);
		}
		return TryParseClientHello(readOnlySpan.Slice(0, num), ref info, options, callback);
	}

	private static bool TryParseClientHello(ReadOnlySpan<byte> clientHello, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback callback)
	{
		ReadOnlySpan<byte> bytes = SkipBytes(clientHello, 34);
		bytes = SkipOpaqueType1(bytes);
		bytes = SkipOpaqueType2(bytes);
		bytes = SkipOpaqueType1(bytes);
		if (bytes.IsEmpty)
		{
			return false;
		}
		int num = BinaryPrimitives.ReadUInt16BigEndian(bytes);
		bytes = SkipBytes(bytes, 2);
		if (num != bytes.Length)
		{
			return false;
		}
		return TryParseHelloExtensions(bytes, ref info, options, callback);
	}

	private static bool TryParseServerHello(ReadOnlySpan<byte> serverHello, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback callback)
	{
		ReadOnlySpan<byte> bytes = SkipBytes(serverHello, 34);
		bytes = SkipOpaqueType1(bytes);
		bytes = SkipBytes(bytes, 3);
		if (bytes.IsEmpty)
		{
			return false;
		}
		int num = BinaryPrimitives.ReadUInt16BigEndian(bytes);
		bytes = SkipBytes(bytes, 2);
		if (num != bytes.Length)
		{
			return false;
		}
		return TryParseHelloExtensions(bytes, ref info, options, callback);
	}

	private static bool TryParseHelloExtensions(ReadOnlySpan<byte> extensions, ref TlsFrameInfo info, ProcessingOptions options, HelloExtensionCallback callback)
	{
		bool result = true;
		while (extensions.Length >= 4)
		{
			ExtensionType extensionType = (ExtensionType)BinaryPrimitives.ReadUInt16BigEndian(extensions);
			extensions = SkipBytes(extensions, 2);
			ushort num = BinaryPrimitives.ReadUInt16BigEndian(extensions);
			extensions = SkipBytes(extensions, 2);
			if (extensions.Length < num)
			{
				result = false;
				break;
			}
			ReadOnlySpan<byte> readOnlySpan = extensions.Slice(0, num);
			if (extensionType == ExtensionType.ServerName && (options == ProcessingOptions.All || (options & ProcessingOptions.ServerName) == ProcessingOptions.ServerName))
			{
				if (!TryGetSniFromServerNameList(readOnlySpan, out var sni))
				{
					return false;
				}
				info.TargetName = sni;
			}
			else if (extensionType == ExtensionType.SupportedVersions && (options == ProcessingOptions.All || (options & ProcessingOptions.Versions) == ProcessingOptions.Versions))
			{
				if (!TryGetSupportedVersionsFromExtension(readOnlySpan, out var protocols))
				{
					return false;
				}
				info.SupportedVersions |= protocols;
			}
			else if (extensionType == ExtensionType.ApplicationProtocols && (options == ProcessingOptions.All || (options & ProcessingOptions.ApplicationProtocol) == ProcessingOptions.ApplicationProtocol))
			{
				if (!TryGetApplicationProtocolsFromExtension(readOnlySpan, out var alpn))
				{
					return false;
				}
				info.ApplicationProtocols |= alpn;
			}
			callback?.Invoke(ref info, extensionType, readOnlySpan);
			extensions = extensions.Slice(num);
		}
		return result;
	}

	private static bool TryGetSniFromServerNameList(ReadOnlySpan<byte> serverNameListExtension, out string sni)
	{
		sni = null;
		if (serverNameListExtension.Length < 2)
		{
			return false;
		}
		int num = BinaryPrimitives.ReadUInt16BigEndian(serverNameListExtension);
		ReadOnlySpan<byte> readOnlySpan = serverNameListExtension.Slice(2);
		if (num != readOnlySpan.Length)
		{
			return false;
		}
		ReadOnlySpan<byte> serverName = readOnlySpan.Slice(0, num);
		sni = GetSniFromServerName(serverName, out var invalid);
		return !invalid;
	}

	private static string GetSniFromServerName(ReadOnlySpan<byte> serverName, out bool invalid)
	{
		if (serverName.Length < 1)
		{
			invalid = true;
			return null;
		}
		NameType nameType = (NameType)serverName[0];
		ReadOnlySpan<byte> hostNameStruct = serverName.Slice(1);
		if (nameType != 0)
		{
			invalid = true;
			return null;
		}
		return GetSniFromHostNameStruct(hostNameStruct, out invalid);
	}

	private static string GetSniFromHostNameStruct(ReadOnlySpan<byte> hostNameStruct, out bool invalid)
	{
		int num = BinaryPrimitives.ReadUInt16BigEndian(hostNameStruct);
		ReadOnlySpan<byte> bytes = hostNameStruct.Slice(2);
		if (num != bytes.Length)
		{
			invalid = true;
			return null;
		}
		invalid = false;
		return DecodeString(bytes);
	}

	private static bool TryGetSupportedVersionsFromExtension(ReadOnlySpan<byte> extensionData, out SslProtocols protocols)
	{
		protocols = SslProtocols.None;
		byte b = extensionData[0];
		extensionData = extensionData.Slice(1);
		if (extensionData.Length != b)
		{
			return false;
		}
		while (extensionData.Length >= 2)
		{
			if (extensionData[0] == 3)
			{
				protocols |= TlsMinorVersionToProtocol(extensionData[1]);
			}
			extensionData = extensionData.Slice(2);
		}
		return true;
	}

	private static bool TryGetApplicationProtocolsFromExtension(ReadOnlySpan<byte> extensionData, out ApplicationProtocolInfo alpn)
	{
		alpn = ApplicationProtocolInfo.None;
		if (extensionData.Length < 2)
		{
			return false;
		}
		int num = BinaryPrimitives.ReadUInt16BigEndian(extensionData);
		ReadOnlySpan<byte> readOnlySpan = extensionData.Slice(2);
		if (num != readOnlySpan.Length)
		{
			return false;
		}
		while (!readOnlySpan.IsEmpty)
		{
			byte b = readOnlySpan[0];
			if (readOnlySpan.Length < b + 1)
			{
				return false;
			}
			ReadOnlySpan<byte> span = readOnlySpan.Slice(1, b);
			if (b == 2)
			{
				if (span.SequenceEqual(SslApplicationProtocol.Http2.Protocol.Span))
				{
					alpn |= ApplicationProtocolInfo.Http2;
				}
				else
				{
					alpn |= ApplicationProtocolInfo.Other;
				}
			}
			else if (b == SslApplicationProtocol.Http11.Protocol.Length && span.SequenceEqual(SslApplicationProtocol.Http11.Protocol.Span))
			{
				alpn |= ApplicationProtocolInfo.Http11;
			}
			else
			{
				alpn |= ApplicationProtocolInfo.Other;
			}
			readOnlySpan = readOnlySpan.Slice(b + 1);
		}
		return true;
	}

	private static SslProtocols TlsMinorVersionToProtocol(byte value)
	{
		return value switch
		{
			4 => SslProtocols.Tls13, 
			3 => SslProtocols.Tls12, 
			2 => SslProtocols.Tls11, 
			1 => SslProtocols.Tls, 
			0 => SslProtocols.Ssl3, 
			_ => SslProtocols.None, 
		};
	}

	private static string DecodeString(ReadOnlySpan<byte> bytes)
	{
		string @string;
		try
		{
			@string = s_encoding.GetString(bytes);
		}
		catch (DecoderFallbackException)
		{
			return null;
		}
		try
		{
			return s_idnMapping.GetUnicode(@string);
		}
		catch (ArgumentException)
		{
			return @string;
		}
	}

	private static int ReadUInt24BigEndian(ReadOnlySpan<byte> bytes)
	{
		return (bytes[0] << 16) | (bytes[1] << 8) | bytes[2];
	}

	private static ReadOnlySpan<byte> SkipBytes(ReadOnlySpan<byte> bytes, int numberOfBytesToSkip)
	{
		if (numberOfBytesToSkip >= bytes.Length)
		{
			return ReadOnlySpan<byte>.Empty;
		}
		return bytes.Slice(numberOfBytesToSkip);
	}

	private static ReadOnlySpan<byte> SkipOpaqueType1(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length < 1)
		{
			return ReadOnlySpan<byte>.Empty;
		}
		byte b = bytes[0];
		int numberOfBytesToSkip = 1 + b;
		return SkipBytes(bytes, numberOfBytesToSkip);
	}

	private static ReadOnlySpan<byte> SkipOpaqueType2(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length < 2)
		{
			return ReadOnlySpan<byte>.Empty;
		}
		ushort num = BinaryPrimitives.ReadUInt16BigEndian(bytes);
		int numberOfBytesToSkip = 2 + num;
		return SkipBytes(bytes, numberOfBytesToSkip);
	}
}
