using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Unicode;

namespace System.Net.Http.Headers;

internal readonly struct HeaderDescriptor : IEquatable<HeaderDescriptor>
{
	private readonly string _headerName;

	private readonly KnownHeader _knownHeader;

	public string Name => _headerName;

	public HttpHeaderParser Parser => _knownHeader?.Parser;

	public HttpHeaderType HeaderType
	{
		get
		{
			if (_knownHeader != null)
			{
				return _knownHeader.HeaderType;
			}
			return HttpHeaderType.Custom;
		}
	}

	public KnownHeader KnownHeader => _knownHeader;

	public HeaderDescriptor(KnownHeader knownHeader)
	{
		_knownHeader = knownHeader;
		_headerName = knownHeader.Name;
	}

	internal HeaderDescriptor(string headerName)
	{
		_headerName = headerName;
		_knownHeader = null;
	}

	public bool Equals(HeaderDescriptor other)
	{
		if (_knownHeader != null)
		{
			return _knownHeader == other._knownHeader;
		}
		return string.Equals(_headerName, other._headerName, StringComparison.OrdinalIgnoreCase);
	}

	public override int GetHashCode()
	{
		return _knownHeader?.GetHashCode() ?? StringComparer.OrdinalIgnoreCase.GetHashCode(_headerName);
	}

	public override bool Equals(object obj)
	{
		throw new InvalidOperationException();
	}

	public static bool TryGet(string headerName, out HeaderDescriptor descriptor)
	{
		KnownHeader knownHeader = KnownHeaders.TryGetKnownHeader(headerName);
		if (knownHeader != null)
		{
			descriptor = new HeaderDescriptor(knownHeader);
			return true;
		}
		if (!HttpRuleParser.IsToken(headerName))
		{
			descriptor = default(HeaderDescriptor);
			return false;
		}
		descriptor = new HeaderDescriptor(headerName);
		return true;
	}

	public static bool TryGet(ReadOnlySpan<byte> headerName, out HeaderDescriptor descriptor)
	{
		KnownHeader knownHeader = KnownHeaders.TryGetKnownHeader(headerName);
		if (knownHeader != null)
		{
			descriptor = new HeaderDescriptor(knownHeader);
			return true;
		}
		if (!HttpRuleParser.IsToken(headerName))
		{
			descriptor = default(HeaderDescriptor);
			return false;
		}
		descriptor = new HeaderDescriptor(HttpRuleParser.GetTokenString(headerName));
		return true;
	}

	internal static bool TryGetStaticQPackHeader(int index, out HeaderDescriptor descriptor, [NotNullWhen(true)] out string knownValue)
	{
		(HeaderDescriptor, string)[] headerLookup = QPackStaticTable.HeaderLookup;
		if ((uint)index < (uint)headerLookup.Length)
		{
			(descriptor, knownValue) = headerLookup[index];
			return true;
		}
		descriptor = default(HeaderDescriptor);
		knownValue = null;
		return false;
	}

	public HeaderDescriptor AsCustomHeader()
	{
		return new HeaderDescriptor(_knownHeader.Name);
	}

	public string GetHeaderValue(ReadOnlySpan<byte> headerValue, Encoding valueEncoding)
	{
		if (headerValue.Length == 0)
		{
			return string.Empty;
		}
		if (_knownHeader != null)
		{
			string[] knownValues = _knownHeader.KnownValues;
			if (knownValues != null)
			{
				for (int i = 0; i < knownValues.Length; i++)
				{
					if (ByteArrayHelpers.EqualsOrdinalAsciiIgnoreCase(knownValues[i], headerValue))
					{
						return knownValues[i];
					}
				}
			}
			string decoded;
			if (_knownHeader == KnownHeaders.ContentType)
			{
				string knownContentType = GetKnownContentType(headerValue);
				if (knownContentType != null)
				{
					return knownContentType;
				}
			}
			else if (_knownHeader == KnownHeaders.Location && TryDecodeUtf8(headerValue, out decoded))
			{
				return decoded;
			}
		}
		return (valueEncoding ?? HttpRuleParser.DefaultHttpEncoding).GetString(headerValue);
	}

	internal static string GetKnownContentType(ReadOnlySpan<byte> contentTypeValue)
	{
		string text = null;
		switch (contentTypeValue.Length)
		{
		case 8:
			switch (contentTypeValue[7] | 0x20)
			{
			case 108:
				text = "text/xml";
				break;
			case 115:
				text = "text/css";
				break;
			case 118:
				text = "text/csv";
				break;
			}
			break;
		case 9:
			switch (contentTypeValue[6] | 0x20)
			{
			case 103:
				text = "image/gif";
				break;
			case 112:
				text = "image/png";
				break;
			case 116:
				text = "text/html";
				break;
			}
			break;
		case 10:
			switch (contentTypeValue[0] | 0x20)
			{
			case 116:
				text = "text/plain";
				break;
			case 105:
				text = "image/jpeg";
				break;
			}
			break;
		case 15:
			switch (contentTypeValue[12] | 0x20)
			{
			case 112:
				text = "application/pdf";
				break;
			case 120:
				text = "application/xml";
				break;
			case 122:
				text = "application/zip";
				break;
			}
			break;
		case 16:
			switch (contentTypeValue[12] | 0x20)
			{
			case 103:
				text = "application/grpc";
				break;
			case 106:
				text = "application/json";
				break;
			}
			break;
		case 19:
			text = "multipart/form-data";
			break;
		case 22:
			text = "application/javascript";
			break;
		case 24:
			switch (contentTypeValue[0] | 0x20)
			{
			case 97:
				text = "application/octet-stream";
				break;
			case 116:
				text = "text/html; charset=utf-8";
				break;
			}
			break;
		case 25:
			text = "text/plain; charset=utf-8";
			break;
		case 31:
			text = "application/json; charset=utf-8";
			break;
		case 33:
			text = "application/x-www-form-urlencoded";
			break;
		}
		if (text == null || !ByteArrayHelpers.EqualsOrdinalAsciiIgnoreCase(text, contentTypeValue))
		{
			return null;
		}
		return text;
	}

	private static bool TryDecodeUtf8(ReadOnlySpan<byte> input, [NotNullWhen(true)] out string decoded)
	{
		char[] array = ArrayPool<char>.Shared.Rent(input.Length);
		try
		{
			if (Utf8.ToUtf16(input, array, out var _, out var charsWritten, replaceInvalidSequences: false) == OperationStatus.Done)
			{
				decoded = new string(array, 0, charsWritten);
				return true;
			}
		}
		finally
		{
			ArrayPool<char>.Shared.Return(array);
		}
		decoded = null;
		return false;
	}
}
