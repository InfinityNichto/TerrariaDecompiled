namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class RequiredAttribute : ValidationAttribute
{
	public bool AllowEmptyStrings { get; set; }

	public RequiredAttribute()
		: base(() => System.SR.RequiredAttribute_ValidationError)
	{
	}

	public override bool IsValid(object? value)
	{
		if (value == null)
		{
			return false;
		}
		if (!AllowEmptyStrings && value is string value2)
		{
			return !string.IsNullOrWhiteSpace(value2);
		}
		return true;
	}
}
