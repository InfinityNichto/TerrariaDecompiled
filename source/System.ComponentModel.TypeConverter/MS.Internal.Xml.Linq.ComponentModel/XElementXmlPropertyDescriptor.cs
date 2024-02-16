using System;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XElementXmlPropertyDescriptor : XPropertyDescriptor<XElement, string>
{
	private XElement _element;

	public XElementXmlPropertyDescriptor()
		: base("Xml")
	{
	}

	public override object GetValue(object component)
	{
		_element = component as XElement;
		if (_element == null)
		{
			return string.Empty;
		}
		return _element.ToString(SaveOptions.DisableFormatting);
	}

	protected override void OnChanged(object sender, XObjectChangeEventArgs args)
	{
		if (_element != null)
		{
			OnValueChanged(_element, EventArgs.Empty);
		}
	}
}
