using System.Collections;

namespace System.Xml.Schema;

internal class ContentValidator
{
	private readonly XmlSchemaContentType _contentType;

	private bool _isOpen;

	private readonly bool _isEmptiable;

	public static readonly ContentValidator Empty = new ContentValidator(XmlSchemaContentType.Empty);

	public static readonly ContentValidator TextOnly = new ContentValidator(XmlSchemaContentType.TextOnly, isOpen: false, isEmptiable: false);

	public static readonly ContentValidator Mixed = new ContentValidator(XmlSchemaContentType.Mixed);

	public static readonly ContentValidator Any = new ContentValidator(XmlSchemaContentType.Mixed, isOpen: true, isEmptiable: true);

	public XmlSchemaContentType ContentType => _contentType;

	public bool PreserveWhitespace
	{
		get
		{
			if (_contentType != 0)
			{
				return _contentType == XmlSchemaContentType.Mixed;
			}
			return true;
		}
	}

	public virtual bool IsEmptiable => _isEmptiable;

	public bool IsOpen
	{
		get
		{
			if (_contentType == XmlSchemaContentType.TextOnly || _contentType == XmlSchemaContentType.Empty)
			{
				return false;
			}
			return _isOpen;
		}
		set
		{
			_isOpen = value;
		}
	}

	public ContentValidator(XmlSchemaContentType contentType)
	{
		_contentType = contentType;
		_isEmptiable = true;
	}

	protected ContentValidator(XmlSchemaContentType contentType, bool isOpen, bool isEmptiable)
	{
		_contentType = contentType;
		_isOpen = isOpen;
		_isEmptiable = isEmptiable;
	}

	public virtual void InitValidation(ValidationState context)
	{
	}

	public virtual object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode)
	{
		if (_contentType == XmlSchemaContentType.TextOnly || _contentType == XmlSchemaContentType.Empty)
		{
			context.NeedValidateChildren = false;
		}
		errorCode = -1;
		return null;
	}

	public virtual bool CompleteValidation(ValidationState context)
	{
		return true;
	}

	public virtual ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly)
	{
		return null;
	}

	public virtual ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet)
	{
		return null;
	}

	public static void AddParticleToExpected(XmlSchemaParticle p, XmlSchemaSet schemaSet, ArrayList particles)
	{
		AddParticleToExpected(p, schemaSet, particles, global: false);
	}

	public static void AddParticleToExpected(XmlSchemaParticle p, XmlSchemaSet schemaSet, ArrayList particles, bool global)
	{
		if (!particles.Contains(p))
		{
			particles.Add(p);
		}
		if (!(p is XmlSchemaElement xmlSchemaElement) || (!global && xmlSchemaElement.RefName.IsEmpty))
		{
			return;
		}
		XmlSchemaObjectTable substitutionGroups = schemaSet.SubstitutionGroups;
		XmlSchemaSubstitutionGroup xmlSchemaSubstitutionGroup = (XmlSchemaSubstitutionGroup)substitutionGroups[xmlSchemaElement.QualifiedName];
		if (xmlSchemaSubstitutionGroup == null)
		{
			return;
		}
		for (int i = 0; i < xmlSchemaSubstitutionGroup.Members.Count; i++)
		{
			XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)xmlSchemaSubstitutionGroup.Members[i];
			if (!xmlSchemaElement.QualifiedName.Equals(xmlSchemaElement2.QualifiedName) && !particles.Contains(xmlSchemaElement2))
			{
				particles.Add(xmlSchemaElement2);
			}
		}
	}
}
