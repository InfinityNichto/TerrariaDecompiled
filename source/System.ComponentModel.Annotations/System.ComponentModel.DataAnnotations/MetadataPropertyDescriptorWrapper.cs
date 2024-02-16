namespace System.ComponentModel.DataAnnotations;

internal sealed class MetadataPropertyDescriptorWrapper : PropertyDescriptor
{
	private readonly PropertyDescriptor _descriptor;

	private readonly bool _isReadOnly;

	public override Type ComponentType => _descriptor.ComponentType;

	public override bool IsReadOnly
	{
		get
		{
			if (!_isReadOnly)
			{
				return _descriptor.IsReadOnly;
			}
			return true;
		}
	}

	public override Type PropertyType => _descriptor.PropertyType;

	public override bool SupportsChangeEvents => _descriptor.SupportsChangeEvents;

	public MetadataPropertyDescriptorWrapper(PropertyDescriptor descriptor, Attribute[] newAttributes)
		: base(descriptor, newAttributes)
	{
		_descriptor = descriptor;
		foreach (Attribute attribute in newAttributes)
		{
			if (attribute is ReadOnlyAttribute readOnlyAttribute)
			{
				_isReadOnly = readOnlyAttribute.IsReadOnly;
				break;
			}
		}
	}

	public override void AddValueChanged(object component, EventHandler handler)
	{
		_descriptor.AddValueChanged(component, handler);
	}

	public override bool CanResetValue(object component)
	{
		return _descriptor.CanResetValue(component);
	}

	public override object GetValue(object component)
	{
		return _descriptor.GetValue(component);
	}

	public override void RemoveValueChanged(object component, EventHandler handler)
	{
		_descriptor.RemoveValueChanged(component, handler);
	}

	public override void ResetValue(object component)
	{
		_descriptor.ResetValue(component);
	}

	public override void SetValue(object component, object value)
	{
		_descriptor.SetValue(component, value);
	}

	public override bool ShouldSerializeValue(object component)
	{
		return _descriptor.ShouldSerializeValue(component);
	}
}
