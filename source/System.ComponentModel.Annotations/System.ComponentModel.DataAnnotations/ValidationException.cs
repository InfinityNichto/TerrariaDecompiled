using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel.DataAnnotations;

[Serializable]
[TypeForwardedFrom("System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
public class ValidationException : Exception
{
	private ValidationResult _validationResult;

	public ValidationAttribute? ValidationAttribute { get; }

	public ValidationResult ValidationResult => _validationResult ?? (_validationResult = new ValidationResult(Message));

	public object? Value { get; }

	public ValidationException(ValidationResult validationResult, ValidationAttribute? validatingAttribute, object? value)
		: this(validationResult.ErrorMessage, validatingAttribute, value)
	{
		_validationResult = validationResult;
	}

	public ValidationException(string? errorMessage, ValidationAttribute? validatingAttribute, object? value)
		: base(errorMessage)
	{
		Value = value;
		ValidationAttribute = validatingAttribute;
	}

	public ValidationException()
	{
	}

	public ValidationException(string? message)
		: base(message)
	{
	}

	public ValidationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected ValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
