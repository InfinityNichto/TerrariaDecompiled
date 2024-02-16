using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Xml.Serialization;

public class CodeIdentifier
{
	[Obsolete("This class should never get constructed as it contains only static methods.")]
	public CodeIdentifier()
	{
	}

	public static string MakePascal(string identifier)
	{
		identifier = MakeValid(identifier);
		if (identifier.Length <= 2)
		{
			return identifier.ToUpperInvariant();
		}
		if (char.IsLower(identifier[0]))
		{
			return string.Create(identifier.Length, identifier, delegate(Span<char> buffer, string identifier)
			{
				identifier.CopyTo(buffer);
				buffer[0] = char.ToUpperInvariant(buffer[0]);
			});
		}
		return identifier;
	}

	public static string MakeCamel(string identifier)
	{
		identifier = MakeValid(identifier);
		if (identifier.Length <= 2)
		{
			return identifier.ToLowerInvariant();
		}
		if (char.IsUpper(identifier[0]))
		{
			return string.Create(identifier.Length, identifier, delegate(Span<char> buffer, string identifier)
			{
				identifier.CopyTo(buffer);
				buffer[0] = char.ToLowerInvariant(buffer[0]);
			});
		}
		return identifier;
	}

	public static string MakeValid(string identifier)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < identifier.Length; i++)
		{
			if (stringBuilder.Length >= 511)
			{
				break;
			}
			char c = identifier[i];
			if (IsValid(c))
			{
				if (stringBuilder.Length == 0 && !IsValidStart(c))
				{
					stringBuilder.Append("Item");
				}
				stringBuilder.Append(c);
			}
		}
		if (stringBuilder.Length == 0)
		{
			return "Item";
		}
		return stringBuilder.ToString();
	}

	internal static string MakeValidInternal(string identifier)
	{
		if (identifier.Length > 30)
		{
			return "Item";
		}
		return MakeValid(identifier);
	}

	private static bool IsValidStart(char c)
	{
		if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber)
		{
			return false;
		}
		return true;
	}

	private static bool IsValid(char c)
	{
		switch (CharUnicodeInfo.GetUnicodeCategory(c))
		{
		case UnicodeCategory.EnclosingMark:
		case UnicodeCategory.LetterNumber:
		case UnicodeCategory.OtherNumber:
		case UnicodeCategory.SpaceSeparator:
		case UnicodeCategory.LineSeparator:
		case UnicodeCategory.ParagraphSeparator:
		case UnicodeCategory.Control:
		case UnicodeCategory.Format:
		case UnicodeCategory.Surrogate:
		case UnicodeCategory.PrivateUse:
		case UnicodeCategory.DashPunctuation:
		case UnicodeCategory.OpenPunctuation:
		case UnicodeCategory.ClosePunctuation:
		case UnicodeCategory.InitialQuotePunctuation:
		case UnicodeCategory.FinalQuotePunctuation:
		case UnicodeCategory.OtherPunctuation:
		case UnicodeCategory.MathSymbol:
		case UnicodeCategory.CurrencySymbol:
		case UnicodeCategory.ModifierSymbol:
		case UnicodeCategory.OtherSymbol:
		case UnicodeCategory.OtherNotAssigned:
			return false;
		default:
			return false;
		case UnicodeCategory.UppercaseLetter:
		case UnicodeCategory.LowercaseLetter:
		case UnicodeCategory.TitlecaseLetter:
		case UnicodeCategory.ModifierLetter:
		case UnicodeCategory.OtherLetter:
		case UnicodeCategory.NonSpacingMark:
		case UnicodeCategory.SpacingCombiningMark:
		case UnicodeCategory.DecimalDigitNumber:
		case UnicodeCategory.ConnectorPunctuation:
			return true;
		}
	}

	internal static void CheckValidIdentifier([NotNull] string ident)
	{
		if (!CSharpHelpers.IsValidLanguageIndependentIdentifier(ident))
		{
			throw new ArgumentException(System.SR.Format(System.SR.XmlInvalidIdentifier, ident), "ident");
		}
	}

	internal static string GetCSharpName(string name)
	{
		return EscapeKeywords(name.Replace('+', '.'));
	}

	private static int GetCSharpName(Type t, Type[] parameters, int index, StringBuilder sb)
	{
		if (t.DeclaringType != null && t.DeclaringType != t)
		{
			index = GetCSharpName(t.DeclaringType, parameters, index, sb);
			sb.Append('.');
		}
		string name = t.Name;
		int num = name.IndexOf('`');
		if (num < 0)
		{
			num = name.IndexOf('!');
		}
		if (num > 0)
		{
			EscapeKeywords(name.Substring(0, num), sb);
			sb.Append('<');
			int num2 = int.Parse(name.AsSpan(num + 1), NumberStyles.Integer, CultureInfo.InvariantCulture) + index;
			while (index < num2)
			{
				sb.Append(GetCSharpName(parameters[index]));
				if (index < num2 - 1)
				{
					sb.Append(',');
				}
				index++;
			}
			sb.Append('>');
		}
		else
		{
			EscapeKeywords(name, sb);
		}
		return index;
	}

	internal static string GetCSharpName(Type t)
	{
		int num = 0;
		while (t.IsArray)
		{
			t = t.GetElementType();
			num++;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("global::");
		string @namespace = t.Namespace;
		if (@namespace != null && @namespace.Length > 0)
		{
			string[] array = @namespace.Split('.');
			for (int i = 0; i < array.Length; i++)
			{
				EscapeKeywords(array[i], stringBuilder);
				stringBuilder.Append('.');
			}
		}
		Type[] parameters = ((t.IsGenericType || t.ContainsGenericParameters) ? t.GetGenericArguments() : Type.EmptyTypes);
		GetCSharpName(t, parameters, 0, stringBuilder);
		for (int j = 0; j < num; j++)
		{
			stringBuilder.Append("[]");
		}
		return stringBuilder.ToString();
	}

	private static void EscapeKeywords(string identifier, StringBuilder sb)
	{
		if (identifier != null && identifier.Length != 0)
		{
			int num = 0;
			while (identifier.EndsWith("[]", StringComparison.Ordinal))
			{
				num++;
				identifier = identifier.Substring(0, identifier.Length - 2);
			}
			if (identifier.Length > 0)
			{
				CheckValidIdentifier(identifier);
				identifier = CSharpHelpers.CreateEscapedIdentifier(identifier);
				sb.Append(identifier);
			}
			for (int i = 0; i < num; i++)
			{
				sb.Append("[]");
			}
		}
	}

	[return: NotNullIfNotNull("identifier")]
	private static string EscapeKeywords(string identifier)
	{
		if (identifier == null || identifier.Length == 0)
		{
			return identifier;
		}
		string[] array = identifier.Split('.', ',', '<', '>');
		StringBuilder stringBuilder = new StringBuilder();
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (num >= 0)
			{
				stringBuilder.Append(identifier[num]);
			}
			num++;
			num += array[i].Length;
			string identifier2 = array[i].Trim();
			EscapeKeywords(identifier2, stringBuilder);
		}
		if (stringBuilder.Length == identifier.Length)
		{
			return identifier;
		}
		return stringBuilder.ToString();
	}
}
