using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XElementElementsPropertyDescriptor : XPropertyDescriptor<XElement, IEnumerable<XElement>>
{
	private XDeferredAxis<XElement> _value;

	private object _changeState;

	public XElementElementsPropertyDescriptor()
		: base("Elements")
	{
	}

	public override object GetValue(object component)
	{
		return _value = new XDeferredAxis<XElement>((XElement e, XName n) => (!(n != null)) ? e.Elements() : e.Elements(n), component as XElement, null);
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
			if (sender is XElement xElement3 && _value.element == xElement3.Parent && (_value.name == xElement3.Name || _value.name == null))
			{
				OnValueChanged(_value.element, EventArgs.Empty);
			}
			break;
		case XObjectChange.Remove:
			if (sender is XElement xElement2 && _value.element == _changeState as XContainer && (_value.name == xElement2.Name || _value.name == null))
			{
				_changeState = null;
				OnValueChanged(_value.element, EventArgs.Empty);
			}
			break;
		case XObjectChange.Name:
			if (sender is XElement xElement && _value.element == xElement.Parent && _value.name != null && (_value.name == xElement.Name || _value.name == _changeState as XName))
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
			switch (args.ObjectChange)
			{
			case XObjectChange.Remove:
				_changeState = (sender as XElement)?.Parent;
				break;
			case XObjectChange.Name:
				_changeState = (sender as XElement)?.Name;
				break;
			}
		}
	}
}
