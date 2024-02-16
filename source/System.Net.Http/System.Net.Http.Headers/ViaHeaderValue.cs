using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

public class ViaHeaderValue : ICloneable
{
	private readonly string _protocolName;

	private readonly string _protocolVersion;

	private readonly string _receivedBy;

	private readonly string _comment;

	public string? ProtocolName => _protocolName;

	public string ProtocolVersion => _protocolVersion;

	public string ReceivedBy => _receivedBy;

	public string? Comment => _comment;

	public ViaHeaderValue(string protocolVersion, string receivedBy)
		: this(protocolVersion, receivedBy, null, null)
	{
	}

	public ViaHeaderValue(string protocolVersion, string receivedBy, string? protocolName)
		: this(protocolVersion, receivedBy, protocolName, null)
	{
	}

	public ViaHeaderValue(string protocolVersion, string receivedBy, string? protocolName, string? comment)
	{
		HeaderUtilities.CheckValidToken(protocolVersion, "protocolVersion");
		CheckReceivedBy(receivedBy);
		if (!string.IsNullOrEmpty(protocolName))
		{
			HeaderUtilities.CheckValidToken(protocolName, "protocolName");
			_protocolName = protocolName;
		}
		if (!string.IsNullOrEmpty(comment))
		{
			HeaderUtilities.CheckValidComment(comment, "comment");
			_comment = comment;
		}
		_protocolVersion = protocolVersion;
		_receivedBy = receivedBy;
	}

	private ViaHeaderValue(ViaHeaderValue source)
	{
		_protocolName = source._protocolName;
		_protocolVersion = source._protocolVersion;
		_receivedBy = source._receivedBy;
		_comment = source._comment;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		if (!string.IsNullOrEmpty(_protocolName))
		{
			stringBuilder.Append(_protocolName);
			stringBuilder.Append('/');
		}
		stringBuilder.Append(_protocolVersion);
		stringBuilder.Append(' ');
		stringBuilder.Append(_receivedBy);
		if (!string.IsNullOrEmpty(_comment))
		{
			stringBuilder.Append(' ');
			stringBuilder.Append(_comment);
		}
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ViaHeaderValue viaHeaderValue))
		{
			return false;
		}
		if (string.Equals(_protocolVersion, viaHeaderValue._protocolVersion, StringComparison.OrdinalIgnoreCase) && string.Equals(_receivedBy, viaHeaderValue._receivedBy, StringComparison.OrdinalIgnoreCase) && string.Equals(_protocolName, viaHeaderValue._protocolName, StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(_comment, viaHeaderValue._comment, StringComparison.Ordinal);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = StringComparer.OrdinalIgnoreCase.GetHashCode(_protocolVersion) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_receivedBy);
		if (!string.IsNullOrEmpty(_protocolName))
		{
			num ^= StringComparer.OrdinalIgnoreCase.GetHashCode(_protocolName);
		}
		if (!string.IsNullOrEmpty(_comment))
		{
			num ^= _comment.GetHashCode();
		}
		return num;
	}

	public static ViaHeaderValue Parse(string? input)
	{
		int index = 0;
		return (ViaHeaderValue)GenericHeaderParser.SingleValueViaParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out ViaHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.SingleValueViaParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (ViaHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetViaLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int protocolEndIndex = GetProtocolEndIndex(input, startIndex, out var protocolName, out var protocolVersion);
		if (protocolEndIndex == startIndex || protocolEndIndex == input.Length)
		{
			return 0;
		}
		string host;
		int hostLength = HttpRuleParser.GetHostLength(input, protocolEndIndex, allowToken: true, out host);
		if (hostLength == 0)
		{
			return 0;
		}
		protocolEndIndex += hostLength;
		protocolEndIndex += HttpRuleParser.GetWhitespaceLength(input, protocolEndIndex);
		string comment = null;
		if (protocolEndIndex < input.Length && input[protocolEndIndex] == '(')
		{
			int length = 0;
			if (HttpRuleParser.GetCommentLength(input, protocolEndIndex, out length) != 0)
			{
				return 0;
			}
			comment = input.Substring(protocolEndIndex, length);
			protocolEndIndex += length;
			protocolEndIndex += HttpRuleParser.GetWhitespaceLength(input, protocolEndIndex);
		}
		parsedValue = new ViaHeaderValue(protocolVersion, host, protocolName, comment);
		return protocolEndIndex - startIndex;
	}

	private static int GetProtocolEndIndex(string input, int startIndex, out string protocolName, out string protocolVersion)
	{
		protocolName = null;
		protocolVersion = null;
		int startIndex2 = startIndex;
		int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex2);
		if (tokenLength == 0)
		{
			return 0;
		}
		startIndex2 = startIndex + tokenLength;
		int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, startIndex2);
		startIndex2 += whitespaceLength;
		if (startIndex2 == input.Length)
		{
			return 0;
		}
		if (input[startIndex2] == '/')
		{
			protocolName = input.Substring(startIndex, tokenLength);
			startIndex2++;
			startIndex2 += HttpRuleParser.GetWhitespaceLength(input, startIndex2);
			tokenLength = HttpRuleParser.GetTokenLength(input, startIndex2);
			if (tokenLength == 0)
			{
				return 0;
			}
			protocolVersion = input.Substring(startIndex2, tokenLength);
			startIndex2 += tokenLength;
			whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, startIndex2);
			startIndex2 += whitespaceLength;
		}
		else
		{
			protocolVersion = input.Substring(startIndex, tokenLength);
		}
		if (whitespaceLength == 0)
		{
			return 0;
		}
		return startIndex2;
	}

	object ICloneable.Clone()
	{
		return new ViaHeaderValue(this);
	}

	private static void CheckReceivedBy(string receivedBy)
	{
		if (string.IsNullOrEmpty(receivedBy))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "receivedBy");
		}
		if (HttpRuleParser.GetHostLength(receivedBy, 0, allowToken: true, out var _) != receivedBy.Length)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, receivedBy));
		}
	}
}
