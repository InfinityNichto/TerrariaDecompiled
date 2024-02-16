using System.Collections;

namespace System.Xml.Schema;

internal sealed class NfaContentValidator : ContentValidator
{
	private readonly BitSet _firstpos;

	private readonly BitSet[] _followpos;

	private readonly SymbolsDictionary _symbols;

	private readonly Positions _positions;

	private readonly int _endMarkerPos;

	internal NfaContentValidator(BitSet firstpos, BitSet[] followpos, SymbolsDictionary symbols, Positions positions, int endMarkerPos, XmlSchemaContentType contentType, bool isOpen, bool isEmptiable)
		: base(contentType, isOpen, isEmptiable)
	{
		_firstpos = firstpos;
		_followpos = followpos;
		_symbols = symbols;
		_positions = positions;
		_endMarkerPos = endMarkerPos;
	}

	public override void InitValidation(ValidationState context)
	{
		context.CurPos[0] = _firstpos.Clone();
		context.CurPos[1] = new BitSet(_firstpos.Count);
		context.CurrentState.CurPosIndex = 0;
	}

	public override object ValidateElement(XmlQualifiedName name, ValidationState context, out int errorCode)
	{
		BitSet bitSet = context.CurPos[context.CurrentState.CurPosIndex];
		int num = (context.CurrentState.CurPosIndex + 1) % 2;
		BitSet bitSet2 = context.CurPos[num];
		bitSet2.Clear();
		int num2 = _symbols[name];
		object result = null;
		errorCode = 0;
		for (int num3 = bitSet.NextSet(-1); num3 != -1; num3 = bitSet.NextSet(num3))
		{
			if (num2 == _positions[num3].symbol)
			{
				bitSet2.Or(_followpos[num3]);
				result = _positions[num3].particle;
				break;
			}
		}
		if (!bitSet2.IsEmpty)
		{
			context.CurrentState.CurPosIndex = num;
			return result;
		}
		if (base.IsOpen && bitSet[_endMarkerPos])
		{
			return null;
		}
		context.NeedValidateChildren = false;
		errorCode = -1;
		return null;
	}

	public override bool CompleteValidation(ValidationState context)
	{
		if (!context.CurPos[context.CurrentState.CurPosIndex][_endMarkerPos])
		{
			return false;
		}
		return true;
	}

	public override ArrayList ExpectedElements(ValidationState context, bool isRequiredOnly)
	{
		ArrayList arrayList = null;
		BitSet bitSet = context.CurPos[context.CurrentState.CurPosIndex];
		for (int num = bitSet.NextSet(-1); num != -1; num = bitSet.NextSet(num))
		{
			if (arrayList == null)
			{
				arrayList = new ArrayList();
			}
			XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)_positions[num].particle;
			if (xmlSchemaParticle == null)
			{
				string text = _symbols.NameOf(_positions[num].symbol);
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
		return arrayList;
	}

	public override ArrayList ExpectedParticles(ValidationState context, bool isRequiredOnly, XmlSchemaSet schemaSet)
	{
		ArrayList arrayList = new ArrayList();
		BitSet bitSet = context.CurPos[context.CurrentState.CurPosIndex];
		for (int num = bitSet.NextSet(-1); num != -1; num = bitSet.NextSet(num))
		{
			XmlSchemaParticle xmlSchemaParticle = (XmlSchemaParticle)_positions[num].particle;
			if (xmlSchemaParticle != null)
			{
				ContentValidator.AddParticleToExpected(xmlSchemaParticle, schemaSet, arrayList);
			}
		}
		return arrayList;
	}
}
