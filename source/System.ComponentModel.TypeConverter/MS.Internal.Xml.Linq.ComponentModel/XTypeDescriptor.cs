using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal sealed class XTypeDescriptor<T> : CustomTypeDescriptor
{
	public XTypeDescriptor(ICustomTypeDescriptor parent)
		: base(parent)
	{
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public override PropertyDescriptorCollection GetProperties()
	{
		return GetProperties(null);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
	{
		PropertyDescriptorCollection propertyDescriptorCollection = new PropertyDescriptorCollection(null);
		if (attributes == null)
		{
			if (typeof(T) == typeof(XElement))
			{
				propertyDescriptorCollection.Add(new XElementAttributePropertyDescriptor());
				propertyDescriptorCollection.Add(new XElementDescendantsPropertyDescriptor());
				propertyDescriptorCollection.Add(new XElementElementPropertyDescriptor());
				propertyDescriptorCollection.Add(new XElementElementsPropertyDescriptor());
				propertyDescriptorCollection.Add(new XElementValuePropertyDescriptor());
				propertyDescriptorCollection.Add(new XElementXmlPropertyDescriptor());
			}
			else if (typeof(T) == typeof(XAttribute))
			{
				propertyDescriptorCollection.Add(new XAttributeValuePropertyDescriptor());
			}
		}
		foreach (PropertyDescriptor property in base.GetProperties(attributes))
		{
			propertyDescriptorCollection.Add(property);
		}
		return propertyDescriptorCollection;
	}
}
