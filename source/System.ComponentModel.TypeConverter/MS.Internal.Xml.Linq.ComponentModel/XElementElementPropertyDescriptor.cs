using System;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XElementElementPropertyDescriptor : XPropertyDescriptor<XElement, object>
{
	private XDeferredSingleton<XElement> _value;

	private XElement _changeState;

	public XElementElementPropertyDescriptor()
		: base("Element")
	{
	}

	public override object GetValue(object component)
	{
		return _value = new XDeferredSingleton<XElement>((XElement e, XName n) => e.Element(n), component as XElement, null);
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
			if (sender is XElement xElement2 && _value.element == xElement2.Parent && _value.name == xElement2.Name && _value.element.Element(_value.name) == xElement2)
			{
				OnValueChanged(_value.element, EventArgs.Empty);
			}
			break;
		case XObjectChange.Remove:
			if (sender is XElement xElement3 && _changeState == xElement3)
			{
				_changeState = null;
				OnValueChanged(_value.element, EventArgs.Empty);
			}
			break;
		case XObjectChange.Name:
			if (sender is XElement xElement)
			{
				if (_value.element == xElement.Parent && _value.name == xElement.Name && _value.element.Element(_value.name) == xElement)
				{
					OnValueChanged(_value.element, EventArgs.Empty);
				}
				else if (_changeState == xElement)
				{
					_changeState = null;
					OnValueChanged(_value.element, EventArgs.Empty);
				}
			}
			break;
		}
	}

	protected override void OnChanging(object sender, XObjectChangeEventArgs args)
	{
		if (_value != null)
		{
			XObjectChange objectChange = args.ObjectChange;
			if ((uint)(objectChange - 1) <= 1u)
			{
				XElement xElement = sender as XElement;
				_changeState = ((xElement != null && _value.element == xElement.Parent && _value.name == xElement.Name && _value.element.Element(_value.name) == xElement) ? xElement : null);
			}
		}
	}
}
