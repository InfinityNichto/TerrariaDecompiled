using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class DisplayAttribute : Attribute
{
	private readonly LocalizableString _description = new LocalizableString("Description");

	private readonly LocalizableString _groupName = new LocalizableString("GroupName");

	private readonly LocalizableString _name = new LocalizableString("Name");

	private readonly LocalizableString _prompt = new LocalizableString("Prompt");

	private readonly LocalizableString _shortName = new LocalizableString("ShortName");

	private bool? _autoGenerateField;

	private bool? _autoGenerateFilter;

	private int? _order;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	private Type _resourceType;

	public string? ShortName
	{
		get
		{
			return _shortName.Value;
		}
		set
		{
			_shortName.Value = value;
		}
	}

	public string? Name
	{
		get
		{
			return _name.Value;
		}
		set
		{
			_name.Value = value;
		}
	}

	public string? Description
	{
		get
		{
			return _description.Value;
		}
		set
		{
			_description.Value = value;
		}
	}

	public string? Prompt
	{
		get
		{
			return _prompt.Value;
		}
		set
		{
			_prompt.Value = value;
		}
	}

	public string? GroupName
	{
		get
		{
			return _groupName.Value;
		}
		set
		{
			_groupName.Value = value;
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public Type? ResourceType
	{
		get
		{
			return _resourceType;
		}
		set
		{
			if (_resourceType != value)
			{
				_resourceType = value;
				_shortName.ResourceType = value;
				_name.ResourceType = value;
				_description.ResourceType = value;
				_prompt.ResourceType = value;
				_groupName.ResourceType = value;
			}
		}
	}

	public bool AutoGenerateField
	{
		get
		{
			if (!_autoGenerateField.HasValue)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.DisplayAttribute_PropertyNotSet, "AutoGenerateField", "GetAutoGenerateField"));
			}
			return _autoGenerateField.GetValueOrDefault();
		}
		set
		{
			_autoGenerateField = value;
		}
	}

	public bool AutoGenerateFilter
	{
		get
		{
			if (!_autoGenerateFilter.HasValue)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.DisplayAttribute_PropertyNotSet, "AutoGenerateFilter", "GetAutoGenerateFilter"));
			}
			return _autoGenerateFilter.GetValueOrDefault();
		}
		set
		{
			_autoGenerateFilter = value;
		}
	}

	public int Order
	{
		get
		{
			if (!_order.HasValue)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.DisplayAttribute_PropertyNotSet, "Order", "GetOrder"));
			}
			return _order.GetValueOrDefault();
		}
		set
		{
			_order = value;
		}
	}

	public string? GetShortName()
	{
		return _shortName.GetLocalizableValue() ?? GetName();
	}

	public string? GetName()
	{
		return _name.GetLocalizableValue();
	}

	public string? GetDescription()
	{
		return _description.GetLocalizableValue();
	}

	public string? GetPrompt()
	{
		return _prompt.GetLocalizableValue();
	}

	public string? GetGroupName()
	{
		return _groupName.GetLocalizableValue();
	}

	public bool? GetAutoGenerateField()
	{
		return _autoGenerateField;
	}

	public bool? GetAutoGenerateFilter()
	{
		return _autoGenerateFilter;
	}

	public int? GetOrder()
	{
		return _order;
	}
}
