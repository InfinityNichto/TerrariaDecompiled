using System.Collections;
using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Runtime;

internal sealed class WhitespaceRuleLookup
{
	private sealed class InternalWhitespaceRule : WhitespaceRule
	{
		private int _priority;

		private int _hashCode;

		public int Priority => _priority;

		public InternalWhitespaceRule()
		{
		}

		public InternalWhitespaceRule(string localName, string namespaceName, bool preserveSpace, int priority)
		{
			Init(localName, namespaceName, preserveSpace, priority);
		}

		public void Init(string localName, string namespaceName, bool preserveSpace, int priority)
		{
			Init(localName, namespaceName, preserveSpace);
			_priority = priority;
			if (localName != null && namespaceName != null)
			{
				_hashCode = localName.GetHashCode();
			}
		}

		public void Atomize(XmlNameTable nameTable)
		{
			if (base.LocalName != null)
			{
				base.LocalName = nameTable.Add(base.LocalName);
			}
			if (base.NamespaceName != null)
			{
				base.NamespaceName = nameTable.Add(base.NamespaceName);
			}
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}

		public override bool Equals(object obj)
		{
			InternalWhitespaceRule internalWhitespaceRule = obj as InternalWhitespaceRule;
			if (base.LocalName == internalWhitespaceRule.LocalName)
			{
				return base.NamespaceName == internalWhitespaceRule.NamespaceName;
			}
			return false;
		}
	}

	private readonly Hashtable _qnames;

	private readonly ArrayList _wildcards;

	private readonly InternalWhitespaceRule _ruleTemp;

	private XmlNameTable _nameTable;

	public WhitespaceRuleLookup()
	{
		_qnames = new Hashtable();
		_wildcards = new ArrayList();
	}

	public WhitespaceRuleLookup(IList<WhitespaceRule> rules)
		: this()
	{
		for (int num = rules.Count - 1; num >= 0; num--)
		{
			WhitespaceRule whitespaceRule = rules[num];
			InternalWhitespaceRule internalWhitespaceRule = new InternalWhitespaceRule(whitespaceRule.LocalName, whitespaceRule.NamespaceName, whitespaceRule.PreserveSpace, -num);
			if (whitespaceRule.LocalName == null || whitespaceRule.NamespaceName == null)
			{
				_wildcards.Add(internalWhitespaceRule);
			}
			else
			{
				_qnames[internalWhitespaceRule] = internalWhitespaceRule;
			}
		}
		_ruleTemp = new InternalWhitespaceRule();
	}

	public void Atomize(XmlNameTable nameTable)
	{
		if (nameTable == _nameTable)
		{
			return;
		}
		_nameTable = nameTable;
		foreach (InternalWhitespaceRule value in _qnames.Values)
		{
			value.Atomize(nameTable);
		}
		foreach (InternalWhitespaceRule wildcard in _wildcards)
		{
			wildcard.Atomize(nameTable);
		}
	}

	public bool ShouldStripSpace(string localName, string namespaceName)
	{
		_ruleTemp.Init(localName, namespaceName, preserveSpace: false, 0);
		InternalWhitespaceRule internalWhitespaceRule = _qnames[_ruleTemp] as InternalWhitespaceRule;
		int count = _wildcards.Count;
		while (count-- != 0)
		{
			InternalWhitespaceRule internalWhitespaceRule2 = _wildcards[count] as InternalWhitespaceRule;
			if (internalWhitespaceRule != null)
			{
				if (internalWhitespaceRule.Priority > internalWhitespaceRule2.Priority)
				{
					return !internalWhitespaceRule.PreserveSpace;
				}
				if (internalWhitespaceRule.PreserveSpace == internalWhitespaceRule2.PreserveSpace)
				{
					continue;
				}
			}
			if ((internalWhitespaceRule2.LocalName == null || (object)internalWhitespaceRule2.LocalName == localName) && (internalWhitespaceRule2.NamespaceName == null || (object)internalWhitespaceRule2.NamespaceName == namespaceName))
			{
				return !internalWhitespaceRule2.PreserveSpace;
			}
		}
		if (internalWhitespaceRule != null)
		{
			return !internalWhitespaceRule.PreserveSpace;
		}
		return false;
	}
}
