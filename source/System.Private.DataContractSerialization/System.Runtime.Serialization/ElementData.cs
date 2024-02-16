using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

internal sealed class ElementData
{
	public string localName;

	public string ns;

	public string prefix;

	public int attributeCount;

	public AttributeData[] attributes;

	public IDataNode dataNode;

	public int childElementIndex;

	public void AddAttribute(string prefix, string ns, string name, string value)
	{
		GrowAttributesIfNeeded();
		AttributeData attributeData = attributes[attributeCount];
		if (attributeData == null)
		{
			attributeData = (attributes[attributeCount] = new AttributeData());
		}
		attributeData.prefix = prefix;
		attributeData.ns = ns;
		attributeData.localName = name;
		attributeData.value = value;
		attributeCount++;
	}

	[MemberNotNull("attributes")]
	private void GrowAttributesIfNeeded()
	{
		if (attributes == null)
		{
			attributes = new AttributeData[4];
		}
		else if (attributes.Length == attributeCount)
		{
			AttributeData[] destinationArray = new AttributeData[attributes.Length * 2];
			Array.Copy(attributes, destinationArray, attributes.Length);
			attributes = destinationArray;
		}
	}
}
