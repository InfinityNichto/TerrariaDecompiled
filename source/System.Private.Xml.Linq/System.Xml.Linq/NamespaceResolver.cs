namespace System.Xml.Linq;

internal struct NamespaceResolver
{
	private sealed class NamespaceDeclaration
	{
		public string prefix;

		public XNamespace ns;

		public int scope;

		public NamespaceDeclaration prev;
	}

	private int _scope;

	private NamespaceDeclaration _declaration;

	private NamespaceDeclaration _rover;

	public void PushScope()
	{
		_scope++;
	}

	public void PopScope()
	{
		NamespaceDeclaration namespaceDeclaration = _declaration;
		if (namespaceDeclaration != null)
		{
			do
			{
				namespaceDeclaration = namespaceDeclaration.prev;
				if (namespaceDeclaration.scope != _scope)
				{
					break;
				}
				if (namespaceDeclaration == _declaration)
				{
					_declaration = null;
				}
				else
				{
					_declaration.prev = namespaceDeclaration.prev;
				}
				_rover = null;
			}
			while (namespaceDeclaration != _declaration && _declaration != null);
		}
		_scope--;
	}

	public void Add(string prefix, XNamespace ns)
	{
		NamespaceDeclaration namespaceDeclaration = new NamespaceDeclaration();
		namespaceDeclaration.prefix = prefix;
		namespaceDeclaration.ns = ns;
		namespaceDeclaration.scope = _scope;
		if (_declaration == null)
		{
			_declaration = namespaceDeclaration;
		}
		else
		{
			namespaceDeclaration.prev = _declaration.prev;
		}
		_declaration.prev = namespaceDeclaration;
		_rover = null;
	}

	public void AddFirst(string prefix, XNamespace ns)
	{
		NamespaceDeclaration namespaceDeclaration = new NamespaceDeclaration();
		namespaceDeclaration.prefix = prefix;
		namespaceDeclaration.ns = ns;
		namespaceDeclaration.scope = _scope;
		if (_declaration == null)
		{
			namespaceDeclaration.prev = namespaceDeclaration;
		}
		else
		{
			namespaceDeclaration.prev = _declaration.prev;
			_declaration.prev = namespaceDeclaration;
		}
		_declaration = namespaceDeclaration;
		_rover = null;
	}

	public string GetPrefixOfNamespace(XNamespace ns, bool allowDefaultNamespace)
	{
		if (_rover != null && _rover.ns == ns && (allowDefaultNamespace || _rover.prefix.Length > 0))
		{
			return _rover.prefix;
		}
		NamespaceDeclaration namespaceDeclaration = _declaration;
		if (namespaceDeclaration != null)
		{
			do
			{
				namespaceDeclaration = namespaceDeclaration.prev;
				if (!(namespaceDeclaration.ns == ns))
				{
					continue;
				}
				NamespaceDeclaration prev = _declaration.prev;
				while (prev != namespaceDeclaration && prev.prefix != namespaceDeclaration.prefix)
				{
					prev = prev.prev;
				}
				if (prev == namespaceDeclaration)
				{
					if (allowDefaultNamespace)
					{
						_rover = namespaceDeclaration;
						return namespaceDeclaration.prefix;
					}
					if (namespaceDeclaration.prefix.Length > 0)
					{
						return namespaceDeclaration.prefix;
					}
				}
			}
			while (namespaceDeclaration != _declaration);
		}
		return null;
	}
}
