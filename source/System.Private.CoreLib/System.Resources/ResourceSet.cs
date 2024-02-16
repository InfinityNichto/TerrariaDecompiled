using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace System.Resources;

public class ResourceSet : IDisposable, IEnumerable
{
	protected IResourceReader Reader;

	private Dictionary<object, object> _table;

	private Dictionary<string, object> _caseInsensitiveTable;

	protected ResourceSet()
	{
		_table = new Dictionary<object, object>();
	}

	internal ResourceSet(bool junk)
	{
	}

	public ResourceSet(string fileName)
		: this()
	{
		Reader = new ResourceReader(fileName);
		ReadResources();
	}

	public ResourceSet(Stream stream)
		: this()
	{
		Reader = new ResourceReader(stream);
		ReadResources();
	}

	public ResourceSet(IResourceReader reader)
		: this()
	{
		Reader = reader ?? throw new ArgumentNullException("reader");
		ReadResources();
	}

	public virtual void Close()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			IResourceReader reader = Reader;
			Reader = null;
			reader?.Close();
		}
		Reader = null;
		_caseInsensitiveTable = null;
		_table = null;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	public virtual Type GetDefaultReader()
	{
		return typeof(ResourceReader);
	}

	public virtual Type GetDefaultWriter()
	{
		return Type.GetType("System.Resources.ResourceWriter, System.Resources.Writer", throwOnError: true);
	}

	public virtual IDictionaryEnumerator GetEnumerator()
	{
		return GetEnumeratorHelper();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumeratorHelper();
	}

	private IDictionaryEnumerator GetEnumeratorHelper()
	{
		IDictionary table = _table;
		if (table == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);
		}
		return table.GetEnumerator();
	}

	public virtual string? GetString(string name)
	{
		object objectInternal = GetObjectInternal(name);
		if (objectInternal is string result)
		{
			return result;
		}
		if (objectInternal == null)
		{
			return null;
		}
		throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
	}

	public virtual string? GetString(string name, bool ignoreCase)
	{
		object objectInternal = GetObjectInternal(name);
		if (objectInternal is string result)
		{
			return result;
		}
		if (objectInternal != null)
		{
			throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
		}
		if (!ignoreCase)
		{
			return null;
		}
		objectInternal = GetCaseInsensitiveObjectInternal(name);
		if (objectInternal is string result2)
		{
			return result2;
		}
		if (objectInternal == null)
		{
			return null;
		}
		throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Name, name));
	}

	public virtual object? GetObject(string name)
	{
		return GetObjectInternal(name);
	}

	public virtual object? GetObject(string name, bool ignoreCase)
	{
		object objectInternal = GetObjectInternal(name);
		if (objectInternal != null || !ignoreCase)
		{
			return objectInternal;
		}
		return GetCaseInsensitiveObjectInternal(name);
	}

	protected virtual void ReadResources()
	{
		IDictionaryEnumerator enumerator = Reader.GetEnumerator();
		while (enumerator.MoveNext())
		{
			_table.Add(enumerator.Key, enumerator.Value);
		}
	}

	private object GetObjectInternal(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		Dictionary<object, object> table = _table;
		if (table == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);
		}
		table.TryGetValue(name, out var value);
		return value;
	}

	private object GetCaseInsensitiveObjectInternal(string name)
	{
		Dictionary<object, object> table = _table;
		if (table == null)
		{
			throw new ObjectDisposedException(null, SR.ObjectDisposed_ResourceSet);
		}
		Dictionary<string, object> dictionary = _caseInsensitiveTable;
		if (dictionary == null)
		{
			dictionary = new Dictionary<string, object>(table.Count, StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<object, object> item in table)
			{
				if (item.Key is string key)
				{
					dictionary.Add(key, item.Value);
				}
			}
			_caseInsensitiveTable = dictionary;
		}
		dictionary.TryGetValue(name, out var value);
		return value;
	}
}
