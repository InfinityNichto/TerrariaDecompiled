using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit;

public class ILGenerator
{
	private int m_length;

	private byte[] m_ILStream;

	private int[] m_labelList;

	private int m_labelCount;

	private __FixupData[] m_fixupData;

	private int m_fixupCount;

	private int[] m_RelocFixupList;

	private int m_RelocFixupCount;

	private int m_exceptionCount;

	private int m_currExcStackCount;

	private __ExceptionInfo[] m_exceptions;

	private __ExceptionInfo[] m_currExcStack;

	internal ScopeTree m_ScopeTree;

	internal MethodInfo m_methodBuilder;

	internal int m_localCount;

	internal SignatureHelper m_localSignature;

	private int m_maxStackSize;

	private int m_maxMidStack;

	private int m_maxMidStackCur;

	internal int CurrExcStackCount => m_currExcStackCount;

	internal __ExceptionInfo[]? CurrExcStack => m_currExcStack;

	public virtual int ILOffset => m_length;

	internal static T[] EnlargeArray<T>(T[] incoming)
	{
		return EnlargeArray(incoming, incoming.Length * 2);
	}

	internal static T[] EnlargeArray<T>(T[] incoming, int requiredSize)
	{
		T[] array = new T[requiredSize];
		Array.Copy(incoming, array, incoming.Length);
		return array;
	}

	internal ILGenerator(MethodInfo methodBuilder)
		: this(methodBuilder, 64)
	{
	}

	internal ILGenerator(MethodInfo methodBuilder, int size)
	{
		m_ILStream = new byte[Math.Max(size, 16)];
		m_ScopeTree = new ScopeTree();
		m_methodBuilder = methodBuilder;
		m_localSignature = SignatureHelper.GetLocalVarSigHelper((m_methodBuilder as MethodBuilder)?.GetTypeBuilder().Module);
	}

	internal virtual void RecordTokenFixup()
	{
		if (m_RelocFixupList == null)
		{
			m_RelocFixupList = new int[8];
		}
		else if (m_RelocFixupList.Length <= m_RelocFixupCount)
		{
			m_RelocFixupList = EnlargeArray(m_RelocFixupList);
		}
		m_RelocFixupList[m_RelocFixupCount++] = m_length;
	}

	internal void InternalEmit(OpCode opcode)
	{
		short value = opcode.Value;
		if (opcode.Size != 1)
		{
			BinaryPrimitives.WriteInt16BigEndian(m_ILStream.AsSpan(m_length), value);
			m_length += 2;
		}
		else
		{
			m_ILStream[m_length++] = (byte)value;
		}
		UpdateStackSize(opcode, opcode.StackChange());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void UpdateStackSize(OpCode opcode, int stackchange)
	{
		m_maxMidStackCur += stackchange;
		if (m_maxMidStackCur > m_maxMidStack)
		{
			m_maxMidStack = m_maxMidStackCur;
		}
		else if (m_maxMidStackCur < 0)
		{
			m_maxMidStackCur = 0;
		}
		if (opcode.EndsUncondJmpBlk())
		{
			m_maxStackSize += m_maxMidStack;
			m_maxMidStack = 0;
			m_maxMidStackCur = 0;
		}
	}

	private int GetMethodToken(MethodBase method, Type[] optionalParameterTypes, bool useMethodDef)
	{
		return ((ModuleBuilder)m_methodBuilder.Module).GetMethodTokenInternal(method, optionalParameterTypes, useMethodDef);
	}

	internal SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
	{
		return GetMemberRefSignature(call, returnType, parameterTypes, null, null, optionalParameterTypes);
	}

	internal virtual SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, Type[] optionalParameterTypes)
	{
		return GetMemberRefSignature(call, returnType, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, optionalParameterTypes, 0);
	}

