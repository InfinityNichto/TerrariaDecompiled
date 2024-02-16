using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class TheQuery
{
	internal InputScopeManager _ScopeManager;

	private readonly CompiledXpathExpr _CompiledQuery;

	internal CompiledXpathExpr CompiledQuery => _CompiledQuery;

	internal TheQuery(CompiledXpathExpr compiledQuery, InputScopeManager manager)
	{
		_CompiledQuery = compiledQuery;
		_ScopeManager = manager.Clone();
	}
}
