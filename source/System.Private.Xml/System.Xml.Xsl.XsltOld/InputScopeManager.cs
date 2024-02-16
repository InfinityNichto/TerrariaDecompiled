using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class InputScopeManager
{
	private InputScope _scopeStack;

	private string _defaultNS = string.Empty;

	private readonly XPathNavigator _navigator;

	internal InputScope CurrentScope => _scopeStack;

	internal InputScope VariableScope => _scopeStack.Parent;

	public XPathNavigator Navigator => _navigator;

	public string DefaultNamespace => _defaultNS;

	public InputScopeManager(XPathNavigator navigator, InputScope rootScope)
	{
		_navigator = navigator;
		_scopeStack = rootScope;
	}

	internal InputScopeManager Clone()
	{
		InputScopeManager inputScopeManager = new InputScopeManager(_navigator, null);
		inputScopeManager._scopeStack = _scopeStack;
		inputScopeManager._defaultNS = _defaultNS;
		return inputScopeManager;
	}

	internal InputScope PushScope()
	{
		_scopeStack = new InputScope(_scopeStack);
		return _scopeStack;
	}

	internal void PopScope()
	{
		if (_scopeStack != null)
		{
			for (NamespaceDecl namespaceDecl = _scopeStack.Scopes; namespaceDecl != null; namespaceDecl = namespaceDecl.Next)
			{
				_defaultNS = namespaceDecl.PrevDefaultNsUri;
			}
			_scopeStack = _scopeStack.Parent;
		}
	}

	internal void PushNamespace(string prefix, string nspace)
	{
		_scopeStack.AddNamespace(prefix, nspace, _defaultNS);
		if (prefix == null || prefix.Length == 0)
		{
			_defaultNS = nspace;
		}
	}

	private string ResolveNonEmptyPrefix(string prefix)
	{
		if (prefix == "xml")
		{
			return "http://www.w3.org/XML/1998/namespace";
		}
		if (prefix == "xmlns")
		{
			return "http://www.w3.org/2000/xmlns/";
		}
		for (InputScope inputScope = _scopeStack; inputScope != null; inputScope = inputScope.Parent)
		{
			string text = inputScope.ResolveNonAtom(prefix);
			if (text != null)
			{
				return text;
			}
		}
		throw XsltException.Create(System.SR.Xslt_InvalidPrefix, prefix);
	}

	public string ResolveXmlNamespace(string prefix)
	{
		if (prefix.Length == 0)
		{
			return _defaultNS;
		}
		return ResolveNonEmptyPrefix(prefix);
	}

	public string ResolveXPathNamespace(string prefix)
	{
		if (prefix.Length == 0)
		{
			return string.Empty;
		}
		return ResolveNonEmptyPrefix(prefix);
	}

	internal void InsertExtensionNamespaces(string[] nsList)
	{
		for (int i = 0; i < nsList.Length; i++)
		{
			_scopeStack.InsertExtensionNamespace(nsList[i]);
		}
	}

	internal bool IsExtensionNamespace(string nspace)
	{
		for (InputScope inputScope = _scopeStack; inputScope != null; inputScope = inputScope.Parent)
		{
			if (inputScope.IsExtensionNamespace(nspace))
			{
				return true;
			}
		}
		return false;
	}

	internal void InsertExcludedNamespaces(string[] nsList)
	{
		for (int i = 0; i < nsList.Length; i++)
		{
			_scopeStack.InsertExcludedNamespace(nsList[i]);
		}
	}

	internal bool IsExcludedNamespace(string nspace)
	{
		for (InputScope inputScope = _scopeStack; inputScope != null; inputScope = inputScope.Parent)
		{
			if (inputScope.IsExcludedNamespace(nspace))
			{
				return true;
			}
		}
		return false;
	}
}
