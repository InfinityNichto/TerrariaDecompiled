using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen;

internal sealed class XmlILVisitor : QilVisitor
{
	private QilExpression _qil;

	private GenerateHelper _helper;

	private IteratorDescriptor _iterCurr;

	private IteratorDescriptor _iterNested;

	private int _indexId;

	[RequiresUnreferencedCode("Method VisitXsltInvokeEarlyBound will require code that cannot be statically analyzed.")]
	public XmlILVisitor()
	{
	}

	public void Visit(QilExpression qil, GenerateHelper helper, MethodInfo methRoot)
	{
		_qil = qil;
		_helper = helper;
		_iterNested = null;
		_indexId = 0;
		PrepareGlobalValues(qil.GlobalParameterList);
		PrepareGlobalValues(qil.GlobalVariableList);
		VisitGlobalValues(qil.GlobalParameterList);
		VisitGlobalValues(qil.GlobalVariableList);
		foreach (QilFunction function in qil.FunctionList)
		{
			Function(function);
		}
		_helper.MethodBegin(methRoot, null, initWriters: true);
		StartNestedIterator(qil.Root);
		Visit(qil.Root);
		EndNestedIterator(qil.Root);
		_helper.MethodEnd();
	}

	private void PrepareGlobalValues(QilList globalIterators)
	{
		foreach (QilIterator globalIterator in globalIterators)
		{
			MethodInfo functionBinding = XmlILAnnotation.Write(globalIterator).FunctionBinding;
			IteratorDescriptor iteratorDescriptor = new IteratorDescriptor(_helper);
			iteratorDescriptor.Storage = StorageDescriptor.Global(functionBinding, GetItemStorageType(globalIterator), !globalIterator.XmlType.IsSingleton);
			XmlILAnnotation.Write(globalIterator).CachedIteratorDescriptor = iteratorDescriptor;
		}
	}

	private void VisitGlobalValues(QilList globalIterators)
	{
		foreach (QilIterator globalIterator in globalIterators)
		{
			QilParameter qilParameter = globalIterator as QilParameter;
			MethodInfo globalLocation = XmlILAnnotation.Write(globalIterator).CachedIteratorDescriptor.Storage.GlobalLocation;
			bool isCached = !globalIterator.XmlType.IsSingleton;
			int num = _helper.StaticData.DeclareGlobalValue(globalIterator.DebugName);
			_helper.MethodBegin(globalLocation, globalIterator.SourceLine, initWriters: false);
			Label label = _helper.DefineLabel();
			Label label2 = _helper.DefineLabel();
			_helper.LoadQueryRuntime();
			_helper.LoadInteger(num);
			_helper.Call(XmlILMethods.GlobalComputed);
			_helper.Emit(OpCodes.Brtrue, label);
			StartNestedIterator(globalIterator);
			if (qilParameter != null)
			{
				LocalBuilder locBldr = _helper.DeclareLocal("$$$param", typeof(object));
				_helper.CallGetParameter(qilParameter.Name.LocalName, qilParameter.Name.NamespaceUri);
				_helper.Emit(OpCodes.Stloc, locBldr);
				_helper.Emit(OpCodes.Ldloc, locBldr);
				_helper.Emit(OpCodes.Brfalse, label2);
				_helper.LoadQueryRuntime();
				_helper.LoadInteger(num);
				_helper.LoadQueryRuntime();
				_helper.LoadInteger(_helper.StaticData.DeclareXmlType(XmlQueryTypeFactory.ItemS));
				_helper.Emit(OpCodes.Ldloc, locBldr);
				_helper.Call(XmlILMethods.ChangeTypeXsltResult);
				_helper.CallSetGlobalValue(typeof(object));
				_helper.EmitUnconditionalBranch(OpCodes.Br, label);
			}
			_helper.MarkLabel(label2);
			if (globalIterator.Binding != null)
			{
				_helper.LoadQueryRuntime();
				_helper.LoadInteger(num);
				NestedVisitEnsureStack(globalIterator.Binding, GetItemStorageType(globalIterator), isCached);
				_helper.CallSetGlobalValue(GetStorageType(globalIterator));
			}
			else
			{
				_helper.LoadQueryRuntime();
				GenerateHelper helper = _helper;
				OpCode ldstr = OpCodes.Ldstr;
				string xmlIl_UnknownParam = System.SR.XmlIl_UnknownParam;
				object[] args = new string[2]
				{
					qilParameter.Name.LocalName,
					qilParameter.Name.NamespaceUri
				};
				helper.Emit(ldstr, System.SR.Format(xmlIl_UnknownParam, args));
				_helper.Call(XmlILMethods.ThrowException);
			}
			EndNestedIterator(globalIterator);
			_helper.MarkLabel(label);
			_helper.CallGetGlobalValue(num, GetStorageType(globalIterator));
			_helper.MethodEnd();
		}
	}

	private void Function(QilFunction ndFunc)
	{
		foreach (QilIterator argument in ndFunc.Arguments)
		{
			IteratorDescriptor iteratorDescriptor = new IteratorDescriptor(_helper);
			int paramIndex = XmlILAnnotation.Write(argument).ArgumentPosition + 1;
			iteratorDescriptor.Storage = StorageDescriptor.Parameter(paramIndex, GetItemStorageType(argument), !argument.XmlType.IsSingleton);
			XmlILAnnotation.Write(argument).CachedIteratorDescriptor = iteratorDescriptor;
		}
		MethodInfo functionBinding = XmlILAnnotation.Write(ndFunc).FunctionBinding;
		bool flag = XmlILConstructInfo.Read(ndFunc).ConstructMethod == XmlILConstructMethod.Writer;
		_helper.MethodBegin(functionBinding, ndFunc.SourceLine, flag);
		foreach (QilIterator argument2 in ndFunc.Arguments)
		{
			if (_qil.IsDebug && argument2.SourceLine != null)
			{
				_helper.DebugSequencePoint(argument2.SourceLine);
			}
			if (argument2.Binding != null)
			{
				int paramIndex = (argument2.Annotation as XmlILAnnotation).ArgumentPosition + 1;
				Label label = _helper.DefineLabel();
				_helper.LoadQueryRuntime();
				_helper.LoadParameter(paramIndex);
				_helper.LoadInteger(29);
				_helper.Call(XmlILMethods.SeqMatchesCode);
				_helper.Emit(OpCodes.Brfalse, label);
				StartNestedIterator(argument2);
				NestedVisitEnsureStack(argument2.Binding, GetItemStorageType(argument2), !argument2.XmlType.IsSingleton);
				EndNestedIterator(argument2);
				_helper.SetParameter(paramIndex);
				_helper.MarkLabel(label);
			}
		}
		StartNestedIterator(ndFunc);
		if (flag)
		{
			NestedVisit(ndFunc.Definition);
		}
		else
		{
			NestedVisitEnsureStack(ndFunc.Definition, GetItemStorageType(ndFunc), !ndFunc.XmlType.IsSingleton);
		}
		EndNestedIterator(ndFunc);
		_helper.MethodEnd();
	}

	protected override QilNode Visit(QilNode nd)
	{
		if (nd == null)
		{
			return null;
		}
		if (_qil.IsDebug && nd.SourceLine != null && !(nd is QilIterator))
		{
			_helper.DebugSequencePoint(nd.SourceLine);
		}
		switch (XmlILConstructInfo.Read(nd).ConstructMethod)
		{
		case XmlILConstructMethod.WriterThenIterator:
			NestedConstruction(nd);
			break;
		case XmlILConstructMethod.IteratorThenWriter:
			CopySequence(nd);
			break;
		default:
			base.Visit(nd);
			break;
		}
		return nd;
	}

	protected override QilNode VisitChildren(QilNode parent)
	{
		return parent;
	}

