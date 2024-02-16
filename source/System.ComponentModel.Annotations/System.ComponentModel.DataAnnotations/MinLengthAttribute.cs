using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class MinLengthAttribute : ValidationAttribute
{
	public int Length { get; }

	[RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
	public MinLengthAttribute(int length)
		: base(System.SR.MinLengthAttribute_ValidationError)
	{
		Length = length;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
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
		return num >= Length;
	}

	public override string FormatErrorMessage(string name)
	{
		return string.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, Length);
	}

	private void EnsureLegalLengths()
	{
		if (Length < 0)
		{
			throw new InvalidOperationException(System.SR.MinLengthAttribute_InvalidMinLength);
		}
	}
}
