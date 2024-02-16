namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class CreditCardAttribute : DataTypeAttribute
{
	public CreditCardAttribute()
		: base(DataType.CreditCard)
	{
		base.DefaultErrorMessage = System.SR.CreditCardAttribute_Invalid;
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
		string text2 = text.Replace("-", string.Empty);
		text2 = text2.Replace(" ", string.Empty);
		int num = 0;
		bool flag = false;
		for (int num2 = text2.Length - 1; num2 >= 0; num2--)
		{
			char c = text2[num2];
			if (c < '0' || c > '9')
			{
				return false;
			}
			int num3 = (c - 48) * ((!flag) ? 1 : 2);
			flag = !flag;
			while (num3 > 0)
			{
				num += num3 % 10;
				num3 /= 10;
			}
		}
		return num % 10 == 0;
	}
}
