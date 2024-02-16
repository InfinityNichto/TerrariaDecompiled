using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal abstract class TypeMapping : Mapping
{
	private TypeDesc _typeDesc;

	private string _typeNs;

	private string _typeName;

	private bool _referencedByElement;

	private bool _referencedByTopLevelElement;

	private bool _includeInSchema = true;

	private bool _reference;

	internal bool ReferencedByTopLevelElement
	{
		set
		{
			_referencedByTopLevelElement = value;
		}
	}

	internal bool ReferencedByElement
	{
		get
		{
			if (!_referencedByElement)
			{
				return _referencedByTopLevelElement;
			}
			return true;
		}
		set
		{
			_referencedByElement = value;
		}
	}

	internal string Namespace
	{
		get
		{
			return _typeNs;
		}
		set
		{
			_typeNs = value;
		}
	}

	internal string TypeName
	{
		get
		{
			return _typeName;
		}
		set
		{
			_typeName = value;
		}
	}

	internal TypeDesc TypeDesc
	{
		get
		{
			return _typeDesc;
		}
		set
		{
			_typeDesc = value;
		}
	}

	internal bool IncludeInSchema
	{
		get
		{
			return _includeInSchema;
		}
		set
		{
			_includeInSchema = value;
		}
	}

	internal virtual bool IsList
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	internal bool IsReference
	{
		set
		{
			_reference = value;
		}
	}

	[MemberNotNullWhen(false, "_typeName")]
	internal bool IsAnonymousType
	{
		[MemberNotNullWhen(false, "_typeName")]
		get
		{
			if (_typeName != null)
			{
				return _typeName.Length == 0;
			}
			return true;
		}
	}

	internal virtual string DefaultElementName
	{
		get
		{
			if (!IsAnonymousType)
			{
				return _typeName;
			}
			return XmlConvert.EncodeLocalName(_typeDesc.Name);
		}
	}
}
