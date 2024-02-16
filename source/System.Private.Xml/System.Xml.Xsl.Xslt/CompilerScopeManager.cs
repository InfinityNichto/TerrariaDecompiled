using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class CompilerScopeManager<V>
{
	public enum ScopeFlags
	{
		BackwardCompatibility = 1,
		ForwardCompatibility = 2,
		CanHaveApplyImports = 4,
		NsDecl = 16,
		NsExcl = 32,
		Variable = 64,
		CompatibilityFlags = 3,
		InheritedFlags = 7,
		ExclusiveFlags = 112
	}

	public struct ScopeRecord
	{
		public int scopeCount;

		public ScopeFlags flags;

		public string ncName;

		public string nsUri;

		[AllowNull]
		public V value;

		public bool IsVariable => (flags & ScopeFlags.Variable) != 0;

		public bool IsNamespace => (flags & ScopeFlags.NsDecl) != 0;
	}

	internal struct NamespaceEnumerator
	{
		private readonly CompilerScopeManager<V> _scope;

		private readonly int _lastRecord;

		private int _currentRecord;

		public ScopeRecord Current => _scope._records[_currentRecord];

		public NamespaceEnumerator(CompilerScopeManager<V> scope)
		{
			_scope = scope;
			_lastRecord = scope._lastRecord;
			_currentRecord = _lastRecord + 1;
		}

		public bool MoveNext()
		{
			while (0 < --_currentRecord)
			{
				if (_scope._records[_currentRecord].IsNamespace && _scope.LookupNamespace(_scope._records[_currentRecord].ncName, _lastRecord, _currentRecord + 1) == null)
				{
					return true;
				}
			}
			return false;
		}
	}

	private ScopeRecord[] _records = new ScopeRecord[32];

	private int _lastRecord;

	private int _lastScopes;

	public bool ForwardCompatibility
	{
		get
		{
			return (_records[_lastRecord].flags & ScopeFlags.ForwardCompatibility) != 0;
		}
		set
		{
			SetFlag(ScopeFlags.ForwardCompatibility, value);
		}
	}

	public bool BackwardCompatibility
	{
		get
		{
			return (_records[_lastRecord].flags & ScopeFlags.BackwardCompatibility) != 0;
		}
		set
		{
			SetFlag(ScopeFlags.BackwardCompatibility, value);
		}
	}

	public bool CanHaveApplyImports
	{
		get
		{
			return (_records[_lastRecord].flags & ScopeFlags.CanHaveApplyImports) != 0;
		}
		set
		{
			SetFlag(ScopeFlags.CanHaveApplyImports, value);
		}
	}

	public CompilerScopeManager()
	{
		_records[0].flags = ScopeFlags.NsDecl;
		_records[0].ncName = "xml";
		_records[0].nsUri = "http://www.w3.org/XML/1998/namespace";
	}

	public CompilerScopeManager(KeywordsTable atoms)
	{
		_records[0].flags = ScopeFlags.NsDecl;
		_records[0].ncName = atoms.Xml;
		_records[0].nsUri = atoms.UriXml;
	}

	public void EnterScope()
	{
		_lastScopes++;
	}

	public void ExitScope()
	{
		if (0 < _lastScopes)
		{
			_lastScopes--;
			return;
		}
		while (_records[--_lastRecord].scopeCount == 0)
		{
		}
		_lastScopes = _records[_lastRecord].scopeCount;
		_lastScopes--;
	}

	public bool EnterScope([NotNullWhen(true)] NsDecl nsDecl)
	{
		_lastScopes++;
		bool result = false;
		bool flag = false;
		while (nsDecl != null)
		{
			if (nsDecl.NsUri == null)
			{
				flag = true;
			}
			else if (nsDecl.Prefix == null)
			{
				AddExNamespace(nsDecl.NsUri);
			}
			else
			{
				result = true;
				AddNsDeclaration(nsDecl.Prefix, nsDecl.NsUri);
			}
			nsDecl = nsDecl.Prev;
		}
		if (flag)
		{
			AddExNamespace(null);
		}
		return result;
	}

	private void AddRecord()
	{
		_records[_lastRecord].scopeCount = _lastScopes;
		if (++_lastRecord == _records.Length)
		{
			ScopeRecord[] array = new ScopeRecord[_lastRecord * 2];
			Array.Copy(_records, array, _lastRecord);
			_records = array;
		}
		_lastScopes = 0;
	}

	private void AddRecord(ScopeFlags flag, string ncName, string uri, [AllowNull] V value)
	{
		ScopeFlags scopeFlags = _records[_lastRecord].flags;
		if (_lastScopes != 0 || (scopeFlags & ScopeFlags.ExclusiveFlags) != 0)
		{
			AddRecord();
			scopeFlags &= ScopeFlags.InheritedFlags;
		}
		_records[_lastRecord].flags = scopeFlags | flag;
		_records[_lastRecord].ncName = ncName;
		_records[_lastRecord].nsUri = uri;
		_records[_lastRecord].value = value;
	}

	private void SetFlag(ScopeFlags flag, bool value)
	{
		ScopeFlags scopeFlags = _records[_lastRecord].flags;
		if ((scopeFlags & flag) != 0 == value)
		{
			return;
		}
		if (_lastScopes != 0)
		{
			AddRecord();
			scopeFlags &= ScopeFlags.InheritedFlags;
		}
		if (flag == ScopeFlags.CanHaveApplyImports)
		{
			scopeFlags ^= flag;
		}
		else
		{
			scopeFlags &= (ScopeFlags)(-4);
			if (value)
			{
				scopeFlags |= flag;
			}
		}
		_records[_lastRecord].flags = scopeFlags;
	}

	public void AddVariable(QilName varName, V value)
	{
		AddRecord(ScopeFlags.Variable, varName.LocalName, varName.NamespaceUri, value);
	}

	private string LookupNamespace(string prefix, int from, int to)
	{
		int num = from;
		while (to <= num)
		{
			string prefix2;
			string nsUri;
			ScopeFlags name = GetName(ref _records[num], out prefix2, out nsUri);
			if ((name & ScopeFlags.NsDecl) != 0 && prefix2 == prefix)
			{
				return nsUri;
			}
			num--;
		}
		return null;
	}

	public string LookupNamespace(string prefix)
	{
		return LookupNamespace(prefix, _lastRecord, 0);
	}

	private static ScopeFlags GetName(ref ScopeRecord re, out string prefix, out string nsUri)
	{
		prefix = re.ncName;
		nsUri = re.nsUri;
		return re.flags;
	}

	public void AddNsDeclaration(string prefix, string nsUri)
	{
		AddRecord(ScopeFlags.NsDecl, prefix, nsUri, default(V));
	}

	public void AddExNamespace(string nsUri)
	{
		AddRecord(ScopeFlags.NsExcl, null, nsUri, default(V));
	}

	public bool IsExNamespace(string nsUri)
	{
		int num = 0;
		int num2 = _lastRecord;
		while (0 <= num2)
		{
			string prefix;
			string nsUri2;
			ScopeFlags name = GetName(ref _records[num2], out prefix, out nsUri2);
			if ((name & ScopeFlags.NsExcl) != 0)
			{
				if (nsUri2 == nsUri)
				{
					return true;
				}
				if (nsUri2 == null)
				{
					num = num2;
				}
			}
			else if (num != 0 && (name & ScopeFlags.NsDecl) != 0 && nsUri2 == nsUri)
			{
				bool flag = false;
				for (int i = num2 + 1; i < num; i++)
				{
					GetName(ref _records[i], out var prefix2, out var _);
					if ((name & ScopeFlags.NsDecl) != 0 && prefix2 == prefix)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return true;
				}
			}
			num2--;
		}
		return false;
	}

	private int SearchVariable(string localName, string uri)
	{
		int num = _lastRecord;
		while (0 <= num)
		{
			string prefix;
			string nsUri;
			ScopeFlags name = GetName(ref _records[num], out prefix, out nsUri);
			if ((name & ScopeFlags.Variable) != 0 && prefix == localName && nsUri == uri)
			{
				return num;
			}
			num--;
		}
		return -1;
	}

	[return: MaybeNull]
	public V LookupVariable(string localName, string uri)
	{
		int num = SearchVariable(localName, uri);
		if (num >= 0)
		{
			return _records[num].value;
		}
		return default(V);
	}

	public bool IsLocalVariable(string localName, string uri)
	{
		int num = SearchVariable(localName, uri);
		while (0 <= --num)
		{
			if (_records[num].scopeCount != 0)
			{
				return true;
			}
		}
		return false;
	}

	internal IEnumerable<ScopeRecord> GetActiveRecords()
	{
		int currentRecord = _lastRecord + 1;
		while (true)
		{
			int num = currentRecord - 1;
			currentRecord = num;
			if (0 < num)
			{
				if (!_records[currentRecord].IsNamespace || LookupNamespace(_records[currentRecord].ncName, _lastRecord, currentRecord + 1) == null)
				{
					yield return _records[currentRecord];
				}
				continue;
			}
			break;
		}
	}

	public NamespaceEnumerator GetEnumerator()
	{
		return new NamespaceEnumerator(this);
	}
}
