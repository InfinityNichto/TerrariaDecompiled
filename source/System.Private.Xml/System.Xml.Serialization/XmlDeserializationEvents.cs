namespace System.Xml.Serialization;

public struct XmlDeserializationEvents
{
	private XmlNodeEventHandler _onUnknownNode;

	private XmlAttributeEventHandler _onUnknownAttribute;

	private XmlElementEventHandler _onUnknownElement;

	private UnreferencedObjectEventHandler _onUnreferencedObject;

	internal object sender;

	public XmlNodeEventHandler? OnUnknownNode
	{
		get
		{
			return _onUnknownNode;
		}
		set
		{
			_onUnknownNode = value;
		}
	}

	public XmlAttributeEventHandler? OnUnknownAttribute
	{
		get
		{
			return _onUnknownAttribute;
		}
		set
		{
			_onUnknownAttribute = value;
		}
	}

	public XmlElementEventHandler? OnUnknownElement
	{
		get
		{
			return _onUnknownElement;
		}
		set
		{
			_onUnknownElement = value;
		}
	}

	public UnreferencedObjectEventHandler? OnUnreferencedObject
	{
		get
		{
			return _onUnreferencedObject;
		}
		set
		{
			_onUnreferencedObject = value;
		}
	}
}
