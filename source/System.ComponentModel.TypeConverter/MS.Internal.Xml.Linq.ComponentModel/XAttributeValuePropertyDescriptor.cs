using System;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XAttributeValuePropertyDescriptor : XPropertyDescriptor<XAttribute, string>
{
	private XAttribute _attribute;

	public override bool IsReadOnly => false;

	public XAttributeValuePropertyDescriptor()
		: base("Value")
	{
	}

	public override object GetValue(object component)
	{
		_attribute = component as XAttribute;
		if (_attribute == null)
		{
			return string.Empty;
		}
		return _attribute.Value;
	}

	public override void SetValue(object component, object value)
	{
		_attribute = component as XAttribute;
		if (_attribute != null)
		{
			_attribute.Value = value as string;
		}
	}

	protected override void OnChanged(object sender, XObjectChangeEventArgs args)
	{
		if (_attribute != null && args.ObjectChange == XObjectChange.Value)
		{
			OnValueChanged(_attribute, EventArgs.Empty);
		}
	}
}
