using System.Collections;

namespace System.Xml.Schema;

internal sealed class DfaContentValidator : ContentValidator
{
	private readonly int[][] _transitionTable;

	private readonly SymbolsDictionary _symbols;

	internal DfaContentValidator(int[][] transitionTable, SymbolsDictionary symbols, XmlSchemaContentType contentType, bool isOpen, bool isEmptiable)
		: base(contentType, isOpen, isEmptiable)
	{
		_transitionTable = transitionTable;
		_symbols = symbols;
	}

	public override void InitValidation(ValidationState context)
	{
		context.CurrentState.State = 0;
		context.HasMatched = _transitionTable[0][_symbols.Count] > 0;
	}

	public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode)
	{
		int num = _symbols[name];
		int num2 = _transitionTable[context.CurrentState.State][num];
		errorCode = 0;
		if (num2 != -1)
		{
			context.CurrentState.State = num2;
			context.HasMatched = _transitionTable[context.CurrentState.State][_symbols.Count] > 0;
			return _symbols.GetParticle(num);
		}
		if (base.IsOpen && context.HasMatched)
		{
			return null;
		}
		context.NeedValidateChildren = false;
		errorCode = -1;
		return null;
	}

	public override bool CompleteValidation(ValidationState context)
	{
		if (!context.HasMatched)
		{
			return false;
		}
		return true;
	}

	public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly)
	{
		ArrayList arrayList = null;
		int[] array = _transitionTable[context.CurrentState.State];
		if (array != null)
		{
			for (int i = 0; i < array.Length - 1; i++)
			{
				if (array[i] == -1)
				{
					continue;
				}
				if (arrayList == null)
				{
					arrayList = new ArrayList();
				}
				XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)_symbols.GetParticle(i);
				if (xmlSchemaParticle == null)
				{
					string text = _symbols.NameOf(i);
					if (text.Length != 0)
					{
						arrayList.Add(text);
					}
				}
				else
				{
					string nameString = xmlSchemaParticle.NameString;
					if (!arrayList.Contains(nameString))
					{
						arrayList.Add(nameString);
					}
				}
			}
		}
		return arrayList;
	}

	public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet)
	{
		ArrayList arrayList = new ArrayList();
		int[] array = _transitionTable[context.CurrentState.State];
		if (array != null)
		{
			for (int i = 0; i < array.Length - 1; i++)
			{
				if (array[i] != -1)
				{
					XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)_symbols.GetParticle(i);
					if (xmlSchemaParticle != null)
					{
						ContentValidator.AddParticleToExpected(xmlSchemaParticle, schemaSet, arrayList);
					}
				}
			}
		}
		return arrayList;
	}
}
