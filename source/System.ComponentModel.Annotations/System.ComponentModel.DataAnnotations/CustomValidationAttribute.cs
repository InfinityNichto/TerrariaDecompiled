using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class CustomValidationAttribute : ValidationAttribute
{
	private readonly Lazy<string> _malformedErrorMessage;

	private bool _isSingleArgumentMethod;

	private string _lastMessage;

	private MethodInfo _methodInfo;

	private Type _firstParameterType;

	private Tuple<string, Type> _typeId;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public Type ValidatorType { get; }

	public override object TypeId
	{
		get
		{
			if (_typeId == null)
			{
				_typeId = new Tuple<string, Type>(Method, ValidatorType);
			}
			return _typeId;
		}
	}

	public string Method { get; }

	public override bool RequiresValidationContext
	{
		get
		{
			ThrowIfAttributeNotWellFormed();
			return !_isSingleArgumentMethod;
		}
	}

	public CustomValidationAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type validatorType, string method)
		: base(() => System.SR.CustomValidationAttribute_ValidationError)
	{
		ValidatorType = validatorType;
		Method = method;
		_malformedErrorMessage = new Lazy<string>(CheckAttributeWellFormed);
	}

	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		ThrowIfAttributeNotWellFormed();
		MethodInfo methodInfo = _methodInfo;
		if (!TryConvertValue(value, out var convertedValue))
		{
			return new ValidationResult(System.SR.Format(System.SR.CustomValidationAttribute_Type_Conversion_Failed, (value != null) ? value.GetType().ToString() : "null", _firstParameterType, ValidatorType, Method));
		}
		try
		{
			object[] parameters = ((!_isSingleArgumentMethod) ? new object[2] { convertedValue, validationContext } : new object[1] { convertedValue });
			ValidationResult validationResult = (ValidationResult)methodInfo.Invoke(null, parameters);
			_lastMessage = null;
			if (validationResult != null)
			{
				_lastMessage = validationResult.ErrorMessage;
			}
			return validationResult;
		}
		catch (TargetInvocationException ex)
		{
			throw ex.InnerException;
		}
	}

	public override string FormatErrorMessage(string name)
	{
		ThrowIfAttributeNotWellFormed();
		if (!string.IsNullOrEmpty(_lastMessage))
		{
			return string.Format(CultureInfo.CurrentCulture, _lastMessage, name);
		}
		return base.FormatErrorMessage(name);
	}

	private string CheckAttributeWellFormed()
	{
		return ValidateValidatorTypeParameter() ?? ValidateMethodParameter();
	}

	private string ValidateValidatorTypeParameter()
	{
		if (ValidatorType == null)
		{
			return System.SR.CustomValidationAttribute_ValidatorType_Required;
		}
		if (!ValidatorType.IsVisible)
		{
			return System.SR.Format(System.SR.CustomValidationAttribute_Type_Must_Be_Public, ValidatorType.Name);
		}
		return null;
	}

	private string ValidateMethodParameter()
	{
		if (string.IsNullOrEmpty(Method))
		{
			return System.SR.CustomValidationAttribute_Method_Required;
		}
		MethodInfo methodInfo = ValidatorType.GetMethods(BindingFlags.Static | BindingFlags.Public).SingleOrDefault((MethodInfo m) => string.Equals(m.Name, Method, StringComparison.Ordinal));
		if (methodInfo == null)
		{
			return System.SR.Format(System.SR.CustomValidationAttribute_Method_Not_Found, Method, ValidatorType.Name);
		}
		if (!typeof(ValidationResult).IsAssignableFrom(methodInfo.ReturnType))
		{
			return System.SR.Format(System.SR.CustomValidationAttribute_Method_Must_Return_ValidationResult, Method, ValidatorType.Name);
		}
		ParameterInfo[] parameters = methodInfo.GetParameters();
		if (parameters.Length == 0 || parameters[0].ParameterType.IsByRef)
		{
			return System.SR.Format(System.SR.CustomValidationAttribute_Method_Signature, Method, ValidatorType.Name);
		}
		_isSingleArgumentMethod = parameters.Length == 1;
		if (!_isSingleArgumentMethod && (parameters.Length != 2 || parameters[1].ParameterType != typeof(ValidationContext)))
		{
			return System.SR.Format(System.SR.CustomValidationAttribute_Method_Signature, Method, ValidatorType.Name);
		}
		_methodInfo = methodInfo;
		_firstParameterType = parameters[0].ParameterType;
		return null;
	}

	private void ThrowIfAttributeNotWellFormed()
	{
		string value = _malformedErrorMessage.Value;
		if (value != null)
		{
			throw new InvalidOperationException(value);
		}
	}

	private bool TryConvertValue(object value, out object convertedValue)
	{
		convertedValue = null;
		Type firstParameterType = _firstParameterType;
		if (value == null)
		{
			if (firstParameterType.IsValueType && (!firstParameterType.IsGenericType || firstParameterType.GetGenericTypeDefinition() != typeof(Nullable<>)))
			{
				return false;
			}
			return true;
		}
		if (firstParameterType.IsInstanceOfType(value))
		{
			convertedValue = value;
			return true;
		}
		try
		{
			convertedValue = Convert.ChangeType(value, firstParameterType, CultureInfo.CurrentCulture);
			return true;
		}
		catch (FormatException)
		{
			return false;
		}
		catch (InvalidCastException)
		{
			return false;
		}
		catch (NotSupportedException)
		{
			return false;
		}
	}
}
