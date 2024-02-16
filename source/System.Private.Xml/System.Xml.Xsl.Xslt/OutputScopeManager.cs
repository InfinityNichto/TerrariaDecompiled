namespace System.Xml.Xsl.Xslt;

internal sealed class OutputScopeManager
{
	public struct ScopeReord
	{
		public int scopeCount;

		public string prefix;

		public string nsUri;
	}

	private ScopeReord[] _records = new ScopeReord[32];

	private int _lastRecord;

	private int _lastScopes;

	public OutputScopeManager()
	{
		Reset();
	}

	public void Reset()
	{
		_records[0].prefix = null;
		_records[0].nsUri = null;
		PushScope();
	}

	public void PushScope()
	{
		_lastScopes++;
	}

	public void PopScope()
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

	public void AddNamespace(string prefix, string uri)
	{
		AddRecord(prefix, uri);
	}

	private void AddRecord(string prefix, string uri)
	{
		_records[_lastRecord].scopeCount = _lastScopes;
		_lastRecord++;
		if (_lastRecord == _records.Length)
		{
			ScopeReord[] array = new ScopeReord[_lastRecord * 2];
			Array.Copy(_records, array, _lastRecord);
			_records = array;
		}
		_lastScopes = 0;
		_records[_lastRecord].prefix = prefix;
		_records[_lastRecord].nsUri = uri;
	}

	public void InvalidateAllPrefixes()
	{
		if (_records[_lastRecord].prefix != null)
		{
			AddRecord(null, null);
		}
	}

	public void InvalidateNonDefaultPrefixes()
	{
		string text = LookupNamespace(string.Empty);
		if (text == null)
		{
			InvalidateAllPrefixes();
		}
		else if (_records[_lastRecord].prefix.Length != 0 || _records[_lastRecord - 1].prefix != null)
		{
			AddRecord(null, null);
			AddRecord(string.Empty, text);
		}
	}

	public string LookupNamespace(string prefix)
	{
		int num = _lastRecord;
		while (_records[num].prefix != null)
		{
			if (_records[num].prefix == prefix)
			{
				return _records[num].nsUri;
			}
			num--;
		}
		return null;
	}
}
