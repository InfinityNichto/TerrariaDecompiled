using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations;

public abstract class ValidationAttribute : Attribute
{
	private string _errorMessage;

	private Func<string> _errorMessageResourceAccessor;

	private string _errorMessageResourceName;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	private Type _errorMessageResourceType;

	private volatile bool _hasBaseIsValid;

	private string _defaultErrorMessage;

	internal string? DefaultErrorMessage
	{
		set
		{
			_defaultErrorMessage = value;
			_errorMessageResourceAccessor = null;
			CustomErrorMessageSet = true;
		}
	}

	protected string ErrorMessageString
	{
		get
		{
			SetupResourceAccessor();
			return _errorMessageResourceAccessor();
		}
	}

	internal bool CustomErrorMessageSet { get; private set; }

	public virtual bool RequiresValidationContext => false;

	public string? ErrorMessage
	{
		get
		{
			return _errorMessage ?? _defaultErrorMessage;
		}
		set
		{
			_errorMessage = value;
			_errorMessageResourceAccessor = null;
			CustomErrorMessageSet = true;
			if (value == null)
			{
				_defaultErrorMessage = null;
			}
		}
	}

	public string? ErrorMessageResourceName
	{
		get
		{
			return _errorMessageResourceName;
		}
		set
		{
			_errorMessageResourceName = value;
			_errorMessageResourceAccessor = null;
			CustomErrorMessageSet = true;
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
	public Type? ErrorMessageResourceType
	{
		get
		{
			return _errorMessageResourceType;
		}
		set
		{
			_errorMessageResourceType = value;
			_errorMessageResourceAccessor = null;
			CustomErrorMessageSet = true;
		}
	}

	protected ValidationAttribute()
		: this(() => System.SR.ValidationAttribute_ValidationError)
	{
	}

	protected ValidationAttribute(string errorMessage)
	{
		string errorMessage2 = errorMessage;
		this._002Ector(() => errorMessage2);
	}

	protected ValidationAttribute(Func<string> errorMessageAccessor)
	{
		_errorMessageResourceAccessor = errorMessageAccessor;
	}

	private void SetupResourceAccessor()
	{
		if (_errorMessageResourceAccessor != null)
		{
			return;
		}
		string localErrorMessage = ErrorMessage;
		bool flag = !string.IsNullOrEmpty(_errorMessageResourceName);
		bool flag2 = !string.IsNullOrEmpty(_errorMessage);
		bool flag3 = _errorMessageResourceType != null;
		bool flag4 = !string.IsNullOrEmpty(_defaultErrorMessage);
		if ((flag && flag2) || !(flag || flag2 || flag4))
		{
			throw new InvalidOperationException(System.SR.ValidationAttribute_Cannot_Set_ErrorMessage_And_Resource);
		}
		if (flag3 != flag)
		{
			throw new InvalidOperationException(System.SR.ValidationAttribute_NeedBothResourceTypeAndResourceName);
		}
		if (flag)
		{
			SetResourceAccessorByPropertyLookup();
			return;
		}
		_errorMessageResourceAccessor = () => localErrorMessage;
	}

	private void SetResourceAccessorByPropertyLookup()
	{
		PropertyInfo property = _errorMessageResourceType.GetProperty(_errorMessageResourceName, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (property != null)
		{
			MethodInfo getMethod = property.GetMethod;
			if (getMethod == null || (!getMethod.IsAssembly && !getMethod.IsPublic))
			{
				property = null;
			}
		}
		if (property == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.ValidationAttribute_ResourceTypeDoesNotHaveProperty, _errorMessageResourceType.FullName, _errorMessageResourceName));
		}
		if (property.PropertyType != typeof(string))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.ValidationAttribute_ResourcePropertyNotStringType, property.Name, _errorMessageResourceType.FullName));
		}
		_errorMessageResourceAccessor = () => (string)property.GetValue(null, null);
	}

	public virtual string FormatErrorMessage(string name)
	{
		return string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name);
	}

	public virtual bool IsValid(object? value)
	{
		if (!_hasBaseIsValid)
		{
			_hasBaseIsValid = true;
		}
		return IsValid(value, null) == ValidationResult.Success;
	}

	protected virtual ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (_hasBaseIsValid)
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.ValidationAttribute_IsValid_NotImplemented);
		}
		ValidationResult result = ValidationResult.Success;
		if (!IsValid(value))
		{
			string memberName = validationContext.MemberName;
			string[] memberNames = ((memberName == null) ? null : new string[1] { memberName });
			result = new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
		}
		return result;
	}

	public ValidationResult? GetValidationResult(object? value, ValidationContext validationContext)
	{
		if (validationContext == null)
		{
			throw new ArgumentNullException("validationContext");
		}
		ValidationResult validationResult = IsValid(value, validationContext);
		if (validationResult != null && string.IsNullOrEmpty(validationResult.ErrorMessage))
		{
			string errorMessage = FormatErrorMessage(validationContext.DisplayName);
			validationResult = new ValidationResult(errorMessage, validationResult.MemberNames);
		}
		return validationResult;
	}

	public void Validate(object? value, string name)
	{
		if (!IsValid(value))
		{
			throw new ValidationException(FormatErrorMessage(name), this, value);
		}
	}

	public void Validate(object? value, ValidationContext validationContext)
	{
		if (validationContext == null)
		{
			throw new ArgumentNullException("validationContext");
		}
		ValidationResult validationResult = GetValidationResult(value, validationContext);
		if (validationResult != null)
		{
			throw new ValidationException(validationResult, this, value);
		}
	}
}
