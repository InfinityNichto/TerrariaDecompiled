using System.Globalization;
using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class RegularExpressionAttribute : ValidationAttribute
{
	public int MatchTimeoutInMilliseconds { get; set; }

	public string Pattern { get; }

	private Regex? Regex { get; set; }

	public RegularExpressionAttribute(string pattern)
		: base(() => System.SR.RegexAttribute_ValidationError)
	{
		Pattern = pattern;
		MatchTimeoutInMilliseconds = 2000;
	}

	public override bool IsValid(object? value)
	{
		SetupRegex();
		string text = Convert.ToString(value, CultureInfo.CurrentCulture);
		if (string.IsNullOrEmpty(text))
		{
			return true;
		}
		Match match = Regex.Match(text);
		if (match.Success && match.Index == 0)
		{
			return match.Length == text.Length;
		}
		return false;
	}

	public override string FormatErrorMessage(string name)
	{
		SetupRegex();
		return string.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, Pattern);
	}

	private void SetupRegex()
	{
		if (Regex == null)
		{
			if (string.IsNullOrEmpty(Pattern))
			{
				throw new InvalidOperationException(System.SR.RegularExpressionAttribute_Empty_Pattern);
			}
			Regex = ((MatchTimeoutInMilliseconds == -1) ? new Regex(Pattern) : new Regex(Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(MatchTimeoutInMilliseconds)));
		}
	}
}
