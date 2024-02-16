using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class StructMapping : TypeMapping, INameScope
{
	private MemberMapping[] _members;

	private StructMapping _baseMapping;

	private StructMapping _derivedMappings;

	private StructMapping _nextDerivedMapping;

	private MemberMapping _xmlnsMember;

	private bool _hasSimpleContent;

	private bool _openModel;

	private bool _isSequence;

	private NameTable _elements;

	private NameTable _attributes;

	private CodeIdentifiers _scope;

	internal StructMapping BaseMapping
	{
		get
		{
			return _baseMapping;
		}
		[param: DisallowNull]
		set
		{
			_baseMapping = value;
			if (!base.IsAnonymousType && _baseMapping != null)
			{
				_nextDerivedMapping = _baseMapping._derivedMappings;
				_baseMapping._derivedMappings = this;
			}
			if (!value._isSequence || _isSequence)
			{
				return;
			}
			_isSequence = true;
			if (_baseMapping.IsSequence)
			{
				for (StructMapping structMapping = _derivedMappings; structMapping != null; structMapping = structMapping.NextDerivedMapping)
				{
					structMapping.SetSequence();
				}
			}
		}
	}

	internal StructMapping DerivedMappings => _derivedMappings;

	internal bool IsFullyInitialized
	{
		get
		{
			if (_baseMapping != null)
			{
				return Members != null;
			}
			return false;
		}
	}

	internal NameTable LocalElements
	{
		get
		{
			if (_elements == null)
			{
				_elements = new NameTable();
			}
			return _elements;
		}
	}

	internal NameTable LocalAttributes
	{
		get
		{
			if (_attributes == null)
			{
				_attributes = new NameTable();
			}
			return _attributes;
		}
	}

	object INameScope.this[string name, string ns]
	{
		get
		{
			object obj = LocalElements[name, ns];
			if (obj != null)
			{
				return obj;
			}
			if (_baseMapping != null)
			{
				return ((INameScope)_baseMapping)[name, ns];
			}
			return null;
		}
		set
		{
			LocalElements[name, ns] = value;
		}
	}

	internal StructMapping NextDerivedMapping => _nextDerivedMapping;

	internal bool HasSimpleContent => _hasSimpleContent;

	internal bool HasXmlnsMember
	{
		get
		{
			for (StructMapping structMapping = this; structMapping != null; structMapping = structMapping.BaseMapping)
			{
				if (structMapping.XmlnsMember != null)
				{
					return true;
				}
			}
			return false;
		}
	}

	internal MemberMapping[] Members
	{
		get
		{
			return _members;
		}
		set
		{
			_members = value;
		}
	}

	internal MemberMapping XmlnsMember
	{
		get
		{
			return _xmlnsMember;
		}
		set
		{
			_xmlnsMember = value;
		}
	}

	internal bool IsOpenModel
	{
		get
		{
			return _openModel;
		}
		set
		{
			_openModel = value;
		}
	}

	internal CodeIdentifiers Scope
	{
		get
		{
			if (_scope == null)
			{
				_scope = new CodeIdentifiers();
			}
			return _scope;
		}
		set
		{
			_scope = value;
		}
	}

	internal bool IsSequence
	{
		get
		{
			if (_isSequence)
			{
				return !base.TypeDesc.IsRoot;
			}
			return false;
		}
		set
		{
			_isSequence = value;
		}
	}

	internal MemberMapping FindDeclaringMapping(MemberMapping member, out StructMapping declaringMapping, string parent)
	{
		declaringMapping = null;
		if (BaseMapping != null)
		{
			MemberMapping memberMapping = BaseMapping.FindDeclaringMapping(member, out declaringMapping, parent);
			if (memberMapping != null)
			{
				return memberMapping;
			}
		}
		if (_members == null)
		{
			return null;
		}
		for (int i = 0; i < _members.Length; i++)
		{
			if (_members[i].Name == member.Name)
			{
				if (_members[i].TypeDesc != member.TypeDesc)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlHiddenMember, parent, member.Name, member.TypeDesc.FullName, base.TypeName, _members[i].Name, _members[i].TypeDesc.FullName));
				}
				if (!_members[i].Match(member))
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidXmlOverride, parent, member.Name, base.TypeName, _members[i].Name));
				}
				declaringMapping = this;
				return _members[i];
			}
		}
		return null;
	}

	internal bool Declares(MemberMapping member, string parent)
	{
		StructMapping declaringMapping;
		return FindDeclaringMapping(member, out declaringMapping, parent) != null;
	}

	internal void SetContentModel(TextAccessor text, bool hasElements)
	{
		if (BaseMapping == null || BaseMapping.TypeDesc.IsRoot)
		{
			_hasSimpleContent = !hasElements && text != null && !text.Mapping.IsList;
		}
		else if (BaseMapping.HasSimpleContent)
		{
			if (text != null || hasElements)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalSimpleContentExtension, base.TypeDesc.FullName, BaseMapping.TypeDesc.FullName));
			}
			_hasSimpleContent = true;
		}
		else
		{
			_hasSimpleContent = false;
		}
		if (!_hasSimpleContent && text != null && !text.Mapping.TypeDesc.CanBeTextValue && (BaseMapping == null || BaseMapping.TypeDesc.IsRoot || (!text.Mapping.TypeDesc.IsEnum && !text.Mapping.TypeDesc.IsPrimitive)))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalTypedTextAttribute, base.TypeDesc.FullName, text.Name, text.Mapping.TypeDesc.FullName));
		}
	}

	internal bool HasExplicitSequence()
	{
		if (_members != null)
		{
			for (int i = 0; i < _members.Length; i++)
			{
				if (_members[i].IsParticle && _members[i].IsSequence)
				{
					return true;
				}
			}
		}
		if (_baseMapping != null)
		{
			return _baseMapping.HasExplicitSequence();
		}
		return false;
	}

	internal void SetSequence()
	{
		if (!base.TypeDesc.IsRoot)
		{
			StructMapping structMapping = this;
			while (structMapping.BaseMapping != null && !structMapping.BaseMapping.IsSequence && !structMapping.BaseMapping.TypeDesc.IsRoot)
			{
				structMapping = structMapping.BaseMapping;
			}
			structMapping.IsSequence = true;
			for (StructMapping structMapping2 = structMapping.DerivedMappings; structMapping2 != null; structMapping2 = structMapping2.NextDerivedMapping)
			{
				structMapping2.SetSequence();
			}
		}
	}
}
