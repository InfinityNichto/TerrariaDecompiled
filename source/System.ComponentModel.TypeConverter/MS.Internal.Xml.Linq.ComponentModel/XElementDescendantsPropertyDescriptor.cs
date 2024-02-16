using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XElementDescendantsPropertyDescriptor : XPropertyDescriptor<XElement, IEnumerable<XElement>>
{
	private XDeferredAxis<XElement> _value;

	private XName _changeState;

	public XElementDescendantsPropertyDescriptor()
		: base("Descendants")
	{
	}

	public override object GetValue(object component)
	{
		return _value = new XDeferredAxis<XElement>((XElement e, XName n) => (!(n != null)) ? e.Descendants() : e.Descendants(n), component as XElement, null);
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
		case XObjectChange.Remove:
			if (sender is XElement xElement2 && (_value.name == xElement2.Name || _value.name == null))
			{
				OnValueChanged(_value.element, EventArgs.Empty);
			}
			break;
		case XObjectChange.Name:
			if (sender is XElement xElement && _value.element != xElement && _value.name != null && (_value.name == xElement.Name || _value.name == _changeState))
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
			if (objectChange == XObjectChange.Name)
			{
				_changeState = (sender as XElement)?.Name;
			}
		}
	}
}
