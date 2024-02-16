using System;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class VariableQuery : ExtensionQuery
{
	private IXsltContextVariable _variable;

	public override XPathResultType StaticType
	{
		get
		{
			if (_variable != null)
			{
				return GetXPathType(Evaluate(null));
			}
			XPathResultType xPathResultType = ((_variable != null) ? _variable.VariableType : XPathResultType.Any);
			if (xPathResultType == XPathResultType.Error)
			{
				xPathResultType = XPathResultType.Any;
			}
			return xPathResultType;
		}
	}

	public VariableQuery(string name, string prefix)
		: base(prefix, name)
	{
	}

	private VariableQuery(VariableQuery other)
		: base(other)
	{
		_variable = other._variable;
	}

	public override void SetXsltContext(XsltContext context)
	{
		if (context == null)
		{
			throw XPathException.Create(System.SR.Xp_NoContext);
		}
		if (xsltContext != context)
		{
			xsltContext = context;
			_variable = xsltContext.ResolveVariable(prefix, name);
			if (_variable == null)
			{
				throw XPathException.Create(System.SR.Xp_UndefVar, base.QName);
			}
		}
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		if (xsltContext == null)
		{
			throw XPathException.Create(System.SR.Xp_NoContext);
		}
		return ProcessResult(_variable.Evaluate(xsltContext));
	}

	public override XPathNodeIterator Clone()
	{
		return new VariableQuery(this);
	}
}
