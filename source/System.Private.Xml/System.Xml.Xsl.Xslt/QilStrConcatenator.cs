using System.Text;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.XPath;

namespace System.Xml.Xsl.Xslt;

internal sealed class QilStrConcatenator
{
	private readonly XPathQilFactory _f;

	private readonly StringBuilder _builder;

	private QilList _concat;

	private bool _inUse;

	public QilStrConcatenator(XPathQilFactory f)
	{
		_f = f;
		_builder = new StringBuilder();
	}

	public void Reset()
	{
		_inUse = true;
		_builder.Length = 0;
		_concat = null;
	}

	private void FlushBuilder()
	{
		if (_concat == null)
		{
			_concat = _f.BaseFactory.Sequence();
		}
		if (_builder.Length != 0)
		{
			_concat.Add(_f.String(_builder.ToString()));
			_builder.Length = 0;
		}
	}

	public void Append(string value)
	{
		_builder.Append(value);
	}

	public void Append(char value)
	{
		_builder.Append(value);
	}

	public void Append(QilNode value)
	{
		if (value != null)
		{
			if (value.NodeType == QilNodeType.LiteralString)
			{
				_builder.Append((string)(QilLiteral)value);
				return;
			}
			FlushBuilder();
			_concat.Add(value);
		}
	}

	public QilNode ToQil()
	{
		_inUse = false;
		if (_concat == null)
		{
			return _f.String(_builder.ToString());
		}
		FlushBuilder();
		return _f.StrConcat(_concat);
	}
}
