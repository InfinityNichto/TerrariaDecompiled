using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Data.Common;

[DefaultMember("Item")]
internal class DbConnectionOptions
{
	private enum ParserState
	{
		NothingYet = 1,
		Key,
		KeyEqual,
		KeyEnd,
		UnquotedValue,
		DoubleQuoteValue,
		DoubleQuoteValueQuote,
		SingleQuoteValue,
		SingleQuoteValueQuote,
		BraceQuoteValue,
		BraceQuoteValueQuote,
		QuotedValueEnd,
		NullTermination
	}

	internal readonly bool _useOdbcRules;

	internal readonly bool _hasUserIdKeyword;

	private static readonly Regex s_connectionStringValidKeyRegex = new Regex("^(?![;\\s])[^\\p{Cc}]+(?<!\\s)$", RegexOptions.Compiled);

	private static readonly Regex s_connectionStringValidValueRegex = new Regex("^[^\0]*$", RegexOptions.Compiled);

	private static readonly Regex s_connectionStringQuoteValueRegex = new Regex("^[^\"'=;\\s\\p{Cc}]*$", RegexOptions.Compiled);

	private static readonly Regex s_connectionStringQuoteOdbcValueRegex = new Regex("^\\{([^\\}\0]|\\}\\})*\\}$", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

	private readonly string _usersConnectionString;

	private readonly Dictionary<string, string> _parsetable;

	internal readonly NameValuePair _keyChain;

	internal readonly bool _hasPasswordKeyword;

	public DbConnectionOptions(string connectionString, Dictionary<string, string> synonyms, bool useOdbcRules)
	{
		_useOdbcRules = useOdbcRules;
		_parsetable = new Dictionary<string, string>();
		_usersConnectionString = ((connectionString != null) ? connectionString : "");
		if (0 < _usersConnectionString.Length)
		{
			_keyChain = ParseInternal(_parsetable, _usersConnectionString, buildChain: true, synonyms, _useOdbcRules);
			_hasPasswordKeyword = _parsetable.ContainsKey("password") || _parsetable.ContainsKey("pwd");
			_hasUserIdKeyword = _parsetable.ContainsKey("user id") || _parsetable.ContainsKey("uid");
		}
	}

	internal static void AppendKeyValuePairBuilder(StringBuilder builder, string keyName, string keyValue, bool useOdbcRules)
	{
		ADP.CheckArgumentNull(builder, "builder");
		ADP.CheckArgumentLength(keyName, "keyName");
		if (keyName == null || !s_connectionStringValidKeyRegex.IsMatch(keyName))
		{
			throw ADP.InvalidKeyname(keyName);
		}
		if (keyValue != null && !IsValueValidInternal(keyValue))
		{
			throw ADP.InvalidValue(keyName);
		}
		if (0 < builder.Length && ';' != builder[builder.Length - 1])
		{
			builder.Append(';');
		}
		if (useOdbcRules)
		{
			builder.Append(keyName);
		}
		else
		{
			builder.Append(keyName.Replace("=", "=="));
		}
		builder.Append('=');
		if (keyValue == null)
		{
			return;
		}
		if (useOdbcRules)
		{
			if (0 < keyValue.Length && ('{' == keyValue[0] || 0 <= keyValue.IndexOf(';') || string.Equals("Driver", keyName, StringComparison.OrdinalIgnoreCase)) && !s_connectionStringQuoteOdbcValueRegex.IsMatch(keyValue))
			{
				builder.Append('{').Append(keyValue.Replace("}", "}}")).Append('}');
			}
			else
			{
				builder.Append(keyValue);
			}
		}
		else if (s_connectionStringQuoteValueRegex.IsMatch(keyValue))
		{
			builder.Append(keyValue);
		}
		else if (keyValue.Contains('"') && !keyValue.Contains('\''))
		{
			builder.Append('\'');
			builder.Append(keyValue);
			builder.Append('\'');
		}
		else
		{
			builder.Append('"');
			builder.Append(keyValue.Replace("\"", "\"\""));
			builder.Append('"');
		}
	}

	internal static void ValidateKeyValuePair(string keyword, string value)
	{
		if (keyword == null || !s_connectionStringValidKeyRegex.IsMatch(keyword))
		{
			throw ADP.InvalidKeyname(keyword);
		}
		if (value != null && !s_connectionStringValidValueRegex.IsMatch(value))
		{
			throw ADP.InvalidValue(keyword);
		}
	}

	private static string GetKeyName(StringBuilder buffer)
	{
		int num = buffer.Length;
		while (0 < num && char.IsWhiteSpace(buffer[num - 1]))
		{
			num--;
		}
		return buffer.ToString(0, num).ToLowerInvariant();
	}

	private static string GetKeyValue(StringBuilder buffer, bool trimWhitespace)
	{
		int num = buffer.Length;
		int i = 0;
		if (trimWhitespace)
		{
			for (; i < num && char.IsWhiteSpace(buffer[i]); i++)
			{
			}
			while (0 < num && char.IsWhiteSpace(buffer[num - 1]))
			{
				num--;
			}
		}
		return buffer.ToString(i, num - i);
	}

	internal static int GetKeyValuePair(string connectionString, int currentPosition, StringBuilder buffer, bool useOdbcRules, out string keyname, out string keyvalue)
	{
		int index = currentPosition;
		buffer.Length = 0;
		keyname = null;
		keyvalue = null;
		char c = '\0';
		ParserState parserState = ParserState.NothingYet;
		for (int length = connectionString.Length; currentPosition < length; currentPosition++)
		{
			c = connectionString[currentPosition];
			switch (parserState)
			{
			case ParserState.NothingYet:
				if (';' == c || char.IsWhiteSpace(c))
				{
					continue;
				}
				if (c == '\0')
				{
					parserState = ParserState.NullTermination;
					continue;
				}
				if (char.IsControl(c))
				{
					throw ADP.ConnectionStringSyntax(index);
				}
				index = currentPosition;
				if ('=' != c)
				{
					parserState = ParserState.Key;
					goto IL_0248;
				}
				parserState = ParserState.KeyEqual;
				continue;
			case ParserState.Key:
				if ('=' == c)
				{
					parserState = ParserState.KeyEqual;
					continue;
				}
				if (!char.IsWhiteSpace(c) && char.IsControl(c))
				{
					throw ADP.ConnectionStringSyntax(index);
				}
				goto IL_0248;
			case ParserState.KeyEqual:
				if (!useOdbcRules && '=' == c)
				{
					parserState = ParserState.Key;
					goto IL_0248;
				}
				keyname = GetKeyName(buffer);
				if (string.IsNullOrEmpty(keyname))
				{
					throw ADP.ConnectionStringSyntax(index);
				}
				buffer.Length = 0;
				parserState = ParserState.KeyEnd;
				goto case ParserState.KeyEnd;
			case ParserState.KeyEnd:
				if (char.IsWhiteSpace(c))
				{
					continue;
				}
				if (useOdbcRules)
				{
					if ('{' == c)
					{
						parserState = ParserState.BraceQuoteValue;
						goto IL_0248;
					}
				}
				else
				{
					if ('\'' == c)
					{
						parserState = ParserState.SingleQuoteValue;
						continue;
					}
					if ('"' == c)
					{
						parserState = ParserState.DoubleQuoteValue;
						continue;
					}
				}
				if (';' == c || c == '\0')
				{
					break;
				}
				if (char.IsControl(c))
				{
					throw ADP.ConnectionStringSyntax(index);
				}
				parserState = ParserState.UnquotedValue;
				goto IL_0248;
			case ParserState.UnquotedValue:
				if (!char.IsWhiteSpace(c) && (char.IsControl(c) || ';' == c))
				{
					break;
				}
				goto IL_0248;
			case ParserState.DoubleQuoteValue:
				if ('"' == c)
				{
					parserState = ParserState.DoubleQuoteValueQuote;
					continue;
				}
				if (c == '\0')
				{
					throw ADP.ConnectionStringSyntax(index);
				}
				goto IL_0248;
			case ParserState.DoubleQuoteValueQuote:
				if ('"' == c)
				{
					parserState = ParserState.DoubleQuoteValue;
					goto IL_0248;
				}
				keyvalue = GetKeyValue(buffer, trimWhitespace: false);
				parserState = ParserState.QuotedValueEnd;
				goto case ParserState.QuotedValueEnd;
			case ParserState.SingleQuoteValue:
				if ('\'' == c)
				{
					parserState = ParserState.SingleQuoteValueQuote;
					continue;
				}
				if (c == '\0')
				{
					throw ADP.ConnectionStringSyntax(index);
				}
				goto IL_0248;
			case ParserState.SingleQuoteValueQuote:
				if ('\'' == c)
				{
					parserState = ParserState.SingleQuoteValue;
					goto IL_0248;
				}
				keyvalue = GetKeyValue(buffer, trimWhitespace: false);
				parserState = ParserState.QuotedValueEnd;
				goto case ParserState.QuotedValueEnd;
			case ParserState.BraceQuoteValue:
				if ('}' == c)
				{
					parserState = ParserState.BraceQuoteValueQuote;
				}
				else if (c == '\0')
				{
					throw ADP.ConnectionStringSyntax(index);
				}
				goto IL_0248;
			case ParserState.BraceQuoteValueQuote:
				if ('}' == c)
				{
					parserState = ParserState.BraceQuoteValue;
					goto IL_0248;
				}
				keyvalue = GetKeyValue(buffer, trimWhitespace: false);
				parserState = ParserState.QuotedValueEnd;
				goto case ParserState.QuotedValueEnd;
			case ParserState.QuotedValueEnd:
				if (char.IsWhiteSpace(c))
				{
					continue;
				}
				if (';' != c)
				{
					if (c == '\0')
					{
						parserState = ParserState.NullTermination;
						continue;
					}
					throw ADP.ConnectionStringSyntax(index);
				}
				break;
			case ParserState.NullTermination:
				if (c == '\0' || char.IsWhiteSpace(c))
				{
					continue;
				}
				throw ADP.ConnectionStringSyntax(currentPosition);
			default:
				{
					throw ADP.InternalError(ADP.InternalErrorCode.InvalidParserState1);
				}
				IL_0248:
				buffer.Append(c);
				continue;
			}
			break;
		}
		switch (parserState)
		{
		case ParserState.Key:
		case ParserState.DoubleQuoteValue:
		case ParserState.SingleQuoteValue:
		case ParserState.BraceQuoteValue:
			throw ADP.ConnectionStringSyntax(index);
		case ParserState.KeyEqual:
			keyname = GetKeyName(buffer);
			if (string.IsNullOrEmpty(keyname))
			{
				throw ADP.ConnectionStringSyntax(index);
			}
			break;
		case ParserState.UnquotedValue:
		{
			keyvalue = GetKeyValue(buffer, trimWhitespace: true);
			char c2 = keyvalue[keyvalue.Length - 1];
			if (!useOdbcRules && ('\'' == c2 || '"' == c2))
			{
				throw ADP.ConnectionStringSyntax(index);
			}
			break;
		}
		case ParserState.DoubleQuoteValueQuote:
		case ParserState.SingleQuoteValueQuote:
		case ParserState.BraceQuoteValueQuote:
		case ParserState.QuotedValueEnd:
			keyvalue = GetKeyValue(buffer, trimWhitespace: false);
			break;
		default:
			throw ADP.InternalError(ADP.InternalErrorCode.InvalidParserState2);
		case ParserState.NothingYet:
		case ParserState.KeyEnd:
		case ParserState.NullTermination:
			break;
		}
		if (';' == c && currentPosition < connectionString.Length)
		{
			currentPosition++;
		}
		return currentPosition;
	}

	private static bool IsValueValidInternal(string keyvalue)
	{
		if (keyvalue != null)
		{
			return -1 == keyvalue.IndexOf('\0');
		}
		return true;
	}

	private static bool IsKeyNameValid([NotNullWhen(true)] string keyname)
	{
		if (keyname != null)
		{
			if (0 < keyname.Length && ';' != keyname[0] && !char.IsWhiteSpace(keyname[0]))
			{
				return -1 == keyname.IndexOf('\0');
			}
			return false;
		}
		return false;
	}

	private static NameValuePair ParseInternal(Dictionary<string, string> parsetable, string connectionString, bool buildChain, Dictionary<string, string> synonyms, bool firstKey)
	{
		StringBuilder buffer = new StringBuilder();
		NameValuePair nameValuePair = null;
		NameValuePair result = null;
		int num = 0;
		int length = connectionString.Length;
		while (num < length)
		{
			int num2 = num;
			num = GetKeyValuePair(connectionString, num2, buffer, firstKey, out var keyname, out var keyvalue);
			if (string.IsNullOrEmpty(keyname))
			{
				break;
			}
			string value;
			string text = ((synonyms == null) ? keyname : (synonyms.TryGetValue(keyname, out value) ? value : null));
			if (!IsKeyNameValid(text))
			{
				throw ADP.KeywordNotSupported(keyname);
			}
			if (!firstKey || !parsetable.ContainsKey(text))
			{
				parsetable[text] = keyvalue;
			}
			if (nameValuePair != null)
			{
				NameValuePair nameValuePair3 = (nameValuePair.Next = new NameValuePair(text, keyvalue, num - num2));
				nameValuePair = nameValuePair3;
			}
			else if (buildChain)
			{
				result = (nameValuePair = new NameValuePair(text, keyvalue, num - num2));
			}
		}
		return result;
	}
}
