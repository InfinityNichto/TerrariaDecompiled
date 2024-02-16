namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class EmailAddressAttribute : DataTypeAttribute
{
	public EmailAddressAttribute()
		: base(DataType.EmailAddress)
	{
		base.DefaultErrorMessage = System.SR.EmailAddressAttribute_Invalid;
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
		int num = text.IndexOf('@');
		if (num > 0 && num != text.Length - 1)
		{
			return num == text.LastIndexOf('@');
		}
		return false;
	}
}
