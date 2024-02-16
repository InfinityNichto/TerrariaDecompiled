using System;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal class CompiledXpathExpr : XPathExpression
{
	private sealed class UndefinedXsltContext : XsltContext
	{
		private readonly IXmlNamespaceResolver _nsResolver;

		public override string DefaultNamespace => string.Empty;

		public override bool Whitespace => false;

		public UndefinedXsltContext(IXmlNamespaceResolver nsResolver)
			: base(dummy: false)
		{
			_nsResolver = nsResolver;
		}

		public override string LookupNamespace(string prefix)
		{
			if (prefix.Length == 0)
			{
				return string.Empty;
			}
			string text = _nsResolver.LookupNamespace(prefix);
			if (text == null)
			{
				throw XPathException.Create(System.SR.XmlUndefinedAlias, prefix);
			}
			return text;
		}

		public override IXsltContextVariable ResolveVariable(string prefix, string name)
		{
			throw XPathException.Create(System.SR.Xp_UndefinedXsltContext);
		}

		public override IXsltContextFunction ResolveFunction(string prefix, string name, XPathResultType[] ArgTypes)
		{
			throw XPathException.Create(System.SR.Xp_UndefinedXsltContext);
		}

		public override bool PreserveWhitespace(XPathNavigator node)
		{
			return false;
		}

		public override int CompareDocument(string baseUri, string nextbaseUri)
		{
			return string.CompareOrdinal(baseUri, nextbaseUri);
		}
	}

	private Query _query;

	private readonly string _expr;

	private bool _needContext;

	internal Query QueryTree
	{
		get
		{
			if (_needContext)
			{
				throw XPathException.Create(System.SR.Xp_NoContext);
			}
			return _query;
		}
	}

	public override string Expression => _expr;

	public override XPathResultType ReturnType => _query.StaticType;

	internal CompiledXpathExpr(Query query, string expression, bool needContext)
	{
		_query = query;
		_expr = expression;
		_needContext = needContext;
	}

	public virtual void CheckErrors()
	{
	}

	public override void AddSort(object expr, IComparer comparer)
	{
		Query evalQuery;
		if (expr is string query)
		{
			evalQuery = new QueryBuilder().Build(query, out _needContext);
		}
		else
		{
			if (!(expr is CompiledXpathExpr compiledXpathExpr))
			{
				throw XPathException.Create(System.SR.Xp_BadQueryObject);
			}
			evalQuery = compiledXpathExpr.QueryTree;
		}
		SortQuery sortQuery = _query as SortQuery;
		if (sortQuery == null)
		{
			sortQuery = (SortQuery)(_query = new SortQuery(_query));
		}
		sortQuery.AddSort(evalQuery, comparer);
	}

	public override void AddSort(object expr, XmlSortOrder order, XmlCaseOrder caseOrder, string lang, XmlDataType dataType)
	{
		AddSort(expr, new XPathComparerHelper(order, caseOrder, lang, dataType));
	}

	public override XPathExpression Clone()
	{
		return new CompiledXpathExpr(Query.Clone(_query), _expr, _needContext);
	}

	public override void SetContext(XmlNamespaceManager nsManager)
	{
		SetContext((IXmlNamespaceResolver?)nsManager);
	}

	public override void SetContext(IXmlNamespaceResolver nsResolver)
	{
		XsltContext xsltContext = nsResolver as XsltContext;
		if (xsltContext == null)
		{
			if (nsResolver == null)
			{
				nsResolver = new XmlNamespaceManager(new NameTable());
			}
			xsltContext = new UndefinedXsltContext(nsResolver);
		}
		_query.SetXsltContext(xsltContext);
		_needContext = false;
	}
}
