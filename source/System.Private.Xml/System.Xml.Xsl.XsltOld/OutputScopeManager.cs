using System.Globalization;

namespace System.Xml.Xsl.XsltOld;

internal sealed class OutputScopeManager
{
	private readonly HWStack _elementScopesStack;

	private string _defaultNS;

	private readonly OutKeywords _atoms;

	private readonly XmlNameTable _nameTable;

	private int _prefixIndex;

	internal string DefaultNamespace => _defaultNS;

	internal OutputScope CurrentElementScope => (OutputScope)_elementScopesStack.Peek();

	internal XmlSpace XmlSpace => CurrentElementScope.Space;

	internal string XmlLang => CurrentElementScope.Lang;

	internal OutputScopeManager(XmlNameTable nameTable, OutKeywords atoms)
	{
		_elementScopesStack = new HWStack(10);
		_nameTable = nameTable;
		_atoms = atoms;
		_defaultNS = _atoms.Empty;
		OutputScope outputScope = (OutputScope)_elementScopesStack.Push();
		if (outputScope == null)
		{
			outputScope = new OutputScope();
			_elementScopesStack.AddToTop(outputScope);
		}
		outputScope.Init(string.Empty, string.Empty, string.Empty, XmlSpace.None, string.Empty, mixed: false);
	}

	internal void PushNamespace(string prefix, string nspace)
	{
		CurrentElementScope.AddNamespace(prefix, nspace, _defaultNS);
		if (prefix == null || prefix.Length == 0)
		{
			_defaultNS = nspace;
		}
	}

	internal void PushScope(string name, string nspace, string prefix)
	{
		OutputScope currentElementScope = CurrentElementScope;
		OutputScope outputScope = (OutputScope)_elementScopesStack.Push();
		if (outputScope == null)
		{
			outputScope = new OutputScope();
			_elementScopesStack.AddToTop(outputScope);
		}
		outputScope.Init(name, nspace, prefix, currentElementScope.Space, currentElementScope.Lang, currentElementScope.Mixed);
	}

	internal void PopScope()
	{
		OutputScope outputScope = (OutputScope)_elementScopesStack.Pop();
		for (NamespaceDecl namespaceDecl = outputScope.Scopes; namespaceDecl != null; namespaceDecl = namespaceDecl.Next)
		{
			_defaultNS = namespaceDecl.PrevDefaultNsUri;
		}
	}

	internal string ResolveNamespace(string prefix)
	{
		bool thisScope;
		return ResolveNamespace(prefix, out thisScope);
	}

	internal string ResolveNamespace(string prefix, out bool thisScope)
	{
		thisScope = true;
		if (prefix == null || prefix.Length == 0)
		{
			return _defaultNS;
		}
		if (Ref.Equal(prefix, _atoms.Xml))
		{
			return _atoms.XmlNamespace;
		}
		if (Ref.Equal(prefix, _atoms.Xmlns))
		{
			return _atoms.XmlnsNamespace;
		}
		for (int num = _elementScopesStack.Length - 1; num >= 0; num--)
		{
			OutputScope outputScope = (OutputScope)_elementScopesStack[num];
			string text = outputScope.ResolveAtom(prefix);
			if (text != null)
			{
				thisScope = num == _elementScopesStack.Length - 1;
				return text;
			}
		}
		return null;
	}

	internal bool FindPrefix(string nspace, out string prefix)
	{
		int num = _elementScopesStack.Length - 1;
		while (0 <= num)
		{
			OutputScope outputScope = (OutputScope)_elementScopesStack[num];
			string prefix2 = null;
			if (outputScope.FindPrefix(nspace, out prefix2))
			{
				string text = ResolveNamespace(prefix2);
				if (text == null || !Ref.Equal(text, nspace))
				{
					break;
				}
				prefix = prefix2;
				return true;
			}
			num--;
		}
		prefix = null;
		return false;
	}

	internal string GeneratePrefix(string format)
	{
		string array;
		do
		{
			array = string.Format(CultureInfo.InvariantCulture, format, _prefixIndex++);
		}
		while (_nameTable.Get(array) != null);
		return _nameTable.Add(array);
	}
}
