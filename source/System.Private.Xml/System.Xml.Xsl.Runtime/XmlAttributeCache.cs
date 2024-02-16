using System.Xml.Schema;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlAttributeCache : XmlRawWriter, IRemovableWriter
{
	private struct AttrNameVal
	{
		private string _localName;

		private string _prefix;

		private string _namespaceName;

		private string _text;

		private XmlAtomicValue _value;

		private int _hashCode;

		private int _nextNameIndex;

		public string LocalName => _localName;

		public string Prefix => _prefix;

		public string Namespace => _namespaceName;

		public string Text => _text;

		public XmlAtomicValue Value => _value;

		public int NextNameIndex
		{
			get
			{
				return _nextNameIndex;
			}
			set
			{
				_nextNameIndex = value;
			}
		}

		public void Init(string prefix, string localName, string ns, int hashCode)
		{
			_localName = localName;
			_prefix = prefix;
			_namespaceName = ns;
			_hashCode = hashCode;
			_nextNameIndex = 0;
		}

		public void Init(string text)
		{
			_text = text;
			_value = null;
		}

		public void Init(XmlAtomicValue value)
		{
			_text = null;
			_value = value;
		}

		public bool IsDuplicate(string localName, string ns, int hashCode)
		{
			if (_localName != null && _hashCode == hashCode && _localName.Equals(localName) && _namespaceName.Equals(ns))
			{
				_localName = null;
				return true;
			}
			return false;
		}
	}

	private XmlRawWriter _wrapped;

	private OnRemoveWriter _onRemove;

	private AttrNameVal[] _arrAttrs;

	private int _numEntries;

	private int _idxLastName;

	private int _hashCodeUnion;

	public int Count => _numEntries;

	public OnRemoveWriter OnRemoveWriterEvent
	{
		set
		{
			_onRemove = value;
		}
	}

	public void Init(XmlRawWriter wrapped)
	{
		SetWrappedWriter(wrapped);
		_numEntries = 0;
		_idxLastName = 0;
		_hashCodeUnion = 0;
	}

	private void SetWrappedWriter(XmlRawWriter writer)
	{
		if (writer is IRemovableWriter removableWriter)
		{
			removableWriter.OnRemoveWriterEvent = SetWrappedWriter;
		}
		_wrapped = writer;
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		int num = 0;
		int num2 = 1 << (localName[0] & 0x1F);
		if ((_hashCodeUnion & num2) != 0)
		{
			while (!_arrAttrs[num].IsDuplicate(localName, ns, num2))
			{
				num = _arrAttrs[num].NextNameIndex;
				if (num == 0)
				{
					break;
				}
			}
		}
		else
		{
			_hashCodeUnion |= num2;
		}
		EnsureAttributeCache();
		if (_numEntries != 0)
		{
			_arrAttrs[_idxLastName].NextNameIndex = _numEntries;
		}
		_idxLastName = _numEntries++;
		_arrAttrs[_idxLastName].Init(prefix, localName, ns, num2);
	}

	public override void WriteEndAttribute()
	{
	}

	internal override void WriteNamespaceDeclaration(string prefix, string ns)
	{
		FlushAttributes();
		_wrapped.WriteNamespaceDeclaration(prefix, ns);
	}

	public override void WriteString(string text)
	{
		EnsureAttributeCache();
		_arrAttrs[_numEntries++].Init(text);
	}

	public override void WriteValue(object value)
	{
		EnsureAttributeCache();
		_arrAttrs[_numEntries++].Init((XmlAtomicValue)value);
	}

	public override void WriteValue(string value)
	{
		WriteValue(value);
	}

	internal override void StartElementContent()
	{
		FlushAttributes();
		_wrapped.StartElementContent();
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
	}

	public override void WriteComment(string text)
	{
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
	}

	public override void WriteEntityRef(string name)
	{
	}

	public override void Close()
	{
		_wrapped.Close();
	}

	public override void Flush()
	{
		_wrapped.Flush();
	}

	private void FlushAttributes()
	{
		int num = 0;
		while (num != _numEntries)
		{
			int num2 = _arrAttrs[num].NextNameIndex;
			if (num2 == 0)
			{
				num2 = _numEntries;
			}
			string localName = _arrAttrs[num].LocalName;
			if (localName != null)
			{
				string prefix = _arrAttrs[num].Prefix;
				string @namespace = _arrAttrs[num].Namespace;
				_wrapped.WriteStartAttribute(prefix, localName, @namespace);
				while (++num != num2)
				{
					string text = _arrAttrs[num].Text;
					if (text != null)
					{
						_wrapped.WriteString(text);
					}
					else
					{
						_wrapped.WriteValue(_arrAttrs[num].Value);
					}
				}
				_wrapped.WriteEndAttribute();
			}
			else
			{
				num = num2;
			}
		}
		if (_onRemove != null)
		{
			_onRemove(_wrapped);
		}
	}

	private void EnsureAttributeCache()
	{
		if (_arrAttrs == null)
		{
			_arrAttrs = new AttrNameVal[32];
		}
		else if (_numEntries >= _arrAttrs.Length)
		{
			AttrNameVal[] array = new AttrNameVal[_numEntries * 2];
			Array.Copy(_arrAttrs, array, _numEntries);
			_arrAttrs = array;
		}
	}
}
