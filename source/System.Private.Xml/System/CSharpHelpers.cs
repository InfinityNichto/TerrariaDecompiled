using System.Collections.Generic;
using System.Globalization;

namespace System;

internal abstract class CSharpHelpers
{
	private static readonly HashSet<string> s_fixedStringLookup = new HashSet<string>
	{
		"as", "do", "if", "in", "is", "for", "int", "new", "out", "ref",
		"try", "base", "bool", "byte", "case", "char", "else", "enum", "goto", "lock",
		"long", "null", "this", "true", "uint", "void", "break", "catch", "class", "const",
		"event", "false", "fixed", "float", "sbyte", "short", "throw", "ulong", "using", "where",
		"while", "yield", "double", "extern", "object", "params", "public", "return", "sealed", "sizeof",
		"static", "string", "struct", "switch", "typeof", "unsafe", "ushort", "checked", "decimal", "default",
		"finally", "foreach", "partial", "private", "virtual", "abstract", "continue", "delegate", "explicit", "implicit",
		"internal", "operator", "override", "readonly", "volatile", "__arglist", "__makeref", "__reftype", "interface", "namespace",
		"protected", "unchecked", "__refvalue", "stackalloc"
	};

	public static string CreateEscapedIdentifier(string name)
	{
		if (IsKeyword(name) || IsPrefixTwoUnderscore(name))
		{
			return "@" + name;
		}
		return name;
	}

	public static bool IsValidLanguageIndependentIdentifier(string value)
	{
		return IsValidTypeNameOrIdentifier(value, isTypeName: false);
	}

	internal static bool IsKeyword(string value)
	{
		return s_fixedStringLookup.Contains(value);
	}

	internal static bool IsPrefixTwoUnderscore(string value)
	{
		if (value.Length < 3)
		{
			return false;
		}
		if (value[0] == '_' && value[1] == '_')
		{
			return value[2] != '_';
		}
		return false;
	}

	internal static bool IsValidTypeNameOrIdentifier(string value, bool isTypeName)
	{
		bool nextMustBeStartChar = true;
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		foreach (char c in value)
		{
			switch (CharUnicodeInfo.GetUnicodeCategory(c))
			{
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.LetterNumber:
				nextMustBeStartChar = false;
				break;
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.DecimalDigitNumber:
			case UnicodeCategory.ConnectorPunctuation:
				if (nextMustBeStartChar && c != '_')
				{
					return false;
				}
				nextMustBeStartChar = false;
				break;
			default:
				if (!isTypeName || !IsSpecialTypeChar(c, ref nextMustBeStartChar))
				{
					return false;
				}
				break;
			}
		}
		return true;
	}

	internal static bool IsSpecialTypeChar(char ch, ref bool nextMustBeStartChar)
	{
		switch (ch)
		{
		case '$':
		case '&':
		case '*':
		case '+':
		case ',':
		case '-':
		case '.':
		case ':':
		case '<':
		case '>':
		case '[':
		case ']':
			nextMustBeStartChar = true;
			return true;
		case '`':
			return true;
		default:
			return false;
		}
	}
}
