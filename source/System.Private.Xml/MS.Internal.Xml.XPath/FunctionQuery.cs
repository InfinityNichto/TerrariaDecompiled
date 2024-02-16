using System;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class FunctionQuery : ExtensionQuery
{
	private readonly IList<Query> _args;

	private IXsltContextFunction _function;

	public override XPathResultType StaticType
	{
		get
		{
			XPathResultType xPathResultType = ((_function != null) ? _function.ReturnType : XPathResultType.Any);
			if (xPathResultType == XPathResultType.Error)
			{
				xPathResultType = XPathResultType.Any;
			}
			return xPathResultType;
		}
	}

	public FunctionQuery(string prefix, string name, List<Query> args)
		: base(prefix, name)
	{
		_args = args;
	}

	private FunctionQuery(FunctionQuery other)
		: base(other)
	{
		_function = other._function;
		Query[] array = new Query[other._args.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Query.Clone(other._args[i]);
		}
		_args = array;
		_args = array;
	}

	public override void SetXsltContext(XsltContext context)
	{
		if (context == null)
		{
			throw XPathException.Create(System.SR.Xp_NoContext);
		}
		if (xsltContext == context)
		{
			return;
		}
		xsltContext = context;
		foreach (Query arg in _args)
		{
			arg.SetXsltContext(context);
		}
		XPathResultType[] array = new XPathResultType[_args.Count];
		for (int i = 0; i < _args.Count; i++)
		{
			array[i] = _args[i].StaticType;
		}
		_function = xsltContext.ResolveFunction(prefix, name, array);
		if (_function == null)
		{
			throw XPathException.Create(System.SR.Xp_UndefFunc, base.QName);
		}
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		if (xsltContext == null)
		{
			throw XPathException.Create(System.SR.Xp_NoContext);
		}
		object[] array = new object[_args.Count];
		for (int i = 0; i < _args.Count; i++)
		{
			array[i] = _args[i].Evaluate(nodeIterator);
			if (array[i] is XPathNodeIterator)
			{
				array[i] = new XPathSelectionIterator(nodeIterator.Current, _args[i]);
			}
		}
		try
		{
			return ProcessResult(_function.Invoke(xsltContext, array, nodeIterator.Current));
		}
		catch (Exception innerException)
		{
			throw XPathException.Create(System.SR.Xp_FunctionFailed, base.QName, innerException);
		}
	}

	public override XPathNavigator MatchNode(XPathNavigator navigator)
	{
		if (name != "key" && prefix.Length != 0)
		{
			throw XPathException.Create(System.SR.Xp_InvalidPattern);
		}
		Evaluate(new XPathSingletonIterator(navigator, moved: true));
		XPathNavigator xPathNavigator = null;
		while ((xPathNavigator = Advance()) != null)
		{
			if (xPathNavigator.IsSamePosition(navigator))
			{
				return xPathNavigator;
			}
		}
		return xPathNavigator;
	}

	public override XPathNodeIterator Clone()
	{
		return new FunctionQuery(this);
	}
}
