using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class DisplayFormatAttribute : Attribute
{
	private readonly LocalizableString _nullDisplayText = new LocalizableString("NullDisplayText");

	public string? DataFormatString { get; set; }

	public string? NullDisplayText
	{
		get
		{
			return _nullDisplayText.Value;
		}
		set
		{
			_nullDisplayText.Value = value;
		}
	}

	public bool ConvertEmptyStringToNull { get; set; }

	public bool ApplyFormatInEditMode { get; set; }

	public bool HtmlEncode { get; set; }

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public Type? NullDisplayTextResourceType
	{
		get
		{
			return _nullDisplayText.ResourceType;
		}
		set
		{
			_nullDisplayText.ResourceType = value;
		}
	}

	public DisplayFormatAttribute()
	{
		ConvertEmptyStringToNull = true;
		HtmlEncode = true;
	}

	public string? GetNullDisplayText()
	{
		return _nullDisplayText.GetLocalizableValue();
	}
}
