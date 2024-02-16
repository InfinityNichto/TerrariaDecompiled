using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

public sealed class ControlFlowBuilder
{
	internal readonly struct BranchInfo
	{
		internal readonly int ILOffset;

		internal readonly LabelHandle Label;

		private readonly byte _opCode;

		internal ILOpCode OpCode => (ILOpCode)_opCode;

		internal BranchInfo(int ilOffset, LabelHandle label, ILOpCode opCode)
		{
			ILOffset = ilOffset;
			Label = label;
			_opCode = (byte)opCode;
		}

		internal int GetBranchDistance(ImmutableArray<int>.Builder labels, ILOpCode branchOpCode, int branchILOffset, bool isShortBranch)
		{
			int num = labels[Label.Id - 1];
			if (num < 0)
			{
				Throw.InvalidOperation_LabelNotMarked(Label.Id);
			}
			int num2 = 1 + (isShortBranch ? 1 : 4);
			int num3 = num - (ILOffset + num2);
			if (isShortBranch && (sbyte)num3 != num3)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.DistanceBetweenInstructionAndLabelTooBig, branchOpCode, branchILOffset, num3));
			}
			return num3;
		}
	}

	internal readonly struct ExceptionHandlerInfo
	{
		public readonly ExceptionRegionKind Kind;

		public readonly LabelHandle TryStart;

		public readonly LabelHandle TryEnd;

		public readonly LabelHandle HandlerStart;

		public readonly LabelHandle HandlerEnd;

		public readonly LabelHandle FilterStart;

		public readonly EntityHandle CatchType;

		public ExceptionHandlerInfo(ExceptionRegionKind kind, LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, LabelHandle filterStart, EntityHandle catchType)
		{
			Kind = kind;
			TryStart = tryStart;
			TryEnd = tryEnd;
			HandlerStart = handlerStart;
			HandlerEnd = handlerEnd;
			FilterStart = filterStart;
			CatchType = catchType;
		}
	}

	private readonly ImmutableArray<BranchInfo>.Builder _branches;

	private readonly ImmutableArray<int>.Builder _labels;

	private ImmutableArray<ExceptionHandlerInfo>.Builder _lazyExceptionHandlers;

	internal IEnumerable<BranchInfo> Branches => _branches;

	internal IEnumerable<int> Labels => _labels;

	internal int BranchCount => _branches.Count;

	internal int ExceptionHandlerCount => _lazyExceptionHandlers?.Count ?? 0;

	public ControlFlowBuilder()
	{
		_branches = ImmutableArray.CreateBuilder<BranchInfo>();
		_labels = ImmutableArray.CreateBuilder<int>();
	}

	internal void Clear()
	{
		_branches.Clear();
		_labels.Clear();
		_lazyExceptionHandlers?.Clear();
	}

	internal LabelHandle AddLabel()
	{
		_labels.Add(-1);
		return new LabelHandle(_labels.Count);
	}

	internal void AddBranch(int ilOffset, LabelHandle label, ILOpCode opCode)
	{
		ValidateLabel(label, "label");
		_branches.Add(new BranchInfo(ilOffset, label, opCode));
	}

	internal void MarkLabel(int ilOffset, LabelHandle label)
	{
		ValidateLabel(label, "label");
		_labels[label.Id - 1] = ilOffset;
	}

	private int GetLabelOffsetChecked(LabelHandle label)
	{
		int num = _labels[label.Id - 1];
		if (num < 0)
		{
			Throw.InvalidOperation_LabelNotMarked(label.Id);
		}
		return num;
	}

	private void ValidateLabel(LabelHandle label, string parameterName)
	{
		if (label.IsNil)
		{
			Throw.ArgumentNull(parameterName);
		}
		if (label.Id > _labels.Count)
		{
			Throw.LabelDoesntBelongToBuilder(parameterName);
		}
	}

	public void AddFinallyRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd)
	{
		AddExceptionRegion(ExceptionRegionKind.Finally, tryStart, tryEnd, handlerStart, handlerEnd);
	}

	public void AddFaultRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd)
	{
		AddExceptionRegion(ExceptionRegionKind.Fault, tryStart, tryEnd, handlerStart, handlerEnd);
	}

	public void AddCatchRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, EntityHandle catchType)
	{
		if (!ExceptionRegionEncoder.IsValidCatchTypeHandle(catchType))
		{
			Throw.InvalidArgument_Handle("catchType");
		}
		AddExceptionRegion(ExceptionRegionKind.Catch, tryStart, tryEnd, handlerStart, handlerEnd, default(LabelHandle), catchType);
	}

	public void AddFilterRegion(LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, LabelHandle filterStart)
	{
		ValidateLabel(filterStart, "filterStart");
		AddExceptionRegion(ExceptionRegionKind.Filter, tryStart, tryEnd, handlerStart, handlerEnd, filterStart);
	}

	private void AddExceptionRegion(ExceptionRegionKind kind, LabelHandle tryStart, LabelHandle tryEnd, LabelHandle handlerStart, LabelHandle handlerEnd, LabelHandle filterStart = default(LabelHandle), EntityHandle catchType = default(EntityHandle))
	{
		ValidateLabel(tryStart, "tryStart");
		ValidateLabel(tryEnd, "tryEnd");
		ValidateLabel(handlerStart, "handlerStart");
		ValidateLabel(handlerEnd, "handlerEnd");
		if (_lazyExceptionHandlers == null)
		{
			_lazyExceptionHandlers = ImmutableArray.CreateBuilder<ExceptionHandlerInfo>();
		}
		_lazyExceptionHandlers.Add(new ExceptionHandlerInfo(kind, tryStart, tryEnd, handlerStart, handlerEnd, filterStart, catchType));
	}

	internal void CopyCodeAndFixupBranches(BlobBuilder srcBuilder, BlobBuilder dstBuilder)
	{
		BranchInfo branchInfo = _branches[0];
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (Blob blob in srcBuilder.GetBlobs())
		{
			while (true)
			{
				int num4 = Math.Min(branchInfo.ILOffset - num2, blob.Length - num3);
				dstBuilder.WriteBytes(blob.Buffer, num3, num4);
				num2 += num4;
				num3 += num4;
				if (num3 == blob.Length)
				{
					num3 = 0;
					break;
				}
				int branchOperandSize = branchInfo.OpCode.GetBranchOperandSize();
				bool flag = branchOperandSize == 1;
				dstBuilder.WriteByte(blob.Buffer[num3]);
				int branchDistance = branchInfo.GetBranchDistance(_labels, branchInfo.OpCode, num2, flag);
				if (flag)
				{
					dstBuilder.WriteSByte((sbyte)branchDistance);
				}
				else
				{
					dstBuilder.WriteInt32(branchDistance);
				}
				num2 += 1 + branchOperandSize;
				num++;
				branchInfo = ((num != _branches.Count) ? _branches[num] : new BranchInfo(int.MaxValue, default(LabelHandle), ILOpCode.Nop));
				if (num3 == blob.Length - 1)
				{
					num3 = branchOperandSize;
					break;
				}
				num3 += 1 + branchOperandSize;
			}
		}
	}

	internal void SerializeExceptionTable(BlobBuilder builder)
	{
		if (_lazyExceptionHandlers == null || _lazyExceptionHandlers.Count == 0)
		{
			return;
		}
		ExceptionRegionEncoder exceptionRegionEncoder = ExceptionRegionEncoder.SerializeTableHeader(builder, _lazyExceptionHandlers.Count, HasSmallExceptionRegions());
		foreach (ExceptionHandlerInfo lazyExceptionHandler in _lazyExceptionHandlers)
		{
			int labelOffsetChecked = GetLabelOffsetChecked(lazyExceptionHandler.TryStart);
			int labelOffsetChecked2 = GetLabelOffsetChecked(lazyExceptionHandler.TryEnd);
			int labelOffsetChecked3 = GetLabelOffsetChecked(lazyExceptionHandler.HandlerStart);
			int labelOffsetChecked4 = GetLabelOffsetChecked(lazyExceptionHandler.HandlerEnd);
			if (labelOffsetChecked > labelOffsetChecked2)
			{
				Throw.InvalidOperation(System.SR.Format(System.SR.InvalidExceptionRegionBounds, labelOffsetChecked, labelOffsetChecked2));
			}
			if (labelOffsetChecked3 > labelOffsetChecked4)
			{
				Throw.InvalidOperation(System.SR.Format(System.SR.InvalidExceptionRegionBounds, labelOffsetChecked3, labelOffsetChecked4));
			}
			int catchTokenOrOffset = lazyExceptionHandler.Kind switch
			{
				ExceptionRegionKind.Catch => MetadataTokens.GetToken(lazyExceptionHandler.CatchType), 
				ExceptionRegionKind.Filter => GetLabelOffsetChecked(lazyExceptionHandler.FilterStart), 
				_ => 0, 
			};
			exceptionRegionEncoder.AddUnchecked(lazyExceptionHandler.Kind, labelOffsetChecked, labelOffsetChecked2 - labelOffsetChecked, labelOffsetChecked3, labelOffsetChecked4 - labelOffsetChecked3, catchTokenOrOffset);
		}
	}

	private bool HasSmallExceptionRegions()
	{
		if (!ExceptionRegionEncoder.IsSmallRegionCount(_lazyExceptionHandlers.Count))
		{
			return false;
		}
		foreach (ExceptionHandlerInfo lazyExceptionHandler in _lazyExceptionHandlers)
		{
			if (!ExceptionRegionEncoder.IsSmallExceptionRegionFromBounds(GetLabelOffsetChecked(lazyExceptionHandler.TryStart), GetLabelOffsetChecked(lazyExceptionHandler.TryEnd)) || !ExceptionRegionEncoder.IsSmallExceptionRegionFromBounds(GetLabelOffsetChecked(lazyExceptionHandler.HandlerStart), GetLabelOffsetChecked(lazyExceptionHandler.HandlerEnd)))
			{
				return false;
			}
		}
		return true;
	}
}
