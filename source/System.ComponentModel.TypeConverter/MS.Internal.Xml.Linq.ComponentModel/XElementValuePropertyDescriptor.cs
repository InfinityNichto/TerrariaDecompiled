using System;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XElementValuePropertyDescriptor : XPropertyDescriptor<XElement, string>
{
	private XElement _element;

	public override bool IsReadOnly => false;

	public XElementValuePropertyDescriptor()
		: base("Value")
	{
	}

	public override object GetValue(object component)
	{
		_element = component as XElement;
		if (_element == null)
		{
			return string.Empty;
		}
		return _element.Value;
	}

	public override void SetValue(object component, object value)
	{
		_element = component as XElement;
		if (_element != null)
		{
			_element.Value = value as string;
		}
	}

	protected override void OnChanged(object sender, XObjectChangeEventArgs args)
	{
		if (_element == null)
		{
			return;
		}
		switch (args.ObjectChange)
		{
		case XObjectChange.Add:
		case XObjectChange.Remove:
			if (sender is XElement || sender is XText)
			{
				OnValueChanged(_element, EventArgs.Empty);
			}
			break;
		case XObjectChange.Value:
			if (sender is XText)
			{
				OnValueChanged(_element, EventArgs.Empty);
			}
			break;
		}
	}
}
