using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Schema;

public class XmlSchemaObjectTable
{
	internal enum EnumeratorType
	{
		Keys,
		Values,
		DictionaryEntry
	}

	internal struct XmlSchemaObjectEntry
	{
		internal XmlQualifiedName qname;

		internal XmlSchemaObject xso;

		public XmlSchemaObjectEntry(XmlQualifiedName name, XmlSchemaObject value)
		{
			qname = name;
			xso = value;
		}
	}

	internal sealed class NamesCollection : ICollection, IEnumerable
	{
		private readonly List<XmlSchemaObjectEntry> _entries;

		private readonly int _size;

		public int Count => _size;

		public object SyncRoot => ((ICollection)_entries).SyncRoot;

		public bool IsSynchronized => ((ICollection)_entries).IsSynchronized;

		internal NamesCollection(List<XmlSchemaObjectEntry> entries, int size)
		{
			_entries = entries;
			_size = size;
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}
			for (int i = 0; i < _size; i++)
			{
				array.SetValue(_entries[i].qname, arrayIndex++);
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new XSOEnumerator(_entries, _size, EnumeratorType.Keys);
		}
	}

	internal sealed class ValuesCollection : ICollection, IEnumerable
	{
		private readonly List<XmlSchemaObjectEntry> _entries;

		private readonly int _size;

		public int Count => _size;

		public object SyncRoot => ((ICollection)_entries).SyncRoot;

		public bool IsSynchronized => ((ICollection)_entries).IsSynchronized;

		internal ValuesCollection(List<XmlSchemaObjectEntry> entries, int size)
		{
			_entries = entries;
			_size = size;
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0)
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}
			for (int i = 0; i < _size; i++)
			{
				array.SetValue(_entries[i].xso, arrayIndex++);
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new XSOEnumerator(_entries, _size, EnumeratorType.Values);
		}
	}

	internal class XSOEnumerator : IEnumerator
	{
		private readonly List<XmlSchemaObjectEntry> _entries;

		private readonly EnumeratorType _enumType;

		protected int currentIndex;

		protected int size;

		protected XmlQualifiedName currentKey;

		protected XmlSchemaObject currentValue;

		public object Current
		{
			get
			{
				if (currentIndex == -1)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumNotStarted, string.Empty));
				}
				if (currentIndex >= size)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumFinished, string.Empty));
				}
				return _enumType switch
				{
					EnumeratorType.Keys => currentKey, 
					EnumeratorType.Values => currentValue, 
					EnumeratorType.DictionaryEntry => new DictionaryEntry(currentKey, currentValue), 
					_ => null, 
				};
			}
		}

		internal XSOEnumerator(List<XmlSchemaObjectEntry> entries, int size, EnumeratorType enumType)
		{
			_entries = entries;
			this.size = size;
			_enumType = enumType;
			currentIndex = -1;
		}

		public bool MoveNext()
		{
			if (currentIndex >= size - 1)
			{
				currentValue = null;
				currentKey = null;
				return false;
			}
			currentIndex++;
			currentValue = _entries[currentIndex].xso;
			currentKey = _entries[currentIndex].qname;
			return true;
		}

		public void Reset()
		{
			currentIndex = -1;
			currentValue = null;
			currentKey = null;
		}
	}

	internal sealed class XSODictionaryEnumerator : XSOEnumerator, IDictionaryEnumerator, IEnumerator
	{
		public DictionaryEntry Entry
		{
			get
			{
				if (currentIndex == -1)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumNotStarted, string.Empty));
				}
				if (currentIndex >= size)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumFinished, string.Empty));
				}
				return new DictionaryEntry(currentKey, currentValue);
			}
		}

		public object Key
		{
			get
			{
				if (currentIndex == -1)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumNotStarted, string.Empty));
				}
				if (currentIndex >= size)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumFinished, string.Empty));
				}
				return currentKey;
			}
		}

		public object Value
		{
			get
			{
				if (currentIndex == -1)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumNotStarted, string.Empty));
				}
				if (currentIndex >= size)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.Sch_EnumFinished, string.Empty));
				}
				return currentValue;
			}
		}

		internal XSODictionaryEnumerator(List<XmlSchemaObjectEntry> entries, int size, EnumeratorType enumType)
			: base(entries, size, enumType)
		{
		}
	}

	private readonly Dictionary<XmlQualifiedName, XmlSchemaObject> _table = new Dictionary<XmlQualifiedName, XmlSchemaObject>();

	private readonly List<XmlSchemaObjectEntry> _entries = new List<XmlSchemaObjectEntry>();

	public int Count => _table.Count;

	public XmlSchemaObject? this[XmlQualifiedName name]
	{
		get
		{
			if (_table.TryGetValue(name, out var value))
			{
				return value;
			}
			return null;
		}
	}

	public ICollection Names => new NamesCollection(_entries, _table.Count);

	public ICollection Values => new ValuesCollection(_entries, _table.Count);

	internal XmlSchemaObjectTable()
	{
	}

	internal void Add(XmlQualifiedName name, XmlSchemaObject value)
	{
		_table.Add(name, value);
		_entries.Add(new XmlSchemaObjectEntry(name, value));
	}

	internal void Insert(XmlQualifiedName name, XmlSchemaObject value)
	{
		XmlSchemaObject value2 = null;
		if (_table.TryGetValue(name, out value2))
		{
			_table[name] = value;
			int index = FindIndexByValue(value2);
			_entries[index] = new XmlSchemaObjectEntry(name, value);
		}
		else
		{
			Add(name, value);
		}
	}

	internal void Replace(XmlQualifiedName name, XmlSchemaObject value)
	{
		if (_table.TryGetValue(name, out var value2))
		{
			_table[name] = value;
			int index = FindIndexByValue(value2);
			_entries[index] = new XmlSchemaObjectEntry(name, value);
		}
	}

	internal void Clear()
	{
		_table.Clear();
		_entries.Clear();
	}

	internal void Remove(XmlQualifiedName name)
	{
		if (_table.TryGetValue(name, out var value))
		{
			_table.Remove(name);
			int index = FindIndexByValue(value);
			_entries.RemoveAt(index);
		}
	}

	private int FindIndexByValue(XmlSchemaObject xso)
	{
		for (int i = 0; i < _entries.Count; i++)
		{
			if (_entries[i].xso == xso)
			{
				return i;
			}
		}
		return -1;
	}

	public bool Contains(XmlQualifiedName name)
	{
		return _table.ContainsKey(name);
	}

	public IDictionaryEnumerator GetEnumerator()
	{
		return new XSODictionaryEnumerator(_entries, _table.Count, EnumeratorType.DictionaryEntry);
	}
}
