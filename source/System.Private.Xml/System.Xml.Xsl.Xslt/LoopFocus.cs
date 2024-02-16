using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal struct LoopFocus : IFocus
{
	private readonly XPathQilFactory _f;

	private QilIterator _current;

	private QilIterator _cached;

	private QilIterator _last;

	public bool IsFocusSet => _current != null;

	public LoopFocus(XPathQilFactory f)
	{
		_f = f;
		_current = (_cached = (_last = null));
	}

	public void SetFocus(QilIterator current)
	{
		_current = current;
		_cached = (_last = null);
	}

	public QilNode GetCurrent()
	{
		return _current;
	}

	public QilNode GetPosition()
	{
		return _f.XsltConvert(_f.PositionOf(_current), XmlQueryTypeFactory.DoubleX);
	}

	public QilNode GetLast()
	{
		if (_last == null)
		{
			_last = _f.Let(_f.Double(0.0));
		}
		return _last;
	}

	public void EnsureCache()
	{
		if (_cached == null)
		{
			_cached = _f.Let(_current.Binding);
			_current.Binding = _cached;
		}
	}

	public void Sort(QilNode sortKeys)
	{
		if (sortKeys != null)
		{
			EnsureCache();
			_current = _f.For(_f.Sort(_current, sortKeys));
		}
	}

	public QilLoop ConstructLoop(QilNode body)
	{
		if (_last != null)
		{
			EnsureCache();
			_last.Binding = _f.XsltConvert(_f.Length(_cached), XmlQueryTypeFactory.DoubleX);
		}
		QilLoop qilLoop = _f.BaseFactory.Loop(_current, body);
		if (_last != null)
		{
			qilLoop = _f.BaseFactory.Loop(_last, qilLoop);
		}
		if (_cached != null)
		{
			qilLoop = _f.BaseFactory.Loop(_cached, qilLoop);
		}
		return qilLoop;
	}
}
