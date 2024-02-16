using System.Collections;

namespace System.Xml.Schema;

internal sealed class AllElementsContentValidator : ContentValidator
{
	private readonly Hashtable _elements;

	private readonly object[] _particles;

	private readonly BitSet _isRequired;

	private int _countRequired;

	public override bool IsEmptiable
	{
		get
		{
			if (!base.IsEmptiable)
			{
				return _countRequired == 0;
			}
			return true;
		}
	}

	public AllElementsContentValidator(XmlSchemaContentType contentType, int size, bool isEmptiable)
		: base(contentType, isOpen: false, isEmptiable)
	{
		_elements = new Hashtable(size);
		_particles = new object[size];
		_isRequired = new BitSet(size);
	}

	public bool AddElement(XmlQualifiedName name, object particle, bool isEmptiable)
	{
		if (_elements[name] != null)
		{
			return false;
		}
		int count = _elements.Count;
		_elements.Add(name, count);
		_particles[count] = particle;
		if (!isEmptiable)
		{
			_isRequired.Set(count);
			_countRequired++;
		}
		return true;
	}

	public override void InitValidation(ValidationState context)
	{
		context.AllElementsSet = new BitSet(_elements.Count);
		context.CurrentState.AllElementsRequired = -1;
	}

	public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode)
	{
		object obj = _elements[name];
		errorCode = 0;
		if (obj == null)
		{
			context.NeedValidateChildren = false;
			return null;
		}
		int num = (int)obj;
		if (context.AllElementsSet[num])
		{
			errorCode = -2;
			return null;
		}
		if (context.CurrentState.AllElementsRequired == -1)
		{
			context.CurrentState.AllElementsRequired = 0;
		}
		context.AllElementsSet.Set(num);
		if (_isRequired[num])
		{
			context.CurrentState.AllElementsRequired++;
		}
		return _particles[num];
	}

	public override bool CompleteValidation(ValidationState context)
	{
		if (context.CurrentState.AllElementsRequired == _countRequired || (IsEmptiable && context.CurrentState.AllElementsRequired == -1))
		{
			return true;
		}
		return false;
	}

	public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly)
	{
		ArrayList arrayList = null;
		foreach (DictionaryEntry element in _elements)
		{
			if (!context.AllElementsSet[(int)element.Value] && (!isRequiredOnly || _isRequired[(int)element.Value]))
			{
				if (arrayList == null)
				{
					arrayList = new ArrayList();
				}
				arrayList.Add(element.Key);
			}
		}
		return arrayList;
	}

	public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet)
	{
		ArrayList arrayList = new ArrayList();
		foreach (DictionaryEntry element in _elements)
		{
			if (!context.AllElementsSet[(int)element.Value] && (!isRequiredOnly || _isRequired[(int)element.Value]))
			{
				ContentValidator.AddParticleToExpected(_particles[(int)element.Value] as XmlSchemaParticle, schemaSet, arrayList);
			}
		}
		return arrayList;
	}
}