	private SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, Type[] optionalParameterTypes, int cGenericParameters)
	{
		return ((ModuleBuilder)m_methodBuilder.Module).GetMemberRefSignature(call, returnType, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, optionalParameterTypes, cGenericParameters);
	}

	internal byte[] BakeByteArray()
	{
		if (m_currExcStackCount != 0)
		{
			throw new ArgumentException(SR.Argument_UnclosedExceptionBlock);
		}
		if (m_length == 0)
		{
			return null;
		}
		byte[] array = new byte[m_length];
		Array.Copy(m_ILStream, array, m_length);
		for (int i = 0; i < m_fixupCount; i++)
		{
			__FixupData _FixupData = m_fixupData[i];
			int num = GetLabelPos(_FixupData.m_fixupLabel) - (_FixupData.m_fixupPos + _FixupData.m_fixupInstSize);
			if (_FixupData.m_fixupInstSize == 1)
			{
				if (num < -128 || num > 127)
				{
					throw new NotSupportedException(SR.Format(SR.NotSupported_IllegalOneByteBranch, _FixupData.m_fixupPos, num));
				}
				array[_FixupData.m_fixupPos] = (byte)num;
			}
			else
			{
				BinaryPrimitives.WriteInt32LittleEndian(array.AsSpan(_FixupData.m_fixupPos), num);
			}
		}
		return array;
	}

	internal __ExceptionInfo[] GetExceptions()
	{
		if (m_currExcStackCount != 0)
		{
			throw new NotSupportedException(SR.Argument_UnclosedExceptionBlock);
		}
		if (m_exceptionCount == 0)
		{
			return null;
		}
		__ExceptionInfo[] array = new __ExceptionInfo[m_exceptionCount];
		Array.Copy(m_exceptions, array, m_exceptionCount);
		SortExceptions(array);
		return array;
	}

	internal void EnsureCapacity(int size)
	{
		if (m_length + size >= m_ILStream.Length)
		{
			IncreaseCapacity(size);
		}
	}

	private void IncreaseCapacity(int size)
	{
		byte[] array = new byte[Math.Max(m_ILStream.Length * 2, m_length + size)];
		Array.Copy(m_ILStream, array, m_ILStream.Length);
		m_ILStream = array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void PutInteger4(int value)
	{
		BinaryPrimitives.WriteInt32LittleEndian(m_ILStream.AsSpan(m_length), value);
		m_length += 4;
	}

	private int GetLabelPos(Label lbl)
	{
		int labelValue = lbl.GetLabelValue();
		if (labelValue < 0 || labelValue >= m_labelCount || m_labelList == null)
		{
			throw new ArgumentException(SR.Argument_BadLabel);
		}
		if (m_labelList[labelValue] < 0)
		{
			throw new ArgumentException(SR.Argument_BadLabelContent);
		}
		return m_labelList[labelValue];
	}

	private void AddFixup(Label lbl, int pos, int instSize)
	{
		if (m_fixupData == null)
		{
			m_fixupData = new __FixupData[8];
		}
		else if (m_fixupData.Length <= m_fixupCount)
		{
			m_fixupData = EnlargeArray(m_fixupData);
		}
		m_fixupData[m_fixupCount++] = new __FixupData
		{
			m_fixupPos = pos,
			m_fixupLabel = lbl,
			m_fixupInstSize = instSize
		};
	}

	internal int GetMaxStackSize()
	{
		return m_maxStackSize;
	}

	private static void SortExceptions(__ExceptionInfo[] exceptions)
	{
		for (int i = 0; i < exceptions.Length; i++)
		{
			int num = i;
			for (int j = i + 1; j < exceptions.Length; j++)
			{
				if (exceptions[num].IsInner(exceptions[j]))
				{
					num = j;
				}
			}
			__ExceptionInfo _ExceptionInfo = exceptions[i];
			exceptions[i] = exceptions[num];
			exceptions[num] = _ExceptionInfo;
		}
	}

	internal int[] GetTokenFixups()
	{
		if (m_RelocFixupCount == 0)
		{
			return null;
		}
		int[] array = new int[m_RelocFixupCount];
		Array.Copy(m_RelocFixupList, array, m_RelocFixupCount);
		return array;
	}

	public virtual void Emit(OpCode opcode)
	{
		EnsureCapacity(3);
		InternalEmit(opcode);
	}

	public virtual void Emit(OpCode opcode, byte arg)
	{
		EnsureCapacity(4);
		InternalEmit(opcode);
		m_ILStream[m_length++] = arg;
	}

	[CLSCompliant(false)]
	public void Emit(OpCode opcode, sbyte arg)
	{
		EnsureCapacity(4);
		InternalEmit(opcode);
		m_ILStream[m_length++] = (byte)arg;
	}

	public virtual void Emit(OpCode opcode, short arg)
	{
		EnsureCapacity(5);
		InternalEmit(opcode);
		BinaryPrimitives.WriteInt16LittleEndian(m_ILStream.AsSpan(m_length), arg);
		m_length += 2;
	}

	public virtual void Emit(OpCode opcode, int arg)
	{
		if (opcode.Equals(OpCodes.Ldc_I4))
		{
			OpCode opCode;
			switch (arg)
			{
			case -1:
				opCode = OpCodes.Ldc_I4_M1;
				goto IL_009b;
			case 0:
				opCode = OpCodes.Ldc_I4_0;
				goto IL_009b;
			case 1:
				opCode = OpCodes.Ldc_I4_1;
				goto IL_009b;
			case 2:
				opCode = OpCodes.Ldc_I4_2;
				goto IL_009b;
			case 3:
				opCode = OpCodes.Ldc_I4_3;
				goto IL_009b;
			case 4:
				opCode = OpCodes.Ldc_I4_4;
				goto IL_009b;
			case 5:
				opCode = OpCodes.Ldc_I4_5;
				goto IL_009b;
			case 6:
				opCode = OpCodes.Ldc_I4_6;
				goto IL_009b;
			case 7:
				opCode = OpCodes.Ldc_I4_7;
				goto IL_009b;
			case 8:
				{
					opCode = OpCodes.Ldc_I4_8;
					goto IL_009b;
				}
				IL_009b:
				opcode = opCode;
				Emit(opcode);
				return;
			}
			if (arg >= -128 && arg <= 127)
			{
				Emit(OpCodes.Ldc_I4_S, (sbyte)arg);
				return;
			}
		}
		else if (opcode.Equals(OpCodes.Ldarg))
		{
			if ((uint)arg <= 3u)
			{
				Emit(arg switch
				{
					0 => OpCodes.Ldarg_0, 
					1 => OpCodes.Ldarg_1, 
					2 => OpCodes.Ldarg_2, 
					_ => OpCodes.Ldarg_3, 
				});
				return;
			}
			if ((uint)arg <= 255u)
			{
				Emit(OpCodes.Ldarg_S, (byte)arg);
				return;
			}
			if ((uint)arg <= 65535u)
			{
				Emit(OpCodes.Ldarg, (short)arg);
				return;
			}
		}
		else if (opcode.Equals(OpCodes.Ldarga))
		{
			if ((uint)arg <= 255u)
			{
				Emit(OpCodes.Ldarga_S, (byte)arg);
				return;
			}
			if ((uint)arg <= 65535u)
			{
				Emit(OpCodes.Ldarga, (short)arg);
				return;
			}
		}
		else if (opcode.Equals(OpCodes.Starg))
		{
			if ((uint)arg <= 255u)
			{
				Emit(OpCodes.Starg_S, (byte)arg);
				return;
			}
			if ((uint)arg <= 65535u)
			{
				Emit(OpCodes.Starg, (short)arg);
				return;
			}
		}
		EnsureCapacity(7);
		InternalEmit(opcode);
		PutInteger4(arg);
	}

	public virtual void Emit(OpCode opcode, MethodInfo meth)
	{
		if (meth == null)
		{
			throw new ArgumentNullException("meth");
		}
		if (opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj))
		{
			EmitCall(opcode, meth, null);
			return;
		}
		bool useMethodDef = opcode.Equals(OpCodes.Ldtoken) || opcode.Equals(OpCodes.Ldftn) || opcode.Equals(OpCodes.Ldvirtftn);
		int methodToken = GetMethodToken(meth, null, useMethodDef);
		EnsureCapacity(7);
		InternalEmit(opcode);
		UpdateStackSize(opcode, 0);
		RecordTokenFixup();
		PutInteger4(methodToken);
	}

	public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes)
	{
		int num = 0;
		if (optionalParameterTypes != null && (callingConvention & CallingConventions.VarArgs) == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
		}
		ModuleBuilder moduleBuilder = (ModuleBuilder)m_methodBuilder.Module;
		SignatureHelper memberRefSignature = GetMemberRefSignature(callingConvention, returnType, parameterTypes, optionalParameterTypes);
		EnsureCapacity(7);
		Emit(OpCodes.Calli);
		if (returnType != typeof(void))
		{
			num++;
		}
		if (parameterTypes != null)
		{
			num -= parameterTypes.Length;
		}
		if (optionalParameterTypes != null)
		{
			num -= optionalParameterTypes.Length;
		}
		if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
		{
			num--;
		}
		num--;
		UpdateStackSize(OpCodes.Calli, num);
		RecordTokenFixup();
		PutInteger4(moduleBuilder.GetSignatureToken(memberRefSignature));
	}

	public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
	{
		int num = 0;
		int num2 = 0;
		ModuleBuilder moduleBuilder = (ModuleBuilder)m_methodBuilder.Module;
		if (parameterTypes != null)
		{
			num2 = parameterTypes.Length;
		}
		SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(moduleBuilder, unmanagedCallConv, returnType);
		if (parameterTypes != null)
		{
			for (int i = 0; i < num2; i++)
			{
				methodSigHelper.AddArgument(parameterTypes[i]);
			}
		}
		if (returnType != typeof(void))
		{
			num++;
		}
		if (parameterTypes != null)
		{
			num -= num2;
		}
		num--;
		UpdateStackSize(OpCodes.Calli, num);
		EnsureCapacity(7);
		Emit(OpCodes.Calli);
		RecordTokenFixup();
		PutInteger4(moduleBuilder.GetSignatureToken(methodSigHelper));
	}

	public virtual void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
	{
		if (methodInfo == null)
		{
			throw new ArgumentNullException("methodInfo");
		}
		if (!opcode.Equals(OpCodes.Call) && !opcode.Equals(OpCodes.Callvirt) && !opcode.Equals(OpCodes.Newobj))
		{
			throw new ArgumentException(SR.Argument_NotMethodCallOpcode, "opcode");
		}
		int num = 0;
		int methodToken = GetMethodToken(methodInfo, optionalParameterTypes, useMethodDef: false);
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (methodInfo.ReturnType != typeof(void))
		{
			num++;
		}
		Type[] parameterTypes = methodInfo.GetParameterTypes();
		if (parameterTypes != null)
		{
			num -= parameterTypes.Length;
		}
		if (!(methodInfo is SymbolMethod) && !methodInfo.IsStatic && !opcode.Equals(OpCodes.Newobj))
		{
			num--;
		}
		if (optionalParameterTypes != null)
		{
			num -= optionalParameterTypes.Length;
		}
		UpdateStackSize(opcode, num);
		RecordTokenFixup();
		PutInteger4(methodToken);
	}

	public virtual void Emit(OpCode opcode, SignatureHelper signature)
	{
		if (signature == null)
		{
			throw new ArgumentNullException("signature");
		}
		int num = 0;
		ModuleBuilder moduleBuilder = (ModuleBuilder)m_methodBuilder.Module;
		int signatureToken = moduleBuilder.GetSignatureToken(signature);
		int value = signatureToken;
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
		{
			num -= signature.ArgumentCount;
			num--;
			UpdateStackSize(opcode, num);
		}
		RecordTokenFixup();
		PutInteger4(value);
	}

	public virtual void Emit(OpCode opcode, ConstructorInfo con)
	{
		if (con == null)
		{
			throw new ArgumentNullException("con");
		}
		int num = 0;
		int methodToken = GetMethodToken(con, null, useMethodDef: true);
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (opcode.StackBehaviourPush == StackBehaviour.Varpush)
		{
			num++;
		}
		if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
		{
			Type[] parameterTypes = con.GetParameterTypes();
			if (parameterTypes != null)
			{
				num -= parameterTypes.Length;
			}
		}
		UpdateStackSize(opcode, num);
		RecordTokenFixup();
		PutInteger4(methodToken);
	}

	public virtual void Emit(OpCode opcode, Type cls)
	{
		ModuleBuilder moduleBuilder = (ModuleBuilder)m_methodBuilder.Module;
		int value = ((!(opcode == OpCodes.Ldtoken) || !(cls != null) || !cls.IsGenericTypeDefinition) ? moduleBuilder.GetTypeTokenInternal(cls) : moduleBuilder.GetTypeToken(cls));
		EnsureCapacity(7);
		InternalEmit(opcode);
		RecordTokenFixup();
		PutInteger4(value);
	}

	public virtual void Emit(OpCode opcode, long arg)
	{
		EnsureCapacity(11);
		InternalEmit(opcode);
		BinaryPrimitives.WriteInt64LittleEndian(m_ILStream.AsSpan(m_length), arg);
		m_length += 8;
	}

	public virtual void Emit(OpCode opcode, float arg)
	{
		EnsureCapacity(7);
		InternalEmit(opcode);
		BinaryPrimitives.WriteInt32LittleEndian(m_ILStream.AsSpan(m_length), BitConverter.SingleToInt32Bits(arg));
		m_length += 4;
	}

	public virtual void Emit(OpCode opcode, double arg)
	{
		EnsureCapacity(11);
		InternalEmit(opcode);
		BinaryPrimitives.WriteInt64LittleEndian(m_ILStream.AsSpan(m_length), BitConverter.DoubleToInt64Bits(arg));
		m_length += 8;
	}

	public virtual void Emit(OpCode opcode, Label label)
	{
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (OpCodes.TakesSingleByteArgument(opcode))
		{
			AddFixup(label, m_length++, 1);
			return;
		}
		AddFixup(label, m_length, 4);
		m_length += 4;
	}

	public virtual void Emit(OpCode opcode, Label[] labels)
	{
		if (labels == null)
		{
			throw new ArgumentNullException("labels");
		}
		int num = labels.Length;
		EnsureCapacity(num * 4 + 7);
		InternalEmit(opcode);
		PutInteger4(num);
		int num2 = num * 4;
		int num3 = 0;
		while (num2 > 0)
		{
			AddFixup(labels[num3], m_length, num2);
			m_length += 4;
			num2 -= 4;
			num3++;
		}
	}

	public virtual void Emit(OpCode opcode, FieldInfo field)
	{
		ModuleBuilder moduleBuilder = (ModuleBuilder)m_methodBuilder.Module;
		int fieldToken = moduleBuilder.GetFieldToken(field);
		EnsureCapacity(7);
		InternalEmit(opcode);
		RecordTokenFixup();
		PutInteger4(fieldToken);
	}

	public virtual void Emit(OpCode opcode, string str)
	{
		ModuleBuilder moduleBuilder = (ModuleBuilder)m_methodBuilder.Module;
		int stringConstant = moduleBuilder.GetStringConstant(str);
		EnsureCapacity(7);
		InternalEmit(opcode);
		PutInteger4(stringConstant);
	}

	public virtual void Emit(OpCode opcode, LocalBuilder local)
	{
		if (local == null)
		{
			throw new ArgumentNullException("local");
		}
		int localIndex = local.GetLocalIndex();
		if (local.GetMethodBuilder() != m_methodBuilder)
		{
			throw new ArgumentException(SR.Argument_UnmatchedMethodForLocal, "local");
		}
		if (opcode.Equals(OpCodes.Ldloc))
		{
			switch (localIndex)
			{
			case 0:
				opcode = OpCodes.Ldloc_0;
				break;
			case 1:
				opcode = OpCodes.Ldloc_1;
				break;
			case 2:
				opcode = OpCodes.Ldloc_2;
				break;
			case 3:
				opcode = OpCodes.Ldloc_3;
				break;
			default:
				if (localIndex <= 255)
				{
					opcode = OpCodes.Ldloc_S;
				}
				break;
			}
		}
		else if (opcode.Equals(OpCodes.Stloc))
		{
			switch (localIndex)
			{
			case 0:
				opcode = OpCodes.Stloc_0;
				break;
			case 1:
				opcode = OpCodes.Stloc_1;
				break;
			case 2:
				opcode = OpCodes.Stloc_2;
				break;
			case 3:
				opcode = OpCodes.Stloc_3;
				break;
			default:
				if (localIndex <= 255)
				{
					opcode = OpCodes.Stloc_S;
				}
				break;
			}
		}
		else if (opcode.Equals(OpCodes.Ldloca) && localIndex <= 255)
		{
			opcode = OpCodes.Ldloca_S;
		}
		EnsureCapacity(7);
		InternalEmit(opcode);
		if (opcode.OperandType == OperandType.InlineNone)
		{
			return;
		}
		if (!OpCodes.TakesSingleByteArgument(opcode))
		{
			BinaryPrimitives.WriteInt16LittleEndian(m_ILStream.AsSpan(m_length), (short)localIndex);
			m_length += 2;
			return;
		}
		if (localIndex > 255)
		{
			throw new InvalidOperationException(SR.InvalidOperation_BadInstructionOrIndexOutOfBound);
		}
		m_ILStream[m_length++] = (byte)localIndex;
	}

	public virtual Label BeginExceptionBlock()
	{
		if (m_exceptions == null)
		{
			m_exceptions = new __ExceptionInfo[2];
		}
		if (m_currExcStack == null)
		{
			m_currExcStack = new __ExceptionInfo[2];
		}
		if (m_exceptionCount >= m_exceptions.Length)
		{
			m_exceptions = EnlargeArray(m_exceptions);
		}
		if (m_currExcStackCount >= m_currExcStack.Length)
		{
			m_currExcStack = EnlargeArray(m_currExcStack);
		}
		Label label = DefineLabel();
		__ExceptionInfo _ExceptionInfo = new __ExceptionInfo(m_length, label);
		m_exceptions[m_exceptionCount++] = _ExceptionInfo;
		m_currExcStack[m_currExcStackCount++] = _ExceptionInfo;
		return label;
	}

	public virtual void EndExceptionBlock()
	{
		if (m_currExcStackCount == 0)
		{
			throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
		}
		__ExceptionInfo _ExceptionInfo = m_currExcStack[m_currExcStackCount - 1];
		m_currExcStack[--m_currExcStackCount] = null;
		Label endLabel = _ExceptionInfo.GetEndLabel();
		switch (_ExceptionInfo.GetCurrentState())
		{
		case 0:
		case 1:
			throw new InvalidOperationException(SR.Argument_BadExceptionCodeGen);
		case 2:
			Emit(OpCodes.Leave, endLabel);
			break;
		case 3:
		case 4:
			Emit(OpCodes.Endfinally);
			break;
		}
		Label loc = ((m_labelList[endLabel.GetLabelValue()] != -1) ? _ExceptionInfo.m_finallyEndLabel : endLabel);
		MarkLabel(loc);
		_ExceptionInfo.Done(m_length);
	}

	public virtual void BeginExceptFilterBlock()
	{
		if (m_currExcStackCount == 0)
		{
			throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
		}
		__ExceptionInfo _ExceptionInfo = m_currExcStack[m_currExcStackCount - 1];
		Emit(OpCodes.Leave, _ExceptionInfo.GetEndLabel());
		_ExceptionInfo.MarkFilterAddr(m_length);
	}

	public virtual void BeginCatchBlock(Type exceptionType)
	{
		if (m_currExcStackCount == 0)
		{
			throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
		}
		__ExceptionInfo _ExceptionInfo = m_currExcStack[m_currExcStackCount - 1];
		if (_ExceptionInfo.GetCurrentState() == 1)
		{
			if (exceptionType != null)
			{
				throw new ArgumentException(SR.Argument_ShouldNotSpecifyExceptionType);
			}
			Emit(OpCodes.Endfilter);
		}
		else
		{
			if (exceptionType == null)
			{
				throw new ArgumentNullException("exceptionType");
			}
			Emit(OpCodes.Leave, _ExceptionInfo.GetEndLabel());
		}
		_ExceptionInfo.MarkCatchAddr(m_length, exceptionType);
	}

	public virtual void BeginFaultBlock()
	{
		if (m_currExcStackCount == 0)
		{
			throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
		}
		__ExceptionInfo _ExceptionInfo = m_currExcStack[m_currExcStackCount - 1];
		Emit(OpCodes.Leave, _ExceptionInfo.GetEndLabel());
		_ExceptionInfo.MarkFaultAddr(m_length);
	}

	public virtual void BeginFinallyBlock()
	{
		if (m_currExcStackCount == 0)
		{
			throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
		}
		__ExceptionInfo _ExceptionInfo = m_currExcStack[m_currExcStackCount - 1];
		int currentState = _ExceptionInfo.GetCurrentState();
		Label endLabel = _ExceptionInfo.GetEndLabel();
		int num = 0;
		if (currentState != 0)
		{
			Emit(OpCodes.Leave, endLabel);
			num = m_length;
		}
		MarkLabel(endLabel);
		Label label = DefineLabel();
		_ExceptionInfo.SetFinallyEndLabel(label);
		Emit(OpCodes.Leave, label);
		if (num == 0)
		{
			num = m_length;
		}
		_ExceptionInfo.MarkFinallyAddr(m_length, num);
	}

	public virtual Label DefineLabel()
	{
		if (m_labelList == null)
		{
			m_labelList = new int[4];
		}
		if (m_labelCount >= m_labelList.Length)
		{
			m_labelList = EnlargeArray(m_labelList);
		}
		m_labelList[m_labelCount] = -1;
		return new Label(m_labelCount++);
	}

	public virtual void MarkLabel(Label loc)
	{
		int labelValue = loc.GetLabelValue();
		if (m_labelList == null || labelValue < 0 || labelValue >= m_labelList.Length)
		{
			throw new ArgumentException(SR.Argument_InvalidLabel);
		}
		if (m_labelList[labelValue] != -1)
		{
			throw new ArgumentException(SR.Argument_RedefinedLabel);
		}
		m_labelList[labelValue] = m_length;
	}

	public virtual void ThrowException([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type excType)
	{
		if (excType == null)
		{
			throw new ArgumentNullException("excType");
		}
		if (!excType.IsSubclassOf(typeof(Exception)) && excType != typeof(Exception))
		{
			throw new ArgumentException(SR.Argument_NotExceptionType);
		}
		ConstructorInfo constructor = excType.GetConstructor(Type.EmptyTypes);
		if (constructor == null)
		{
			throw new ArgumentException(SR.Argument_MissingDefaultConstructor);
		}
		Emit(OpCodes.Newobj, constructor);
		Emit(OpCodes.Throw);
	}

	public virtual void EmitWriteLine(string value)
	{
		Emit(OpCodes.Ldstr, value);
		Type[] types = new Type[1] { typeof(string) };
		Type type = Type.GetType("System.Console, System.Console", throwOnError: true);
		MethodInfo method = type.GetMethod("WriteLine", types);
		Emit(OpCodes.Call, method);
	}

	public virtual void EmitWriteLine(LocalBuilder localBuilder)
	{
		if (m_methodBuilder == null)
		{
			throw new ArgumentException(SR.InvalidOperation_BadILGeneratorUsage);
		}
		Type type = Type.GetType("System.Console, System.Console", throwOnError: true);
		MethodInfo method = type.GetMethod("get_Out");
		Emit(OpCodes.Call, method);
		Emit(OpCodes.Ldloc, localBuilder);
		Type[] array = new Type[1];
		Type localType = localBuilder.LocalType;
		if (localType is TypeBuilder || localType is EnumBuilder)
		{
			throw new ArgumentException(SR.NotSupported_OutputStreamUsingTypeBuilder);
		}
		array[0] = localType;
		MethodInfo method2 = typeof(TextWriter).GetMethod("WriteLine", array);
		if (method2 == null)
		{
			throw new ArgumentException(SR.Argument_EmitWriteLineType, "localBuilder");
		}
		Emit(OpCodes.Callvirt, method2);
	}

	public virtual void EmitWriteLine(FieldInfo fld)
	{
		if (fld == null)
		{
			throw new ArgumentNullException("fld");
		}
		Type type = Type.GetType("System.Console, System.Console", throwOnError: true);
		MethodInfo method = type.GetMethod("get_Out");
		Emit(OpCodes.Call, method);
		if ((fld.Attributes & FieldAttributes.Static) != 0)
		{
			Emit(OpCodes.Ldsfld, fld);
		}
		else
		{
			Emit(OpCodes.Ldarg, (short)0);
			Emit(OpCodes.Ldfld, fld);
		}
		Type[] array = new Type[1];
		Type fieldType = fld.FieldType;
		if (fieldType is TypeBuilder || fieldType is EnumBuilder)
		{
			throw new NotSupportedException(SR.NotSupported_OutputStreamUsingTypeBuilder);
		}
		array[0] = fieldType;
		MethodInfo method2 = typeof(TextWriter).GetMethod("WriteLine", array);
		if (method2 == null)
		{
			throw new ArgumentException(SR.Argument_EmitWriteLineType, "fld");
		}
		Emit(OpCodes.Callvirt, method2);
	}

	public virtual LocalBuilder DeclareLocal(Type localType)
	{
		return DeclareLocal(localType, pinned: false);
	}

	public virtual LocalBuilder DeclareLocal(Type localType, bool pinned)
	{
		MethodBuilder methodBuilder = m_methodBuilder as MethodBuilder;
		if (methodBuilder == null)
		{
			throw new NotSupportedException();
		}
		if (methodBuilder.IsTypeCreated())
		{
			throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
		}
		if (localType == null)
		{
			throw new ArgumentNullException("localType");
		}
		if (methodBuilder.m_bIsBaked)
		{
			throw new InvalidOperationException(SR.InvalidOperation_MethodBaked);
		}
		m_localSignature.AddArgument(localType, pinned);
		return new LocalBuilder(m_localCount++, localType, methodBuilder, pinned);
	}

	public virtual void UsingNamespace(string usingNamespace)
	{
		if (usingNamespace == null)
		{
			throw new ArgumentNullException("usingNamespace");
		}
		if (usingNamespace.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyName, "usingNamespace");
		}
		MethodBuilder methodBuilder = m_methodBuilder as MethodBuilder;
		if (methodBuilder == null)
		{
			throw new NotSupportedException();
		}
		int currentActiveScopeIndex = methodBuilder.GetILGenerator().m_ScopeTree.GetCurrentActiveScopeIndex();
		if (currentActiveScopeIndex == -1)
		{
			methodBuilder.m_localSymInfo.AddUsingNamespace(usingNamespace);
		}
		else
		{
			m_ScopeTree.AddUsingNamespaceToCurrentScope(usingNamespace);
		}
	}

	public virtual void BeginScope()
	{
		m_ScopeTree.AddScopeInfo(ScopeAction.Open, m_length);
	}

	public virtual void EndScope()
	{
		m_ScopeTree.AddScopeInfo(ScopeAction.Close, m_length);
	}
}