	private void NestedConstruction(QilNode nd)
	{
		_helper.CallStartSequenceConstruction();
		base.Visit(nd);
		_helper.CallEndSequenceConstruction();
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), isCached: true);
	}

	private void CopySequence(QilNode nd)
	{
		XmlQueryType xmlType = nd.XmlType;
		StartWriterLoop(nd, out var hasOnEnd, out var lblOnEnd);
		if (xmlType.IsSingleton)
		{
			_helper.LoadQueryOutput();
			base.Visit(nd);
			_iterCurr.EnsureItemStorageType(nd.XmlType, typeof(XPathItem));
		}
		else
		{
			base.Visit(nd);
			_iterCurr.EnsureItemStorageType(nd.XmlType, typeof(XPathItem));
			_iterCurr.EnsureNoStackNoCache("$$$copyTemp");
			_helper.LoadQueryOutput();
		}
		_iterCurr.EnsureStackNoCache();
		_helper.Call(XmlILMethods.WriteItem);
		EndWriterLoop(nd, hasOnEnd, lblOnEnd);
	}

	protected override QilNode VisitDataSource(QilDataSource ndSrc)
	{
		_helper.LoadQueryContext();
		NestedVisitEnsureStack(ndSrc.Name);
		NestedVisitEnsureStack(ndSrc.BaseUri);
		_helper.Call(XmlILMethods.GetDataSource);
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$navDoc", typeof(XPathNavigator));
		_helper.Emit(OpCodes.Stloc, localBuilder);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		_helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());
		_iterCurr.Storage = StorageDescriptor.Local(localBuilder, typeof(XPathNavigator), isCached: false);
		return ndSrc;
	}

	protected override QilNode VisitNop(QilUnary ndNop)
	{
		return Visit(ndNop.Child);
	}

	protected override QilNode VisitOptimizeBarrier(QilUnary ndBarrier)
	{
		return Visit(ndBarrier.Child);
	}

	protected override QilNode VisitError(QilUnary ndErr)
	{
		_helper.LoadQueryRuntime();
		NestedVisitEnsureStack(ndErr.Child);
		_helper.Call(XmlILMethods.ThrowException);
		if (XmlILConstructInfo.Read(ndErr).ConstructMethod == XmlILConstructMethod.Writer)
		{
			_iterCurr.Storage = StorageDescriptor.None();
		}
		else
		{
			_helper.Emit(OpCodes.Ldnull);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), isCached: false);
		}
		return ndErr;
	}

	protected override QilNode VisitWarning(QilUnary ndWarning)
	{
		_helper.LoadQueryRuntime();
		NestedVisitEnsureStack(ndWarning.Child);
		_helper.Call(XmlILMethods.SendMessage);
		if (XmlILConstructInfo.Read(ndWarning).ConstructMethod == XmlILConstructMethod.Writer)
		{
			_iterCurr.Storage = StorageDescriptor.None();
		}
		else
		{
			VisitEmpty(ndWarning);
		}
		return ndWarning;
	}

	protected override QilNode VisitTrue(QilNode ndTrue)
	{
		if (_iterCurr.CurrentBranchingContext != 0)
		{
			_helper.EmitUnconditionalBranch((_iterCurr.CurrentBranchingContext == BranchingContext.OnTrue) ? OpCodes.Brtrue : OpCodes.Brfalse, _iterCurr.LabelBranch);
			_iterCurr.Storage = StorageDescriptor.None();
		}
		else
		{
			_helper.LoadBoolean(boolVal: true);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
		}
		return ndTrue;
	}

	protected override QilNode VisitFalse(QilNode ndFalse)
	{
		if (_iterCurr.CurrentBranchingContext != 0)
		{
			_helper.EmitUnconditionalBranch((_iterCurr.CurrentBranchingContext == BranchingContext.OnFalse) ? OpCodes.Brtrue : OpCodes.Brfalse, _iterCurr.LabelBranch);
			_iterCurr.Storage = StorageDescriptor.None();
		}
		else
		{
			_helper.LoadBoolean(boolVal: false);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
		}
		return ndFalse;
	}

	protected override QilNode VisitLiteralString(QilLiteral ndStr)
	{
		_helper.Emit(OpCodes.Ldstr, (string)ndStr);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(string), isCached: false);
		return ndStr;
	}

	protected override QilNode VisitLiteralInt32(QilLiteral ndInt)
	{
		_helper.LoadInteger(ndInt);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(int), isCached: false);
		return ndInt;
	}

	protected override QilNode VisitLiteralInt64(QilLiteral ndLong)
	{
		_helper.Emit(OpCodes.Ldc_I8, (long)ndLong);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(long), isCached: false);
		return ndLong;
	}

	protected override QilNode VisitLiteralDouble(QilLiteral ndDbl)
	{
		_helper.Emit(OpCodes.Ldc_R8, (double)ndDbl);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(double), isCached: false);
		return ndDbl;
	}

	protected override QilNode VisitLiteralDecimal(QilLiteral ndDec)
	{
		_helper.ConstructLiteralDecimal(ndDec);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(decimal), isCached: false);
		return ndDec;
	}

	protected override QilNode VisitLiteralQName(QilName ndQName)
	{
		_helper.ConstructLiteralQName(ndQName.LocalName, ndQName.NamespaceUri);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(XmlQualifiedName), isCached: false);
		return ndQName;
	}

	protected override QilNode VisitAnd(QilBinary ndAnd)
	{
		IteratorDescriptor iterCurr = _iterCurr;
		StartNestedIterator(ndAnd.Left);
		Label lblOnFalse = StartConjunctiveTests(iterCurr.CurrentBranchingContext, iterCurr.LabelBranch);
		Visit(ndAnd.Left);
		EndNestedIterator(ndAnd.Left);
		StartNestedIterator(ndAnd.Right);
		StartLastConjunctiveTest(iterCurr.CurrentBranchingContext, iterCurr.LabelBranch, lblOnFalse);
		Visit(ndAnd.Right);
		EndNestedIterator(ndAnd.Right);
		EndConjunctiveTests(iterCurr.CurrentBranchingContext, iterCurr.LabelBranch, lblOnFalse);
		return ndAnd;
	}

	private Label StartConjunctiveTests(BranchingContext brctxt, Label lblBranch)
	{
		if (brctxt == BranchingContext.OnFalse)
		{
			_iterCurr.SetBranching(BranchingContext.OnFalse, lblBranch);
			return lblBranch;
		}
		Label label = _helper.DefineLabel();
		_iterCurr.SetBranching(BranchingContext.OnFalse, label);
		return label;
	}

	private void StartLastConjunctiveTest(BranchingContext brctxt, Label lblBranch, Label lblOnFalse)
	{
		if (brctxt == BranchingContext.OnTrue)
		{
			_iterCurr.SetBranching(BranchingContext.OnTrue, lblBranch);
		}
		else
		{
			_iterCurr.SetBranching(BranchingContext.OnFalse, lblOnFalse);
		}
	}

	private void EndConjunctiveTests(BranchingContext brctxt, Label lblBranch, Label lblOnFalse)
	{
		switch (brctxt)
		{
		case BranchingContext.OnTrue:
			_helper.MarkLabel(lblOnFalse);
			goto case BranchingContext.OnFalse;
		case BranchingContext.OnFalse:
			_iterCurr.Storage = StorageDescriptor.None();
			break;
		case BranchingContext.None:
			_helper.ConvBranchToBool(lblOnFalse, isTrueBranch: false);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
			break;
		}
	}

	protected override QilNode VisitOr(QilBinary ndOr)
	{
		Label label = default(Label);
		switch (_iterCurr.CurrentBranchingContext)
		{
		case BranchingContext.OnFalse:
			label = _helper.DefineLabel();
			NestedVisitWithBranch(ndOr.Left, BranchingContext.OnTrue, label);
			break;
		case BranchingContext.OnTrue:
			NestedVisitWithBranch(ndOr.Left, BranchingContext.OnTrue, _iterCurr.LabelBranch);
			break;
		default:
			label = _helper.DefineLabel();
			NestedVisitWithBranch(ndOr.Left, BranchingContext.OnTrue, label);
			break;
		}
		switch (_iterCurr.CurrentBranchingContext)
		{
		case BranchingContext.OnFalse:
			NestedVisitWithBranch(ndOr.Right, BranchingContext.OnFalse, _iterCurr.LabelBranch);
			break;
		case BranchingContext.OnTrue:
			NestedVisitWithBranch(ndOr.Right, BranchingContext.OnTrue, _iterCurr.LabelBranch);
			break;
		default:
			NestedVisitWithBranch(ndOr.Right, BranchingContext.OnTrue, label);
			break;
		}
		switch (_iterCurr.CurrentBranchingContext)
		{
		case BranchingContext.OnFalse:
			_helper.MarkLabel(label);
			goto case BranchingContext.OnTrue;
		case BranchingContext.OnTrue:
			_iterCurr.Storage = StorageDescriptor.None();
			break;
		case BranchingContext.None:
			_helper.ConvBranchToBool(label, isTrueBranch: true);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
			break;
		}
		return ndOr;
	}

	protected override QilNode VisitNot(QilUnary ndNot)
	{
		Label lblBranch = default(Label);
		switch (_iterCurr.CurrentBranchingContext)
		{
		case BranchingContext.OnFalse:
			NestedVisitWithBranch(ndNot.Child, BranchingContext.OnTrue, _iterCurr.LabelBranch);
			break;
		case BranchingContext.OnTrue:
			NestedVisitWithBranch(ndNot.Child, BranchingContext.OnFalse, _iterCurr.LabelBranch);
			break;
		default:
			lblBranch = _helper.DefineLabel();
			NestedVisitWithBranch(ndNot.Child, BranchingContext.OnTrue, lblBranch);
			break;
		}
		if (_iterCurr.CurrentBranchingContext == BranchingContext.None)
		{
			_helper.ConvBranchToBool(lblBranch, isTrueBranch: false);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
		}
		else
		{
			_iterCurr.Storage = StorageDescriptor.None();
		}
		return ndNot;
	}

	protected override QilNode VisitConditional(QilTernary ndCond)
	{
		XmlILConstructInfo xmlILConstructInfo = XmlILConstructInfo.Read(ndCond);
		if (xmlILConstructInfo.ConstructMethod == XmlILConstructMethod.Writer)
		{
			Label label = _helper.DefineLabel();
			NestedVisitWithBranch(ndCond.Left, BranchingContext.OnFalse, label);
			NestedVisit(ndCond.Center);
			if (ndCond.Right.NodeType == QilNodeType.Sequence && ndCond.Right.Count == 0)
			{
				_helper.MarkLabel(label);
				NestedVisit(ndCond.Right);
			}
			else
			{
				Label label2 = _helper.DefineLabel();
				_helper.EmitUnconditionalBranch(OpCodes.Br, label2);
				_helper.MarkLabel(label);
				NestedVisit(ndCond.Right);
				_helper.MarkLabel(label2);
			}
			_iterCurr.Storage = StorageDescriptor.None();
		}
		else
		{
			LocalBuilder localBuilder = null;
			LocalBuilder localBuilder2 = null;
			Type itemStorageType = GetItemStorageType(ndCond);
			Label label3 = _helper.DefineLabel();
			if (ndCond.XmlType.IsSingleton)
			{
				NestedVisitWithBranch(ndCond.Left, BranchingContext.OnFalse, label3);
			}
			else
			{
				localBuilder2 = _helper.DeclareLocal("$$$cond", itemStorageType);
				localBuilder = _helper.DeclareLocal("$$$boolResult", typeof(bool));
				NestedVisitEnsureLocal(ndCond.Left, localBuilder);
				_helper.Emit(OpCodes.Ldloc, localBuilder);
				_helper.Emit(OpCodes.Brfalse, label3);
			}
			ConditionalBranch(ndCond.Center, itemStorageType, localBuilder2);
			IteratorDescriptor iterNested = _iterNested;
			Label label4 = _helper.DefineLabel();
			_helper.EmitUnconditionalBranch(OpCodes.Br, label4);
			_helper.MarkLabel(label3);
			ConditionalBranch(ndCond.Right, itemStorageType, localBuilder2);
			if (!ndCond.XmlType.IsSingleton)
			{
				_helper.EmitUnconditionalBranch(OpCodes.Brtrue, label4);
				Label label5 = _helper.DefineLabel();
				_helper.MarkLabel(label5);
				_helper.Emit(OpCodes.Ldloc, localBuilder);
				_helper.Emit(OpCodes.Brtrue, iterNested.GetLabelNext());
				_helper.EmitUnconditionalBranch(OpCodes.Br, _iterNested.GetLabelNext());
				_iterCurr.SetIterator(label5, StorageDescriptor.Local(localBuilder2, itemStorageType, isCached: false));
			}
			_helper.MarkLabel(label4);
		}
		return ndCond;
	}

	private void ConditionalBranch(QilNode ndBranch, Type itemStorageType, LocalBuilder locResult)
	{
		if (locResult == null)
		{
			if (_iterCurr.IsBranching)
			{
				NestedVisitWithBranch(ndBranch, _iterCurr.CurrentBranchingContext, _iterCurr.LabelBranch);
			}
			else
			{
				NestedVisitEnsureStack(ndBranch, itemStorageType, isCached: false);
			}
		}
		else
		{
			NestedVisit(ndBranch, _iterCurr.GetLabelNext());
			_iterCurr.EnsureItemStorageType(ndBranch.XmlType, itemStorageType);
			_iterCurr.EnsureLocalNoCache(locResult);
		}
	}

	protected override QilNode VisitChoice(QilChoice ndChoice)
	{
		NestedVisit(ndChoice.Expression);
		QilNode branches = ndChoice.Branches;
		int num = branches.Count - 1;
		Label[] array = new Label[num];
		int i;
		for (i = 0; i < num; i++)
		{
			array[i] = _helper.DefineLabel();
		}
		Label label = _helper.DefineLabel();
		Label label2 = _helper.DefineLabel();
		_helper.Emit(OpCodes.Switch, array);
		_helper.EmitUnconditionalBranch(OpCodes.Br, label);
		for (i = 0; i < num; i++)
		{
			_helper.MarkLabel(array[i]);
			NestedVisit(branches[i]);
			_helper.EmitUnconditionalBranch(OpCodes.Br, label2);
		}
		_helper.MarkLabel(label);
		NestedVisit(branches[i]);
		_helper.MarkLabel(label2);
		_iterCurr.Storage = StorageDescriptor.None();
		return ndChoice;
	}

	protected override QilNode VisitLength(QilUnary ndSetLen)
	{
		Label label = _helper.DefineLabel();
		OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(ndSetLen);
		if (CachesResult(ndSetLen.Child))
		{
			NestedVisitEnsureStack(ndSetLen.Child);
			_helper.CallCacheCount(_iterNested.Storage.ItemStorageType);
		}
		else
		{
			_helper.Emit(OpCodes.Ldc_I4_0);
			StartNestedIterator(ndSetLen.Child, label);
			Visit(ndSetLen.Child);
			_iterCurr.EnsureNoCache();
			_iterCurr.DiscardStack();
			_helper.Emit(OpCodes.Ldc_I4_1);
			_helper.Emit(OpCodes.Add);
			if (optimizerPatterns.MatchesPattern(OptimizerPatternName.MaxPosition))
			{
				_helper.Emit(OpCodes.Dup);
				_helper.LoadInteger((int)optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
				_helper.Emit(OpCodes.Bgt, label);
			}
			_iterCurr.LoopToEnd(label);
			EndNestedIterator(ndSetLen.Child);
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(int), isCached: false);
		return ndSetLen;
	}

	protected override QilNode VisitSequence(QilList ndSeq)
	{
		if (XmlILConstructInfo.Read(ndSeq).ConstructMethod == XmlILConstructMethod.Writer)
		{
			foreach (QilNode item in ndSeq)
			{
				NestedVisit(item);
			}
		}
		else if (ndSeq.Count == 0)
		{
			VisitEmpty(ndSeq);
		}
		else
		{
			Sequence(ndSeq);
		}
		return ndSeq;
	}

	private void VisitEmpty(QilNode nd)
	{
		_helper.EmitUnconditionalBranch(OpCodes.Brtrue, _iterCurr.GetLabelNext());
		_helper.Emit(OpCodes.Ldnull);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), isCached: false);
	}

	private void Sequence(QilList ndSeq)
	{
		Label label = default(Label);
		Type itemStorageType = GetItemStorageType(ndSeq);
		if (ndSeq.XmlType.IsSingleton)
		{
			foreach (QilNode item in ndSeq)
			{
				if (item.XmlType.IsSingleton)
				{
					NestedVisitEnsureStack(item);
					continue;
				}
				label = _helper.DefineLabel();
				NestedVisit(item, label);
				_iterCurr.DiscardStack();
				_helper.MarkLabel(label);
			}
			_iterCurr.Storage = StorageDescriptor.Stack(itemStorageType, isCached: false);
			return;
		}
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$itemList", itemStorageType);
		LocalBuilder locBldr = _helper.DeclareLocal("$$$idxList", typeof(int));
		Label[] array = new Label[ndSeq.Count];
		Label label2 = _helper.DefineLabel();
		for (int i = 0; i < ndSeq.Count; i++)
		{
			if (i != 0)
			{
				_helper.MarkLabel(label);
			}
			label = ((i != ndSeq.Count - 1) ? _helper.DefineLabel() : _iterCurr.GetLabelNext());
			_helper.LoadInteger(i);
			_helper.Emit(OpCodes.Stloc, locBldr);
			NestedVisit(ndSeq[i], label);
			_iterCurr.EnsureItemStorageType(ndSeq[i].XmlType, itemStorageType);
			_iterCurr.EnsureLocalNoCache(localBuilder);
			array[i] = _iterNested.GetLabelNext();
			_helper.EmitUnconditionalBranch(OpCodes.Brtrue, label2);
		}
		Label label3 = _helper.DefineLabel();
		_helper.MarkLabel(label3);
		_helper.Emit(OpCodes.Ldloc, locBldr);
		_helper.Emit(OpCodes.Switch, array);
		_helper.MarkLabel(label2);
		_iterCurr.SetIterator(label3, StorageDescriptor.Local(localBuilder, itemStorageType, isCached: false));
	}

	protected override QilNode VisitUnion(QilBinary ndUnion)
	{
		return CreateSetIterator(ndUnion, "$$$iterUnion", typeof(UnionIterator), XmlILMethods.UnionCreate, XmlILMethods.UnionNext, XmlILMethods.UnionCurrent);
	}

	protected override QilNode VisitIntersection(QilBinary ndInter)
	{
		return CreateSetIterator(ndInter, "$$$iterInter", typeof(IntersectIterator), XmlILMethods.InterCreate, XmlILMethods.InterNext, XmlILMethods.InterCurrent);
	}

	protected override QilNode VisitDifference(QilBinary ndDiff)
	{
		return CreateSetIterator(ndDiff, "$$$iterDiff", typeof(DifferenceIterator), XmlILMethods.DiffCreate, XmlILMethods.DiffNext, XmlILMethods.DiffCurrent);
	}

	private QilNode CreateSetIterator(QilBinary ndSet, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent)
	{
		LocalBuilder localBuilder = _helper.DeclareLocal(iterName, iterType);
		LocalBuilder localBuilder2 = _helper.DeclareLocal("$$$navSet", typeof(XPathNavigator));
		_helper.Emit(OpCodes.Ldloca, localBuilder);
		_helper.LoadQueryRuntime();
		_helper.Call(methCreate);
		Label label = _helper.DefineLabel();
		Label label2 = _helper.DefineLabel();
		Label label3 = _helper.DefineLabel();
		NestedVisit(ndSet.Left, label);
		Label labelNext = _iterNested.GetLabelNext();
		_iterCurr.EnsureLocal(localBuilder2);
		_helper.EmitUnconditionalBranch(OpCodes.Brtrue, label2);
		_helper.MarkLabel(label3);
		NestedVisit(ndSet.Right, label);
		Label labelNext2 = _iterNested.GetLabelNext();
		_iterCurr.EnsureLocal(localBuilder2);
		_helper.EmitUnconditionalBranch(OpCodes.Brtrue, label2);
		_helper.MarkLabel(label);
		_helper.Emit(OpCodes.Ldnull);
		_helper.Emit(OpCodes.Stloc, localBuilder2);
		_helper.MarkLabel(label2);
		_helper.Emit(OpCodes.Ldloca, localBuilder);
		_helper.Emit(OpCodes.Ldloc, localBuilder2);
		_helper.Call(methNext);
		if (ndSet.XmlType.IsSingleton)
		{
			_helper.Emit(OpCodes.Switch, new Label[3] { label3, labelNext, labelNext2 });
			_iterCurr.Storage = StorageDescriptor.Current(localBuilder, methCurrent, typeof(XPathNavigator));
		}
		else
		{
			_helper.Emit(OpCodes.Switch, new Label[4]
			{
				_iterCurr.GetLabelNext(),
				label3,
				labelNext,
				labelNext2
			});
			_iterCurr.SetIterator(label, StorageDescriptor.Current(localBuilder, methCurrent, typeof(XPathNavigator)));
		}
		return ndSet;
	}

	protected override QilNode VisitAverage(QilUnary ndAvg)
	{
		XmlILStorageMethods xmlILStorageMethods = XmlILMethods.StorageMethods[GetItemStorageType(ndAvg)];
		return CreateAggregator(ndAvg, "$$$aggAvg", xmlILStorageMethods, xmlILStorageMethods.AggAvg, xmlILStorageMethods.AggAvgResult);
	}

	protected override QilNode VisitSum(QilUnary ndSum)
	{
		XmlILStorageMethods xmlILStorageMethods = XmlILMethods.StorageMethods[GetItemStorageType(ndSum)];
		return CreateAggregator(ndSum, "$$$aggSum", xmlILStorageMethods, xmlILStorageMethods.AggSum, xmlILStorageMethods.AggSumResult);
	}

	protected override QilNode VisitMinimum(QilUnary ndMin)
	{
		XmlILStorageMethods xmlILStorageMethods = XmlILMethods.StorageMethods[GetItemStorageType(ndMin)];
		return CreateAggregator(ndMin, "$$$aggMin", xmlILStorageMethods, xmlILStorageMethods.AggMin, xmlILStorageMethods.AggMinResult);
	}

	protected override QilNode VisitMaximum(QilUnary ndMax)
	{
		XmlILStorageMethods xmlILStorageMethods = XmlILMethods.StorageMethods[GetItemStorageType(ndMax)];
		return CreateAggregator(ndMax, "$$$aggMax", xmlILStorageMethods, xmlILStorageMethods.AggMax, xmlILStorageMethods.AggMaxResult);
	}

	private QilNode CreateAggregator(QilUnary ndAgg, string aggName, XmlILStorageMethods methods, MethodInfo methAgg, MethodInfo methResult)
	{
		Label lblOnEnd = _helper.DefineLabel();
		Type declaringType = methAgg.DeclaringType;
		LocalBuilder locBldr = _helper.DeclareLocal(aggName, declaringType);
		_helper.Emit(OpCodes.Ldloca, locBldr);
		_helper.Call(methods.AggCreate);
		StartNestedIterator(ndAgg.Child, lblOnEnd);
		_helper.Emit(OpCodes.Ldloca, locBldr);
		Visit(ndAgg.Child);
		_iterCurr.EnsureStackNoCache();
		_iterCurr.EnsureItemStorageType(ndAgg.XmlType, GetItemStorageType(ndAgg));
		_helper.Call(methAgg);
		_helper.Emit(OpCodes.Ldloca, locBldr);
		_iterCurr.LoopToEnd(lblOnEnd);
		EndNestedIterator(ndAgg.Child);
		if (ndAgg.XmlType.MaybeEmpty)
		{
			_helper.Call(methods.AggIsEmpty);
			_helper.Emit(OpCodes.Brtrue, _iterCurr.GetLabelNext());
			_helper.Emit(OpCodes.Ldloca, locBldr);
		}
		_helper.Call(methResult);
		_iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndAgg), isCached: false);
		return ndAgg;
	}

	protected override QilNode VisitNegate(QilUnary ndNeg)
	{
		NestedVisitEnsureStack(ndNeg.Child);
		_helper.CallArithmeticOp(QilNodeType.Negate, ndNeg.XmlType.TypeCode);
		_iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndNeg), isCached: false);
		return ndNeg;
	}

	protected override QilNode VisitAdd(QilBinary ndPlus)
	{
		return ArithmeticOp(ndPlus);
	}

	protected override QilNode VisitSubtract(QilBinary ndMinus)
	{
		return ArithmeticOp(ndMinus);
	}

	protected override QilNode VisitMultiply(QilBinary ndMul)
	{
		return ArithmeticOp(ndMul);
	}

	protected override QilNode VisitDivide(QilBinary ndDiv)
	{
		return ArithmeticOp(ndDiv);
	}

	protected override QilNode VisitModulo(QilBinary ndMod)
	{
		return ArithmeticOp(ndMod);
	}

	private QilNode ArithmeticOp(QilBinary ndOp)
	{
		NestedVisitEnsureStack(ndOp.Left, ndOp.Right);
		_helper.CallArithmeticOp(ndOp.NodeType, ndOp.XmlType.TypeCode);
		_iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndOp), isCached: false);
		return ndOp;
	}

	protected override QilNode VisitStrLength(QilUnary ndLen)
	{
		NestedVisitEnsureStack(ndLen.Child);
		_helper.Call(XmlILMethods.StrLen);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(int), isCached: false);
		return ndLen;
	}

	protected override QilNode VisitStrConcat(QilStrConcat ndStrConcat)
	{
		QilNode qilNode = ndStrConcat.Delimiter;
		if (qilNode.NodeType == QilNodeType.LiteralString && ((string)(QilLiteral)qilNode).Length == 0)
		{
			qilNode = null;
		}
		QilNode values = ndStrConcat.Values;
		bool flag;
		if (values.NodeType == QilNodeType.Sequence && values.Count < 5)
		{
			flag = true;
			foreach (QilNode item in values)
			{
				if (!item.XmlType.IsSingleton)
				{
					flag = false;
				}
			}
		}
		else
		{
			flag = false;
		}
		if (flag)
		{
			foreach (QilNode item2 in values)
			{
				NestedVisitEnsureStack(item2);
			}
			_helper.CallConcatStrings(values.Count);
		}
		else
		{
			LocalBuilder localBuilder = _helper.DeclareLocal("$$$strcat", typeof(StringConcat));
			_helper.Emit(OpCodes.Ldloca, localBuilder);
			_helper.Call(XmlILMethods.StrCatClear);
			if (qilNode != null)
			{
				_helper.Emit(OpCodes.Ldloca, localBuilder);
				NestedVisitEnsureStack(qilNode);
				_helper.Call(XmlILMethods.StrCatDelim);
			}
			_helper.Emit(OpCodes.Ldloca, localBuilder);
			if (values.NodeType == QilNodeType.Sequence)
			{
				foreach (QilNode item3 in values)
				{
					GenerateConcat(item3, localBuilder);
				}
			}
			else
			{
				GenerateConcat(values, localBuilder);
			}
			_helper.Call(XmlILMethods.StrCatResult);
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(string), isCached: false);
		return ndStrConcat;
	}

	private void GenerateConcat(QilNode ndStr, LocalBuilder locStringConcat)
	{
		Label lblOnEnd = _helper.DefineLabel();
		StartNestedIterator(ndStr, lblOnEnd);
		Visit(ndStr);
		_iterCurr.EnsureStackNoCache();
		_iterCurr.EnsureItemStorageType(ndStr.XmlType, typeof(string));
		_helper.Call(XmlILMethods.StrCatCat);
		_helper.Emit(OpCodes.Ldloca, locStringConcat);
		_iterCurr.LoopToEnd(lblOnEnd);
		EndNestedIterator(ndStr);
	}

	protected override QilNode VisitStrParseQName(QilBinary ndParsedTagName)
	{
		VisitStrParseQName(ndParsedTagName, preservePrefix: false);
		return ndParsedTagName;
	}

	private void VisitStrParseQName(QilBinary ndParsedTagName, bool preservePrefix)
	{
		if (!preservePrefix)
		{
			_helper.LoadQueryRuntime();
		}
		NestedVisitEnsureStack(ndParsedTagName.Left);
		if (ndParsedTagName.Right.XmlType.TypeCode == XmlTypeCode.String)
		{
			NestedVisitEnsureStack(ndParsedTagName.Right);
			if (!preservePrefix)
			{
				_helper.CallParseTagName(GenerateNameType.TagNameAndNamespace);
			}
		}
		else
		{
			if (ndParsedTagName.Right.NodeType == QilNodeType.Sequence)
			{
				_helper.LoadInteger(_helper.StaticData.DeclarePrefixMappings(ndParsedTagName.Right));
			}
			else
			{
				_helper.LoadInteger(_helper.StaticData.DeclarePrefixMappings(new QilNode[1] { ndParsedTagName.Right }));
			}
			if (!preservePrefix)
			{
				_helper.CallParseTagName(GenerateNameType.TagNameAndMappings);
			}
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(XmlQualifiedName), isCached: false);
	}

	protected override QilNode VisitNe(QilBinary ndNe)
	{
		Compare(ndNe);
		return ndNe;
	}

	protected override QilNode VisitEq(QilBinary ndEq)
	{
		Compare(ndEq);
		return ndEq;
	}

	protected override QilNode VisitGt(QilBinary ndGt)
	{
		Compare(ndGt);
		return ndGt;
	}

	protected override QilNode VisitGe(QilBinary ndGe)
	{
		Compare(ndGe);
		return ndGe;
	}

	protected override QilNode VisitLt(QilBinary ndLt)
	{
		Compare(ndLt);
		return ndLt;
	}

	protected override QilNode VisitLe(QilBinary ndLe)
	{
		Compare(ndLe);
		return ndLe;
	}

	private void Compare(QilBinary ndComp)
	{
		QilNodeType nodeType = ndComp.NodeType;
		if ((nodeType == QilNodeType.Eq || nodeType == QilNodeType.Ne) && (TryZeroCompare(nodeType, ndComp.Left, ndComp.Right) || TryZeroCompare(nodeType, ndComp.Right, ndComp.Left) || TryNameCompare(nodeType, ndComp.Left, ndComp.Right) || TryNameCompare(nodeType, ndComp.Right, ndComp.Left)))
		{
			return;
		}
		NestedVisitEnsureStack(ndComp.Left, ndComp.Right);
		XmlTypeCode typeCode = ndComp.Left.XmlType.TypeCode;
		switch (typeCode)
		{
		case XmlTypeCode.String:
		case XmlTypeCode.Decimal:
		case XmlTypeCode.QName:
			if (nodeType == QilNodeType.Eq || nodeType == QilNodeType.Ne)
			{
				_helper.CallCompareEquals(typeCode);
				ZeroCompare((nodeType == QilNodeType.Eq) ? QilNodeType.Ne : QilNodeType.Eq, isBoolVal: true);
			}
			else
			{
				_helper.CallCompare(typeCode);
				_helper.Emit(OpCodes.Ldc_I4_0);
				ClrCompare(nodeType, typeCode);
			}
			break;
		case XmlTypeCode.Boolean:
		case XmlTypeCode.Double:
		case XmlTypeCode.Integer:
		case XmlTypeCode.Int:
			ClrCompare(nodeType, typeCode);
			break;
		}
	}

	protected override QilNode VisitIs(QilBinary ndIs)
	{
		NestedVisitEnsureStack(ndIs.Left, ndIs.Right);
		_helper.Call(XmlILMethods.NavSamePos);
		ZeroCompare(QilNodeType.Ne, isBoolVal: true);
		return ndIs;
	}

	protected override QilNode VisitBefore(QilBinary ndBefore)
	{
		ComparePosition(ndBefore);
		return ndBefore;
	}

	protected override QilNode VisitAfter(QilBinary ndAfter)
	{
		ComparePosition(ndAfter);
		return ndAfter;
	}

	private void ComparePosition(QilBinary ndComp)
	{
		_helper.LoadQueryRuntime();
		NestedVisitEnsureStack(ndComp.Left, ndComp.Right);
		_helper.Call(XmlILMethods.CompPos);
		_helper.LoadInteger(0);
		ClrCompare((ndComp.NodeType == QilNodeType.Before) ? QilNodeType.Lt : QilNodeType.Gt, XmlTypeCode.String);
	}

	protected override QilNode VisitFor(QilIterator ndFor)
	{
		IteratorDescriptor cachedIteratorDescriptor = XmlILAnnotation.Write(ndFor).CachedIteratorDescriptor;
		_iterCurr.Storage = cachedIteratorDescriptor.Storage;
		if (_iterCurr.Storage.Location == ItemLocation.Global)
		{
			_iterCurr.EnsureStack();
		}
		return ndFor;
	}

	protected override QilNode VisitLet(QilIterator ndLet)
	{
		return VisitFor(ndLet);
	}

	protected override QilNode VisitParameter(QilParameter ndParameter)
	{
		return VisitFor(ndParameter);
	}

	protected override QilNode VisitLoop(QilLoop ndLoop)
	{
		StartWriterLoop(ndLoop, out var hasOnEnd, out var lblOnEnd);
		StartBinding(ndLoop.Variable);
		Visit(ndLoop.Body);
		EndBinding(ndLoop.Variable);
		EndWriterLoop(ndLoop, hasOnEnd, lblOnEnd);
		return ndLoop;
	}

	protected override QilNode VisitFilter(QilLoop ndFilter)
	{
		if (HandleFilterPatterns(ndFilter))
		{
			return ndFilter;
		}
		StartBinding(ndFilter.Variable);
		_iterCurr.SetIterator(_iterNested);
		StartNestedIterator(ndFilter.Body);
		_iterCurr.SetBranching(BranchingContext.OnFalse, _iterCurr.ParentIterator.GetLabelNext());
		Visit(ndFilter.Body);
		EndNestedIterator(ndFilter.Body);
		EndBinding(ndFilter.Variable);
		return ndFilter;
	}

	private bool HandleFilterPatterns(QilLoop ndFilter)
	{
		OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(ndFilter);
		bool flag = optimizerPatterns.MatchesPattern(OptimizerPatternName.FilterElements);
		if (flag || optimizerPatterns.MatchesPattern(OptimizerPatternName.FilterContentKind))
		{
			XmlNodeKindFlags xmlNodeKindFlags;
			QilName qilName;
			if (flag)
			{
				xmlNodeKindFlags = XmlNodeKindFlags.Element;
				qilName = (QilName)optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName);
			}
			else
			{
				xmlNodeKindFlags = ((XmlQueryType)optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName)).NodeKinds;
				qilName = null;
			}
			QilNode qilNode = (QilNode)optimizerPatterns.GetArgument(OptimizerPatternArgument.StepNode);
			QilNode qilNode2 = (QilNode)optimizerPatterns.GetArgument(OptimizerPatternArgument.StepInput);
			switch (qilNode.NodeType)
			{
			case QilNodeType.Content:
				if (flag)
				{
					LocalBuilder localBuilder = _helper.DeclareLocal("$$$iterElemContent", typeof(ElementContentIterator));
					_helper.Emit(OpCodes.Ldloca, localBuilder);
					NestedVisitEnsureStack(qilNode2);
					_helper.CallGetAtomizedName(_helper.StaticData.DeclareName(qilName.LocalName));
					_helper.CallGetAtomizedName(_helper.StaticData.DeclareName(qilName.NamespaceUri));
					_helper.Call(XmlILMethods.ElemContentCreate);
					GenerateSimpleIterator(typeof(XPathNavigator), localBuilder, XmlILMethods.ElemContentNext, XmlILMethods.ElemContentCurrent);
				}
				else if (xmlNodeKindFlags == XmlNodeKindFlags.Content)
				{
					CreateSimpleIterator(qilNode2, "$$$iterContent", typeof(ContentIterator), XmlILMethods.ContentCreate, XmlILMethods.ContentNext, XmlILMethods.ContentCurrent);
				}
				else
				{
					LocalBuilder localBuilder = _helper.DeclareLocal("$$$iterContent", typeof(NodeKindContentIterator));
					_helper.Emit(OpCodes.Ldloca, localBuilder);
					NestedVisitEnsureStack(qilNode2);
					_helper.LoadInteger((int)QilXmlToXPathNodeType(xmlNodeKindFlags));
					_helper.Call(XmlILMethods.KindContentCreate);
					GenerateSimpleIterator(typeof(XPathNavigator), localBuilder, XmlILMethods.KindContentNext, XmlILMethods.KindContentCurrent);
				}
				return true;
			case QilNodeType.Parent:
				CreateFilteredIterator(qilNode2, "$$$iterPar", typeof(ParentIterator), XmlILMethods.ParentCreate, XmlILMethods.ParentNext, XmlILMethods.ParentCurrent, xmlNodeKindFlags, qilName, TriState.Unknown, null);
				return true;
			case QilNodeType.Ancestor:
			case QilNodeType.AncestorOrSelf:
				CreateFilteredIterator(qilNode2, "$$$iterAnc", typeof(AncestorIterator), XmlILMethods.AncCreate, XmlILMethods.AncNext, XmlILMethods.AncCurrent, xmlNodeKindFlags, qilName, (qilNode.NodeType != QilNodeType.Ancestor) ? TriState.True : TriState.False, null);
				return true;
			case QilNodeType.Descendant:
			case QilNodeType.DescendantOrSelf:
				CreateFilteredIterator(qilNode2, "$$$iterDesc", typeof(DescendantIterator), XmlILMethods.DescCreate, XmlILMethods.DescNext, XmlILMethods.DescCurrent, xmlNodeKindFlags, qilName, (qilNode.NodeType != QilNodeType.Descendant) ? TriState.True : TriState.False, null);
				return true;
			case QilNodeType.Preceding:
				CreateFilteredIterator(qilNode2, "$$$iterPrec", typeof(PrecedingIterator), XmlILMethods.PrecCreate, XmlILMethods.PrecNext, XmlILMethods.PrecCurrent, xmlNodeKindFlags, qilName, TriState.Unknown, null);
				return true;
			case QilNodeType.FollowingSibling:
				CreateFilteredIterator(qilNode2, "$$$iterFollSib", typeof(FollowingSiblingIterator), XmlILMethods.FollSibCreate, XmlILMethods.FollSibNext, XmlILMethods.FollSibCurrent, xmlNodeKindFlags, qilName, TriState.Unknown, null);
				return true;
			case QilNodeType.PrecedingSibling:
				CreateFilteredIterator(qilNode2, "$$$iterPreSib", typeof(PrecedingSiblingIterator), XmlILMethods.PreSibCreate, XmlILMethods.PreSibNext, XmlILMethods.PreSibCurrent, xmlNodeKindFlags, qilName, TriState.Unknown, null);
				return true;
			case QilNodeType.NodeRange:
				CreateFilteredIterator(qilNode2, "$$$iterRange", typeof(NodeRangeIterator), XmlILMethods.NodeRangeCreate, XmlILMethods.NodeRangeNext, XmlILMethods.NodeRangeCurrent, xmlNodeKindFlags, qilName, TriState.Unknown, ((QilBinary)qilNode).Right);
				return true;
			case QilNodeType.XPathFollowing:
				CreateFilteredIterator(qilNode2, "$$$iterFoll", typeof(XPathFollowingIterator), XmlILMethods.XPFollCreate, XmlILMethods.XPFollNext, XmlILMethods.XPFollCurrent, xmlNodeKindFlags, qilName, TriState.Unknown, null);
				return true;
			case QilNodeType.XPathPreceding:
				CreateFilteredIterator(qilNode2, "$$$iterPrec", typeof(XPathPrecedingIterator), XmlILMethods.XPPrecCreate, XmlILMethods.XPPrecNext, XmlILMethods.XPPrecCurrent, xmlNodeKindFlags, qilName, TriState.Unknown, null);
				return true;
			}
		}
		else
		{
			if (optimizerPatterns.MatchesPattern(OptimizerPatternName.FilterAttributeKind))
			{
				QilNode qilNode2 = (QilNode)optimizerPatterns.GetArgument(OptimizerPatternArgument.StepInput);
				CreateSimpleIterator(qilNode2, "$$$iterAttr", typeof(AttributeIterator), XmlILMethods.AttrCreate, XmlILMethods.AttrNext, XmlILMethods.AttrCurrent);
				return true;
			}
			if (optimizerPatterns.MatchesPattern(OptimizerPatternName.EqualityIndex))
			{
				Label lblOnEnd = _helper.DefineLabel();
				Label label = _helper.DefineLabel();
				QilIterator qilIterator = (QilIterator)optimizerPatterns.GetArgument(OptimizerPatternArgument.StepNode);
				QilNode n = (QilNode)optimizerPatterns.GetArgument(OptimizerPatternArgument.StepInput);
				LocalBuilder locBldr = _helper.DeclareLocal("$$$index", typeof(XmlILIndex));
				_helper.LoadQueryRuntime();
				_helper.Emit(OpCodes.Ldarg_1);
				_helper.LoadInteger(_indexId);
				_helper.Emit(OpCodes.Ldloca, locBldr);
				_helper.Call(XmlILMethods.FindIndex);
				_helper.Emit(OpCodes.Brtrue, label);
				_helper.LoadQueryRuntime();
				_helper.Emit(OpCodes.Ldarg_1);
				_helper.LoadInteger(_indexId);
				_helper.Emit(OpCodes.Ldloc, locBldr);
				StartNestedIterator(qilIterator, lblOnEnd);
				StartBinding(qilIterator);
				Visit(n);
				_iterCurr.EnsureStackNoCache();
				VisitFor(qilIterator);
				_iterCurr.EnsureStackNoCache();
				_iterCurr.EnsureItemStorageType(qilIterator.XmlType, typeof(XPathNavigator));
				_helper.Call(XmlILMethods.IndexAdd);
				_helper.Emit(OpCodes.Ldloc, locBldr);
				_iterCurr.LoopToEnd(lblOnEnd);
				EndBinding(qilIterator);
				EndNestedIterator(qilIterator);
				_helper.Call(XmlILMethods.AddNewIndex);
				_helper.MarkLabel(label);
				_helper.Emit(OpCodes.Ldloc, locBldr);
				_helper.Emit(OpCodes.Ldarg_2);
				_helper.Call(XmlILMethods.IndexLookup);
				_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), isCached: true);
				_indexId++;
				return true;
			}
		}
		return false;
	}

	private void StartBinding(QilIterator ndIter)
	{
		OptimizerPatterns patt = OptimizerPatterns.Read(ndIter);
		if (_qil.IsDebug && ndIter.SourceLine != null)
		{
			_helper.DebugSequencePoint(ndIter.SourceLine);
		}
		if (ndIter.NodeType == QilNodeType.For || ndIter.XmlType.IsSingleton)
		{
			StartForBinding(ndIter, patt);
		}
		else
		{
			StartLetBinding(ndIter);
		}
		XmlILAnnotation.Write(ndIter).CachedIteratorDescriptor = _iterNested;
	}

	private void StartForBinding(QilIterator ndFor, OptimizerPatterns patt)
	{
		LocalBuilder localBuilder = null;
		if (_iterCurr.HasLabelNext)
		{
			StartNestedIterator(ndFor.Binding, _iterCurr.GetLabelNext());
		}
		else
		{
			StartNestedIterator(ndFor.Binding);
		}
		if (patt.MatchesPattern(OptimizerPatternName.IsPositional))
		{
			localBuilder = _helper.DeclareLocal("$$$pos", typeof(int));
			_helper.Emit(OpCodes.Ldc_I4_0);
			_helper.Emit(OpCodes.Stloc, localBuilder);
		}
		Visit(ndFor.Binding);
		if (_qil.IsDebug && ndFor.DebugName != null)
		{
			_helper.DebugStartScope();
			_iterCurr.EnsureLocalNoCache("$$$for");
		}
		else
		{
			_iterCurr.EnsureNoStackNoCache("$$$for");
		}
		if (patt.MatchesPattern(OptimizerPatternName.IsPositional))
		{
			_helper.Emit(OpCodes.Ldloc, localBuilder);
			_helper.Emit(OpCodes.Ldc_I4_1);
			_helper.Emit(OpCodes.Add);
			_helper.Emit(OpCodes.Stloc, localBuilder);
			if (patt.MatchesPattern(OptimizerPatternName.MaxPosition))
			{
				_helper.Emit(OpCodes.Ldloc, localBuilder);
				_helper.LoadInteger((int)patt.GetArgument(OptimizerPatternArgument.ElementQName));
				_helper.Emit(OpCodes.Bgt, _iterCurr.ParentIterator.GetLabelNext());
			}
			_iterCurr.LocalPosition = localBuilder;
		}
		EndNestedIterator(ndFor.Binding);
		_iterCurr.SetIterator(_iterNested);
	}

	public void StartLetBinding(QilIterator ndLet)
	{
		StartNestedIterator(ndLet);
		NestedVisit(ndLet.Binding, GetItemStorageType(ndLet), !ndLet.XmlType.IsSingleton);
		if (_qil.IsDebug && ndLet.DebugName != null)
		{
			_helper.DebugStartScope();
			_iterCurr.EnsureLocal("$$$cache");
		}
		else
		{
			_iterCurr.EnsureNoStack("$$$cache");
		}
		EndNestedIterator(ndLet);
	}

	private void EndBinding(QilIterator ndIter)
	{
		if (_qil.IsDebug && ndIter.DebugName != null)
		{
			_helper.DebugEndScope();
		}
	}

	protected override QilNode VisitPositionOf(QilUnary ndPos)
	{
		QilIterator nd = ndPos.Child as QilIterator;
		LocalBuilder localPosition = XmlILAnnotation.Write(nd).CachedIteratorDescriptor.LocalPosition;
		_iterCurr.Storage = StorageDescriptor.Local(localPosition, typeof(int), isCached: false);
		return ndPos;
	}

	protected override QilNode VisitSort(QilLoop ndSort)
	{
		Type itemStorageType = GetItemStorageType(ndSort);
		Label lblOnEnd = _helper.DefineLabel();
		XmlILStorageMethods xmlILStorageMethods = XmlILMethods.StorageMethods[itemStorageType];
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$cache", xmlILStorageMethods.SeqType);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		_helper.Call(xmlILStorageMethods.SeqReuse);
		_helper.Emit(OpCodes.Stloc, localBuilder);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		LocalBuilder localBuilder2 = _helper.DeclareLocal("$$$keys", typeof(XmlSortKeyAccumulator));
		_helper.Emit(OpCodes.Ldloca, localBuilder2);
		_helper.Call(XmlILMethods.SortKeyCreate);
		StartNestedIterator(ndSort.Variable, lblOnEnd);
		StartBinding(ndSort.Variable);
		_iterCurr.EnsureStackNoCache();
		_iterCurr.EnsureItemStorageType(ndSort.Variable.XmlType, GetItemStorageType(ndSort.Variable));
		_helper.Call(xmlILStorageMethods.SeqAdd);
		_helper.Emit(OpCodes.Ldloca, localBuilder2);
		foreach (QilSortKey item in ndSort.Body)
		{
			VisitSortKey(item, localBuilder2);
		}
		_helper.Call(XmlILMethods.SortKeyFinish);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		_iterCurr.LoopToEnd(lblOnEnd);
		_helper.Emit(OpCodes.Pop);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		_helper.Emit(OpCodes.Ldloca, localBuilder2);
		_helper.Call(XmlILMethods.SortKeyKeys);
		_helper.Call(xmlILStorageMethods.SeqSortByKeys);
		_iterCurr.Storage = StorageDescriptor.Local(localBuilder, itemStorageType, isCached: true);
		EndBinding(ndSort.Variable);
		EndNestedIterator(ndSort.Variable);
		_iterCurr.SetIterator(_iterNested);
		return ndSort;
	}

	private void VisitSortKey(QilSortKey ndKey, LocalBuilder locKeys)
	{
		_helper.Emit(OpCodes.Ldloca, locKeys);
		if (ndKey.Collation.NodeType == QilNodeType.LiteralString)
		{
			_helper.CallGetCollation(_helper.StaticData.DeclareCollation((QilLiteral)ndKey.Collation));
		}
		else
		{
			_helper.LoadQueryRuntime();
			NestedVisitEnsureStack(ndKey.Collation);
			_helper.Call(XmlILMethods.CreateCollation);
		}
		if (ndKey.XmlType.IsSingleton)
		{
			NestedVisitEnsureStack(ndKey.Key);
			_helper.AddSortKey(ndKey.Key.XmlType);
			return;
		}
		Label label = _helper.DefineLabel();
		StartNestedIterator(ndKey.Key, label);
		Visit(ndKey.Key);
		_iterCurr.EnsureStackNoCache();
		_iterCurr.EnsureItemStorageType(ndKey.Key.XmlType, GetItemStorageType(ndKey.Key));
		_helper.AddSortKey(ndKey.Key.XmlType);
		Label label2 = _helper.DefineLabel();
		_helper.EmitUnconditionalBranch(OpCodes.Br_S, label2);
		_helper.MarkLabel(label);
		_helper.AddSortKey(null);
		_helper.MarkLabel(label2);
		EndNestedIterator(ndKey.Key);
	}

	protected override QilNode VisitDocOrderDistinct(QilUnary ndDod)
	{
		if (ndDod.XmlType.IsSingleton)
		{
			return Visit(ndDod.Child);
		}
		if (HandleDodPatterns(ndDod))
		{
			return ndDod;
		}
		_helper.LoadQueryRuntime();
		NestedVisitEnsureCache(ndDod.Child, typeof(XPathNavigator));
		_iterCurr.EnsureStack();
		_helper.Call(XmlILMethods.DocOrder);
		return ndDod;
	}

	private bool HandleDodPatterns(QilUnary ndDod)
	{
		OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(ndDod);
		bool flag = optimizerPatterns.MatchesPattern(OptimizerPatternName.JoinAndDod);
		if (flag || optimizerPatterns.MatchesPattern(OptimizerPatternName.DodReverse))
		{
			OptimizerPatterns optimizerPatterns2 = OptimizerPatterns.Read((QilNode)optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
			XmlNodeKindFlags kinds;
			QilName ndName;
			if (optimizerPatterns2.MatchesPattern(OptimizerPatternName.FilterElements))
			{
				kinds = XmlNodeKindFlags.Element;
				ndName = (QilName)optimizerPatterns2.GetArgument(OptimizerPatternArgument.ElementQName);
			}
			else if (optimizerPatterns2.MatchesPattern(OptimizerPatternName.FilterContentKind))
			{
				kinds = ((XmlQueryType)optimizerPatterns2.GetArgument(OptimizerPatternArgument.ElementQName)).NodeKinds;
				ndName = null;
			}
			else
			{
				kinds = (((ndDod.XmlType.NodeKinds & XmlNodeKindFlags.Attribute) != 0) ? XmlNodeKindFlags.Any : XmlNodeKindFlags.Content);
				ndName = null;
			}
			QilNode qilNode = (QilNode)optimizerPatterns2.GetArgument(OptimizerPatternArgument.StepNode);
			if (flag)
			{
				switch (qilNode.NodeType)
				{
				case QilNodeType.Content:
					CreateContainerIterator(ndDod, "$$$iterContent", typeof(ContentMergeIterator), XmlILMethods.ContentMergeCreate, XmlILMethods.ContentMergeNext, XmlILMethods.ContentMergeCurrent, kinds, ndName, TriState.Unknown);
					return true;
				case QilNodeType.Descendant:
				case QilNodeType.DescendantOrSelf:
					CreateContainerIterator(ndDod, "$$$iterDesc", typeof(DescendantMergeIterator), XmlILMethods.DescMergeCreate, XmlILMethods.DescMergeNext, XmlILMethods.DescMergeCurrent, kinds, ndName, (qilNode.NodeType != QilNodeType.Descendant) ? TriState.True : TriState.False);
					return true;
				case QilNodeType.XPathFollowing:
					CreateContainerIterator(ndDod, "$$$iterFoll", typeof(XPathFollowingMergeIterator), XmlILMethods.XPFollMergeCreate, XmlILMethods.XPFollMergeNext, XmlILMethods.XPFollMergeCurrent, kinds, ndName, TriState.Unknown);
					return true;
				case QilNodeType.FollowingSibling:
					CreateContainerIterator(ndDod, "$$$iterFollSib", typeof(FollowingSiblingMergeIterator), XmlILMethods.FollSibMergeCreate, XmlILMethods.FollSibMergeNext, XmlILMethods.FollSibMergeCurrent, kinds, ndName, TriState.Unknown);
					return true;
				case QilNodeType.XPathPreceding:
					CreateContainerIterator(ndDod, "$$$iterPrec", typeof(XPathPrecedingMergeIterator), XmlILMethods.XPPrecMergeCreate, XmlILMethods.XPPrecMergeNext, XmlILMethods.XPPrecMergeCurrent, kinds, ndName, TriState.Unknown);
					return true;
				}
			}
			else
			{
				QilNode ndCtxt = (QilNode)optimizerPatterns2.GetArgument(OptimizerPatternArgument.StepInput);
				switch (qilNode.NodeType)
				{
				case QilNodeType.Ancestor:
				case QilNodeType.AncestorOrSelf:
					CreateFilteredIterator(ndCtxt, "$$$iterAnc", typeof(AncestorDocOrderIterator), XmlILMethods.AncDOCreate, XmlILMethods.AncDONext, XmlILMethods.AncDOCurrent, kinds, ndName, (qilNode.NodeType != QilNodeType.Ancestor) ? TriState.True : TriState.False, null);
					return true;
				case QilNodeType.PrecedingSibling:
					CreateFilteredIterator(ndCtxt, "$$$iterPreSib", typeof(PrecedingSiblingDocOrderIterator), XmlILMethods.PreSibDOCreate, XmlILMethods.PreSibDONext, XmlILMethods.PreSibDOCurrent, kinds, ndName, TriState.Unknown, null);
					return true;
				case QilNodeType.XPathPreceding:
					CreateFilteredIterator(ndCtxt, "$$$iterPrec", typeof(XPathPrecedingDocOrderIterator), XmlILMethods.XPPrecDOCreate, XmlILMethods.XPPrecDONext, XmlILMethods.XPPrecDOCurrent, kinds, ndName, TriState.Unknown, null);
					return true;
				}
			}
		}
		else if (optimizerPatterns.MatchesPattern(OptimizerPatternName.DodMerge))
		{
			LocalBuilder locBldr = _helper.DeclareLocal("$$$dodMerge", typeof(DodSequenceMerge));
			Label lblOnEnd = _helper.DefineLabel();
			_helper.Emit(OpCodes.Ldloca, locBldr);
			_helper.LoadQueryRuntime();
			_helper.Call(XmlILMethods.DodMergeCreate);
			_helper.Emit(OpCodes.Ldloca, locBldr);
			StartNestedIterator(ndDod.Child, lblOnEnd);
			Visit(ndDod.Child);
			_iterCurr.EnsureStack();
			_helper.Call(XmlILMethods.DodMergeAdd);
			_helper.Emit(OpCodes.Ldloca, locBldr);
			_iterCurr.LoopToEnd(lblOnEnd);
			EndNestedIterator(ndDod.Child);
			_helper.Call(XmlILMethods.DodMergeSeq);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), isCached: true);
			return true;
		}
		return false;
	}

	protected override QilNode VisitInvoke(QilInvoke ndInvoke)
	{
		QilFunction function = ndInvoke.Function;
		MethodInfo functionBinding = XmlILAnnotation.Write(function).FunctionBinding;
		bool flag = XmlILConstructInfo.Read(function).ConstructMethod == XmlILConstructMethod.Writer;
		_helper.LoadQueryRuntime();
		for (int i = 0; i < ndInvoke.Arguments.Count; i++)
		{
			QilNode nd = ndInvoke.Arguments[i];
			QilNode qilNode = ndInvoke.Function.Arguments[i];
			NestedVisitEnsureStack(nd, GetItemStorageType(qilNode), !qilNode.XmlType.IsSingleton);
		}
		if (OptimizerPatterns.Read(ndInvoke).MatchesPattern(OptimizerPatternName.TailCall))
		{
			_helper.TailCall(functionBinding);
		}
		else
		{
			_helper.Call(functionBinding);
		}
		if (!flag)
		{
			_iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndInvoke), !ndInvoke.XmlType.IsSingleton);
		}
		else
		{
			_iterCurr.Storage = StorageDescriptor.None();
		}
		return ndInvoke;
	}

	protected override QilNode VisitContent(QilUnary ndContent)
	{
		CreateSimpleIterator(ndContent.Child, "$$$iterAttrContent", typeof(AttributeContentIterator), XmlILMethods.AttrContentCreate, XmlILMethods.AttrContentNext, XmlILMethods.AttrContentCurrent);
		return ndContent;
	}

	protected override QilNode VisitAttribute(QilBinary ndAttr)
	{
		QilName qilName = ndAttr.Right as QilName;
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$navAttr", typeof(XPathNavigator));
		SyncToNavigator(localBuilder, ndAttr.Left);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		_helper.CallGetAtomizedName(_helper.StaticData.DeclareName(qilName.LocalName));
		_helper.CallGetAtomizedName(_helper.StaticData.DeclareName(qilName.NamespaceUri));
		_helper.Call(XmlILMethods.NavMoveAttr);
		_helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());
		_iterCurr.Storage = StorageDescriptor.Local(localBuilder, typeof(XPathNavigator), isCached: false);
		return ndAttr;
	}

	protected override QilNode VisitParent(QilUnary ndParent)
	{
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$navParent", typeof(XPathNavigator));
		SyncToNavigator(localBuilder, ndParent.Child);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		_helper.Call(XmlILMethods.NavMoveParent);
		_helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());
		_iterCurr.Storage = StorageDescriptor.Local(localBuilder, typeof(XPathNavigator), isCached: false);
		return ndParent;
	}

	protected override QilNode VisitRoot(QilUnary ndRoot)
	{
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$navRoot", typeof(XPathNavigator));
		SyncToNavigator(localBuilder, ndRoot.Child);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		_helper.Call(XmlILMethods.NavMoveRoot);
		_iterCurr.Storage = StorageDescriptor.Local(localBuilder, typeof(XPathNavigator), isCached: false);
		return ndRoot;
	}

	protected override QilNode VisitXmlContext(QilNode ndCtxt)
	{
		_helper.LoadQueryContext();
		_helper.Call(XmlILMethods.GetDefaultDataSource);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), isCached: false);
		return ndCtxt;
	}

	protected override QilNode VisitDescendant(QilUnary ndDesc)
	{
		CreateFilteredIterator(ndDesc.Child, "$$$iterDesc", typeof(DescendantIterator), XmlILMethods.DescCreate, XmlILMethods.DescNext, XmlILMethods.DescCurrent, XmlNodeKindFlags.Any, null, TriState.False, null);
		return ndDesc;
	}

	protected override QilNode VisitDescendantOrSelf(QilUnary ndDesc)
	{
		CreateFilteredIterator(ndDesc.Child, "$$$iterDesc", typeof(DescendantIterator), XmlILMethods.DescCreate, XmlILMethods.DescNext, XmlILMethods.DescCurrent, XmlNodeKindFlags.Any, null, TriState.True, null);
		return ndDesc;
	}

	protected override QilNode VisitAncestor(QilUnary ndAnc)
	{
		CreateFilteredIterator(ndAnc.Child, "$$$iterAnc", typeof(AncestorIterator), XmlILMethods.AncCreate, XmlILMethods.AncNext, XmlILMethods.AncCurrent, XmlNodeKindFlags.Any, null, TriState.False, null);
		return ndAnc;
	}

	protected override QilNode VisitAncestorOrSelf(QilUnary ndAnc)
	{
		CreateFilteredIterator(ndAnc.Child, "$$$iterAnc", typeof(AncestorIterator), XmlILMethods.AncCreate, XmlILMethods.AncNext, XmlILMethods.AncCurrent, XmlNodeKindFlags.Any, null, TriState.True, null);
		return ndAnc;
	}

	protected override QilNode VisitPreceding(QilUnary ndPrec)
	{
		CreateFilteredIterator(ndPrec.Child, "$$$iterPrec", typeof(PrecedingIterator), XmlILMethods.PrecCreate, XmlILMethods.PrecNext, XmlILMethods.PrecCurrent, XmlNodeKindFlags.Any, null, TriState.Unknown, null);
		return ndPrec;
	}

	protected override QilNode VisitFollowingSibling(QilUnary ndFollSib)
	{
		CreateFilteredIterator(ndFollSib.Child, "$$$iterFollSib", typeof(FollowingSiblingIterator), XmlILMethods.FollSibCreate, XmlILMethods.FollSibNext, XmlILMethods.FollSibCurrent, XmlNodeKindFlags.Any, null, TriState.Unknown, null);
		return ndFollSib;
	}

	protected override QilNode VisitPrecedingSibling(QilUnary ndPreSib)
	{
		CreateFilteredIterator(ndPreSib.Child, "$$$iterPreSib", typeof(PrecedingSiblingIterator), XmlILMethods.PreSibCreate, XmlILMethods.PreSibNext, XmlILMethods.PreSibCurrent, XmlNodeKindFlags.Any, null, TriState.Unknown, null);
		return ndPreSib;
	}

	protected override QilNode VisitNodeRange(QilBinary ndRange)
	{
		CreateFilteredIterator(ndRange.Left, "$$$iterRange", typeof(NodeRangeIterator), XmlILMethods.NodeRangeCreate, XmlILMethods.NodeRangeNext, XmlILMethods.NodeRangeCurrent, XmlNodeKindFlags.Any, null, TriState.Unknown, ndRange.Right);
		return ndRange;
	}

	protected override QilNode VisitDeref(QilBinary ndDeref)
	{
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$iterId", typeof(IdIterator));
		_helper.Emit(OpCodes.Ldloca, localBuilder);
		NestedVisitEnsureStack(ndDeref.Left);
		NestedVisitEnsureStack(ndDeref.Right);
		_helper.Call(XmlILMethods.IdCreate);
		GenerateSimpleIterator(typeof(XPathNavigator), localBuilder, XmlILMethods.IdNext, XmlILMethods.IdCurrent);
		return ndDeref;
	}

	protected override QilNode VisitElementCtor(QilBinary ndElem)
	{
		XmlILConstructInfo xmlILConstructInfo = XmlILConstructInfo.Read(ndElem);
		bool flag = CheckWithinContent(xmlILConstructInfo) || !xmlILConstructInfo.IsNamespaceInScope || ElementCachesAttributes(xmlILConstructInfo);
		if (XmlILConstructInfo.Read(ndElem.Right).FinalStates == PossibleXmlStates.Any)
		{
			flag = true;
		}
		if (xmlILConstructInfo.FinalStates == PossibleXmlStates.Any)
		{
			flag = true;
		}
		if (!flag)
		{
			BeforeStartChecks(ndElem);
		}
		GenerateNameType nameType = LoadNameAndType(XPathNodeType.Element, ndElem.Left, isStart: true, flag);
		_helper.CallWriteStartElement(nameType, flag);
		NestedVisit(ndElem.Right);
		if (XmlILConstructInfo.Read(ndElem.Right).FinalStates == PossibleXmlStates.EnumAttrs && !flag)
		{
			_helper.CallStartElementContent();
		}
		nameType = LoadNameAndType(XPathNodeType.Element, ndElem.Left, isStart: false, flag);
		_helper.CallWriteEndElement(nameType, flag);
		if (!flag)
		{
			AfterEndChecks(ndElem);
		}
		_iterCurr.Storage = StorageDescriptor.None();
		return ndElem;
	}

	protected override QilNode VisitAttributeCtor(QilBinary ndAttr)
	{
		XmlILConstructInfo xmlILConstructInfo = XmlILConstructInfo.Read(ndAttr);
		bool flag = CheckEnumAttrs(xmlILConstructInfo) || !xmlILConstructInfo.IsNamespaceInScope;
		if (!flag)
		{
			BeforeStartChecks(ndAttr);
		}
		GenerateNameType nameType = LoadNameAndType(XPathNodeType.Attribute, ndAttr.Left, isStart: true, flag);
		_helper.CallWriteStartAttribute(nameType, flag);
		NestedVisit(ndAttr.Right);
		_helper.CallWriteEndAttribute(flag);
		if (!flag)
		{
			AfterEndChecks(ndAttr);
		}
		_iterCurr.Storage = StorageDescriptor.None();
		return ndAttr;
	}

	protected override QilNode VisitCommentCtor(QilUnary ndComment)
	{
		_helper.CallWriteStartComment();
		NestedVisit(ndComment.Child);
		_helper.CallWriteEndComment();
		_iterCurr.Storage = StorageDescriptor.None();
		return ndComment;
	}

	protected override QilNode VisitPICtor(QilBinary ndPI)
	{
		_helper.LoadQueryOutput();
		NestedVisitEnsureStack(ndPI.Left);
		_helper.CallWriteStartPI();
		NestedVisit(ndPI.Right);
		_helper.CallWriteEndPI();
		_iterCurr.Storage = StorageDescriptor.None();
		return ndPI;
	}

	protected override QilNode VisitTextCtor(QilUnary ndText)
	{
		return VisitTextCtor(ndText, disableOutputEscaping: false);
	}

	protected override QilNode VisitRawTextCtor(QilUnary ndText)
	{
		return VisitTextCtor(ndText, disableOutputEscaping: true);
	}

	private QilNode VisitTextCtor(QilUnary ndText, bool disableOutputEscaping)
	{
		XmlILConstructInfo xmlILConstructInfo = XmlILConstructInfo.Read(ndText);
		PossibleXmlStates initialStates = xmlILConstructInfo.InitialStates;
		bool flag = (uint)(initialStates - 4) > 2u && CheckWithinContent(xmlILConstructInfo);
		if (!flag)
		{
			BeforeStartChecks(ndText);
		}
		_helper.LoadQueryOutput();
		NestedVisitEnsureStack(ndText.Child);
		switch (xmlILConstructInfo.InitialStates)
		{
		case PossibleXmlStates.WithinAttr:
			_helper.CallWriteString(disableOutputEscaping: false, flag);
			break;
		case PossibleXmlStates.WithinComment:
			_helper.Call(XmlILMethods.CommentText);
			break;
		case PossibleXmlStates.WithinPI:
			_helper.Call(XmlILMethods.PIText);
			break;
		default:
			_helper.CallWriteString(disableOutputEscaping, flag);
			break;
		}
		if (!flag)
		{
			AfterEndChecks(ndText);
		}
		_iterCurr.Storage = StorageDescriptor.None();
		return ndText;
	}

	protected override QilNode VisitDocumentCtor(QilUnary ndDoc)
	{
		_helper.CallWriteStartRoot();
		NestedVisit(ndDoc.Child);
		_helper.CallWriteEndRoot();
		_iterCurr.Storage = StorageDescriptor.None();
		return ndDoc;
	}

	protected override QilNode VisitNamespaceDecl(QilBinary ndNmsp)
	{
		XmlILConstructInfo info = XmlILConstructInfo.Read(ndNmsp);
		bool flag = CheckEnumAttrs(info) || MightHaveNamespacesAfterAttributes(info);
		if (!flag)
		{
			BeforeStartChecks(ndNmsp);
		}
		_helper.LoadQueryOutput();
		NestedVisitEnsureStack(ndNmsp.Left);
		NestedVisitEnsureStack(ndNmsp.Right);
		_helper.CallWriteNamespaceDecl(flag);
		if (!flag)
		{
			AfterEndChecks(ndNmsp);
		}
		_iterCurr.Storage = StorageDescriptor.None();
		return ndNmsp;
	}

	protected override QilNode VisitRtfCtor(QilBinary ndRtf)
	{
		OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(ndRtf);
		string text = (QilLiteral)ndRtf.Right;
		if (optimizerPatterns.MatchesPattern(OptimizerPatternName.SingleTextRtf))
		{
			_helper.LoadQueryRuntime();
			NestedVisitEnsureStack((QilNode)optimizerPatterns.GetArgument(OptimizerPatternArgument.ElementQName));
			_helper.Emit(OpCodes.Ldstr, text);
			_helper.Call(XmlILMethods.RtfConstr);
		}
		else
		{
			_helper.CallStartRtfConstruction(text);
			NestedVisit(ndRtf.Left);
			_helper.CallEndRtfConstruction();
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathNavigator), isCached: false);
		return ndRtf;
	}

	protected override QilNode VisitNameOf(QilUnary ndName)
	{
		return VisitNodeProperty(ndName);
	}

	protected override QilNode VisitLocalNameOf(QilUnary ndName)
	{
		return VisitNodeProperty(ndName);
	}

	protected override QilNode VisitNamespaceUriOf(QilUnary ndName)
	{
		return VisitNodeProperty(ndName);
	}

	protected override QilNode VisitPrefixOf(QilUnary ndName)
	{
		return VisitNodeProperty(ndName);
	}

	private QilNode VisitNodeProperty(QilUnary ndProp)
	{
		NestedVisitEnsureStack(ndProp.Child);
		switch (ndProp.NodeType)
		{
		case QilNodeType.NameOf:
			_helper.Emit(OpCodes.Dup);
			_helper.Call(XmlILMethods.NavLocalName);
			_helper.Call(XmlILMethods.NavNmsp);
			_helper.Construct(XmlILConstructors.QName);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(XmlQualifiedName), isCached: false);
			break;
		case QilNodeType.LocalNameOf:
			_helper.Call(XmlILMethods.NavLocalName);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(string), isCached: false);
			break;
		case QilNodeType.NamespaceUriOf:
			_helper.Call(XmlILMethods.NavNmsp);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(string), isCached: false);
			break;
		case QilNodeType.PrefixOf:
			_helper.Call(XmlILMethods.NavPrefix);
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(string), isCached: false);
			break;
		}
		return ndProp;
	}

	protected override QilNode VisitTypeAssert(QilTargetType ndTypeAssert)
	{
		if (!ndTypeAssert.Source.XmlType.IsSingleton && ndTypeAssert.XmlType.IsSingleton && !_iterCurr.HasLabelNext)
		{
			Label label = _helper.DefineLabel();
			_helper.MarkLabel(label);
			NestedVisit(ndTypeAssert.Source, label);
		}
		else
		{
			Visit(ndTypeAssert.Source);
		}
		_iterCurr.EnsureItemStorageType(ndTypeAssert.Source.XmlType, GetItemStorageType(ndTypeAssert));
		return ndTypeAssert;
	}

	protected override QilNode VisitIsType(QilTargetType ndIsType)
	{
		XmlQueryType xmlType = ndIsType.Source.XmlType;
		XmlQueryType targetType = ndIsType.TargetType;
		if (xmlType.IsSingleton && (object)targetType == XmlQueryTypeFactory.Node)
		{
			NestedVisitEnsureStack(ndIsType.Source);
			_helper.Call(XmlILMethods.ItemIsNode);
			ZeroCompare(QilNodeType.Ne, isBoolVal: true);
			return ndIsType;
		}
		if (MatchesNodeKinds(ndIsType, xmlType, targetType))
		{
			return ndIsType;
		}
		XmlTypeCode xmlTypeCode = (((object)targetType == XmlQueryTypeFactory.Double) ? XmlTypeCode.Double : (((object)targetType == XmlQueryTypeFactory.String) ? XmlTypeCode.String : (((object)targetType == XmlQueryTypeFactory.Boolean) ? XmlTypeCode.Boolean : (((object)targetType == XmlQueryTypeFactory.Node) ? XmlTypeCode.Node : XmlTypeCode.None))));
		if (xmlTypeCode != 0)
		{
			_helper.LoadQueryRuntime();
			NestedVisitEnsureStack(ndIsType.Source, typeof(XPathItem), !xmlType.IsSingleton);
			_helper.LoadInteger((int)xmlTypeCode);
			_helper.Call(xmlType.IsSingleton ? XmlILMethods.ItemMatchesCode : XmlILMethods.SeqMatchesCode);
			ZeroCompare(QilNodeType.Ne, isBoolVal: true);
			return ndIsType;
		}
		_helper.LoadQueryRuntime();
		NestedVisitEnsureStack(ndIsType.Source, typeof(XPathItem), !xmlType.IsSingleton);
		_helper.LoadInteger(_helper.StaticData.DeclareXmlType(targetType));
		_helper.Call(xmlType.IsSingleton ? XmlILMethods.ItemMatchesType : XmlILMethods.SeqMatchesType);
		ZeroCompare(QilNodeType.Ne, isBoolVal: true);
		return ndIsType;
	}

	private bool MatchesNodeKinds(QilTargetType ndIsType, XmlQueryType typDerived, XmlQueryType typBase)
	{
		bool flag = true;
		if (!typBase.IsNode || !typBase.IsSingleton)
		{
			return false;
		}
		if (!typDerived.IsNode || !typDerived.IsSingleton || !typDerived.IsNotRtf)
		{
			return false;
		}
		XmlNodeKindFlags xmlNodeKindFlags = XmlNodeKindFlags.None;
		foreach (XmlQueryType item in typBase)
		{
			if ((object)item == XmlQueryTypeFactory.Element)
			{
				xmlNodeKindFlags |= XmlNodeKindFlags.Element;
				continue;
			}
			if ((object)item == XmlQueryTypeFactory.Attribute)
			{
				xmlNodeKindFlags |= XmlNodeKindFlags.Attribute;
				continue;
			}
			if ((object)item == XmlQueryTypeFactory.Text)
			{
				xmlNodeKindFlags |= XmlNodeKindFlags.Text;
				continue;
			}
			if ((object)item == XmlQueryTypeFactory.Document)
			{
				xmlNodeKindFlags |= XmlNodeKindFlags.Document;
				continue;
			}
			if ((object)item == XmlQueryTypeFactory.Comment)
			{
				xmlNodeKindFlags |= XmlNodeKindFlags.Comment;
				continue;
			}
			if ((object)item == XmlQueryTypeFactory.PI)
			{
				xmlNodeKindFlags |= XmlNodeKindFlags.PI;
				continue;
			}
			if ((object)item == XmlQueryTypeFactory.Namespace)
			{
				xmlNodeKindFlags |= XmlNodeKindFlags.Namespace;
				continue;
			}
			return false;
		}
		xmlNodeKindFlags = typDerived.NodeKinds & xmlNodeKindFlags;
		if (!Bits.ExactlyOne((uint)xmlNodeKindFlags))
		{
			xmlNodeKindFlags = ~xmlNodeKindFlags & XmlNodeKindFlags.Any;
			flag = !flag;
		}
		XPathNodeType xPathNodeType;
		switch (xmlNodeKindFlags)
		{
		case XmlNodeKindFlags.Element:
			xPathNodeType = XPathNodeType.Element;
			break;
		case XmlNodeKindFlags.Attribute:
			xPathNodeType = XPathNodeType.Attribute;
			break;
		case XmlNodeKindFlags.Namespace:
			xPathNodeType = XPathNodeType.Namespace;
			break;
		case XmlNodeKindFlags.PI:
			xPathNodeType = XPathNodeType.ProcessingInstruction;
			break;
		case XmlNodeKindFlags.Comment:
			xPathNodeType = XPathNodeType.Comment;
			break;
		case XmlNodeKindFlags.Document:
			xPathNodeType = XPathNodeType.Root;
			break;
		default:
			_helper.Emit(OpCodes.Ldc_I4_1);
			xPathNodeType = XPathNodeType.All;
			break;
		}
		NestedVisitEnsureStack(ndIsType.Source);
		_helper.Call(XmlILMethods.NavType);
		if (xPathNodeType == XPathNodeType.All)
		{
			_helper.Emit(OpCodes.Shl);
			int num = 0;
			if ((xmlNodeKindFlags & XmlNodeKindFlags.Document) != 0)
			{
				num |= 1;
			}
			if ((xmlNodeKindFlags & XmlNodeKindFlags.Element) != 0)
			{
				num |= 2;
			}
			if ((xmlNodeKindFlags & XmlNodeKindFlags.Attribute) != 0)
			{
				num |= 4;
			}
			if ((xmlNodeKindFlags & XmlNodeKindFlags.Text) != 0)
			{
				num |= 0x70;
			}
			if ((xmlNodeKindFlags & XmlNodeKindFlags.Comment) != 0)
			{
				num |= 0x100;
			}
			if ((xmlNodeKindFlags & XmlNodeKindFlags.PI) != 0)
			{
				num |= 0x80;
			}
			if ((xmlNodeKindFlags & XmlNodeKindFlags.Namespace) != 0)
			{
				num |= 8;
			}
			_helper.LoadInteger(num);
			_helper.Emit(OpCodes.And);
			ZeroCompare(flag ? QilNodeType.Ne : QilNodeType.Eq, isBoolVal: false);
		}
		else
		{
			_helper.LoadInteger((int)xPathNodeType);
			ClrCompare(flag ? QilNodeType.Eq : QilNodeType.Ne, XmlTypeCode.Int);
		}
		return true;
	}

	protected override QilNode VisitIsEmpty(QilUnary ndIsEmpty)
	{
		if (CachesResult(ndIsEmpty.Child))
		{
			NestedVisitEnsureStack(ndIsEmpty.Child);
			_helper.CallCacheCount(_iterNested.Storage.ItemStorageType);
			switch (_iterCurr.CurrentBranchingContext)
			{
			case BranchingContext.OnFalse:
				_helper.TestAndBranch(0, _iterCurr.LabelBranch, OpCodes.Bne_Un);
				break;
			case BranchingContext.OnTrue:
				_helper.TestAndBranch(0, _iterCurr.LabelBranch, OpCodes.Beq);
				break;
			default:
			{
				Label label = _helper.DefineLabel();
				_helper.Emit(OpCodes.Brfalse_S, label);
				_helper.ConvBranchToBool(label, isTrueBranch: true);
				break;
			}
			}
		}
		else
		{
			Label label2 = _helper.DefineLabel();
			IteratorDescriptor iterCurr = _iterCurr;
			if (iterCurr.CurrentBranchingContext == BranchingContext.OnTrue)
			{
				StartNestedIterator(ndIsEmpty.Child, _iterCurr.LabelBranch);
			}
			else
			{
				StartNestedIterator(ndIsEmpty.Child, label2);
			}
			Visit(ndIsEmpty.Child);
			_iterCurr.EnsureNoCache();
			_iterCurr.DiscardStack();
			switch (iterCurr.CurrentBranchingContext)
			{
			case BranchingContext.OnFalse:
				_helper.EmitUnconditionalBranch(OpCodes.Br, iterCurr.LabelBranch);
				_helper.MarkLabel(label2);
				break;
			case BranchingContext.None:
				_helper.ConvBranchToBool(label2, isTrueBranch: true);
				break;
			}
			EndNestedIterator(ndIsEmpty.Child);
		}
		if (_iterCurr.IsBranching)
		{
			_iterCurr.Storage = StorageDescriptor.None();
		}
		else
		{
			_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
		}
		return ndIsEmpty;
	}

	protected override QilNode VisitXPathNodeValue(QilUnary ndVal)
	{
		if (ndVal.Child.XmlType.IsSingleton)
		{
			NestedVisitEnsureStack(ndVal.Child, typeof(XPathNavigator), isCached: false);
			_helper.Call(XmlILMethods.Value);
		}
		else
		{
			Label label = _helper.DefineLabel();
			StartNestedIterator(ndVal.Child, label);
			Visit(ndVal.Child);
			_iterCurr.EnsureStackNoCache();
			_helper.Call(XmlILMethods.Value);
			Label label2 = _helper.DefineLabel();
			_helper.EmitUnconditionalBranch(OpCodes.Br, label2);
			_helper.MarkLabel(label);
			_helper.Emit(OpCodes.Ldstr, "");
			_helper.MarkLabel(label2);
			EndNestedIterator(ndVal.Child);
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(string), isCached: false);
		return ndVal;
	}

	protected override QilNode VisitXPathFollowing(QilUnary ndFoll)
	{
		CreateFilteredIterator(ndFoll.Child, "$$$iterFoll", typeof(XPathFollowingIterator), XmlILMethods.XPFollCreate, XmlILMethods.XPFollNext, XmlILMethods.XPFollCurrent, XmlNodeKindFlags.Any, null, TriState.Unknown, null);
		return ndFoll;
	}

	protected override QilNode VisitXPathPreceding(QilUnary ndPrec)
	{
		CreateFilteredIterator(ndPrec.Child, "$$$iterPrec", typeof(XPathPrecedingIterator), XmlILMethods.XPPrecCreate, XmlILMethods.XPPrecNext, XmlILMethods.XPPrecCurrent, XmlNodeKindFlags.Any, null, TriState.Unknown, null);
		return ndPrec;
	}

	protected override QilNode VisitXPathNamespace(QilUnary ndNmsp)
	{
		CreateSimpleIterator(ndNmsp.Child, "$$$iterNmsp", typeof(NamespaceIterator), XmlILMethods.NmspCreate, XmlILMethods.NmspNext, XmlILMethods.NmspCurrent);
		return ndNmsp;
	}

	protected override QilNode VisitXsltGenerateId(QilUnary ndGenId)
	{
		_helper.LoadQueryRuntime();
		if (ndGenId.Child.XmlType.IsSingleton)
		{
			NestedVisitEnsureStack(ndGenId.Child, typeof(XPathNavigator), isCached: false);
			_helper.Call(XmlILMethods.GenId);
		}
		else
		{
			Label label = _helper.DefineLabel();
			StartNestedIterator(ndGenId.Child, label);
			Visit(ndGenId.Child);
			_iterCurr.EnsureStackNoCache();
			_iterCurr.EnsureItemStorageType(ndGenId.Child.XmlType, typeof(XPathNavigator));
			_helper.Call(XmlILMethods.GenId);
			Label label2 = _helper.DefineLabel();
			_helper.EmitUnconditionalBranch(OpCodes.Br, label2);
			_helper.MarkLabel(label);
			_helper.Emit(OpCodes.Pop);
			_helper.Emit(OpCodes.Ldstr, "");
			_helper.MarkLabel(label2);
			EndNestedIterator(ndGenId.Child);
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(string), isCached: false);
		return ndGenId;
	}

	protected override QilNode VisitXsltInvokeLateBound(QilInvokeLateBound ndInvoke)
	{
		LocalBuilder locBldr = _helper.DeclareLocal("$$$args", typeof(IList<XPathItem>[]));
		QilName name = ndInvoke.Name;
		_helper.LoadQueryContext();
		_helper.Emit(OpCodes.Ldstr, name.LocalName);
		_helper.Emit(OpCodes.Ldstr, name.NamespaceUri);
		_helper.LoadInteger(ndInvoke.Arguments.Count);
		_helper.Emit(OpCodes.Newarr, typeof(IList<XPathItem>));
		_helper.Emit(OpCodes.Stloc, locBldr);
		for (int i = 0; i < ndInvoke.Arguments.Count; i++)
		{
			QilNode nd = ndInvoke.Arguments[i];
			_helper.Emit(OpCodes.Ldloc, locBldr);
			_helper.LoadInteger(i);
			_helper.Emit(OpCodes.Ldelema, typeof(IList<XPathItem>));
			NestedVisitEnsureCache(nd, typeof(XPathItem));
			_iterCurr.EnsureStack();
			_helper.Emit(OpCodes.Stobj, typeof(IList<XPathItem>));
		}
		_helper.Emit(OpCodes.Ldloc, locBldr);
		_helper.Call(XmlILMethods.InvokeXsltLate);
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(XPathItem), isCached: true);
		return ndInvoke;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:RequiresUnreferencedCode", Justification = "Supressing warning about not having the RequiresUnreferencedCode attribute since we added the attribute to this subclass' constructor. This allows us to not have to annotate the whole QilNode hirerarchy.")]
	protected override QilNode VisitXsltInvokeEarlyBound(QilInvokeEarlyBound ndInvoke)
	{
		QilName name = ndInvoke.Name;
		XmlExtensionFunction xmlExtensionFunction = new XmlExtensionFunction(name.LocalName, name.NamespaceUri, ndInvoke.ClrMethod);
		Type clrReturnType = xmlExtensionFunction.ClrReturnType;
		Type storageType = GetStorageType(ndInvoke);
		if (clrReturnType != storageType && !ndInvoke.XmlType.IsEmpty)
		{
			_helper.LoadQueryRuntime();
			_helper.LoadInteger(_helper.StaticData.DeclareXmlType(ndInvoke.XmlType));
		}
		if (!xmlExtensionFunction.Method.IsStatic)
		{
			if (name.NamespaceUri.Length == 0)
			{
				_helper.LoadXsltLibrary();
			}
			else
			{
				_helper.CallGetEarlyBoundObject(_helper.StaticData.DeclareEarlyBound(name.NamespaceUri, xmlExtensionFunction.Method.DeclaringType), xmlExtensionFunction.Method.DeclaringType);
			}
		}
		for (int i = 0; i < ndInvoke.Arguments.Count; i++)
		{
			QilNode qilNode = ndInvoke.Arguments[i];
			XmlQueryType xmlArgumentType = xmlExtensionFunction.GetXmlArgumentType(i);
			Type clrArgumentType = xmlExtensionFunction.GetClrArgumentType(i);
			if (name.NamespaceUri.Length == 0)
			{
				Type itemStorageType = GetItemStorageType(qilNode);
				if (clrArgumentType == XmlILMethods.StorageMethods[itemStorageType].IListType)
				{
					NestedVisitEnsureStack(qilNode, itemStorageType, isCached: true);
				}
				else if (clrArgumentType == XmlILMethods.StorageMethods[typeof(XPathItem)].IListType)
				{
					NestedVisitEnsureStack(qilNode, typeof(XPathItem), isCached: true);
				}
				else if ((qilNode.XmlType.IsSingleton && clrArgumentType == itemStorageType) || qilNode.XmlType.TypeCode == XmlTypeCode.None)
				{
					NestedVisitEnsureStack(qilNode, clrArgumentType, isCached: false);
				}
				else if (qilNode.XmlType.IsSingleton && clrArgumentType == typeof(XPathItem))
				{
					NestedVisitEnsureStack(qilNode, typeof(XPathItem), isCached: false);
				}
			}
			else
			{
				Type storageType2 = GetStorageType(xmlArgumentType);
				if (xmlArgumentType.TypeCode == XmlTypeCode.Item || !clrArgumentType.IsAssignableFrom(storageType2))
				{
					_helper.LoadQueryRuntime();
					_helper.LoadInteger(_helper.StaticData.DeclareXmlType(xmlArgumentType));
					NestedVisitEnsureStack(qilNode, GetItemStorageType(xmlArgumentType), !xmlArgumentType.IsSingleton);
					_helper.TreatAs(storageType2, typeof(object));
					_helper.LoadType(clrArgumentType);
					_helper.Call(XmlILMethods.ChangeTypeXsltArg);
					_helper.TreatAs(typeof(object), clrArgumentType);
				}
				else
				{
					NestedVisitEnsureStack(qilNode, GetItemStorageType(xmlArgumentType), !xmlArgumentType.IsSingleton);
				}
			}
		}
		_helper.Call(xmlExtensionFunction.Method);
		if (ndInvoke.XmlType.IsEmpty)
		{
			_helper.Emit(OpCodes.Ldsfld, XmlILMethods.StorageMethods[typeof(XPathItem)].SeqEmpty);
		}
		else if (clrReturnType != storageType)
		{
			_helper.TreatAs(clrReturnType, typeof(object));
			_helper.Call(XmlILMethods.ChangeTypeXsltResult);
			_helper.TreatAs(typeof(object), storageType);
		}
		else if (name.NamespaceUri.Length != 0 && !clrReturnType.IsValueType)
		{
			Label label = _helper.DefineLabel();
			_helper.Emit(OpCodes.Dup);
			_helper.Emit(OpCodes.Brtrue, label);
			_helper.LoadQueryRuntime();
			_helper.Emit(OpCodes.Ldstr, System.SR.Xslt_ItemNull);
			_helper.Call(XmlILMethods.ThrowException);
			_helper.MarkLabel(label);
		}
		_iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(ndInvoke), !ndInvoke.XmlType.IsSingleton);
		return ndInvoke;
	}

	protected override QilNode VisitXsltCopy(QilBinary ndCopy)
	{
		Label label = _helper.DefineLabel();
		_helper.LoadQueryOutput();
		NestedVisitEnsureStack(ndCopy.Left);
		_helper.Call(XmlILMethods.StartCopy);
		_helper.Emit(OpCodes.Brfalse, label);
		NestedVisit(ndCopy.Right);
		_helper.LoadQueryOutput();
		NestedVisitEnsureStack(ndCopy.Left);
		_helper.Call(XmlILMethods.EndCopy);
		_helper.MarkLabel(label);
		_iterCurr.Storage = StorageDescriptor.None();
		return ndCopy;
	}

	protected override QilNode VisitXsltCopyOf(QilUnary ndCopyOf)
	{
		_helper.LoadQueryOutput();
		NestedVisitEnsureStack(ndCopyOf.Child);
		_helper.Call(XmlILMethods.CopyOf);
		_iterCurr.Storage = StorageDescriptor.None();
		return ndCopyOf;
	}

	protected override QilNode VisitXsltConvert(QilTargetType ndConv)
	{
		XmlQueryType xmlType = ndConv.Source.XmlType;
		XmlQueryType targetType = ndConv.TargetType;
		if (GetXsltConvertMethod(xmlType, targetType, out var meth))
		{
			NestedVisitEnsureStack(ndConv.Source);
		}
		else
		{
			NestedVisitEnsureStack(ndConv.Source, typeof(XPathItem), !xmlType.IsSingleton);
			GetXsltConvertMethod(xmlType.IsSingleton ? XmlQueryTypeFactory.Item : XmlQueryTypeFactory.ItemS, targetType, out meth);
		}
		if (meth != null)
		{
			_helper.Call(meth);
		}
		_iterCurr.Storage = StorageDescriptor.Stack(GetItemStorageType(targetType), !targetType.IsSingleton);
		return ndConv;
	}

	private bool GetXsltConvertMethod(XmlQueryType typSrc, XmlQueryType typDst, out MethodInfo meth)
	{
		meth = null;
		if ((object)typDst == XmlQueryTypeFactory.BooleanX)
		{
			if ((object)typSrc == XmlQueryTypeFactory.Item)
			{
				meth = XmlILMethods.ItemToBool;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.ItemS)
			{
				meth = XmlILMethods.ItemsToBool;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.DateTimeX)
		{
			if ((object)typSrc == XmlQueryTypeFactory.StringX)
			{
				meth = XmlILMethods.StrToDT;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.DecimalX)
		{
			if ((object)typSrc == XmlQueryTypeFactory.DoubleX)
			{
				meth = XmlILMethods.DblToDec;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.DoubleX)
		{
			if ((object)typSrc == XmlQueryTypeFactory.DecimalX)
			{
				meth = XmlILMethods.DecToDbl;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.IntX)
			{
				meth = XmlILMethods.IntToDbl;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.Item)
			{
				meth = XmlILMethods.ItemToDbl;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.ItemS)
			{
				meth = XmlILMethods.ItemsToDbl;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.LongX)
			{
				meth = XmlILMethods.LngToDbl;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.StringX)
			{
				meth = XmlILMethods.StrToDbl;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.IntX)
		{
			if ((object)typSrc == XmlQueryTypeFactory.DoubleX)
			{
				meth = XmlILMethods.DblToInt;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.LongX)
		{
			if ((object)typSrc == XmlQueryTypeFactory.DoubleX)
			{
				meth = XmlILMethods.DblToLng;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.NodeNotRtf)
		{
			if ((object)typSrc == XmlQueryTypeFactory.Item)
			{
				meth = XmlILMethods.ItemToNode;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.ItemS)
			{
				meth = XmlILMethods.ItemsToNode;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.NodeSDod || (object)typDst == XmlQueryTypeFactory.NodeNotRtfS)
		{
			if ((object)typSrc == XmlQueryTypeFactory.Item)
			{
				meth = XmlILMethods.ItemToNodes;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.ItemS)
			{
				meth = XmlILMethods.ItemsToNodes;
			}
		}
		else if ((object)typDst == XmlQueryTypeFactory.StringX)
		{
			if ((object)typSrc == XmlQueryTypeFactory.DateTimeX)
			{
				meth = XmlILMethods.DTToStr;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.DoubleX)
			{
				meth = XmlILMethods.DblToStr;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.Item)
			{
				meth = XmlILMethods.ItemToStr;
			}
			else if ((object)typSrc == XmlQueryTypeFactory.ItemS)
			{
				meth = XmlILMethods.ItemsToStr;
			}
		}
		return meth != null;
	}

	private void SyncToNavigator(LocalBuilder locNav, QilNode ndCtxt)
	{
		_helper.Emit(OpCodes.Ldloc, locNav);
		NestedVisitEnsureStack(ndCtxt);
		_helper.CallSyncToNavigator();
		_helper.Emit(OpCodes.Stloc, locNav);
	}

	private void CreateSimpleIterator(QilNode ndCtxt, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent)
	{
		LocalBuilder localBuilder = _helper.DeclareLocal(iterName, iterType);
		_helper.Emit(OpCodes.Ldloca, localBuilder);
		NestedVisitEnsureStack(ndCtxt);
		_helper.Call(methCreate);
		GenerateSimpleIterator(typeof(XPathNavigator), localBuilder, methNext, methCurrent);
	}

	private void CreateFilteredIterator(QilNode ndCtxt, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent, XmlNodeKindFlags kinds, QilName ndName, TriState orSelf, QilNode ndEnd)
	{
		LocalBuilder localBuilder = _helper.DeclareLocal(iterName, iterType);
		_helper.Emit(OpCodes.Ldloca, localBuilder);
		NestedVisitEnsureStack(ndCtxt);
		LoadSelectFilter(kinds, ndName);
		if (orSelf != TriState.Unknown)
		{
			_helper.LoadBoolean(orSelf == TriState.True);
		}
		if (ndEnd != null)
		{
			NestedVisitEnsureStack(ndEnd);
		}
		_helper.Call(methCreate);
		GenerateSimpleIterator(typeof(XPathNavigator), localBuilder, methNext, methCurrent);
	}

	private void CreateContainerIterator(QilUnary ndDod, string iterName, Type iterType, MethodInfo methCreate, MethodInfo methNext, MethodInfo methCurrent, XmlNodeKindFlags kinds, QilName ndName, TriState orSelf)
	{
		LocalBuilder localBuilder = _helper.DeclareLocal(iterName, iterType);
		QilLoop qilLoop = (QilLoop)ndDod.Child;
		_helper.Emit(OpCodes.Ldloca, localBuilder);
		LoadSelectFilter(kinds, ndName);
		if (orSelf != TriState.Unknown)
		{
			_helper.LoadBoolean(orSelf == TriState.True);
		}
		_helper.Call(methCreate);
		Label label = _helper.DefineLabel();
		StartNestedIterator(qilLoop, label);
		StartBinding(qilLoop.Variable);
		EndBinding(qilLoop.Variable);
		EndNestedIterator(qilLoop.Variable);
		_iterCurr.Storage = _iterNested.Storage;
		GenerateContainerIterator(ndDod, localBuilder, label, methNext, methCurrent, typeof(XPathNavigator));
	}

	private void GenerateSimpleIterator(Type itemStorageType, LocalBuilder locIter, MethodInfo methNext, MethodInfo methCurrent)
	{
		Label label = _helper.DefineLabel();
		_helper.MarkLabel(label);
		_helper.Emit(OpCodes.Ldloca, locIter);
		_helper.Call(methNext);
		_helper.Emit(OpCodes.Brfalse, _iterCurr.GetLabelNext());
		_iterCurr.SetIterator(label, StorageDescriptor.Current(locIter, methCurrent, itemStorageType));
	}

	private void GenerateContainerIterator(QilNode nd, LocalBuilder locIter, Label lblOnEndNested, MethodInfo methNext, MethodInfo methCurrent, Type itemStorageType)
	{
		Label label = _helper.DefineLabel();
		_iterCurr.EnsureNoStackNoCache(nd.XmlType.IsNode ? "$$$navInput" : "$$$itemInput");
		_helper.Emit(OpCodes.Ldloca, locIter);
		_iterCurr.PushValue();
		_helper.EmitUnconditionalBranch(OpCodes.Br, label);
		_helper.MarkLabel(lblOnEndNested);
		_helper.Emit(OpCodes.Ldloca, locIter);
		_helper.Emit(OpCodes.Ldnull);
		_helper.MarkLabel(label);
		_helper.Call(methNext);
		if (nd.XmlType.IsSingleton)
		{
			_helper.LoadInteger(1);
			_helper.Emit(OpCodes.Beq, _iterNested.GetLabelNext());
			_iterCurr.Storage = StorageDescriptor.Current(locIter, methCurrent, itemStorageType);
		}
		else
		{
			_helper.Emit(OpCodes.Switch, new Label[2]
			{
				_iterCurr.GetLabelNext(),
				_iterNested.GetLabelNext()
			});
			_iterCurr.SetIterator(lblOnEndNested, StorageDescriptor.Current(locIter, methCurrent, itemStorageType));
		}
	}

	private GenerateNameType LoadNameAndType(XPathNodeType nodeType, QilNode ndName, bool isStart, bool callChk)
	{
		_helper.LoadQueryOutput();
		GenerateNameType result = GenerateNameType.StackName;
		if (ndName.NodeType == QilNodeType.LiteralQName)
		{
			if (isStart || !callChk)
			{
				QilName qilName = ndName as QilName;
				string prefix = qilName.Prefix;
				string localName = qilName.LocalName;
				string namespaceUri = qilName.NamespaceUri;
				if (qilName.NamespaceUri.Length == 0)
				{
					_helper.Emit(OpCodes.Ldstr, qilName.LocalName);
					return GenerateNameType.LiteralLocalName;
				}
				if (!ValidateNames.ValidateName(prefix, localName, namespaceUri, nodeType, ValidateNames.Flags.CheckPrefixMapping))
				{
					if (isStart)
					{
						_helper.Emit(OpCodes.Ldstr, localName);
						_helper.Emit(OpCodes.Ldstr, namespaceUri);
						_helper.Construct(XmlILConstructors.QName);
						result = GenerateNameType.QName;
					}
				}
				else
				{
					_helper.Emit(OpCodes.Ldstr, prefix);
					_helper.Emit(OpCodes.Ldstr, localName);
					_helper.Emit(OpCodes.Ldstr, namespaceUri);
					result = GenerateNameType.LiteralName;
				}
			}
		}
		else if (isStart)
		{
			if (ndName.NodeType == QilNodeType.NameOf)
			{
				NestedVisitEnsureStack((ndName as QilUnary).Child);
				result = GenerateNameType.CopiedName;
			}
			else if (ndName.NodeType == QilNodeType.StrParseQName)
			{
				VisitStrParseQName(ndName as QilBinary, preservePrefix: true);
				result = (((ndName as QilBinary).Right.XmlType.TypeCode != XmlTypeCode.String) ? GenerateNameType.TagNameAndMappings : GenerateNameType.TagNameAndNamespace);
			}
			else
			{
				NestedVisitEnsureStack(ndName);
				result = GenerateNameType.QName;
			}
		}
		return result;
	}

	private bool TryZeroCompare(QilNodeType relOp, QilNode ndFirst, QilNode ndSecond)
	{
		switch (ndFirst.NodeType)
		{
		case QilNodeType.LiteralInt64:
			if ((int)(QilLiteral)ndFirst != 0)
			{
				return false;
			}
			break;
		case QilNodeType.LiteralInt32:
			if ((int)(QilLiteral)ndFirst != 0)
			{
				return false;
			}
			break;
		case QilNodeType.True:
			relOp = ((relOp == QilNodeType.Eq) ? QilNodeType.Ne : QilNodeType.Eq);
			break;
		default:
			return false;
		case QilNodeType.False:
			break;
		}
		NestedVisitEnsureStack(ndSecond);
		ZeroCompare(relOp, ndSecond.XmlType.TypeCode == XmlTypeCode.Boolean);
		return true;
	}

	private bool TryNameCompare(QilNodeType relOp, QilNode ndFirst, QilNode ndSecond)
	{
		if (ndFirst.NodeType == QilNodeType.NameOf)
		{
			QilNodeType nodeType = ndSecond.NodeType;
			if (nodeType == QilNodeType.LiteralQName || nodeType == QilNodeType.NameOf)
			{
				_helper.LoadQueryRuntime();
				NestedVisitEnsureStack((ndFirst as QilUnary).Child);
				if (ndSecond.NodeType == QilNodeType.LiteralQName)
				{
					QilName qilName = ndSecond as QilName;
					_helper.LoadInteger(_helper.StaticData.DeclareName(qilName.LocalName));
					_helper.LoadInteger(_helper.StaticData.DeclareName(qilName.NamespaceUri));
					_helper.Call(XmlILMethods.QNameEqualLit);
				}
				else
				{
					NestedVisitEnsureStack(ndSecond);
					_helper.Call(XmlILMethods.QNameEqualNav);
				}
				ZeroCompare((relOp == QilNodeType.Eq) ? QilNodeType.Ne : QilNodeType.Eq, isBoolVal: true);
				return true;
			}
		}
		return false;
	}

	private void ClrCompare(QilNodeType relOp, XmlTypeCode code)
	{
		OpCode opcode;
		switch (_iterCurr.CurrentBranchingContext)
		{
		case BranchingContext.OnFalse:
			opcode = ((code == XmlTypeCode.Double || code == XmlTypeCode.Float) ? (relOp switch
			{
				QilNodeType.Gt => OpCodes.Ble_Un, 
				QilNodeType.Ge => OpCodes.Blt_Un, 
				QilNodeType.Lt => OpCodes.Bge_Un, 
				QilNodeType.Le => OpCodes.Bgt_Un, 
				QilNodeType.Eq => OpCodes.Bne_Un, 
				QilNodeType.Ne => OpCodes.Beq, 
				_ => OpCodes.Nop, 
			}) : (relOp switch
			{
				QilNodeType.Gt => OpCodes.Ble, 
				QilNodeType.Ge => OpCodes.Blt, 
				QilNodeType.Lt => OpCodes.Bge, 
				QilNodeType.Le => OpCodes.Bgt, 
				QilNodeType.Eq => OpCodes.Bne_Un, 
				QilNodeType.Ne => OpCodes.Beq, 
				_ => OpCodes.Nop, 
			}));
			_helper.Emit(opcode, _iterCurr.LabelBranch);
			_iterCurr.Storage = StorageDescriptor.None();
			return;
		case BranchingContext.OnTrue:
			opcode = relOp switch
			{
				QilNodeType.Gt => OpCodes.Bgt, 
				QilNodeType.Ge => OpCodes.Bge, 
				QilNodeType.Lt => OpCodes.Blt, 
				QilNodeType.Le => OpCodes.Ble, 
				QilNodeType.Eq => OpCodes.Beq, 
				QilNodeType.Ne => OpCodes.Bne_Un, 
				_ => OpCodes.Nop, 
			};
			_helper.Emit(opcode, _iterCurr.LabelBranch);
			_iterCurr.Storage = StorageDescriptor.None();
			return;
		}
		Label label;
		switch (relOp)
		{
		case QilNodeType.Gt:
			_helper.Emit(OpCodes.Cgt);
			break;
		case QilNodeType.Lt:
			_helper.Emit(OpCodes.Clt);
			break;
		case QilNodeType.Eq:
			_helper.Emit(OpCodes.Ceq);
			break;
		case QilNodeType.Ge:
			opcode = OpCodes.Bge_S;
			goto IL_0207;
		case QilNodeType.Le:
			opcode = OpCodes.Ble_S;
			goto IL_0207;
		case QilNodeType.Ne:
			opcode = OpCodes.Bne_Un_S;
			goto IL_0207;
		default:
			{
				opcode = OpCodes.Nop;
				goto IL_0207;
			}
			IL_0207:
			label = _helper.DefineLabel();
			_helper.Emit(opcode, label);
			_helper.ConvBranchToBool(label, isTrueBranch: true);
			break;
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
	}

	private void ZeroCompare(QilNodeType relOp, bool isBoolVal)
	{
		switch (_iterCurr.CurrentBranchingContext)
		{
		case BranchingContext.OnTrue:
			_helper.Emit((relOp == QilNodeType.Eq) ? OpCodes.Brfalse : OpCodes.Brtrue, _iterCurr.LabelBranch);
			_iterCurr.Storage = StorageDescriptor.None();
			return;
		case BranchingContext.OnFalse:
			_helper.Emit((relOp == QilNodeType.Eq) ? OpCodes.Brtrue : OpCodes.Brfalse, _iterCurr.LabelBranch);
			_iterCurr.Storage = StorageDescriptor.None();
			return;
		}
		if (!isBoolVal || relOp == QilNodeType.Eq)
		{
			Label label = _helper.DefineLabel();
			_helper.Emit((relOp == QilNodeType.Eq) ? OpCodes.Brfalse : OpCodes.Brtrue, label);
			_helper.ConvBranchToBool(label, isTrueBranch: true);
		}
		_iterCurr.Storage = StorageDescriptor.Stack(typeof(bool), isCached: false);
	}

	private void StartWriterLoop(QilNode nd, out bool hasOnEnd, out Label lblOnEnd)
	{
		XmlILConstructInfo xmlILConstructInfo = XmlILConstructInfo.Read(nd);
		hasOnEnd = false;
		lblOnEnd = default(Label);
		if (xmlILConstructInfo.PushToWriterLast && !nd.XmlType.IsSingleton && !_iterCurr.HasLabelNext)
		{
			hasOnEnd = true;
			lblOnEnd = _helper.DefineLabel();
			_iterCurr.SetIterator(lblOnEnd, StorageDescriptor.None());
		}
	}

	private void EndWriterLoop(QilNode nd, bool hasOnEnd, Label lblOnEnd)
	{
		XmlILConstructInfo xmlILConstructInfo = XmlILConstructInfo.Read(nd);
		if (xmlILConstructInfo.PushToWriterLast)
		{
			_iterCurr.Storage = StorageDescriptor.None();
			if (!nd.XmlType.IsSingleton && hasOnEnd)
			{
				_iterCurr.LoopToEnd(lblOnEnd);
			}
		}
	}

	private bool MightHaveNamespacesAfterAttributes(XmlILConstructInfo info)
	{
		if (info != null)
		{
			info = info.ParentElementInfo;
		}
		return info?.MightHaveNamespacesAfterAttributes ?? true;
	}

	private bool ElementCachesAttributes(XmlILConstructInfo info)
	{
		if (!info.MightHaveDuplicateAttributes)
		{
			return info.MightHaveNamespacesAfterAttributes;
		}
		return true;
	}

	private void BeforeStartChecks(QilNode ndCtor)
	{
		switch (XmlILConstructInfo.Read(ndCtor).InitialStates)
		{
		case PossibleXmlStates.WithinSequence:
			_helper.CallStartTree(QilConstructorToNodeType(ndCtor.NodeType));
			break;
		case PossibleXmlStates.EnumAttrs:
		{
			QilNodeType nodeType = ndCtor.NodeType;
			if (nodeType == QilNodeType.ElementCtor || (uint)(nodeType - 83) <= 3u)
			{
				_helper.CallStartElementContent();
			}
			break;
		}
		}
	}

	private void AfterEndChecks(QilNode ndCtor)
	{
		if (XmlILConstructInfo.Read(ndCtor).FinalStates == PossibleXmlStates.WithinSequence)
		{
			_helper.CallEndTree();
		}
	}

	private bool CheckWithinContent(XmlILConstructInfo info)
	{
		PossibleXmlStates initialStates = info.InitialStates;
		if ((uint)(initialStates - 1) <= 2u)
		{
			return false;
		}
		return true;
	}

	private bool CheckEnumAttrs(XmlILConstructInfo info)
	{
		PossibleXmlStates initialStates = info.InitialStates;
		if ((uint)(initialStates - 1) <= 1u)
		{
			return false;
		}
		return true;
	}

	private XPathNodeType QilXmlToXPathNodeType(XmlNodeKindFlags xmlTypes)
	{
		return xmlTypes switch
		{
			XmlNodeKindFlags.Element => XPathNodeType.Element, 
			XmlNodeKindFlags.Attribute => XPathNodeType.Attribute, 
			XmlNodeKindFlags.Text => XPathNodeType.Text, 
			XmlNodeKindFlags.Comment => XPathNodeType.Comment, 
			_ => XPathNodeType.ProcessingInstruction, 
		};
	}

	private XPathNodeType QilConstructorToNodeType(QilNodeType typ)
	{
		return typ switch
		{
			QilNodeType.DocumentCtor => XPathNodeType.Root, 
			QilNodeType.ElementCtor => XPathNodeType.Element, 
			QilNodeType.TextCtor => XPathNodeType.Text, 
			QilNodeType.RawTextCtor => XPathNodeType.Text, 
			QilNodeType.PICtor => XPathNodeType.ProcessingInstruction, 
			QilNodeType.CommentCtor => XPathNodeType.Comment, 
			QilNodeType.AttributeCtor => XPathNodeType.Attribute, 
			QilNodeType.NamespaceDecl => XPathNodeType.Namespace, 
			_ => XPathNodeType.All, 
		};
	}

	private void LoadSelectFilter(XmlNodeKindFlags xmlTypes, QilName ndName)
	{
		if (ndName != null)
		{
			_helper.CallGetNameFilter(_helper.StaticData.DeclareNameFilter(ndName.LocalName, ndName.NamespaceUri));
		}
		else if (IsNodeTypeUnion(xmlTypes))
		{
			if ((xmlTypes & XmlNodeKindFlags.Attribute) != 0)
			{
				_helper.CallGetTypeFilter(XPathNodeType.All);
			}
			else
			{
				_helper.CallGetTypeFilter(XPathNodeType.Attribute);
			}
		}
		else
		{
			_helper.CallGetTypeFilter(QilXmlToXPathNodeType(xmlTypes));
		}
	}

	private static bool IsNodeTypeUnion(XmlNodeKindFlags xmlTypes)
	{
		return (xmlTypes & (xmlTypes - 1)) != 0;
	}

	[MemberNotNull("_iterCurr")]
	private void StartNestedIterator(QilNode nd)
	{
		IteratorDescriptor iterCurr = _iterCurr;
		if (iterCurr == null)
		{
			_iterCurr = new IteratorDescriptor(_helper);
		}
		else
		{
			_iterCurr = new IteratorDescriptor(iterCurr);
		}
		_iterNested = null;
	}

	private void StartNestedIterator(QilNode nd, Label lblOnEnd)
	{
		StartNestedIterator(nd);
		_iterCurr.SetIterator(lblOnEnd, StorageDescriptor.None());
	}

	private void EndNestedIterator(QilNode nd)
	{
		if (_iterCurr.IsBranching && _iterCurr.Storage.Location != 0)
		{
			_iterCurr.EnsureItemStorageType(nd.XmlType, typeof(bool));
			_iterCurr.EnsureStackNoCache();
			if (_iterCurr.CurrentBranchingContext == BranchingContext.OnTrue)
			{
				_helper.Emit(OpCodes.Brtrue, _iterCurr.LabelBranch);
			}
			else
			{
				_helper.Emit(OpCodes.Brfalse, _iterCurr.LabelBranch);
			}
			_iterCurr.Storage = StorageDescriptor.None();
		}
		_iterNested = _iterCurr;
		_iterCurr = _iterCurr.ParentIterator;
	}

	private void NestedVisit(QilNode nd, Type itemStorageType, bool isCached)
	{
		if (XmlILConstructInfo.Read(nd).PushToWriterLast)
		{
			StartNestedIterator(nd);
			Visit(nd);
			EndNestedIterator(nd);
			_iterCurr.Storage = StorageDescriptor.None();
		}
		else if (!isCached && nd.XmlType.IsSingleton)
		{
			StartNestedIterator(nd);
			Visit(nd);
			_iterCurr.EnsureNoCache();
			_iterCurr.EnsureItemStorageType(nd.XmlType, itemStorageType);
			EndNestedIterator(nd);
			_iterCurr.Storage = _iterNested.Storage;
		}
		else
		{
			NestedVisitEnsureCache(nd, itemStorageType);
		}
	}

	private void NestedVisit(QilNode nd)
	{
		NestedVisit(nd, GetItemStorageType(nd), !nd.XmlType.IsSingleton);
	}

	private void NestedVisit(QilNode nd, Label lblOnEnd)
	{
		StartNestedIterator(nd, lblOnEnd);
		Visit(nd);
		_iterCurr.EnsureNoCache();
		_iterCurr.EnsureItemStorageType(nd.XmlType, GetItemStorageType(nd));
		EndNestedIterator(nd);
		_iterCurr.Storage = _iterNested.Storage;
	}

	private void NestedVisitEnsureStack(QilNode nd)
	{
		NestedVisit(nd);
		_iterCurr.EnsureStack();
	}

	private void NestedVisitEnsureStack(QilNode ndLeft, QilNode ndRight)
	{
		NestedVisitEnsureStack(ndLeft);
		NestedVisitEnsureStack(ndRight);
	}

	private void NestedVisitEnsureStack(QilNode nd, Type itemStorageType, bool isCached)
	{
		NestedVisit(nd, itemStorageType, isCached);
		_iterCurr.EnsureStack();
	}

	private void NestedVisitEnsureLocal(QilNode nd, LocalBuilder loc)
	{
		NestedVisit(nd);
		_iterCurr.EnsureLocal(loc);
	}

	private void NestedVisitWithBranch(QilNode nd, BranchingContext brctxt, Label lblBranch)
	{
		StartNestedIterator(nd);
		_iterCurr.SetBranching(brctxt, lblBranch);
		Visit(nd);
		EndNestedIterator(nd);
		_iterCurr.Storage = StorageDescriptor.None();
	}

	private void NestedVisitEnsureCache(QilNode nd, Type itemStorageType)
	{
		bool flag = CachesResult(nd);
		Label lblOnEnd = _helper.DefineLabel();
		if (flag)
		{
			StartNestedIterator(nd);
			Visit(nd);
			EndNestedIterator(nd);
			_iterCurr.Storage = _iterNested.Storage;
			if (_iterCurr.Storage.ItemStorageType == itemStorageType)
			{
				return;
			}
			if (_iterCurr.Storage.ItemStorageType == typeof(XPathNavigator) || itemStorageType == typeof(XPathNavigator))
			{
				_iterCurr.EnsureItemStorageType(nd.XmlType, itemStorageType);
				return;
			}
			_iterCurr.EnsureNoStack("$$$cacheResult");
		}
		Type type = ((GetItemStorageType(nd) == typeof(XPathNavigator)) ? typeof(XPathNavigator) : itemStorageType);
		XmlILStorageMethods xmlILStorageMethods = XmlILMethods.StorageMethods[type];
		LocalBuilder localBuilder = _helper.DeclareLocal("$$$cache", xmlILStorageMethods.SeqType);
		_helper.Emit(OpCodes.Ldloc, localBuilder);
		if (nd.XmlType.IsSingleton)
		{
			NestedVisitEnsureStack(nd, type, isCached: false);
			_helper.Call(xmlILStorageMethods.SeqReuseSgl);
			_helper.Emit(OpCodes.Stloc, localBuilder);
		}
		else
		{
			_helper.Call(xmlILStorageMethods.SeqReuse);
			_helper.Emit(OpCodes.Stloc, localBuilder);
			_helper.Emit(OpCodes.Ldloc, localBuilder);
			StartNestedIterator(nd, lblOnEnd);
			if (flag)
			{
				_iterCurr.Storage = _iterCurr.ParentIterator.Storage;
			}
			else
			{
				Visit(nd);
			}
			_iterCurr.EnsureItemStorageType(nd.XmlType, type);
			_iterCurr.EnsureStackNoCache();
			_helper.Call(xmlILStorageMethods.SeqAdd);
			_helper.Emit(OpCodes.Ldloc, localBuilder);
			_iterCurr.LoopToEnd(lblOnEnd);
			EndNestedIterator(nd);
			_helper.Emit(OpCodes.Pop);
		}
		_iterCurr.Storage = StorageDescriptor.Local(localBuilder, itemStorageType, isCached: true);
	}

	private bool CachesResult(QilNode nd)
	{
		switch (nd.NodeType)
		{
		case QilNodeType.Let:
		case QilNodeType.Parameter:
		case QilNodeType.Invoke:
		case QilNodeType.XsltInvokeLateBound:
		case QilNodeType.XsltInvokeEarlyBound:
			return !nd.XmlType.IsSingleton;
		case QilNodeType.Filter:
		{
			OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(nd);
			return optimizerPatterns.MatchesPattern(OptimizerPatternName.EqualityIndex);
		}
		case QilNodeType.DocOrderDistinct:
		{
			if (nd.XmlType.IsSingleton)
			{
				return false;
			}
			OptimizerPatterns optimizerPatterns = OptimizerPatterns.Read(nd);
			if (!optimizerPatterns.MatchesPattern(OptimizerPatternName.JoinAndDod))
			{
				return !optimizerPatterns.MatchesPattern(OptimizerPatternName.DodReverse);
			}
			return false;
		}
		case QilNodeType.TypeAssert:
		{
			QilTargetType qilTargetType = (QilTargetType)nd;
			if (CachesResult(qilTargetType.Source))
			{
				return GetItemStorageType(qilTargetType.Source) == GetItemStorageType(qilTargetType);
			}
			return false;
		}
		default:
			return false;
		}
	}

	private Type GetStorageType(QilNode nd)
	{
		return XmlILTypeHelper.GetStorageType(nd.XmlType);
	}

	private Type GetStorageType(XmlQueryType typ)
	{
		return XmlILTypeHelper.GetStorageType(typ);
	}

	private Type GetItemStorageType(QilNode nd)
	{
		return XmlILTypeHelper.GetStorageType(nd.XmlType.Prime);
	}

	private Type GetItemStorageType(XmlQueryType typ)
	{
		return XmlILTypeHelper.GetStorageType(typ.Prime);
	}
}
