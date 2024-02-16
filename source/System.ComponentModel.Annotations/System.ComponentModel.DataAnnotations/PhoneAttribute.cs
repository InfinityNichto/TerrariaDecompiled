namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PhoneAttribute : DataTypeAttribute
{
	public PhoneAttribute()
		: base(DataType.PhoneNumber)
	{
		base.DefaultErrorMessage = System.SR.PhoneAttribute_Invalid;
	}

	public override bool IsValid(object? value)
	{
		if (value == null)
		{
			return true;
		}
		if (!(value is string text))
		{
			return false;
		}
		string potentialPhoneNumber = text.Replace("+", string.Empty).TrimEnd();
		potentialPhoneNumber = RemoveExtension(potentialPhoneNumber);
		bool flag = false;
		string text2 = potentialPhoneNumber;
		foreach (char c in text2)
		{
			if (char.IsDigit(c))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		string text3 = potentialPhoneNumber;
		foreach (char c2 in text3)
		{
			if (!char.IsDigit(c2) && !char.IsWhiteSpace(c2) && "-.()".IndexOf(c2) == -1)
			{
				return false;
			}
		}
		return true;
	}

	private static string RemoveExtension(string potentialPhoneNumber)
	{
		int num = potentialPhoneNumber.LastIndexOf("ext.", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			string potentialExtension = potentialPhoneNumber.Substring(num + "ext.".Length);
			if (MatchesExtension(potentialExtension))
			{
				return potentialPhoneNumber.Substring(0, num);
			}
		}
		num = potentialPhoneNumber.LastIndexOf("ext", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			string potentialExtension2 = potentialPhoneNumber.Substring(num + "ext".Length);
			if (MatchesExtension(potentialExtension2))
			{
				return potentialPhoneNumber.Substring(0, num);
			}
		}
		num = potentialPhoneNumber.LastIndexOf("x", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			string potentialExtension3 = potentialPhoneNumber.Substring(num + "x".Length);
			if (MatchesExtension(potentialExtension3))
			{
				return potentialPhoneNumber.Substring(0, num);
			}
		}
		return potentialPhoneNumber;
	}

	private static bool MatchesExtension(string potentialExtension)
	{
		potentialExtension = potentialExtension.TrimStart();
		if (potentialExtension.Length == 0)
		{
			return false;
		}
		string text = potentialExtension;
		foreach (char c in text)
		{
			if (!char.IsDigit(c))
			{
				return false;
			}
		}
		return true;
	}
}
