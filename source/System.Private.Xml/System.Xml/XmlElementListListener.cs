namespace System.Xml;

internal sealed class XmlElementListListener
{
	private WeakReference<XmlElementList> _elemList;

	private readonly XmlDocument _doc;

	private readonly XmlNodeChangedEventHandler _nodeChangeHandler;

	internal XmlElementListListener(XmlDocument doc, XmlElementList elemList)
	{
		_doc = doc;
		_elemList = new WeakReference<XmlElementList>(elemList);
		_nodeChangeHandler = OnListChanged;
		doc.NodeInserted += _nodeChangeHandler;
		doc.NodeRemoved += _nodeChangeHandler;
	}

	private void OnListChanged(object sender, XmlNodeChangedEventArgs args)
	{
		lock (this)
		{
			if (_elemList != null)
			{
				if (_elemList.TryGetTarget(out var target))
				{
					target.ConcurrencyCheck(args);
					return;
				}
				_doc.NodeInserted -= _nodeChangeHandler;
				_doc.NodeRemoved -= _nodeChangeHandler;
				_elemList = null;
			}
		}
	}

	internal void Unregister()
	{
		lock (this)
		{
			if (_elemList != null)
			{
				_doc.NodeInserted -= _nodeChangeHandler;
				_doc.NodeRemoved -= _nodeChangeHandler;
				_elemList = null;
			}
		}
	}
}
