using System;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XElementAttributePropertyDescriptor : XPropertyDescriptor<XElement, object>
{
	private XDeferredSingleton<XAttribute> _value;

	private XAttribute _changeState;

	public XElementAttributePropertyDescriptor()
		: base("Attribute")
	{
	}

	public override object GetValue(object component)
	{
		return _value = new XDeferredSingleton<XAttribute>((XElement e, XName n) => e.Attribute(n), component as XElement, null);
	}

	protected override void OnChanged(object sender, XObjectChangeEventArgs args)
	{
		if (_value == null)
		{
			return;
		}
		switch (args.ObjectChange)
		{
		case XObjectChange.Add:
			if (sender is XAttribute xAttribute2 && _value.element == xAttribute2.Parent && _value.name == xAttribute2.Name)
			{
				OnValueChanged(_value.element, EventArgs.Empty);
			}
			break;
		case XObjectChange.Remove:
			if (sender is XAttribute xAttribute && _changeState == xAttribute)
			{
				_changeState = null;
				OnValueChanged(_value.element, EventArgs.Empty);
			}
			break;
		}
	}

	protected override void OnChanging(object sender, XObjectChangeEventArgs args)
	{
		if (_value != null)
		{
			XObjectChange objectChange = args.ObjectChange;
			if (objectChange == XObjectChange.Remove)
			{
				XAttribute xAttribute = sender as XAttribute;
				_changeState = ((xAttribute != null && _value.element == xAttribute.Parent && _value.name == xAttribute.Name) ? xAttribute : null);
			}
		}
	}
}
