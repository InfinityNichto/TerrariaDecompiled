using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class MaxLengthAttribute : ValidationAttribute
{
	public int Length { get; }

	private static string DefaultErrorMessageString => System.SR.MaxLengthAttribute_ValidationError;

	[RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
	public MaxLengthAttribute(int length)
		: base(() => DefaultErrorMessageString)
	{
		Length = length;
	}

	[RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
	public MaxLengthAttribute()
		: base(() => DefaultErrorMessageString)
	{
		Length = -1;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctors are marked with RequiresUnreferencedCode.")]
	public override bool IsValid(object? value)
	{
		EnsureLegalLengths();
		if (value == null)
		{
			return true;
		}
		int num;
		if (value is string text)
		{
			num = text.Length;
		}
		else
		{
			if (!CountPropertyHelper.TryGetCount(value, out var count))
			{
				throw new InvalidCastException(System.SR.Format(System.SR.LengthAttribute_InvalidValueType, value.GetType()));
			}
			num = count;
		}
		if (-1 != Length)
		{
			return num <= Length;
		}
		return true;
	}

	public override string FormatErrorMessage(string name)
	{
		return string.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, Length);
	}

	private void EnsureLegalLengths()
	{
		if (Length == 0 || Length < -1)
		{
			throw new InvalidOperationException(System.SR.MaxLengthAttribute_InvalidMaxLength);
		}
	}
}
