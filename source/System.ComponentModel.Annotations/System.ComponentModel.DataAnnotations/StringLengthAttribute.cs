using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class StringLengthAttribute : ValidationAttribute
{
	public int MaximumLength { get; }

	public int MinimumLength { get; set; }

	public StringLengthAttribute(int maximumLength)
		: base(() => System.SR.StringLengthAttribute_ValidationError)
	{
		MaximumLength = maximumLength;
	}

	public override bool IsValid(object? value)
	{
		EnsureLegalLengths();
		if (value == null)
		{
			return true;
		}
		int length = ((string)value).Length;
		if (length >= MinimumLength)
		{
			return length <= MaximumLength;
		}
		return false;
	}

	public override string FormatErrorMessage(string name)
	{
		EnsureLegalLengths();
		string format = ((MinimumLength != 0 && !base.CustomErrorMessageSet) ? System.SR.StringLengthAttribute_ValidationErrorIncludingMinimum : base.ErrorMessageString);
		return string.Format(CultureInfo.CurrentCulture, format, name, MaximumLength, MinimumLength);
	}

	private void EnsureLegalLengths()
	{
		if (MaximumLength < 0)
		{
			throw new InvalidOperationException(System.SR.StringLengthAttribute_InvalidMaxLength);
		}
		if (MaximumLength < MinimumLength)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.RangeAttribute_MinGreaterThanMax, MaximumLength, MinimumLength));
		}
	}
}
