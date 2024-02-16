using System.Collections.Generic;
using System.Dynamic.Utils;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler;

internal sealed class LabelInfo
{
	private readonly LabelTarget _node;

	private Label _label;

	private bool _labelDefined;

	private LocalBuilder _value;

	private readonly HashSet<LabelScopeInfo> _definitions = new HashSet<LabelScopeInfo>();

	private readonly List<LabelScopeInfo> _references = new List<LabelScopeInfo>();

	private readonly bool _canReturn;

	private bool _acrossBlockJump;

	private OpCode _opCode = OpCodes.Leave;

	private readonly ILGenerator _ilg;

	internal Label Label
	{
		get
		{
			EnsureLabelAndValue();
			return _label;
		}
	}

	internal bool CanReturn => _canReturn;

	internal bool CanBranch => _opCode != OpCodes.Leave;

	internal LabelInfo(ILGenerator il, LabelTarget node, bool canReturn)
	{
		_ilg = il;
		_node = node;
		_canReturn = canReturn;
	}

	internal void Reference(LabelScopeInfo block)
	{
		_references.Add(block);
		if (_definitions.Count > 0)
		{
			ValidateJump(block);
		}
	}

	internal void Define(LabelScopeInfo block)
	{
		for (LabelScopeInfo labelScopeInfo = block; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
		{
			if (labelScopeInfo.ContainsTarget(_node))
			{
				throw Error.LabelTargetAlreadyDefined(_node.Name);
			}
		}
		_definitions.Add(block);
		block.AddLabelInfo(_node, this);
		if (_definitions.Count == 1)
		{
			foreach (LabelScopeInfo reference in _references)
			{
				ValidateJump(reference);
			}
			return;
		}
		if (_acrossBlockJump)
		{
			throw Error.AmbiguousJump(_node.Name);
		}
		_labelDefined = false;
	}

	private void ValidateJump(LabelScopeInfo reference)
	{
		_opCode = (_canReturn ? OpCodes.Ret : OpCodes.Br);
		for (LabelScopeInfo labelScopeInfo = reference; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
		{
			if (_definitions.Contains(labelScopeInfo))
			{
				return;
			}
			if (labelScopeInfo.Kind == LabelScopeKind.Finally || labelScopeInfo.Kind == LabelScopeKind.Filter)
			{
				break;
			}
			if (labelScopeInfo.Kind == LabelScopeKind.Try || labelScopeInfo.Kind == LabelScopeKind.Catch)
			{
				_opCode = OpCodes.Leave;
			}
		}
		_acrossBlockJump = true;
		if (_node != null && _node.Type != typeof(void))
		{
			throw Error.NonLocalJumpWithValue(_node.Name);
		}
		if (_definitions.Count > 1)
		{
			throw Error.AmbiguousJump(_node.Name);
		}
		LabelScopeInfo labelScopeInfo2 = _definitions.First();
		LabelScopeInfo labelScopeInfo3 = Helpers.CommonNode(labelScopeInfo2, reference, (LabelScopeInfo b) => b.Parent);
		_opCode = (_canReturn ? OpCodes.Ret : OpCodes.Br);
		for (LabelScopeInfo labelScopeInfo4 = reference; labelScopeInfo4 != labelScopeInfo3; labelScopeInfo4 = labelScopeInfo4.Parent)
		{
			if (labelScopeInfo4.Kind == LabelScopeKind.Finally)
			{
				throw Error.ControlCannotLeaveFinally();
			}
			if (labelScopeInfo4.Kind == LabelScopeKind.Filter)
			{
				throw Error.ControlCannotLeaveFilterTest();
			}
			if (labelScopeInfo4.Kind == LabelScopeKind.Try || labelScopeInfo4.Kind == LabelScopeKind.Catch)
			{
				_opCode = OpCodes.Leave;
			}
		}
		for (LabelScopeInfo labelScopeInfo5 = labelScopeInfo2; labelScopeInfo5 != labelScopeInfo3; labelScopeInfo5 = labelScopeInfo5.Parent)
		{
			if (!labelScopeInfo5.CanJumpInto)
			{
				if (labelScopeInfo5.Kind == LabelScopeKind.Expression)
				{
					throw Error.ControlCannotEnterExpression();
				}
				throw Error.ControlCannotEnterTry();
			}
		}
	}

	internal void ValidateFinish()
	{
		if (_references.Count > 0 && _definitions.Count == 0)
		{
			throw Error.LabelTargetUndefined(_node.Name);
		}
	}

	internal void EmitJump()
	{
		if (_opCode == OpCodes.Ret)
		{
			_ilg.Emit(OpCodes.Ret);
			return;
		}
		StoreValue();
		_ilg.Emit(_opCode, Label);
	}

	private void StoreValue()
	{
		EnsureLabelAndValue();
		if (_value != null)
		{
			_ilg.Emit(OpCodes.Stloc, _value);
		}
	}

	internal void Mark()
	{
		if (_canReturn)
		{
			if (!_labelDefined)
			{
				return;
			}
			_ilg.Emit(OpCodes.Ret);
		}
		else
		{
			StoreValue();
		}
		MarkWithEmptyStack();
	}

	internal void MarkWithEmptyStack()
	{
		_ilg.MarkLabel(Label);
		if (_value != null)
		{
			_ilg.Emit(OpCodes.Ldloc, _value);
		}
	}

	private void EnsureLabelAndValue()
	{
		if (!_labelDefined)
		{
			_labelDefined = true;
			_label = _ilg.DefineLabel();
			if (_node != null && _node.Type != typeof(void))
			{
				_value = _ilg.DeclareLocal(_node.Type);
			}
		}
	}
}
