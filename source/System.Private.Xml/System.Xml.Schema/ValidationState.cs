using System.Collections.Generic;

namespace System.Xml.Schema;

internal sealed class ValidationState
{
	public bool IsNill;

	public bool IsDefault;

	public bool NeedValidateChildren;

	public bool CheckRequiredAttribute;

	public bool ValidationSkipped;

	public XmlSchemaContentProcessing ProcessContents;

	public XmlSchemaValidity Validity;

	public SchemaElementDecl ElementDecl;

	public SchemaElementDecl ElementDeclBeforeXsi;

	public string LocalName;

	public string Namespace;

	public ConstraintStruct[] Constr;

	public StateUnion CurrentState;

	public bool HasMatched;

	private BitSet[] _curPos;

	public BitSet AllElementsSet;

	public List<RangePositionInfo> RunningPositions;

	public bool TooComplex;

	public BitSet[] CurPos => _curPos ?? (_curPos = new BitSet[2]);
}
