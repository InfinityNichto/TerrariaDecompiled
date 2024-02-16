using System;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal abstract class ExtensionQuery : Query
{
	protected string prefix;

	protected string name;

	protected XsltContext xsltContext;

	private ResetableIterator _queryIterator;

	public override XPathNavigator Current
	{
		get
		{
			if (_queryIterator == null)
			{
				throw XPathException.Create(System.SR.Xp_NodeSetExpected);
			}
			if (_queryIterator.CurrentPosition == 0)
			{
				Advance();
			}
			return _queryIterator.Current;
		}
	}

	public override int CurrentPosition
	{
		get
		{
			if (_queryIterator != null)
			{
				return _queryIterator.CurrentPosition;
			}
			return 0;
		}
	}

	protected string QName
	{
		get
		{
			if (prefix.Length == 0)
			{
				return name;
			}
			return prefix + ":" + name;
		}
	}

	public override int Count
	{
		get
		{
			if (_queryIterator != null)
			{
				return _queryIterator.Count;
			}
			return 1;
		}
	}

	public override XPathResultType StaticType => XPathResultType.Any;

	public ExtensionQuery(string prefix, string name)
	{
		this.prefix = prefix;
		this.name = name;
	}

	protected ExtensionQuery(ExtensionQuery other)
		: base(other)
	{
		prefix = other.prefix;
		name = other.name;
		xsltContext = other.xsltContext;
		_queryIterator = (ResetableIterator)Query.Clone(other._queryIterator);
	}

	public override void Reset()
	{
		if (_queryIterator != null)
		{
			_queryIterator.Reset();
		}
	}

	public override XPathNavigator Advance()
	{
		if (_queryIterator == null)
		{
			throw XPathException.Create(System.SR.Xp_NodeSetExpected);
		}
		if (_queryIterator.MoveNext())
		{
			return _queryIterator.Current;
		}
		return null;
	}

	protected object ProcessResult(object value)
	{
		if (value is string)
		{
			return value;
		}
		if (value is double)
		{
			return value;
		}
		if (value is bool)
		{
			return value;
		}
		if (value is XPathNavigator)
		{
			return value;
		}
		if (value is int)
		{
			return (double)(int)value;
		}
		if (value == null)
		{
			_queryIterator = XPathEmptyIterator.Instance;
			return this;
		}
		if (value is ResetableIterator resetableIterator)
		{
			_queryIterator = (ResetableIterator)resetableIterator.Clone();
			return this;
		}
		if (value is XPathNodeIterator nodeIterator)
		{
			_queryIterator = new XPathArrayIterator(nodeIterator);
			return this;
		}
		if (value is IXPathNavigable iXPathNavigable)
		{
			return iXPathNavigable.CreateNavigator();
		}
		if (value is short)
		{
			return (double)(short)value;
		}
		if (value is long)
		{
			return (double)(long)value;
		}
		if (value is uint)
		{
			return (double)(uint)value;
		}
		if (value is ushort)
		{
			return (double)(int)(ushort)value;
		}
		if (value is ulong)
		{
			return (double)(ulong)value;
		}
		if (value is float)
		{
			return (double)(float)value;
		}
		if (value is decimal)
		{
			return (double)(decimal)value;
		}
		return value.ToString();
	}
}
