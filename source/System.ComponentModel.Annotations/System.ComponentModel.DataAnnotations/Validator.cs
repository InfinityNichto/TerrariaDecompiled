using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.ComponentModel.DataAnnotations;

public static class Validator
{
	private sealed class ValidationError
	{
		private readonly object _value;

		private readonly ValidationAttribute _validationAttribute;

		internal ValidationResult ValidationResult { get; }

		internal ValidationError(ValidationAttribute validationAttribute, object value, ValidationResult validationResult)
		{
			_validationAttribute = validationAttribute;
			ValidationResult = validationResult;
			_value = value;
		}

		internal void ThrowValidationException()
		{
			throw new ValidationException(ValidationResult, _validationAttribute, _value);
		}
	}

	private static readonly ValidationAttributeStore _store = ValidationAttributeStore.Instance;

	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	public static bool TryValidateProperty(object? value, ValidationContext validationContext, ICollection<ValidationResult>? validationResults)
	{
		Type propertyType = _store.GetPropertyType(validationContext);
		string memberName = validationContext.MemberName;
		EnsureValidPropertyType(memberName, propertyType, value);
		bool result = true;
		bool breakOnFirstError = validationResults == null;
		IEnumerable<ValidationAttribute> propertyValidationAttributes = _store.GetPropertyValidationAttributes(validationContext);
		foreach (ValidationError validationError in GetValidationErrors(value, validationContext, propertyValidationAttributes, breakOnFirstError))
		{
			result = false;
			validationResults?.Add(validationError.ValidationResult);
		}
		return result;
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult>? validationResults)
	{
		return TryValidateObject(instance, validationContext, validationResults, validateAllProperties: false);
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult>? validationResults, bool validateAllProperties)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (validationContext != null && instance != validationContext.ObjectInstance)
		{
			throw new ArgumentException(System.SR.Validator_InstanceMustMatchValidationContextInstance, "instance");
		}
		bool result = true;
		bool breakOnFirstError = validationResults == null;
		foreach (ValidationError objectValidationError in GetObjectValidationErrors(instance, validationContext, validateAllProperties, breakOnFirstError))
		{
			result = false;
			validationResults?.Add(objectValidationError.ValidationResult);
		}
		return result;
	}

	public static bool TryValidateValue(object value, ValidationContext validationContext, ICollection<ValidationResult>? validationResults, IEnumerable<ValidationAttribute> validationAttributes)
	{
		ArgumentNullException.ThrowIfNull(validationAttributes, "validationAttributes");
		bool result = true;
		bool breakOnFirstError = validationResults == null;
		foreach (ValidationError validationError in GetValidationErrors(value, validationContext, validationAttributes, breakOnFirstError))
		{
			result = false;
			validationResults?.Add(validationError.ValidationResult);
		}
		return result;
	}

	[RequiresUnreferencedCode("The Type of validationContext.ObjectType cannot be statically discovered.")]
	public static void ValidateProperty(object? value, ValidationContext validationContext)
	{
		Type propertyType = _store.GetPropertyType(validationContext);
		EnsureValidPropertyType(validationContext.MemberName, propertyType, value);
		IEnumerable<ValidationAttribute> propertyValidationAttributes = _store.GetPropertyValidationAttributes(validationContext);
		List<ValidationError> validationErrors = GetValidationErrors(value, validationContext, propertyValidationAttributes, breakOnFirstError: false);
		if (validationErrors.Count > 0)
		{
			validationErrors[0].ThrowValidationException();
		}
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public static void ValidateObject(object instance, ValidationContext validationContext)
	{
		ValidateObject(instance, validationContext, validateAllProperties: false);
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	public static void ValidateObject(object instance, ValidationContext validationContext, bool validateAllProperties)
	{
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		if (validationContext == null)
		{
			throw new ArgumentNullException("validationContext");
		}
		if (instance != validationContext.ObjectInstance)
		{
			throw new ArgumentException(System.SR.Validator_InstanceMustMatchValidationContextInstance, "instance");
		}
		List<ValidationError> objectValidationErrors = GetObjectValidationErrors(instance, validationContext, validateAllProperties, breakOnFirstError: false);
		if (objectValidationErrors.Count > 0)
		{
			objectValidationErrors[0].ThrowValidationException();
		}
	}

	public static void ValidateValue(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes)
	{
		ArgumentNullException.ThrowIfNull(validationContext, "validationContext");
		ArgumentNullException.ThrowIfNull(validationAttributes, "validationAttributes");
		List<ValidationError> validationErrors = GetValidationErrors(value, validationContext, validationAttributes, breakOnFirstError: false);
		if (validationErrors.Count > 0)
		{
			validationErrors[0].ThrowValidationException();
		}
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	private static ValidationContext CreateValidationContext(object instance, ValidationContext validationContext)
	{
		return new ValidationContext(instance, validationContext, validationContext.Items);
	}

	private static bool CanBeAssigned(Type destinationType, object value)
	{
		if (value == null)
		{
			if (destinationType.IsValueType)
			{
				if (destinationType.IsGenericType)
				{
					return destinationType.GetGenericTypeDefinition() == typeof(Nullable<>);
				}
				return false;
			}
			return true;
		}
		return destinationType.IsInstanceOfType(value);
	}

	private static void EnsureValidPropertyType(string propertyName, Type propertyType, object value)
	{
		if (!CanBeAssigned(propertyType, value))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Validator_Property_Value_Wrong_Type, propertyName, propertyType), "value");
		}
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	private static List<ValidationError> GetObjectValidationErrors(object instance, ValidationContext validationContext, bool validateAllProperties, bool breakOnFirstError)
	{
		if (validationContext == null)
		{
			throw new ArgumentNullException("validationContext");
		}
		List<ValidationError> list = new List<ValidationError>();
		list.AddRange(GetObjectPropertyValidationErrors(instance, validationContext, validateAllProperties, breakOnFirstError));
		if (list.Count > 0)
		{
			return list;
		}
		IEnumerable<ValidationAttribute> typeValidationAttributes = _store.GetTypeValidationAttributes(validationContext);
		list.AddRange(GetValidationErrors(instance, validationContext, typeValidationAttributes, breakOnFirstError));
		if (list.Count > 0)
		{
			return list;
		}
		if (instance is IValidatableObject validatableObject)
		{
			IEnumerable<ValidationResult> enumerable = validatableObject.Validate(validationContext);
			if (enumerable != null)
			{
				foreach (ValidationResult item in enumerable)
				{
					if (item != ValidationResult.Success)
					{
						list.Add(new ValidationError(null, instance, item));
					}
				}
			}
		}
		return list;
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	private static IEnumerable<ValidationError> GetObjectPropertyValidationErrors(object instance, ValidationContext validationContext, bool validateAllProperties, bool breakOnFirstError)
	{
		ICollection<KeyValuePair<ValidationContext, object>> propertyValues = GetPropertyValues(instance, validationContext);
		List<ValidationError> list = new List<ValidationError>();
		foreach (KeyValuePair<ValidationContext, object> item in propertyValues)
		{
			IEnumerable<ValidationAttribute> propertyValidationAttributes = _store.GetPropertyValidationAttributes(item.Key);
			if (validateAllProperties)
			{
				list.AddRange(GetValidationErrors(item.Value, item.Key, propertyValidationAttributes, breakOnFirstError));
			}
			else
			{
				foreach (ValidationAttribute item2 in propertyValidationAttributes)
				{
					if (item2 is RequiredAttribute requiredAttribute)
					{
						ValidationResult validationResult = requiredAttribute.GetValidationResult(item.Value, item.Key);
						if (validationResult != ValidationResult.Success)
						{
							list.Add(new ValidationError(requiredAttribute, item.Value, validationResult));
						}
						break;
					}
				}
			}
			if (breakOnFirstError && list.Count > 0)
			{
				break;
			}
		}
		return list;
	}

	[RequiresUnreferencedCode("The Type of instance cannot be statically discovered.")]
	private static ICollection<KeyValuePair<ValidationContext, object>> GetPropertyValues(object instance, ValidationContext validationContext)
	{
		PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(instance);
		List<KeyValuePair<ValidationContext, object>> list = new List<KeyValuePair<ValidationContext, object>>(properties.Count);
		foreach (PropertyDescriptor item in properties)
		{
			ValidationContext validationContext2 = CreateValidationContext(instance, validationContext);
			validationContext2.MemberName = item.Name;
			if (_store.GetPropertyValidationAttributes(validationContext2).Any())
			{
				list.Add(new KeyValuePair<ValidationContext, object>(validationContext2, item.GetValue(instance)));
			}
		}
		return list;
	}

	private static List<ValidationError> GetValidationErrors(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> attributes, bool breakOnFirstError)
	{
		if (validationContext == null)
		{
			throw new ArgumentNullException("validationContext");
		}
		List<ValidationError> list = new List<ValidationError>();
		RequiredAttribute requiredAttribute = null;
		ValidationError validationError;
		foreach (ValidationAttribute attribute in attributes)
		{
			requiredAttribute = attribute as RequiredAttribute;
			if (requiredAttribute != null)
			{
				if (!TryValidate(value, validationContext, requiredAttribute, out validationError))
				{
					list.Add(validationError);
					return list;
				}
				break;
			}
		}
		foreach (ValidationAttribute attribute2 in attributes)
		{
			if (attribute2 != requiredAttribute && !TryValidate(value, validationContext, attribute2, out validationError))
			{
				list.Add(validationError);
				if (breakOnFirstError)
				{
					break;
				}
			}
		}
		return list;
	}

	private static bool TryValidate(object value, ValidationContext validationContext, ValidationAttribute attribute, [NotNullWhen(false)] out ValidationError validationError)
	{
		ValidationResult validationResult = attribute.GetValidationResult(value, validationContext);
		if (validationResult != ValidationResult.Success)
		{
			validationError = new ValidationError(attribute, value, validationResult);
			return false;
		}
		validationError = null;
		return true;
	}
}
