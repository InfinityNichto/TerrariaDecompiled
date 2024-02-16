namespace System.Xml;

internal interface IXmlDataVirtualNode
{
	bool IsOnNode(XmlNode nodeToCheck);

	bool IsInUse();

	void OnFoliated(XmlNode foliatedNode);
}
