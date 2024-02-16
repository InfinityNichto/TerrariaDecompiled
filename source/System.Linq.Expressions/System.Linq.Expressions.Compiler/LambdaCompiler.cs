using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions.Compiler;

internal sealed class LambdaCompiler : ILocalCache
{
	private delegate void WriteBack(LambdaCompiler compiler);

	[Flags]
	internal enum CompilationFlags
	{
		EmitExpressionStart = 1,
		EmitNoExpressionStart = 2,
		EmitAsDefaultType = 0x10,
		EmitAsVoidType = 0x20,
		EmitAsTail = 0x100,
		EmitAsMiddle = 0x200,
		EmitAsNoTail = 0x400,
		EmitExpressionStartMask = 0xF,
		EmitAsTypeMask = 0xF0,
		EmitAsTailCallMask = 0xF00
	}

	private sealed class SwitchLabel
	{
		internal readonly decimal Key;

		internal readonly Label Label;

		internal readonly object Constant;

		internal SwitchLabel(decimal key, object constant, Label label)
		{
			Key = key;
			Constant = constant;
			Label = label;
		}
	}

	private sealed class SwitchInfo
	{
		internal readonly SwitchExpression Node;

		internal readonly LocalBuilder Value;

		internal readonly Label Default;

		internal readonly Type Type;

		internal readonly bool IsUnsigned;

		internal readonly bool Is64BitSwitch;

		internal SwitchInfo(SwitchExpression node, LocalBuilder value, Label @default)
		{
			Node = node;
			Value = value;
			Default = @default;
			Type = Node.SwitchValue.Type;
			IsUnsigned = Type.IsUnsigned();
			TypeCode typeCode = Type.GetTypeCode();
			Is64BitSwitch = typeCode == TypeCode.UInt64 || typeCode == TypeCode.Int64;
		}
	}

	private static int s_lambdaMethodIndex;

	private readonly AnalyzedTree _tree;

	private readonly ILGenerator _ilg;

	private readonly MethodInfo _method;

	private LabelScopeInfo _labelBlock = new LabelScopeInfo(null, LabelScopeKind.Lambda);

	private readonly Dictionary<LabelTarget, LabelInfo> _labelInfo = new Dictionary<LabelTarget, LabelInfo>();

	private CompilerScope _scope;

	private readonly LambdaExpression _lambda;

	private readonly bool _hasClosureArgument;

	private readonly BoundConstants _boundConstants;

	private readonly KeyedStack<Type, LocalBuilder> _freeLocals = new KeyedStack<Type, LocalBuilder>();

	private static readonly FieldInfo s_callSiteTargetField = typeof(CallSite<>).GetField("Target");

	private readonly StackGuard _guard = new StackGuard();

	internal ILGenerator IL => _ilg;

	internal IParameterProvider Parameters => _lambda;

	private void EmitAddress(Expression node, Type type)
	{
		EmitAddress(node, type, CompilationFlags.EmitExpressionStart);
	}

	private void EmitAddress(Expression node, Type type, CompilationFlags flags)
	{
		bool flag = (flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart;
		CompilationFlags flags2 = (flag ? EmitExpressionStart(node) : CompilationFlags.EmitNoExpressionStart);
		switch (node.NodeType)
		{
		default:
			EmitExpressionAddress(node, type);
			break;
		case ExpressionType.ArrayIndex:
			AddressOf((BinaryExpression)node, type);
			break;
		case ExpressionType.Parameter:
			AddressOf((ParameterExpression)node, type);
			break;
		case ExpressionType.MemberAccess:
			AddressOf((MemberExpression)node, type);
			break;
		case ExpressionType.Unbox:
			AddressOf((UnaryExpression)node, type);
			break;
		case ExpressionType.Call:
			AddressOf((MethodCallExpression)node, type);
			break;
		case ExpressionType.Index:
			AddressOf((IndexExpression)node, type);
			break;
		}
		if (flag)
		{
			EmitExpressionEnd(flags2);
		}
	}

	private void AddressOf(BinaryExpression node, Type type)
	{
		if (TypeUtils.AreEquivalent(type, node.Type))
		{
			EmitExpression(node.Left);
			EmitExpression(node.Right);
			_ilg.Emit(OpCodes.Ldelema, node.Type);
		}
		else
		{
			EmitExpressionAddress(node, type);
		}
	}

	private void AddressOf(ParameterExpression node, Type type)
	{
		if (TypeUtils.AreEquivalent(type, node.Type))
		{
			if (node.IsByRef)
			{
				_scope.EmitGet(node);
			}
			else
			{
				_scope.EmitAddressOf(node);
			}
		}
		else if (node.Type.IsByRef && node.Type.GetElementType() == type)
		{
			EmitExpression(node);
		}
		else
		{
			EmitExpressionAddress(node, type);
		}
	}

	private void AddressOf(MemberExpression node, Type type)
	{
		if (TypeUtils.AreEquivalent(type, node.Type))
		{
			Type type2 = null;
			if (node.Expression != null)
			{
				EmitInstance(node.Expression, out type2);
			}
			EmitMemberAddress(node.Member, type2);
		}
		else
		{
			EmitExpressionAddress(node, type);
		}
	}

	private void EmitMemberAddress(MemberInfo member, Type objectType)
	{
		if (member is FieldInfo { IsLiteral: false, IsInitOnly: false } fieldInfo)
		{
			_ilg.EmitFieldAddress(fieldInfo);
			return;
		}
		EmitMemberGet(member, objectType);
		LocalBuilder local = GetLocal(GetMemberType(member));
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
	}

	private void AddressOf(MethodCallExpression node, Type type)
	{
		if (!node.Method.IsStatic && node.Object.Type.IsArray && node.Method == TypeUtils.GetArrayGetMethod(node.Object.Type))
		{
			MethodInfo arrayAddressMethod = TypeUtils.GetArrayAddressMethod(node.Object.Type);
			EmitMethodCall(node.Object, arrayAddressMethod, node);
		}
		else
		{
			EmitExpressionAddress(node, type);
		}
	}

	private void AddressOf(IndexExpression node, Type type)
	{
		if (!TypeUtils.AreEquivalent(type, node.Type) || node.Indexer != null)
		{
			EmitExpressionAddress(node, type);
		}
		else if (node.ArgumentCount == 1)
		{
			EmitExpression(node.Object);
			EmitExpression(node.GetArgument(0));
			_ilg.Emit(OpCodes.Ldelema, node.Type);
		}
		else
		{
			MethodInfo arrayAddressMethod = TypeUtils.GetArrayAddressMethod(node.Object.Type);
			EmitMethodCall(node.Object, arrayAddressMethod, node);
		}
	}

	private void AddressOf(UnaryExpression node, Type type)
	{
		EmitExpression(node.Operand);
		_ilg.Emit(OpCodes.Unbox, type);
	}

	private void EmitExpressionAddress(Expression node, Type type)
	{
		EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
		LocalBuilder local = GetLocal(type);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
	}

	private WriteBack EmitAddressWriteBack(Expression node, Type type)
	{
		CompilationFlags flags = EmitExpressionStart(node);
		WriteBack writeBack = null;
		if (TypeUtils.AreEquivalent(type, node.Type))
		{
			switch (node.NodeType)
			{
			case ExpressionType.MemberAccess:
				writeBack = AddressOfWriteBack((MemberExpression)node);
				break;
			case ExpressionType.Index:
				writeBack = AddressOfWriteBack((IndexExpression)node);
				break;
			}
		}
		if (writeBack == null)
		{
			EmitAddress(node, type, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
		}
		EmitExpressionEnd(flags);
		return writeBack;
	}

	private WriteBack AddressOfWriteBack(MemberExpression node)
	{
		if (!(node.Member is PropertyInfo { CanWrite: not false }))
		{
			return null;
		}
		return AddressOfWriteBackCore(node);
	}

	private WriteBack AddressOfWriteBackCore(MemberExpression node)
	{
		LocalBuilder instanceLocal = null;
		Type type = null;
		if (node.Expression != null)
		{
			EmitInstance(node.Expression, out type);
			_ilg.Emit(OpCodes.Dup);
			_ilg.Emit(OpCodes.Stloc, instanceLocal = GetInstanceLocal(type));
		}
		PropertyInfo pi = (PropertyInfo)node.Member;
		EmitCall(type, pi.GetGetMethod(nonPublic: true));
		LocalBuilder valueLocal = GetLocal(node.Type);
		_ilg.Emit(OpCodes.Stloc, valueLocal);
		_ilg.Emit(OpCodes.Ldloca, valueLocal);
		return delegate(LambdaCompiler @this)
		{
			if (instanceLocal != null)
			{
				@this._ilg.Emit(OpCodes.Ldloc, instanceLocal);
				@this.FreeLocal(instanceLocal);
			}
			@this._ilg.Emit(OpCodes.Ldloc, valueLocal);
			@this.FreeLocal(valueLocal);
			@this.EmitCall(instanceLocal?.LocalType, pi.GetSetMethod(nonPublic: true));
		};
	}

	private WriteBack AddressOfWriteBack(IndexExpression node)
	{
		if (node.Indexer == null || !node.Indexer.CanWrite)
		{
			return null;
		}
		return AddressOfWriteBackCore(node);
	}

	private WriteBack AddressOfWriteBackCore(IndexExpression node)
	{
		LocalBuilder instanceLocal = null;
		Type type = null;
		if (node.Object != null)
		{
			EmitInstance(node.Object, out type);
			_ilg.Emit(OpCodes.Dup);
			_ilg.Emit(OpCodes.Stloc, instanceLocal = GetInstanceLocal(type));
		}
		int argumentCount = node.ArgumentCount;
		LocalBuilder[] args = new LocalBuilder[argumentCount];
		for (int i = 0; i < argumentCount; i++)
		{
			Expression argument = node.GetArgument(i);
			EmitExpression(argument);
			LocalBuilder local = GetLocal(argument.Type);
			_ilg.Emit(OpCodes.Dup);
			_ilg.Emit(OpCodes.Stloc, local);
			args[i] = local;
		}
		EmitGetIndexCall(node, type);
		LocalBuilder valueLocal = GetLocal(node.Type);
		_ilg.Emit(OpCodes.Stloc, valueLocal);
		_ilg.Emit(OpCodes.Ldloca, valueLocal);
		return delegate(LambdaCompiler @this)
		{
			if (instanceLocal != null)
			{
				@this._ilg.Emit(OpCodes.Ldloc, instanceLocal);
				@this.FreeLocal(instanceLocal);
			}
			LocalBuilder[] array = args;
			foreach (LocalBuilder local2 in array)
			{
				@this._ilg.Emit(OpCodes.Ldloc, local2);
				@this.FreeLocal(local2);
			}
			@this._ilg.Emit(OpCodes.Ldloc, valueLocal);
			@this.FreeLocal(valueLocal);
			@this.EmitSetIndexCall(node, instanceLocal?.LocalType);
		};
	}

	private LocalBuilder GetInstanceLocal(Type type)
	{
		Type type2 = (type.IsValueType ? type.MakeByRefType() : type);
		return GetLocal(type2);
	}

	private void EmitBinaryExpression(Expression expr)
	{
		EmitBinaryExpression(expr, CompilationFlags.EmitAsNoTail);
	}

	private void EmitBinaryExpression(Expression expr, CompilationFlags flags)
	{
		BinaryExpression binaryExpression = (BinaryExpression)expr;
		if (binaryExpression.Method != null)
		{
			EmitBinaryMethod(binaryExpression, flags);
			return;
		}
		if ((binaryExpression.NodeType == ExpressionType.Equal || binaryExpression.NodeType == ExpressionType.NotEqual) && (binaryExpression.Type == typeof(bool) || binaryExpression.Type == typeof(bool?)))
		{
			if (ConstantCheck.IsNull(binaryExpression.Left) && !ConstantCheck.IsNull(binaryExpression.Right) && binaryExpression.Right.Type.IsNullableType())
			{
				EmitNullEquality(binaryExpression.NodeType, binaryExpression.Right, binaryExpression.IsLiftedToNull);
				return;
			}
			if (ConstantCheck.IsNull(binaryExpression.Right) && !ConstantCheck.IsNull(binaryExpression.Left) && binaryExpression.Left.Type.IsNullableType())
			{
				EmitNullEquality(binaryExpression.NodeType, binaryExpression.Left, binaryExpression.IsLiftedToNull);
				return;
			}
			EmitExpression(GetEqualityOperand(binaryExpression.Left));
			EmitExpression(GetEqualityOperand(binaryExpression.Right));
		}
		else
		{
			EmitExpression(binaryExpression.Left);
			EmitExpression(binaryExpression.Right);
		}
		EmitBinaryOperator(binaryExpression.NodeType, binaryExpression.Left.Type, binaryExpression.Right.Type, binaryExpression.Type, binaryExpression.IsLiftedToNull);
	}

	private void EmitNullEquality(ExpressionType op, Expression e, bool isLiftedToNull)
	{
		if (isLiftedToNull)
		{
			EmitExpressionAsVoid(e);
			_ilg.EmitDefault(typeof(bool?), this);
			return;
		}
		EmitAddress(e, e.Type);
		_ilg.EmitHasValue(e.Type);
		if (op == ExpressionType.Equal)
		{
			_ilg.Emit(OpCodes.Ldc_I4_0);
			_ilg.Emit(OpCodes.Ceq);
		}
	}

	private void EmitBinaryMethod(BinaryExpression b, CompilationFlags flags)
	{
		if (b.IsLifted)
		{
			ParameterExpression parameterExpression = Expression.Variable(b.Left.Type.GetNonNullableType(), null);
			ParameterExpression parameterExpression2 = Expression.Variable(b.Right.Type.GetNonNullableType(), null);
			MethodCallExpression methodCallExpression = Expression.Call(null, b.Method, parameterExpression, parameterExpression2);
			EmitLift(resultType: (!b.IsLiftedToNull) ? typeof(bool) : methodCallExpression.Type.GetNullableType(), nodeType: b.NodeType, mc: methodCallExpression, paramList: new ParameterExpression[2] { parameterExpression, parameterExpression2 }, argList: new Expression[2] { b.Left, b.Right });
		}
		else
		{
			EmitMethodCallExpression(Expression.Call(null, b.Method, b.Left, b.Right), flags);
		}
	}

	private void EmitBinaryOperator(ExpressionType op, Type leftType, Type rightType, Type resultType, bool liftedToNull)
	{
		if (op == ExpressionType.ArrayIndex)
		{
			EmitGetArrayElement(leftType);
		}
		else if (leftType.IsNullableType() || rightType.IsNullableType())
		{
			EmitLiftedBinaryOp(op, leftType, rightType, resultType, liftedToNull);
		}
		else
		{
			EmitUnliftedBinaryOp(op, leftType, rightType);
		}
	}

	private void EmitUnliftedBinaryOp(ExpressionType op, Type leftType, Type rightType)
	{
		switch (op)
		{
		case ExpressionType.NotEqual:
			if (leftType.GetTypeCode() != TypeCode.Boolean)
			{
				_ilg.Emit(OpCodes.Ceq);
				_ilg.Emit(OpCodes.Ldc_I4_0);
				goto case ExpressionType.Equal;
			}
			goto case ExpressionType.ExclusiveOr;
		case ExpressionType.Equal:
			_ilg.Emit(OpCodes.Ceq);
			return;
		case ExpressionType.Add:
			_ilg.Emit(OpCodes.Add);
			break;
		case ExpressionType.AddChecked:
			_ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Add : (leftType.IsUnsigned() ? OpCodes.Add_Ovf_Un : OpCodes.Add_Ovf));
			break;
		case ExpressionType.Subtract:
			_ilg.Emit(OpCodes.Sub);
			break;
		case ExpressionType.SubtractChecked:
			if (leftType.IsUnsigned())
			{
				_ilg.Emit(OpCodes.Sub_Ovf_Un);
				return;
			}
			_ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Sub : OpCodes.Sub_Ovf);
			break;
		case ExpressionType.Multiply:
			_ilg.Emit(OpCodes.Mul);
			break;
		case ExpressionType.MultiplyChecked:
			_ilg.Emit(leftType.IsFloatingPoint() ? OpCodes.Mul : (leftType.IsUnsigned() ? OpCodes.Mul_Ovf_Un : OpCodes.Mul_Ovf));
			break;
		case ExpressionType.Divide:
			_ilg.Emit(leftType.IsUnsigned() ? OpCodes.Div_Un : OpCodes.Div);
			break;
		case ExpressionType.Modulo:
			_ilg.Emit(leftType.IsUnsigned() ? OpCodes.Rem_Un : OpCodes.Rem);
			return;
		case ExpressionType.And:
		case ExpressionType.AndAlso:
			_ilg.Emit(OpCodes.And);
			return;
		case ExpressionType.Or:
		case ExpressionType.OrElse:
			_ilg.Emit(OpCodes.Or);
			return;
		case ExpressionType.LessThan:
			_ilg.Emit(leftType.IsUnsigned() ? OpCodes.Clt_Un : OpCodes.Clt);
			return;
		case ExpressionType.LessThanOrEqual:
			_ilg.Emit((leftType.IsUnsigned() || leftType.IsFloatingPoint()) ? OpCodes.Cgt_Un : OpCodes.Cgt);
			_ilg.Emit(OpCodes.Ldc_I4_0);
			_ilg.Emit(OpCodes.Ceq);
			return;
		case ExpressionType.GreaterThan:
			_ilg.Emit(leftType.IsUnsigned() ? OpCodes.Cgt_Un : OpCodes.Cgt);
			return;
		case ExpressionType.GreaterThanOrEqual:
			_ilg.Emit((leftType.IsUnsigned() || leftType.IsFloatingPoint()) ? OpCodes.Clt_Un : OpCodes.Clt);
			_ilg.Emit(OpCodes.Ldc_I4_0);
			_ilg.Emit(OpCodes.Ceq);
			return;
		case ExpressionType.ExclusiveOr:
			_ilg.Emit(OpCodes.Xor);
			return;
		case ExpressionType.LeftShift:
			EmitShiftMask(leftType);
			_ilg.Emit(OpCodes.Shl);
			break;
		case ExpressionType.RightShift:
			EmitShiftMask(leftType);
			_ilg.Emit(leftType.IsUnsigned() ? OpCodes.Shr_Un : OpCodes.Shr);
			return;
		}
		EmitConvertArithmeticResult(op, leftType);
	}

	private void EmitShiftMask(Type leftType)
	{
		int value = (leftType.IsInteger64() ? 63 : 31);
		_ilg.EmitPrimitive(value);
		_ilg.Emit(OpCodes.And);
	}

	private void EmitConvertArithmeticResult(ExpressionType op, Type resultType)
	{
		switch (resultType.GetTypeCode())
		{
		case TypeCode.Byte:
			_ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1);
			break;
		case TypeCode.SByte:
			_ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1);
			break;
		case TypeCode.UInt16:
			_ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2);
			break;
		case TypeCode.Int16:
			_ilg.Emit(IsChecked(op) ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2);
			break;
		}
	}

	private void EmitLiftedBinaryOp(ExpressionType op, Type leftType, Type rightType, Type resultType, bool liftedToNull)
	{
		switch (op)
		{
		case ExpressionType.And:
			if (leftType == typeof(bool?))
			{
				EmitLiftedBooleanAnd();
			}
			else
			{
				EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
			}
			break;
		case ExpressionType.Or:
			if (leftType == typeof(bool?))
			{
				EmitLiftedBooleanOr();
			}
			else
			{
				EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
			}
			break;
		case ExpressionType.Add:
		case ExpressionType.AddChecked:
		case ExpressionType.Divide:
		case ExpressionType.ExclusiveOr:
		case ExpressionType.LeftShift:
		case ExpressionType.Modulo:
		case ExpressionType.Multiply:
		case ExpressionType.MultiplyChecked:
		case ExpressionType.RightShift:
		case ExpressionType.Subtract:
		case ExpressionType.SubtractChecked:
			EmitLiftedBinaryArithmetic(op, leftType, rightType, resultType);
			break;
		case ExpressionType.Equal:
		case ExpressionType.GreaterThan:
		case ExpressionType.GreaterThanOrEqual:
		case ExpressionType.LessThan:
		case ExpressionType.LessThanOrEqual:
		case ExpressionType.NotEqual:
			if (liftedToNull)
			{
				EmitLiftedToNullRelational(op, leftType);
			}
			else
			{
				EmitLiftedRelational(op, leftType);
			}
			break;
		}
	}

	private void EmitLiftedRelational(ExpressionType op, Type type)
	{
		bool flag = op == ExpressionType.NotEqual;
		if (flag)
		{
			op = ExpressionType.Equal;
		}
		LocalBuilder local = GetLocal(type);
		LocalBuilder local2 = GetLocal(type);
		_ilg.Emit(OpCodes.Stloc, local2);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitGetValueOrDefault(type);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitGetValueOrDefault(type);
		Type nonNullableType = type.GetNonNullableType();
		EmitUnliftedBinaryOp(op, nonNullableType, nonNullableType);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitHasValue(type);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitHasValue(type);
		FreeLocal(local);
		FreeLocal(local2);
		_ilg.Emit((op == ExpressionType.Equal) ? OpCodes.Ceq : OpCodes.And);
		_ilg.Emit(OpCodes.And);
		if (flag)
		{
			_ilg.Emit(OpCodes.Ldc_I4_0);
			_ilg.Emit(OpCodes.Ceq);
		}
	}

	private void EmitLiftedToNullRelational(ExpressionType op, Type type)
	{
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		LocalBuilder local = GetLocal(type);
		LocalBuilder local2 = GetLocal(type);
		_ilg.Emit(OpCodes.Stloc, local2);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitHasValue(type);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitHasValue(type);
		_ilg.Emit(OpCodes.And);
		_ilg.Emit(OpCodes.Brtrue_S, label);
		_ilg.EmitDefault(typeof(bool?), this);
		_ilg.Emit(OpCodes.Br_S, label2);
		_ilg.MarkLabel(label);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitGetValueOrDefault(type);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitGetValueOrDefault(type);
		FreeLocal(local);
		FreeLocal(local2);
		Type nonNullableType = type.GetNonNullableType();
		EmitUnliftedBinaryOp(op, nonNullableType, nonNullableType);
		_ilg.Emit(OpCodes.Newobj, CachedReflectionInfo.Nullable_Boolean_Ctor);
		_ilg.MarkLabel(label2);
	}

	private void EmitLiftedBinaryArithmetic(ExpressionType op, Type leftType, Type rightType, Type resultType)
	{
		bool flag = leftType.IsNullableType();
		bool flag2 = rightType.IsNullableType();
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		LocalBuilder local = GetLocal(leftType);
		LocalBuilder local2 = GetLocal(rightType);
		LocalBuilder local3 = GetLocal(resultType);
		_ilg.Emit(OpCodes.Stloc, local2);
		_ilg.Emit(OpCodes.Stloc, local);
		if (flag)
		{
			_ilg.Emit(OpCodes.Ldloca, local);
			_ilg.EmitHasValue(leftType);
		}
		if (flag2)
		{
			_ilg.Emit(OpCodes.Ldloca, local2);
			_ilg.EmitHasValue(rightType);
			if (flag)
			{
				_ilg.Emit(OpCodes.And);
			}
		}
		_ilg.Emit(OpCodes.Brfalse_S, label);
		if (flag)
		{
			_ilg.Emit(OpCodes.Ldloca, local);
			_ilg.EmitGetValueOrDefault(leftType);
		}
		else
		{
			_ilg.Emit(OpCodes.Ldloc, local);
		}
		if (flag2)
		{
			_ilg.Emit(OpCodes.Ldloca, local2);
			_ilg.EmitGetValueOrDefault(rightType);
		}
		else
		{
			_ilg.Emit(OpCodes.Ldloc, local2);
		}
		FreeLocal(local);
		FreeLocal(local2);
		Type nonNullableType = resultType.GetNonNullableType();
		EmitBinaryOperator(op, leftType.GetNonNullableType(), rightType.GetNonNullableType(), nonNullableType, liftedToNull: false);
		ConstructorInfo nullableConstructor = TypeUtils.GetNullableConstructor(resultType);
		_ilg.Emit(OpCodes.Newobj, nullableConstructor);
		_ilg.Emit(OpCodes.Stloc, local3);
		_ilg.Emit(OpCodes.Br_S, label2);
		_ilg.MarkLabel(label);
		_ilg.Emit(OpCodes.Ldloca, local3);
		_ilg.Emit(OpCodes.Initobj, resultType);
		_ilg.MarkLabel(label2);
		_ilg.Emit(OpCodes.Ldloc, local3);
		FreeLocal(local3);
	}

	private void EmitLiftedBooleanAnd()
	{
		Type typeFromHandle = typeof(bool?);
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		LocalBuilder local = GetLocal(typeFromHandle);
		LocalBuilder local2 = GetLocal(typeFromHandle);
		_ilg.Emit(OpCodes.Stloc, local2);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Brtrue_S, label);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitHasValue(typeFromHandle);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Or);
		_ilg.Emit(OpCodes.Brfalse_S, label);
		_ilg.Emit(OpCodes.Ldloc, local);
		FreeLocal(local);
		_ilg.Emit(OpCodes.Br_S, label2);
		_ilg.MarkLabel(label);
		_ilg.Emit(OpCodes.Ldloc, local2);
		FreeLocal(local2);
		_ilg.MarkLabel(label2);
	}

	private void EmitLiftedBooleanOr()
	{
		Type typeFromHandle = typeof(bool?);
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		LocalBuilder local = GetLocal(typeFromHandle);
		LocalBuilder local2 = GetLocal(typeFromHandle);
		_ilg.Emit(OpCodes.Stloc, local2);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Brtrue_S, label);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitHasValue(typeFromHandle);
		_ilg.Emit(OpCodes.Or);
		_ilg.Emit(OpCodes.Brfalse_S, label);
		_ilg.Emit(OpCodes.Ldloc, local2);
		FreeLocal(local2);
		_ilg.Emit(OpCodes.Br_S, label2);
		_ilg.MarkLabel(label);
		_ilg.Emit(OpCodes.Ldloc, local);
		FreeLocal(local);
		_ilg.MarkLabel(label2);
	}

	private LabelInfo EnsureLabel(LabelTarget node)
	{
		if (!_labelInfo.TryGetValue(node, out var value))
		{
			_labelInfo.Add(node, value = new LabelInfo(_ilg, node, canReturn: false));
		}
		return value;
	}

	private LabelInfo ReferenceLabel(LabelTarget node)
	{
		LabelInfo labelInfo = EnsureLabel(node);
		labelInfo.Reference(_labelBlock);
		return labelInfo;
	}

	private LabelInfo DefineLabel(LabelTarget node)
	{
		if (node == null)
		{
			return new LabelInfo(_ilg, null, canReturn: false);
		}
		LabelInfo labelInfo = EnsureLabel(node);
		labelInfo.Define(_labelBlock);
		return labelInfo;
	}

	private void PushLabelBlock(LabelScopeKind type)
	{
		_labelBlock = new LabelScopeInfo(_labelBlock, type);
	}

	private void PopLabelBlock(LabelScopeKind kind)
	{
		_labelBlock = _labelBlock.Parent;
	}

	private void EmitLabelExpression(Expression expr, CompilationFlags flags)
	{
		LabelExpression labelExpression = (LabelExpression)expr;
		LabelInfo info = null;
		if (_labelBlock.Kind == LabelScopeKind.Block)
		{
			_labelBlock.TryGetLabelInfo(labelExpression.Target, out info);
			if (info == null && _labelBlock.Parent.Kind == LabelScopeKind.Switch)
			{
				_labelBlock.Parent.TryGetLabelInfo(labelExpression.Target, out info);
			}
		}
		if (info == null)
		{
			info = DefineLabel(labelExpression.Target);
		}
		if (labelExpression.DefaultValue != null)
		{
			if (labelExpression.Target.Type == typeof(void))
			{
				EmitExpressionAsVoid(labelExpression.DefaultValue, flags);
			}
			else
			{
				flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
				EmitExpression(labelExpression.DefaultValue, flags);
			}
		}
		info.Mark();
	}

	private void EmitGotoExpression(Expression expr, CompilationFlags flags)
	{
		GotoExpression gotoExpression = (GotoExpression)expr;
		LabelInfo labelInfo = ReferenceLabel(gotoExpression.Target);
		CompilationFlags compilationFlags = flags & CompilationFlags.EmitAsTailCallMask;
		if (compilationFlags != CompilationFlags.EmitAsNoTail)
		{
			compilationFlags = (labelInfo.CanReturn ? CompilationFlags.EmitAsTail : CompilationFlags.EmitAsNoTail);
			flags = UpdateEmitAsTailCallFlag(flags, compilationFlags);
		}
		if (gotoExpression.Value != null)
		{
			if (gotoExpression.Target.Type == typeof(void))
			{
				EmitExpressionAsVoid(gotoExpression.Value, flags);
			}
			else
			{
				flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
				EmitExpression(gotoExpression.Value, flags);
			}
		}
		labelInfo.EmitJump();
		EmitUnreachable(gotoExpression, flags);
	}

	private void EmitUnreachable(Expression node, CompilationFlags flags)
	{
		if (node.Type != typeof(void) && (flags & CompilationFlags.EmitAsVoidType) == 0)
		{
			_ilg.EmitDefault(node.Type, this);
		}
	}

	private bool TryPushLabelBlock(Expression node)
	{
		switch (node.NodeType)
		{
		default:
			if (_labelBlock.Kind != LabelScopeKind.Expression)
			{
				PushLabelBlock(LabelScopeKind.Expression);
				return true;
			}
			return false;
		case ExpressionType.Label:
			if (_labelBlock.Kind == LabelScopeKind.Block)
			{
				LabelTarget target = ((LabelExpression)node).Target;
				if (_labelBlock.ContainsTarget(target))
				{
					return false;
				}
				if (_labelBlock.Parent.Kind == LabelScopeKind.Switch && _labelBlock.Parent.ContainsTarget(target))
				{
					return false;
				}
			}
			PushLabelBlock(LabelScopeKind.Statement);
			return true;
		case ExpressionType.Block:
			if (!(node is SpilledExpressionBlock))
			{
				PushLabelBlock(LabelScopeKind.Block);
				if (_labelBlock.Parent.Kind != LabelScopeKind.Switch)
				{
					DefineBlockLabels(node);
				}
				return true;
			}
			goto default;
		case ExpressionType.Switch:
		{
			PushLabelBlock(LabelScopeKind.Switch);
			SwitchExpression switchExpression = (SwitchExpression)node;
			foreach (SwitchCase @case in switchExpression.Cases)
			{
				DefineBlockLabels(@case.Body);
			}
			DefineBlockLabels(switchExpression.DefaultBody);
			return true;
		}
		case ExpressionType.Convert:
			if (!(node.Type != typeof(void)))
			{
				PushLabelBlock(LabelScopeKind.Statement);
				return true;
			}
			goto default;
		case ExpressionType.Conditional:
		case ExpressionType.Goto:
		case ExpressionType.Loop:
			PushLabelBlock(LabelScopeKind.Statement);
			return true;
		}
	}

	private void DefineBlockLabels(Expression node)
	{
		if (!(node is BlockExpression blockExpression) || blockExpression is SpilledExpressionBlock)
		{
			return;
		}
		int i = 0;
		for (int expressionCount = blockExpression.ExpressionCount; i < expressionCount; i++)
		{
			Expression expression = blockExpression.GetExpression(i);
			if (expression is LabelExpression labelExpression)
			{
				DefineLabel(labelExpression.Target);
			}
		}
	}

	private void AddReturnLabel(LambdaExpression lambda)
	{
		Expression expression = lambda.Body;
		while (true)
		{
			switch (expression.NodeType)
			{
			default:
				return;
			case ExpressionType.Label:
			{
				LabelTarget target = ((LabelExpression)expression).Target;
				_labelInfo.Add(target, new LabelInfo(_ilg, target, TypeUtils.AreReferenceAssignable(lambda.ReturnType, target.Type)));
				return;
			}
			case ExpressionType.Block:
			{
				BlockExpression blockExpression = (BlockExpression)expression;
				if (blockExpression.ExpressionCount == 0)
				{
					return;
				}
				for (int num = blockExpression.ExpressionCount - 1; num >= 0; num--)
				{
					expression = blockExpression.GetExpression(num);
					if (Significant(expression))
					{
						break;
					}
				}
				break;
			}
			}
		}
	}

	private LambdaCompiler(AnalyzedTree tree, LambdaExpression lambda)
	{
		Type[] parameterTypes = GetParameterTypes(lambda, typeof(Closure));
		int num = Interlocked.Increment(ref s_lambdaMethodIndex);
		DynamicMethod dynamicMethod = new DynamicMethod(lambda.Name ?? ("lambda_method" + num), lambda.ReturnType, parameterTypes, restrictedSkipVisibility: true);
		_tree = tree;
		_lambda = lambda;
		_method = dynamicMethod;
		_ilg = dynamicMethod.GetILGenerator();
		_hasClosureArgument = true;
		_scope = tree.Scopes[lambda];
		_boundConstants = tree.Constants[lambda];
		InitializeMethod();
	}

	private LambdaCompiler(LambdaCompiler parent, LambdaExpression lambda, InvocationExpression invocation)
	{
		_tree = parent._tree;
		_lambda = lambda;
		_method = parent._method;
		_ilg = parent._ilg;
		_hasClosureArgument = parent._hasClosureArgument;
		_scope = _tree.Scopes[invocation];
		_boundConstants = parent._boundConstants;
	}

	private void InitializeMethod()
	{
		AddReturnLabel(_lambda);
		_boundConstants.EmitCacheConstants(this);
	}

	internal static Delegate Compile(LambdaExpression lambda)
	{
		lambda.ValidateArgumentCount();
		AnalyzedTree tree = AnalyzeLambda(ref lambda);
		LambdaCompiler lambdaCompiler = new LambdaCompiler(tree, lambda);
		lambdaCompiler.EmitLambdaBody();
		return lambdaCompiler.CreateDelegate();
	}

	private static AnalyzedTree AnalyzeLambda(ref LambdaExpression lambda)
	{
		lambda = StackSpiller.AnalyzeLambda(lambda);
		return VariableBinder.Bind(lambda);
	}

	public LocalBuilder GetLocal(Type type)
	{
		return _freeLocals.TryPop(type) ?? _ilg.DeclareLocal(type);
	}

	public void FreeLocal(LocalBuilder local)
	{
		_freeLocals.Push(local.LocalType, local);
	}

	internal int GetLambdaArgument(int index)
	{
		return index + (_hasClosureArgument ? 1 : 0) + ((!_method.IsStatic) ? 1 : 0);
	}

	internal void EmitLambdaArgument(int index)
	{
		_ilg.EmitLoadArg(GetLambdaArgument(index));
	}

	internal void EmitClosureArgument()
	{
		_ilg.EmitLoadArg(0);
	}

	private Delegate CreateDelegate()
	{
		return _method.CreateDelegate(_lambda.Type, new Closure(_boundConstants.ToArray(), null));
	}

	private MemberExpression CreateLazyInitializedField<T>(string name)
	{
		return Utils.GetStrongBoxValueField(Expression.Constant(new StrongBox<T>()));
	}

	private static CompilationFlags UpdateEmitAsTailCallFlag(CompilationFlags flags, CompilationFlags newValue)
	{
		CompilationFlags compilationFlags = flags & CompilationFlags.EmitAsTailCallMask;
		return (flags ^ compilationFlags) | newValue;
	}

	private static CompilationFlags UpdateEmitExpressionStartFlag(CompilationFlags flags, CompilationFlags newValue)
	{
		CompilationFlags compilationFlags = flags & CompilationFlags.EmitExpressionStartMask;
		return (flags ^ compilationFlags) | newValue;
	}

	private static CompilationFlags UpdateEmitAsTypeFlag(CompilationFlags flags, CompilationFlags newValue)
	{
		CompilationFlags compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
		return (flags ^ compilationFlags) | newValue;
	}

	internal void EmitExpression(Expression node)
	{
		EmitExpression(node, CompilationFlags.EmitExpressionStart | CompilationFlags.EmitAsNoTail);
	}

	private void EmitExpressionAsVoid(Expression node)
	{
		EmitExpressionAsVoid(node, CompilationFlags.EmitAsNoTail);
	}

	private void EmitExpressionAsVoid(Expression node, CompilationFlags flags)
	{
		CompilationFlags flags2 = EmitExpressionStart(node);
		switch (node.NodeType)
		{
		case ExpressionType.Assign:
			EmitAssign((AssignBinaryExpression)node, CompilationFlags.EmitAsVoidType);
			break;
		case ExpressionType.Block:
			Emit((BlockExpression)node, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsVoidType));
			break;
		case ExpressionType.Throw:
			EmitThrow((UnaryExpression)node, CompilationFlags.EmitAsVoidType);
			break;
		case ExpressionType.Goto:
			EmitGotoExpression(node, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsVoidType));
			break;
		default:
			if (node.Type == typeof(void))
			{
				EmitExpression(node, UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitNoExpressionStart));
				break;
			}
			EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
			_ilg.Emit(OpCodes.Pop);
			break;
		case ExpressionType.Constant:
		case ExpressionType.Parameter:
		case ExpressionType.Default:
			break;
		}
		EmitExpressionEnd(flags2);
	}

	private void EmitExpressionAsType(Expression node, Type type, CompilationFlags flags)
	{
		if (type == typeof(void))
		{
			EmitExpressionAsVoid(node, flags);
		}
		else if (!TypeUtils.AreEquivalent(node.Type, type))
		{
			EmitExpression(node);
			_ilg.Emit(OpCodes.Castclass, type);
		}
		else
		{
			EmitExpression(node, UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart));
		}
	}

	private CompilationFlags EmitExpressionStart(Expression node)
	{
		if (TryPushLabelBlock(node))
		{
			return CompilationFlags.EmitExpressionStart;
		}
		return CompilationFlags.EmitNoExpressionStart;
	}

	private void EmitExpressionEnd(CompilationFlags flags)
	{
		if ((flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart)
		{
			PopLabelBlock(_labelBlock.Kind);
		}
	}

	private void EmitInvocationExpression(Expression expr, CompilationFlags flags)
	{
		InvocationExpression invocationExpression = (InvocationExpression)expr;
		if (invocationExpression.LambdaOperand != null)
		{
			EmitInlinedInvoke(invocationExpression, flags);
			return;
		}
		expr = invocationExpression.Expression;
		if (typeof(LambdaExpression).IsAssignableFrom(expr.Type))
		{
			expr = Expression.Call(expr, LambdaExpression.GetCompileMethod(expr.Type));
		}
		EmitMethodCall(expr, expr.Type.GetInvokeMethod(), invocationExpression, CompilationFlags.EmitExpressionStart | CompilationFlags.EmitAsNoTail);
	}

	private void EmitInlinedInvoke(InvocationExpression invoke, CompilationFlags flags)
	{
		LambdaExpression lambdaOperand = invoke.LambdaOperand;
		List<WriteBack> list = EmitArguments(lambdaOperand.Type.GetInvokeMethod(), invoke);
		LambdaCompiler lambdaCompiler = new LambdaCompiler(this, lambdaOperand, invoke);
		if (list != null)
		{
			flags = UpdateEmitAsTailCallFlag(flags, CompilationFlags.EmitAsNoTail);
		}
		lambdaCompiler.EmitLambdaBody(_scope, inlined: true, flags);
		EmitWriteBack(list);
	}

	private void EmitIndexExpression(Expression expr)
	{
		IndexExpression indexExpression = (IndexExpression)expr;
		Type type = null;
		if (indexExpression.Object != null)
		{
			EmitInstance(indexExpression.Object, out type);
		}
		int i = 0;
		for (int argumentCount = indexExpression.ArgumentCount; i < argumentCount; i++)
		{
			Expression argument = indexExpression.GetArgument(i);
			EmitExpression(argument);
		}
		EmitGetIndexCall(indexExpression, type);
	}

	private void EmitIndexAssignment(AssignBinaryExpression node, CompilationFlags flags)
	{
		IndexExpression indexExpression = (IndexExpression)node.Left;
		CompilationFlags compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
		Type type = null;
		if (indexExpression.Object != null)
		{
			EmitInstance(indexExpression.Object, out type);
		}
		int i = 0;
		for (int argumentCount = indexExpression.ArgumentCount; i < argumentCount; i++)
		{
			Expression argument = indexExpression.GetArgument(i);
			EmitExpression(argument);
		}
		EmitExpression(node.Right);
		LocalBuilder local = null;
		if (compilationFlags != CompilationFlags.EmitAsVoidType)
		{
			_ilg.Emit(OpCodes.Dup);
			_ilg.Emit(OpCodes.Stloc, local = GetLocal(node.Type));
		}
		EmitSetIndexCall(indexExpression, type);
		if (compilationFlags != CompilationFlags.EmitAsVoidType)
		{
			_ilg.Emit(OpCodes.Ldloc, local);
			FreeLocal(local);
		}
	}

	private void EmitGetIndexCall(IndexExpression node, Type objectType)
	{
		if (node.Indexer != null)
		{
			MethodInfo getMethod = node.Indexer.GetGetMethod(nonPublic: true);
			EmitCall(objectType, getMethod);
		}
		else
		{
			EmitGetArrayElement(objectType);
		}
	}

	private void EmitGetArrayElement(Type arrayType)
	{
		if (arrayType.IsSZArray)
		{
			_ilg.EmitLoadElement(arrayType.GetElementType());
		}
		else
		{
			_ilg.Emit(OpCodes.Call, TypeUtils.GetArrayGetMethod(arrayType));
		}
	}

	private void EmitSetIndexCall(IndexExpression node, Type objectType)
	{
		if (node.Indexer != null)
		{
			MethodInfo setMethod = node.Indexer.GetSetMethod(nonPublic: true);
			EmitCall(objectType, setMethod);
		}
		else
		{
			EmitSetArrayElement(objectType);
		}
	}

	private void EmitSetArrayElement(Type arrayType)
	{
		if (arrayType.IsSZArray)
		{
			_ilg.EmitStoreElement(arrayType.GetElementType());
		}
		else
		{
			_ilg.Emit(OpCodes.Call, TypeUtils.GetArraySetMethod(arrayType));
		}
	}

	private void EmitMethodCallExpression(Expression expr, CompilationFlags flags)
	{
		MethodCallExpression methodCallExpression = (MethodCallExpression)expr;
		EmitMethodCall(methodCallExpression.Object, methodCallExpression.Method, methodCallExpression, flags);
	}

	private void EmitMethodCallExpression(Expression expr)
	{
		EmitMethodCallExpression(expr, CompilationFlags.EmitAsNoTail);
	}

	private void EmitMethodCall(Expression obj, MethodInfo method, IArgumentProvider methodCallExpr)
	{
		EmitMethodCall(obj, method, methodCallExpr, CompilationFlags.EmitAsNoTail);
	}

	private void EmitMethodCall(Expression obj, MethodInfo method, IArgumentProvider methodCallExpr, CompilationFlags flags)
	{
		Type type = null;
		if (!method.IsStatic)
		{
			EmitInstance(obj, out type);
		}
		if (obj != null && obj.Type.IsValueType)
		{
			EmitMethodCall(method, methodCallExpr, type);
		}
		else
		{
			EmitMethodCall(method, methodCallExpr, type, flags);
		}
	}

	private void EmitMethodCall(MethodInfo mi, IArgumentProvider args, Type objectType)
	{
		EmitMethodCall(mi, args, objectType, CompilationFlags.EmitAsNoTail);
	}

	private void EmitMethodCall(MethodInfo mi, IArgumentProvider args, Type objectType, CompilationFlags flags)
	{
		List<WriteBack> writeBacks = EmitArguments(mi, args);
		OpCode opCode = (UseVirtual(mi) ? OpCodes.Callvirt : OpCodes.Call);
		if (opCode == OpCodes.Callvirt && objectType.IsValueType)
		{
			_ilg.Emit(OpCodes.Constrained, objectType);
		}
		if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail && !MethodHasByRefParameter(mi))
		{
			_ilg.Emit(OpCodes.Tailcall);
		}
		if (mi.CallingConvention == CallingConventions.VarArgs)
		{
			int argumentCount = args.ArgumentCount;
			Type[] array = new Type[argumentCount];
			for (int i = 0; i < argumentCount; i++)
			{
				array[i] = args.GetArgument(i).Type;
			}
			_ilg.EmitCall(opCode, mi, array);
		}
		else
		{
			_ilg.Emit(opCode, mi);
		}
		EmitWriteBack(writeBacks);
	}

	private static bool MethodHasByRefParameter(MethodInfo mi)
	{
		ParameterInfo[] parametersCached = mi.GetParametersCached();
		foreach (ParameterInfo pi in parametersCached)
		{
			if (pi.IsByRefParameter())
			{
				return true;
			}
		}
		return false;
	}

	private void EmitCall(Type objectType, MethodInfo method)
	{
		if (method.CallingConvention == CallingConventions.VarArgs)
		{
			throw Error.UnexpectedVarArgsCall(method);
		}
		OpCode opCode = (UseVirtual(method) ? OpCodes.Callvirt : OpCodes.Call);
		if (opCode == OpCodes.Callvirt && objectType.IsValueType)
		{
			_ilg.Emit(OpCodes.Constrained, objectType);
		}
		_ilg.Emit(opCode, method);
	}

	private static bool UseVirtual(MethodInfo mi)
	{
		if (mi.IsStatic)
		{
			return false;
		}
		if (mi.DeclaringType.IsValueType)
		{
			return false;
		}
		return true;
	}

	private List<WriteBack> EmitArguments(MethodBase method, IArgumentProvider args)
	{
		return EmitArguments(method, args, 0);
	}

	private List<WriteBack> EmitArguments(MethodBase method, IArgumentProvider args, int skipParameters)
	{
		ParameterInfo[] parametersCached = method.GetParametersCached();
		List<WriteBack> list = null;
		int i = skipParameters;
		for (int num = parametersCached.Length; i < num; i++)
		{
			ParameterInfo parameterInfo = parametersCached[i];
			Expression argument = args.GetArgument(i - skipParameters);
			Type parameterType = parameterInfo.ParameterType;
			if (parameterType.IsByRef)
			{
				parameterType = parameterType.GetElementType();
				WriteBack writeBack = EmitAddressWriteBack(argument, parameterType);
				if (writeBack != null)
				{
					if (list == null)
					{
						list = new List<WriteBack>();
					}
					list.Add(writeBack);
				}
			}
			else
			{
				EmitExpression(argument);
			}
		}
		return list;
	}

	private void EmitWriteBack(List<WriteBack> writeBacks)
	{
		if (writeBacks == null)
		{
			return;
		}
		foreach (WriteBack writeBack in writeBacks)
		{
			writeBack(this);
		}
	}

	private void EmitConstantExpression(Expression expr)
	{
		ConstantExpression constantExpression = (ConstantExpression)expr;
		EmitConstant(constantExpression.Value, constantExpression.Type);
	}

	private void EmitConstant(object value)
	{
		EmitConstant(value, value.GetType());
	}

	private void EmitConstant(object value, Type type)
	{
		if (!_ilg.TryEmitConstant(value, type, this))
		{
			_boundConstants.EmitConstant(this, value, type);
		}
	}

	private void EmitDynamicExpression(Expression expr)
	{
		IDynamicExpression dynamicExpression = (IDynamicExpression)expr;
		object obj = dynamicExpression.CreateCallSite();
		Type type = obj.GetType();
		MethodInfo invokeMethod = dynamicExpression.DelegateType.GetInvokeMethod();
		EmitConstant(obj, type);
		_ilg.Emit(OpCodes.Dup);
		LocalBuilder local = GetLocal(type);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldfld, GetCallSiteTargetField(type));
		_ilg.Emit(OpCodes.Ldloc, local);
		FreeLocal(local);
		List<WriteBack> writeBacks = EmitArguments(invokeMethod, dynamicExpression, 1);
		_ilg.Emit(OpCodes.Callvirt, invokeMethod);
		EmitWriteBack(writeBacks);
	}

	private static FieldInfo GetCallSiteTargetField(Type siteType)
	{
		return (FieldInfo)siteType.GetMemberWithSameMetadataDefinitionAs(s_callSiteTargetField);
	}

	private void EmitNewExpression(Expression expr)
	{
		NewExpression newExpression = (NewExpression)expr;
		if (newExpression.Constructor != null)
		{
			if (newExpression.Constructor.DeclaringType.IsAbstract)
			{
				throw Error.NonAbstractConstructorRequired();
			}
			List<WriteBack> writeBacks = EmitArguments(newExpression.Constructor, newExpression);
			_ilg.Emit(OpCodes.Newobj, newExpression.Constructor);
			EmitWriteBack(writeBacks);
		}
		else
		{
			LocalBuilder local = GetLocal(newExpression.Type);
			_ilg.Emit(OpCodes.Ldloca, local);
			_ilg.Emit(OpCodes.Initobj, newExpression.Type);
			_ilg.Emit(OpCodes.Ldloc, local);
			FreeLocal(local);
		}
	}

	private void EmitTypeBinaryExpression(Expression expr)
	{
		TypeBinaryExpression typeBinaryExpression = (TypeBinaryExpression)expr;
		if (typeBinaryExpression.NodeType == ExpressionType.TypeEqual)
		{
			EmitExpression(typeBinaryExpression.ReduceTypeEqual());
			return;
		}
		Type type = typeBinaryExpression.Expression.Type;
		AnalyzeTypeIsResult analyzeTypeIsResult = ConstantCheck.AnalyzeTypeIs(typeBinaryExpression);
		switch (analyzeTypeIsResult)
		{
		case AnalyzeTypeIsResult.KnownFalse:
		case AnalyzeTypeIsResult.KnownTrue:
			EmitExpressionAsVoid(typeBinaryExpression.Expression);
			_ilg.EmitPrimitive(analyzeTypeIsResult == AnalyzeTypeIsResult.KnownTrue);
			return;
		case AnalyzeTypeIsResult.KnownAssignable:
			if (type.IsNullableType())
			{
				EmitAddress(typeBinaryExpression.Expression, type);
				_ilg.EmitHasValue(type);
			}
			else
			{
				EmitExpression(typeBinaryExpression.Expression);
				_ilg.Emit(OpCodes.Ldnull);
				_ilg.Emit(OpCodes.Cgt_Un);
			}
			return;
		}
		EmitExpression(typeBinaryExpression.Expression);
		if (type.IsValueType)
		{
			_ilg.Emit(OpCodes.Box, type);
		}
		_ilg.Emit(OpCodes.Isinst, typeBinaryExpression.TypeOperand);
		_ilg.Emit(OpCodes.Ldnull);
		_ilg.Emit(OpCodes.Cgt_Un);
	}

	private void EmitVariableAssignment(AssignBinaryExpression node, CompilationFlags flags)
	{
		ParameterExpression parameterExpression = (ParameterExpression)node.Left;
		CompilationFlags compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
		if (node.IsByRef)
		{
			EmitAddress(node.Right, node.Right.Type);
		}
		else
		{
			EmitExpression(node.Right);
		}
		if (compilationFlags != CompilationFlags.EmitAsVoidType)
		{
			_ilg.Emit(OpCodes.Dup);
		}
		if (parameterExpression.IsByRef)
		{
			LocalBuilder local = GetLocal(parameterExpression.Type);
			_ilg.Emit(OpCodes.Stloc, local);
			_scope.EmitGet(parameterExpression);
			_ilg.Emit(OpCodes.Ldloc, local);
			FreeLocal(local);
			_ilg.EmitStoreValueIndirect(parameterExpression.Type);
		}
		else
		{
			_scope.EmitSet(parameterExpression);
		}
	}

	private void EmitAssignBinaryExpression(Expression expr)
	{
		EmitAssign((AssignBinaryExpression)expr, CompilationFlags.EmitAsDefaultType);
	}

	private void EmitAssign(AssignBinaryExpression node, CompilationFlags emitAs)
	{
		switch (node.Left.NodeType)
		{
		case ExpressionType.Index:
			EmitIndexAssignment(node, emitAs);
			break;
		case ExpressionType.MemberAccess:
			EmitMemberAssignment(node, emitAs);
			break;
		case ExpressionType.Parameter:
			EmitVariableAssignment(node, emitAs);
			break;
		default:
			throw ContractUtils.Unreachable;
		}
	}

	private void EmitParameterExpression(Expression expr)
	{
		ParameterExpression parameterExpression = (ParameterExpression)expr;
		_scope.EmitGet(parameterExpression);
		if (parameterExpression.IsByRef)
		{
			_ilg.EmitLoadValueIndirect(parameterExpression.Type);
		}
	}

	private void EmitLambdaExpression(Expression expr)
	{
		LambdaExpression lambda = (LambdaExpression)expr;
		EmitDelegateConstruction(lambda);
	}

	private void EmitRuntimeVariablesExpression(Expression expr)
	{
		RuntimeVariablesExpression runtimeVariablesExpression = (RuntimeVariablesExpression)expr;
		_scope.EmitVariableAccess(this, runtimeVariablesExpression.Variables);
	}

	private void EmitMemberAssignment(AssignBinaryExpression node, CompilationFlags flags)
	{
		MemberExpression memberExpression = (MemberExpression)node.Left;
		MemberInfo member = memberExpression.Member;
		Type type = null;
		if (memberExpression.Expression != null)
		{
			EmitInstance(memberExpression.Expression, out type);
		}
		EmitExpression(node.Right);
		LocalBuilder local = null;
		CompilationFlags compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
		if (compilationFlags != CompilationFlags.EmitAsVoidType)
		{
			_ilg.Emit(OpCodes.Dup);
			_ilg.Emit(OpCodes.Stloc, local = GetLocal(node.Type));
		}
		if (member is FieldInfo)
		{
			_ilg.EmitFieldSet((FieldInfo)member);
		}
		else
		{
			PropertyInfo propertyInfo = (PropertyInfo)member;
			EmitCall(type, propertyInfo.GetSetMethod(nonPublic: true));
		}
		if (compilationFlags != CompilationFlags.EmitAsVoidType)
		{
			_ilg.Emit(OpCodes.Ldloc, local);
			FreeLocal(local);
		}
	}

	private void EmitMemberExpression(Expression expr)
	{
		MemberExpression memberExpression = (MemberExpression)expr;
		Type type = null;
		if (memberExpression.Expression != null)
		{
			EmitInstance(memberExpression.Expression, out type);
		}
		EmitMemberGet(memberExpression.Member, type);
	}

	private void EmitMemberGet(MemberInfo member, Type objectType)
	{
		if (member is FieldInfo fieldInfo)
		{
			if (fieldInfo.IsLiteral)
			{
				EmitConstant(fieldInfo.GetRawConstantValue(), fieldInfo.FieldType);
			}
			else
			{
				_ilg.EmitFieldGet(fieldInfo);
			}
		}
		else
		{
			PropertyInfo propertyInfo = (PropertyInfo)member;
			EmitCall(objectType, propertyInfo.GetGetMethod(nonPublic: true));
		}
	}

	private void EmitInstance(Expression instance, out Type type)
	{
		type = instance.Type;
		if (type.IsByRef)
		{
			type = type.GetElementType();
			EmitExpression(instance);
		}
		else if (type.IsValueType)
		{
			EmitAddress(instance, type);
		}
		else
		{
			EmitExpression(instance);
		}
	}

	private void EmitNewArrayExpression(Expression expr)
	{
		NewArrayExpression newArrayExpression = (NewArrayExpression)expr;
		ReadOnlyCollection<Expression> expressions = newArrayExpression.Expressions;
		int count = expressions.Count;
		if (newArrayExpression.NodeType == ExpressionType.NewArrayInit)
		{
			Type elementType = newArrayExpression.Type.GetElementType();
			_ilg.EmitArray(elementType, count);
			for (int i = 0; i < count; i++)
			{
				_ilg.Emit(OpCodes.Dup);
				_ilg.EmitPrimitive(i);
				EmitExpression(expressions[i]);
				_ilg.EmitStoreElement(elementType);
			}
		}
		else
		{
			for (int j = 0; j < count; j++)
			{
				Expression expression = expressions[j];
				EmitExpression(expression);
				_ilg.EmitConvertToType(expression.Type, typeof(int), isChecked: true, this);
			}
			_ilg.EmitArray(newArrayExpression.Type);
		}
	}

	private void EmitDebugInfoExpression(Expression expr)
	{
	}

	private void EmitListInitExpression(Expression expr)
	{
		EmitListInit((ListInitExpression)expr);
	}

	private void EmitMemberInitExpression(Expression expr)
	{
		EmitMemberInit((MemberInitExpression)expr);
	}

	private void EmitBinding(MemberBinding binding, Type objectType)
	{
		switch (binding.BindingType)
		{
		case MemberBindingType.Assignment:
			EmitMemberAssignment((MemberAssignment)binding, objectType);
			break;
		case MemberBindingType.ListBinding:
			EmitMemberListBinding((MemberListBinding)binding);
			break;
		case MemberBindingType.MemberBinding:
			EmitMemberMemberBinding((MemberMemberBinding)binding);
			break;
		}
	}

	private void EmitMemberAssignment(MemberAssignment binding, Type objectType)
	{
		EmitExpression(binding.Expression);
		if (binding.Member is FieldInfo field)
		{
			_ilg.Emit(OpCodes.Stfld, field);
		}
		else
		{
			EmitCall(objectType, (binding.Member as PropertyInfo).GetSetMethod(nonPublic: true));
		}
	}

	private void EmitMemberMemberBinding(MemberMemberBinding binding)
	{
		Type memberType = GetMemberType(binding.Member);
		if (binding.Member is PropertyInfo && memberType.IsValueType)
		{
			throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(binding.Member);
		}
		if (memberType.IsValueType)
		{
			EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
		}
		else
		{
			EmitMemberGet(binding.Member, binding.Member.DeclaringType);
		}
		EmitMemberInit(binding.Bindings, keepOnStack: false, memberType);
	}

	private void EmitMemberListBinding(MemberListBinding binding)
	{
		Type memberType = GetMemberType(binding.Member);
		if (binding.Member is PropertyInfo && memberType.IsValueType)
		{
			throw Error.CannotAutoInitializeValueTypeElementThroughProperty(binding.Member);
		}
		if (memberType.IsValueType)
		{
			EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
		}
		else
		{
			EmitMemberGet(binding.Member, binding.Member.DeclaringType);
		}
		EmitListInit(binding.Initializers, keepOnStack: false, memberType);
	}

	private void EmitMemberInit(MemberInitExpression init)
	{
		EmitExpression(init.NewExpression);
		LocalBuilder localBuilder = null;
		if (init.NewExpression.Type.IsValueType && init.Bindings.Count > 0)
		{
			localBuilder = GetLocal(init.NewExpression.Type);
			_ilg.Emit(OpCodes.Stloc, localBuilder);
			_ilg.Emit(OpCodes.Ldloca, localBuilder);
		}
		EmitMemberInit(init.Bindings, localBuilder == null, init.NewExpression.Type);
		if (localBuilder != null)
		{
			_ilg.Emit(OpCodes.Ldloc, localBuilder);
			FreeLocal(localBuilder);
		}
	}

	private void EmitMemberInit(ReadOnlyCollection<MemberBinding> bindings, bool keepOnStack, Type objectType)
	{
		int count = bindings.Count;
		if (count == 0)
		{
			if (!keepOnStack)
			{
				_ilg.Emit(OpCodes.Pop);
			}
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (keepOnStack || i < count - 1)
			{
				_ilg.Emit(OpCodes.Dup);
			}
			EmitBinding(bindings[i], objectType);
		}
	}

	private void EmitListInit(ListInitExpression init)
	{
		EmitExpression(init.NewExpression);
		LocalBuilder localBuilder = null;
		if (init.NewExpression.Type.IsValueType)
		{
			localBuilder = GetLocal(init.NewExpression.Type);
			_ilg.Emit(OpCodes.Stloc, localBuilder);
			_ilg.Emit(OpCodes.Ldloca, localBuilder);
		}
		EmitListInit(init.Initializers, localBuilder == null, init.NewExpression.Type);
		if (localBuilder != null)
		{
			_ilg.Emit(OpCodes.Ldloc, localBuilder);
			FreeLocal(localBuilder);
		}
	}

	private void EmitListInit(ReadOnlyCollection<ElementInit> initializers, bool keepOnStack, Type objectType)
	{
		int count = initializers.Count;
		if (count == 0)
		{
			if (!keepOnStack)
			{
				_ilg.Emit(OpCodes.Pop);
			}
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (keepOnStack || i < count - 1)
			{
				_ilg.Emit(OpCodes.Dup);
			}
			EmitMethodCall(initializers[i].AddMethod, initializers[i], objectType);
			if (initializers[i].AddMethod.ReturnType != typeof(void))
			{
				_ilg.Emit(OpCodes.Pop);
			}
		}
	}

	private static Type GetMemberType(MemberInfo member)
	{
		if (!(member is FieldInfo fieldInfo))
		{
			return (member as PropertyInfo).PropertyType;
		}
		return fieldInfo.FieldType;
	}

	private void EmitLift(ExpressionType nodeType, Type resultType, MethodCallExpression mc, ParameterExpression[] paramList, Expression[] argList)
	{
		switch (nodeType)
		{
		default:
		{
			Label label4 = _ilg.DefineLabel();
			Label label5 = _ilg.DefineLabel();
			LocalBuilder local3 = GetLocal(typeof(bool));
			_ilg.Emit(OpCodes.Ldc_I4_0);
			_ilg.Emit(OpCodes.Stloc, local3);
			int j = 0;
			for (int num2 = paramList.Length; j < num2; j++)
			{
				ParameterExpression variable2 = paramList[j];
				Expression expression2 = argList[j];
				if (expression2.Type.IsNullableType())
				{
					_scope.AddLocal(this, variable2);
					EmitAddress(expression2, expression2.Type);
					_ilg.Emit(OpCodes.Dup);
					_ilg.EmitHasValue(expression2.Type);
					_ilg.Emit(OpCodes.Ldc_I4_0);
					_ilg.Emit(OpCodes.Ceq);
					_ilg.Emit(OpCodes.Stloc, local3);
					_ilg.EmitGetValueOrDefault(expression2.Type);
					_scope.EmitSet(variable2);
				}
				else
				{
					_scope.AddLocal(this, variable2);
					EmitExpression(expression2);
					if (!expression2.Type.IsValueType)
					{
						_ilg.Emit(OpCodes.Dup);
						_ilg.Emit(OpCodes.Ldnull);
						_ilg.Emit(OpCodes.Ceq);
						_ilg.Emit(OpCodes.Stloc, local3);
					}
					_scope.EmitSet(variable2);
				}
				_ilg.Emit(OpCodes.Ldloc, local3);
				_ilg.Emit(OpCodes.Brtrue, label5);
			}
			EmitMethodCallExpression(mc);
			if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
			{
				ConstructorInfo nullableConstructor2 = TypeUtils.GetNullableConstructor(resultType);
				_ilg.Emit(OpCodes.Newobj, nullableConstructor2);
			}
			_ilg.Emit(OpCodes.Br_S, label4);
			_ilg.MarkLabel(label5);
			if (TypeUtils.AreEquivalent(resultType, mc.Type.GetNullableType()))
			{
				if (resultType.IsValueType)
				{
					LocalBuilder local4 = GetLocal(resultType);
					_ilg.Emit(OpCodes.Ldloca, local4);
					_ilg.Emit(OpCodes.Initobj, resultType);
					_ilg.Emit(OpCodes.Ldloc, local4);
					FreeLocal(local4);
				}
				else
				{
					_ilg.Emit(OpCodes.Ldnull);
				}
			}
			else
			{
				_ilg.Emit(OpCodes.Ldc_I4_0);
			}
			_ilg.MarkLabel(label4);
			FreeLocal(local3);
			break;
		}
		case ExpressionType.Equal:
		case ExpressionType.NotEqual:
			if (!TypeUtils.AreEquivalent(resultType, mc.Type.GetNullableType()))
			{
				Label label = _ilg.DefineLabel();
				Label label2 = _ilg.DefineLabel();
				Label label3 = _ilg.DefineLabel();
				LocalBuilder local = GetLocal(typeof(bool));
				LocalBuilder local2 = GetLocal(typeof(bool));
				_ilg.Emit(OpCodes.Ldc_I4_0);
				_ilg.Emit(OpCodes.Stloc, local);
				_ilg.Emit(OpCodes.Ldc_I4_1);
				_ilg.Emit(OpCodes.Stloc, local2);
				int i = 0;
				for (int num = paramList.Length; i < num; i++)
				{
					ParameterExpression variable = paramList[i];
					Expression expression = argList[i];
					_scope.AddLocal(this, variable);
					if (expression.Type.IsNullableType())
					{
						EmitAddress(expression, expression.Type);
						_ilg.Emit(OpCodes.Dup);
						_ilg.EmitHasValue(expression.Type);
						_ilg.Emit(OpCodes.Ldc_I4_0);
						_ilg.Emit(OpCodes.Ceq);
						_ilg.Emit(OpCodes.Dup);
						_ilg.Emit(OpCodes.Ldloc, local);
						_ilg.Emit(OpCodes.Or);
						_ilg.Emit(OpCodes.Stloc, local);
						_ilg.Emit(OpCodes.Ldloc, local2);
						_ilg.Emit(OpCodes.And);
						_ilg.Emit(OpCodes.Stloc, local2);
						_ilg.EmitGetValueOrDefault(expression.Type);
					}
					else
					{
						EmitExpression(expression);
						if (!expression.Type.IsValueType)
						{
							_ilg.Emit(OpCodes.Dup);
							_ilg.Emit(OpCodes.Ldnull);
							_ilg.Emit(OpCodes.Ceq);
							_ilg.Emit(OpCodes.Dup);
							_ilg.Emit(OpCodes.Ldloc, local);
							_ilg.Emit(OpCodes.Or);
							_ilg.Emit(OpCodes.Stloc, local);
							_ilg.Emit(OpCodes.Ldloc, local2);
							_ilg.Emit(OpCodes.And);
							_ilg.Emit(OpCodes.Stloc, local2);
						}
						else
						{
							_ilg.Emit(OpCodes.Ldc_I4_0);
							_ilg.Emit(OpCodes.Stloc, local2);
						}
					}
					_scope.EmitSet(variable);
				}
				_ilg.Emit(OpCodes.Ldloc, local2);
				_ilg.Emit(OpCodes.Brtrue, label2);
				_ilg.Emit(OpCodes.Ldloc, local);
				_ilg.Emit(OpCodes.Brtrue, label3);
				EmitMethodCallExpression(mc);
				if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
				{
					ConstructorInfo nullableConstructor = TypeUtils.GetNullableConstructor(resultType);
					_ilg.Emit(OpCodes.Newobj, nullableConstructor);
				}
				_ilg.Emit(OpCodes.Br_S, label);
				_ilg.MarkLabel(label2);
				_ilg.EmitPrimitive(nodeType == ExpressionType.Equal);
				_ilg.Emit(OpCodes.Br_S, label);
				_ilg.MarkLabel(label3);
				_ilg.EmitPrimitive(nodeType == ExpressionType.NotEqual);
				_ilg.MarkLabel(label);
				FreeLocal(local);
				FreeLocal(local2);
				break;
			}
			goto default;
		}
	}

	private void EmitExpression(Expression node, CompilationFlags flags)
	{
		if (!_guard.TryEnterOnCurrentStack())
		{
			_guard.RunOnEmptyStack(delegate(LambdaCompiler @this, Expression n, CompilationFlags f)
			{
				@this.EmitExpression(n, f);
			}, this, node, flags);
			return;
		}
		bool flag = (flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart;
		CompilationFlags flags2 = (flag ? EmitExpressionStart(node) : CompilationFlags.EmitNoExpressionStart);
		flags &= CompilationFlags.EmitAsTailCallMask;
		switch (node.NodeType)
		{
		case ExpressionType.Add:
		case ExpressionType.AddChecked:
		case ExpressionType.And:
		case ExpressionType.ArrayIndex:
		case ExpressionType.Divide:
		case ExpressionType.Equal:
		case ExpressionType.ExclusiveOr:
		case ExpressionType.GreaterThan:
		case ExpressionType.GreaterThanOrEqual:
		case ExpressionType.LeftShift:
		case ExpressionType.LessThan:
		case ExpressionType.LessThanOrEqual:
		case ExpressionType.Modulo:
		case ExpressionType.Multiply:
		case ExpressionType.MultiplyChecked:
		case ExpressionType.NotEqual:
		case ExpressionType.Or:
		case ExpressionType.Power:
		case ExpressionType.RightShift:
		case ExpressionType.Subtract:
		case ExpressionType.SubtractChecked:
			EmitBinaryExpression(node, flags);
			break;
		case ExpressionType.AndAlso:
			EmitAndAlsoBinaryExpression(node, flags);
			break;
		case ExpressionType.OrElse:
			EmitOrElseBinaryExpression(node, flags);
			break;
		case ExpressionType.Coalesce:
			EmitCoalesceBinaryExpression(node);
			break;
		case ExpressionType.Assign:
			EmitAssignBinaryExpression(node);
			break;
		case ExpressionType.ArrayLength:
		case ExpressionType.Negate:
		case ExpressionType.UnaryPlus:
		case ExpressionType.NegateChecked:
		case ExpressionType.Not:
		case ExpressionType.TypeAs:
		case ExpressionType.Decrement:
		case ExpressionType.Increment:
		case ExpressionType.OnesComplement:
		case ExpressionType.IsTrue:
		case ExpressionType.IsFalse:
			EmitUnaryExpression(node, flags);
			break;
		case ExpressionType.Convert:
		case ExpressionType.ConvertChecked:
			EmitConvertUnaryExpression(node, flags);
			break;
		case ExpressionType.Quote:
			EmitQuoteUnaryExpression(node);
			break;
		case ExpressionType.Throw:
			EmitThrowUnaryExpression(node);
			break;
		case ExpressionType.Unbox:
			EmitUnboxUnaryExpression(node);
			break;
		case ExpressionType.Call:
			EmitMethodCallExpression(node, flags);
			break;
		case ExpressionType.Conditional:
			EmitConditionalExpression(node, flags);
			break;
		case ExpressionType.Constant:
			EmitConstantExpression(node);
			break;
		case ExpressionType.Invoke:
			EmitInvocationExpression(node, flags);
			break;
		case ExpressionType.Lambda:
			EmitLambdaExpression(node);
			break;
		case ExpressionType.ListInit:
			EmitListInitExpression(node);
			break;
		case ExpressionType.MemberAccess:
			EmitMemberExpression(node);
			break;
		case ExpressionType.MemberInit:
			EmitMemberInitExpression(node);
			break;
		case ExpressionType.New:
			EmitNewExpression(node);
			break;
		case ExpressionType.NewArrayInit:
		case ExpressionType.NewArrayBounds:
			EmitNewArrayExpression(node);
			break;
		case ExpressionType.Parameter:
			EmitParameterExpression(node);
			break;
		case ExpressionType.TypeIs:
		case ExpressionType.TypeEqual:
			EmitTypeBinaryExpression(node);
			break;
		case ExpressionType.Block:
			EmitBlockExpression(node, flags);
			break;
		case ExpressionType.DebugInfo:
			EmitDebugInfoExpression(node);
			break;
		case ExpressionType.Dynamic:
			EmitDynamicExpression(node);
			break;
		case ExpressionType.Default:
			EmitDefaultExpression(node);
			break;
		case ExpressionType.Goto:
			EmitGotoExpression(node, flags);
			break;
		case ExpressionType.Index:
			EmitIndexExpression(node);
			break;
		case ExpressionType.Label:
			EmitLabelExpression(node, flags);
			break;
		case ExpressionType.RuntimeVariables:
			EmitRuntimeVariablesExpression(node);
			break;
		case ExpressionType.Loop:
			EmitLoopExpression(node);
			break;
		case ExpressionType.Switch:
			EmitSwitchExpression(node, flags);
			break;
		case ExpressionType.Try:
			EmitTryExpression(node);
			break;
		}
		if (flag)
		{
			EmitExpressionEnd(flags2);
		}
	}

	private static bool IsChecked(ExpressionType op)
	{
		switch (op)
		{
		case ExpressionType.AddChecked:
		case ExpressionType.ConvertChecked:
		case ExpressionType.MultiplyChecked:
		case ExpressionType.NegateChecked:
		case ExpressionType.SubtractChecked:
		case ExpressionType.AddAssignChecked:
		case ExpressionType.MultiplyAssignChecked:
		case ExpressionType.SubtractAssignChecked:
			return true;
		default:
			return false;
		}
	}

	internal void EmitConstantArray<T>(T[] array)
	{
		EmitConstant(array, typeof(T[]));
	}

	private void EmitClosureCreation(LambdaCompiler inner)
	{
		bool needsClosure = inner._scope.NeedsClosure;
		bool flag = inner._boundConstants.Count > 0;
		if (!needsClosure && !flag)
		{
			_ilg.EmitNull();
			return;
		}
		if (flag)
		{
			_boundConstants.EmitConstant(this, inner._boundConstants.ToArray(), typeof(object[]));
		}
		else
		{
			_ilg.EmitNull();
		}
		if (needsClosure)
		{
			_scope.EmitGet(_scope.NearestHoistedLocals.SelfVariable);
		}
		else
		{
			_ilg.EmitNull();
		}
		_ilg.EmitNew(CachedReflectionInfo.Closure_ObjectArray_ObjectArray);
	}

	private void EmitDelegateConstruction(LambdaCompiler inner)
	{
		Type type = inner._lambda.Type;
		DynamicMethod value = inner._method as DynamicMethod;
		_boundConstants.EmitConstant(this, value, typeof(MethodInfo));
		_ilg.EmitType(type);
		EmitClosureCreation(inner);
		_ilg.Emit(OpCodes.Callvirt, CachedReflectionInfo.MethodInfo_CreateDelegate_Type_Object);
		_ilg.Emit(OpCodes.Castclass, type);
	}

	private void EmitDelegateConstruction(LambdaExpression lambda)
	{
		LambdaCompiler lambdaCompiler = new LambdaCompiler(_tree, lambda);
		lambdaCompiler.EmitLambdaBody(_scope, inlined: false, CompilationFlags.EmitAsNoTail);
		EmitDelegateConstruction(lambdaCompiler);
	}

	private static Type[] GetParameterTypes(LambdaExpression lambda, Type firstType)
	{
		int parameterCount = lambda.ParameterCount;
		Type[] array;
		int num;
		if (firstType != null)
		{
			array = new Type[parameterCount + 1];
			array[0] = firstType;
			num = 1;
		}
		else
		{
			array = new Type[parameterCount];
			num = 0;
		}
		int num2 = 0;
		while (num2 < parameterCount)
		{
			ParameterExpression parameter = lambda.GetParameter(num2);
			array[num] = (parameter.IsByRef ? parameter.Type.MakeByRefType() : parameter.Type);
			num2++;
			num++;
		}
		return array;
	}

	private void EmitLambdaBody()
	{
		CompilationFlags flags = (_lambda.TailCall ? CompilationFlags.EmitAsTail : CompilationFlags.EmitAsNoTail);
		EmitLambdaBody(null, inlined: false, flags);
	}

	private void EmitLambdaBody(CompilerScope parent, bool inlined, CompilationFlags flags)
	{
		_scope.Enter(this, parent);
		if (inlined)
		{
			for (int num = _lambda.ParameterCount - 1; num >= 0; num--)
			{
				_scope.EmitSet(_lambda.GetParameter(num));
			}
		}
		flags = UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart);
		if (_lambda.ReturnType == typeof(void))
		{
			EmitExpressionAsVoid(_lambda.Body, flags);
		}
		else
		{
			EmitExpression(_lambda.Body, flags);
		}
		if (!inlined)
		{
			_ilg.Emit(OpCodes.Ret);
		}
		_scope.Exit();
		foreach (LabelInfo value in _labelInfo.Values)
		{
			value.ValidateFinish();
		}
	}

	private void EmitConditionalExpression(Expression expr, CompilationFlags flags)
	{
		ConditionalExpression conditionalExpression = (ConditionalExpression)expr;
		Label label = _ilg.DefineLabel();
		EmitExpressionAndBranch(branchValue: false, conditionalExpression.Test, label);
		EmitExpressionAsType(conditionalExpression.IfTrue, conditionalExpression.Type, flags);
		if (NotEmpty(conditionalExpression.IfFalse))
		{
			Label label2 = _ilg.DefineLabel();
			if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
			{
				_ilg.Emit(OpCodes.Ret);
			}
			else
			{
				_ilg.Emit(OpCodes.Br, label2);
			}
			_ilg.MarkLabel(label);
			EmitExpressionAsType(conditionalExpression.IfFalse, conditionalExpression.Type, flags);
			_ilg.MarkLabel(label2);
		}
		else
		{
			_ilg.MarkLabel(label);
		}
	}

	private static bool NotEmpty(Expression node)
	{
		if (!(node is DefaultExpression defaultExpression) || defaultExpression.Type != typeof(void))
		{
			return true;
		}
		return false;
	}

	private static bool Significant(Expression node)
	{
		if (node is BlockExpression blockExpression)
		{
			for (int i = 0; i < blockExpression.ExpressionCount; i++)
			{
				if (Significant(blockExpression.GetExpression(i)))
				{
					return true;
				}
			}
			return false;
		}
		if (NotEmpty(node))
		{
			return !(node is DebugInfoExpression);
		}
		return false;
	}

	private void EmitCoalesceBinaryExpression(Expression expr)
	{
		BinaryExpression binaryExpression = (BinaryExpression)expr;
		if (binaryExpression.Left.Type.IsNullableType())
		{
			EmitNullableCoalesce(binaryExpression);
		}
		else if (binaryExpression.Conversion != null)
		{
			EmitLambdaReferenceCoalesce(binaryExpression);
		}
		else
		{
			EmitReferenceCoalesceWithoutConversion(binaryExpression);
		}
	}

	private void EmitNullableCoalesce(BinaryExpression b)
	{
		LocalBuilder local = GetLocal(b.Left.Type);
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		EmitExpression(b.Left);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitHasValue(b.Left.Type);
		_ilg.Emit(OpCodes.Brfalse, label);
		Type nonNullableType = b.Left.Type.GetNonNullableType();
		if (b.Conversion != null)
		{
			ParameterExpression parameter = b.Conversion.GetParameter(0);
			EmitLambdaExpression(b.Conversion);
			if (!parameter.Type.IsAssignableFrom(b.Left.Type))
			{
				_ilg.Emit(OpCodes.Ldloca, local);
				_ilg.EmitGetValueOrDefault(b.Left.Type);
			}
			else
			{
				_ilg.Emit(OpCodes.Ldloc, local);
			}
			_ilg.Emit(OpCodes.Callvirt, b.Conversion.Type.GetInvokeMethod());
		}
		else if (TypeUtils.AreEquivalent(b.Type, b.Left.Type))
		{
			_ilg.Emit(OpCodes.Ldloc, local);
		}
		else
		{
			_ilg.Emit(OpCodes.Ldloca, local);
			_ilg.EmitGetValueOrDefault(b.Left.Type);
			if (!TypeUtils.AreEquivalent(b.Type, nonNullableType))
			{
				_ilg.EmitConvertToType(nonNullableType, b.Type, isChecked: true, this);
			}
		}
		FreeLocal(local);
		_ilg.Emit(OpCodes.Br, label2);
		_ilg.MarkLabel(label);
		EmitExpression(b.Right);
		if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
		{
			_ilg.EmitConvertToType(b.Right.Type, b.Type, isChecked: true, this);
		}
		_ilg.MarkLabel(label2);
	}

	private void EmitLambdaReferenceCoalesce(BinaryExpression b)
	{
		LocalBuilder local = GetLocal(b.Left.Type);
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		EmitExpression(b.Left);
		_ilg.Emit(OpCodes.Dup);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Brtrue, label2);
		EmitExpression(b.Right);
		_ilg.Emit(OpCodes.Br, label);
		_ilg.MarkLabel(label2);
		EmitLambdaExpression(b.Conversion);
		_ilg.Emit(OpCodes.Ldloc, local);
		FreeLocal(local);
		_ilg.Emit(OpCodes.Callvirt, b.Conversion.Type.GetInvokeMethod());
		_ilg.MarkLabel(label);
	}

	private void EmitReferenceCoalesceWithoutConversion(BinaryExpression b)
	{
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		EmitExpression(b.Left);
		_ilg.Emit(OpCodes.Dup);
		_ilg.Emit(OpCodes.Brtrue, label2);
		_ilg.Emit(OpCodes.Pop);
		EmitExpression(b.Right);
		if (!TypeUtils.AreEquivalent(b.Right.Type, b.Type))
		{
			if (b.Right.Type.IsValueType)
			{
				_ilg.Emit(OpCodes.Box, b.Right.Type);
			}
			_ilg.Emit(OpCodes.Castclass, b.Type);
		}
		_ilg.Emit(OpCodes.Br_S, label);
		_ilg.MarkLabel(label2);
		if (!TypeUtils.AreEquivalent(b.Left.Type, b.Type))
		{
			_ilg.Emit(OpCodes.Castclass, b.Type);
		}
		_ilg.MarkLabel(label);
	}

	private void EmitLiftedAndAlso(BinaryExpression b)
	{
		Type typeFromHandle = typeof(bool?);
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		Label label3 = _ilg.DefineLabel();
		EmitExpression(b.Left);
		LocalBuilder local = GetLocal(typeFromHandle);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitHasValue(typeFromHandle);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Not);
		_ilg.Emit(OpCodes.And);
		_ilg.Emit(OpCodes.Brtrue, label);
		EmitExpression(b.Right);
		LocalBuilder local2 = GetLocal(typeFromHandle);
		_ilg.Emit(OpCodes.Stloc, local2);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Brtrue_S, label2);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Brtrue_S, label);
		_ilg.MarkLabel(label2);
		_ilg.Emit(OpCodes.Ldloc, local2);
		FreeLocal(local2);
		_ilg.Emit(OpCodes.Br_S, label3);
		_ilg.MarkLabel(label);
		_ilg.Emit(OpCodes.Ldloc, local);
		FreeLocal(local);
		_ilg.MarkLabel(label3);
	}

	private void EmitMethodAndAlso(BinaryExpression b, CompilationFlags flags)
	{
		Label label = _ilg.DefineLabel();
		EmitExpression(b.Left);
		_ilg.Emit(OpCodes.Dup);
		MethodInfo booleanOperator = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_False");
		_ilg.Emit(OpCodes.Call, booleanOperator);
		_ilg.Emit(OpCodes.Brtrue, label);
		EmitExpression(b.Right);
		if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
		{
			_ilg.Emit(OpCodes.Tailcall);
		}
		_ilg.Emit(OpCodes.Call, b.Method);
		_ilg.MarkLabel(label);
	}

	private void EmitUnliftedAndAlso(BinaryExpression b)
	{
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		EmitExpressionAndBranch(branchValue: false, b.Left, label);
		EmitExpression(b.Right);
		_ilg.Emit(OpCodes.Br, label2);
		_ilg.MarkLabel(label);
		_ilg.Emit(OpCodes.Ldc_I4_0);
		_ilg.MarkLabel(label2);
	}

	private void EmitAndAlsoBinaryExpression(Expression expr, CompilationFlags flags)
	{
		BinaryExpression binaryExpression = (BinaryExpression)expr;
		if (binaryExpression.Method != null)
		{
			if (binaryExpression.IsLiftedLogical)
			{
				EmitExpression(binaryExpression.ReduceUserdefinedLifted());
			}
			else
			{
				EmitMethodAndAlso(binaryExpression, flags);
			}
		}
		else if (binaryExpression.Left.Type == typeof(bool?))
		{
			EmitLiftedAndAlso(binaryExpression);
		}
		else
		{
			EmitUnliftedAndAlso(binaryExpression);
		}
	}

	private void EmitLiftedOrElse(BinaryExpression b)
	{
		Type typeFromHandle = typeof(bool?);
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		LocalBuilder local = GetLocal(typeFromHandle);
		EmitExpression(b.Left);
		_ilg.Emit(OpCodes.Stloc, local);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Brtrue, label);
		EmitExpression(b.Right);
		LocalBuilder local2 = GetLocal(typeFromHandle);
		_ilg.Emit(OpCodes.Stloc, local2);
		_ilg.Emit(OpCodes.Ldloca, local2);
		_ilg.EmitGetValueOrDefault(typeFromHandle);
		_ilg.Emit(OpCodes.Ldloca, local);
		_ilg.EmitHasValue(typeFromHandle);
		_ilg.Emit(OpCodes.Or);
		_ilg.Emit(OpCodes.Brfalse_S, label);
		_ilg.Emit(OpCodes.Ldloc, local2);
		FreeLocal(local2);
		_ilg.Emit(OpCodes.Br_S, label2);
		_ilg.MarkLabel(label);
		_ilg.Emit(OpCodes.Ldloc, local);
		FreeLocal(local);
		_ilg.MarkLabel(label2);
	}

	private void EmitUnliftedOrElse(BinaryExpression b)
	{
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		EmitExpressionAndBranch(branchValue: false, b.Left, label);
		_ilg.Emit(OpCodes.Ldc_I4_1);
		_ilg.Emit(OpCodes.Br, label2);
		_ilg.MarkLabel(label);
		EmitExpression(b.Right);
		_ilg.MarkLabel(label2);
	}

	private void EmitMethodOrElse(BinaryExpression b, CompilationFlags flags)
	{
		Label label = _ilg.DefineLabel();
		EmitExpression(b.Left);
		_ilg.Emit(OpCodes.Dup);
		MethodInfo booleanOperator = TypeUtils.GetBooleanOperator(b.Method.DeclaringType, "op_True");
		_ilg.Emit(OpCodes.Call, booleanOperator);
		_ilg.Emit(OpCodes.Brtrue, label);
		EmitExpression(b.Right);
		if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
		{
			_ilg.Emit(OpCodes.Tailcall);
		}
		_ilg.Emit(OpCodes.Call, b.Method);
		_ilg.MarkLabel(label);
	}

	private void EmitOrElseBinaryExpression(Expression expr, CompilationFlags flags)
	{
		BinaryExpression binaryExpression = (BinaryExpression)expr;
		if (binaryExpression.Method != null)
		{
			if (binaryExpression.IsLiftedLogical)
			{
				EmitExpression(binaryExpression.ReduceUserdefinedLifted());
			}
			else
			{
				EmitMethodOrElse(binaryExpression, flags);
			}
		}
		else if (binaryExpression.Left.Type == typeof(bool?))
		{
			EmitLiftedOrElse(binaryExpression);
		}
		else
		{
			EmitUnliftedOrElse(binaryExpression);
		}
	}

	private void EmitExpressionAndBranch(bool branchValue, Expression node, Label label)
	{
		CompilationFlags flags = EmitExpressionStart(node);
		switch (node.NodeType)
		{
		case ExpressionType.Not:
			EmitBranchNot(branchValue, (UnaryExpression)node, label);
			break;
		case ExpressionType.AndAlso:
		case ExpressionType.OrElse:
			EmitBranchLogical(branchValue, (BinaryExpression)node, label);
			break;
		case ExpressionType.Block:
			EmitBranchBlock(branchValue, (BlockExpression)node, label);
			break;
		case ExpressionType.Equal:
		case ExpressionType.NotEqual:
			EmitBranchComparison(branchValue, (BinaryExpression)node, label);
			break;
		default:
			EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
			EmitBranchOp(branchValue, label);
			break;
		}
		EmitExpressionEnd(flags);
	}

	private void EmitBranchOp(bool branch, Label label)
	{
		_ilg.Emit(branch ? OpCodes.Brtrue : OpCodes.Brfalse, label);
	}

	private void EmitBranchNot(bool branch, UnaryExpression node, Label label)
	{
		if (node.Method != null)
		{
			EmitExpression(node, CompilationFlags.EmitNoExpressionStart | CompilationFlags.EmitAsNoTail);
			EmitBranchOp(branch, label);
		}
		else
		{
			EmitExpressionAndBranch(!branch, node.Operand, label);
		}
	}

	private void EmitBranchComparison(bool branch, BinaryExpression node, Label label)
	{
		bool flag = branch == (node.NodeType == ExpressionType.Equal);
		if (node.Method != null)
		{
			EmitBinaryMethod(node, CompilationFlags.EmitAsNoTail);
			EmitBranchOp(branch, label);
		}
		else if (ConstantCheck.IsNull(node.Left))
		{
			if (node.Right.Type.IsNullableType())
			{
				EmitAddress(node.Right, node.Right.Type);
				_ilg.EmitHasValue(node.Right.Type);
			}
			else
			{
				EmitExpression(GetEqualityOperand(node.Right));
			}
			EmitBranchOp(!flag, label);
		}
		else if (ConstantCheck.IsNull(node.Right))
		{
			if (node.Left.Type.IsNullableType())
			{
				EmitAddress(node.Left, node.Left.Type);
				_ilg.EmitHasValue(node.Left.Type);
			}
			else
			{
				EmitExpression(GetEqualityOperand(node.Left));
			}
			EmitBranchOp(!flag, label);
		}
		else if (node.Left.Type.IsNullableType() || node.Right.Type.IsNullableType())
		{
			EmitBinaryExpression(node);
			EmitBranchOp(branch, label);
		}
		else
		{
			EmitExpression(GetEqualityOperand(node.Left));
			EmitExpression(GetEqualityOperand(node.Right));
			_ilg.Emit(flag ? OpCodes.Beq : OpCodes.Bne_Un, label);
		}
	}

	private static Expression GetEqualityOperand(Expression expression)
	{
		if (expression.NodeType == ExpressionType.Convert)
		{
			UnaryExpression unaryExpression = (UnaryExpression)expression;
			if (TypeUtils.AreReferenceAssignable(unaryExpression.Type, unaryExpression.Operand.Type))
			{
				return unaryExpression.Operand;
			}
		}
		return expression;
	}

	private void EmitBranchLogical(bool branch, BinaryExpression node, Label label)
	{
		if (node.Method != null || node.IsLifted)
		{
			EmitExpression(node);
			EmitBranchOp(branch, label);
			return;
		}
		bool flag = node.NodeType == ExpressionType.AndAlso;
		if (branch == flag)
		{
			EmitBranchAnd(branch, node, label);
		}
		else
		{
			EmitBranchOr(branch, node, label);
		}
	}

	private void EmitBranchAnd(bool branch, BinaryExpression node, Label label)
	{
		Label label2 = _ilg.DefineLabel();
		EmitExpressionAndBranch(!branch, node.Left, label2);
		EmitExpressionAndBranch(branch, node.Right, label);
		_ilg.MarkLabel(label2);
	}

	private void EmitBranchOr(bool branch, BinaryExpression node, Label label)
	{
		EmitExpressionAndBranch(branch, node.Left, label);
		EmitExpressionAndBranch(branch, node.Right, label);
	}

	private void EmitBranchBlock(bool branch, BlockExpression node, Label label)
	{
		EnterScope(node);
		int expressionCount = node.ExpressionCount;
		for (int i = 0; i < expressionCount - 1; i++)
		{
			EmitExpressionAsVoid(node.GetExpression(i));
		}
		EmitExpressionAndBranch(branch, node.GetExpression(expressionCount - 1), label);
		ExitScope(node);
	}

	private void EmitBlockExpression(Expression expr, CompilationFlags flags)
	{
		Emit((BlockExpression)expr, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsDefaultType));
	}

	private void Emit(BlockExpression node, CompilationFlags flags)
	{
		int expressionCount = node.ExpressionCount;
		if (expressionCount != 0)
		{
			EnterScope(node);
			CompilationFlags compilationFlags = flags & CompilationFlags.EmitAsTypeMask;
			CompilationFlags compilationFlags2 = flags & CompilationFlags.EmitAsTailCallMask;
			for (int i = 0; i < expressionCount - 1; i++)
			{
				Expression expression = node.GetExpression(i);
				Expression expression2 = node.GetExpression(i + 1);
				CompilationFlags newValue = ((compilationFlags2 == CompilationFlags.EmitAsNoTail) ? CompilationFlags.EmitAsNoTail : ((!(expression2 is GotoExpression gotoExpression) || (gotoExpression.Value != null && Significant(gotoExpression.Value)) || !ReferenceLabel(gotoExpression.Target).CanReturn) ? CompilationFlags.EmitAsMiddle : CompilationFlags.EmitAsTail));
				flags = UpdateEmitAsTailCallFlag(flags, newValue);
				EmitExpressionAsVoid(expression, flags);
			}
			if (compilationFlags == CompilationFlags.EmitAsVoidType || node.Type == typeof(void))
			{
				EmitExpressionAsVoid(node.GetExpression(expressionCount - 1), compilationFlags2);
			}
			else
			{
				EmitExpressionAsType(node.GetExpression(expressionCount - 1), node.Type, compilationFlags2);
			}
			ExitScope(node);
		}
	}

	private void EnterScope(object node)
	{
		if (HasVariables(node) && (_scope.MergedScopes == null || !_scope.MergedScopes.Contains(node)))
		{
			if (!_tree.Scopes.TryGetValue(node, out var value))
			{
				value = new CompilerScope(node, isMethod: false)
				{
					NeedsClosure = _scope.NeedsClosure
				};
			}
			_scope = value.Enter(this, _scope);
		}
	}

	private static bool HasVariables(object node)
	{
		if (node is BlockExpression blockExpression)
		{
			return blockExpression.Variables.Count > 0;
		}
		return ((CatchBlock)node).Variable != null;
	}

	private void ExitScope(object node)
	{
		if (_scope.Node == node)
		{
			_scope = _scope.Exit();
		}
	}

	private void EmitDefaultExpression(Expression expr)
	{
		DefaultExpression defaultExpression = (DefaultExpression)expr;
		if (defaultExpression.Type != typeof(void))
		{
			_ilg.EmitDefault(defaultExpression.Type, this);
		}
	}

	private void EmitLoopExpression(Expression expr)
	{
		LoopExpression loopExpression = (LoopExpression)expr;
		PushLabelBlock(LabelScopeKind.Statement);
		LabelInfo labelInfo = DefineLabel(loopExpression.BreakLabel);
		LabelInfo labelInfo2 = DefineLabel(loopExpression.ContinueLabel);
		labelInfo2.MarkWithEmptyStack();
		EmitExpressionAsVoid(loopExpression.Body);
		_ilg.Emit(OpCodes.Br, labelInfo2.Label);
		PopLabelBlock(LabelScopeKind.Statement);
		labelInfo.MarkWithEmptyStack();
	}

	private void EmitSwitchExpression(Expression expr, CompilationFlags flags)
	{
		SwitchExpression switchExpression = (SwitchExpression)expr;
		if (switchExpression.Cases.Count == 0)
		{
			EmitExpressionAsVoid(switchExpression.SwitchValue);
			if (switchExpression.DefaultBody != null)
			{
				EmitExpressionAsType(switchExpression.DefaultBody, switchExpression.Type, flags);
			}
		}
		else
		{
			if (TryEmitSwitchInstruction(switchExpression, flags) || TryEmitHashtableSwitch(switchExpression, flags))
			{
				return;
			}
			ParameterExpression parameterExpression = Expression.Parameter(switchExpression.SwitchValue.Type, "switchValue");
			ParameterExpression parameterExpression2 = Expression.Parameter(GetTestValueType(switchExpression), "testValue");
			_scope.AddLocal(this, parameterExpression);
			_scope.AddLocal(this, parameterExpression2);
			EmitExpression(switchExpression.SwitchValue);
			_scope.EmitSet(parameterExpression);
			Label[] array = new Label[switchExpression.Cases.Count];
			bool[] array2 = new bool[switchExpression.Cases.Count];
			int i = 0;
			for (int count = switchExpression.Cases.Count; i < count; i++)
			{
				DefineSwitchCaseLabel(switchExpression.Cases[i], out array[i], out array2[i]);
				foreach (Expression testValue in switchExpression.Cases[i].TestValues)
				{
					EmitExpression(testValue);
					_scope.EmitSet(parameterExpression2);
					EmitExpressionAndBranch(branchValue: true, Expression.Equal(parameterExpression, parameterExpression2, liftToNull: false, switchExpression.Comparison), array[i]);
				}
			}
			Label label = _ilg.DefineLabel();
			Label @default = ((switchExpression.DefaultBody == null) ? label : _ilg.DefineLabel());
			EmitSwitchCases(switchExpression, array, array2, @default, label, flags);
		}
	}

	private static Type GetTestValueType(SwitchExpression node)
	{
		if (node.Comparison == null)
		{
			return node.Cases[0].TestValues[0].Type;
		}
		Type type = node.Comparison.GetParametersCached()[1].ParameterType.GetNonRefType();
		if (node.IsLifted)
		{
			type = type.GetNullableType();
		}
		return type;
	}

	private static bool FitsInBucket(List<SwitchLabel> buckets, decimal key, int count)
	{
		decimal num = key - buckets[0].Key + 1m;
		if (num > 2147483647m)
		{
			return false;
		}
		return (decimal)((buckets.Count + count) * 2) > num;
	}

	private static void MergeBuckets(List<List<SwitchLabel>> buckets)
	{
		while (buckets.Count > 1)
		{
			List<SwitchLabel> list = buckets[buckets.Count - 2];
			List<SwitchLabel> list2 = buckets[buckets.Count - 1];
			if (!FitsInBucket(list, list2[list2.Count - 1].Key, list2.Count))
			{
				break;
			}
			list.AddRange(list2);
			buckets.RemoveAt(buckets.Count - 1);
		}
	}

	private static void AddToBuckets(List<List<SwitchLabel>> buckets, SwitchLabel key)
	{
		if (buckets.Count > 0)
		{
			List<SwitchLabel> list = buckets[buckets.Count - 1];
			if (FitsInBucket(list, key.Key, 1))
			{
				list.Add(key);
				MergeBuckets(buckets);
				return;
			}
		}
		buckets.Add(new List<SwitchLabel> { key });
	}

	private static bool CanOptimizeSwitchType(Type valueType)
	{
		TypeCode typeCode = valueType.GetTypeCode();
		if ((uint)(typeCode - 4) <= 8u)
		{
			return true;
		}
		return false;
	}

	private bool TryEmitSwitchInstruction(SwitchExpression node, CompilationFlags flags)
	{
		if (node.Comparison != null)
		{
			return false;
		}
		Type type = node.SwitchValue.Type;
		if (!CanOptimizeSwitchType(type) || !TypeUtils.AreEquivalent(type, node.Cases[0].TestValues[0].Type))
		{
			return false;
		}
		if (!node.Cases.All((SwitchCase c) => c.TestValues.All((Expression t) => t is ConstantExpression)))
		{
			return false;
		}
		Label[] array = new Label[node.Cases.Count];
		bool[] array2 = new bool[node.Cases.Count];
		HashSet<decimal> hashSet = new HashSet<decimal>();
		List<SwitchLabel> list = new List<SwitchLabel>();
		for (int i = 0; i < node.Cases.Count; i++)
		{
			DefineSwitchCaseLabel(node.Cases[i], out array[i], out array2[i]);
			foreach (ConstantExpression testValue in node.Cases[i].TestValues)
			{
				decimal num = ConvertSwitchValue(testValue.Value);
				if (hashSet.Add(num))
				{
					list.Add(new SwitchLabel(num, testValue.Value, array[i]));
				}
			}
		}
		list.Sort((SwitchLabel x, SwitchLabel y) => Math.Sign(x.Key - y.Key));
		List<List<SwitchLabel>> list2 = new List<List<SwitchLabel>>();
		foreach (SwitchLabel item in list)
		{
			AddToBuckets(list2, item);
		}
		LocalBuilder local = GetLocal(node.SwitchValue.Type);
		EmitExpression(node.SwitchValue);
		_ilg.Emit(OpCodes.Stloc, local);
		Label label = _ilg.DefineLabel();
		Label @default = ((node.DefaultBody == null) ? label : _ilg.DefineLabel());
		SwitchInfo info = new SwitchInfo(node, local, @default);
		EmitSwitchBuckets(info, list2, 0, list2.Count - 1);
		EmitSwitchCases(node, array, array2, @default, label, flags);
		FreeLocal(local);
		return true;
	}

	private static decimal ConvertSwitchValue(object value)
	{
		if (value is char)
		{
			return (int)(char)value;
		}
		return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
	}

	private void DefineSwitchCaseLabel(SwitchCase @case, out Label label, out bool isGoto)
	{
		if (@case.Body is GotoExpression { Value: null } gotoExpression)
		{
			LabelInfo labelInfo = ReferenceLabel(gotoExpression.Target);
			if (labelInfo.CanBranch)
			{
				label = labelInfo.Label;
				isGoto = true;
				return;
			}
		}
		label = _ilg.DefineLabel();
		isGoto = false;
	}

	private void EmitSwitchCases(SwitchExpression node, Label[] labels, bool[] isGoto, Label @default, Label end, CompilationFlags flags)
	{
		_ilg.Emit(OpCodes.Br, @default);
		int i = 0;
		for (int count = node.Cases.Count; i < count; i++)
		{
			if (isGoto[i])
			{
				continue;
			}
			_ilg.MarkLabel(labels[i]);
			EmitExpressionAsType(node.Cases[i].Body, node.Type, flags);
			if (node.DefaultBody != null || i < count - 1)
			{
				if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
				{
					_ilg.Emit(OpCodes.Ret);
				}
				else
				{
					_ilg.Emit(OpCodes.Br, end);
				}
			}
		}
		if (node.DefaultBody != null)
		{
			_ilg.MarkLabel(@default);
			EmitExpressionAsType(node.DefaultBody, node.Type, flags);
		}
		_ilg.MarkLabel(end);
	}

	private void EmitSwitchBuckets(SwitchInfo info, List<List<SwitchLabel>> buckets, int first, int last)
	{
		while (first != last)
		{
			int num = (int)(((long)first + (long)last + 1) / 2);
			if (first == num - 1)
			{
				EmitSwitchBucket(info, buckets[first]);
			}
			else
			{
				Label label = _ilg.DefineLabel();
				_ilg.Emit(OpCodes.Ldloc, info.Value);
				EmitConstant(buckets[num - 1][^1].Constant);
				_ilg.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, label);
				EmitSwitchBuckets(info, buckets, first, num - 1);
				_ilg.MarkLabel(label);
			}
			first = num;
		}
		EmitSwitchBucket(info, buckets[first]);
	}

	private void EmitSwitchBucket(SwitchInfo info, List<SwitchLabel> bucket)
	{
		if (bucket.Count == 1)
		{
			_ilg.Emit(OpCodes.Ldloc, info.Value);
			EmitConstant(bucket[0].Constant);
			_ilg.Emit(OpCodes.Beq, bucket[0].Label);
			return;
		}
		Label? label = null;
		if (info.Is64BitSwitch)
		{
			label = _ilg.DefineLabel();
			_ilg.Emit(OpCodes.Ldloc, info.Value);
			EmitConstant(bucket[^1].Constant);
			_ilg.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, label.Value);
			_ilg.Emit(OpCodes.Ldloc, info.Value);
			EmitConstant(bucket[0].Constant);
			_ilg.Emit(info.IsUnsigned ? OpCodes.Blt_Un : OpCodes.Blt, label.Value);
		}
		_ilg.Emit(OpCodes.Ldloc, info.Value);
		decimal key = bucket[0].Key;
		if (key != 0m)
		{
			EmitConstant(bucket[0].Constant);
			_ilg.Emit(OpCodes.Sub);
		}
		if (info.Is64BitSwitch)
		{
			_ilg.Emit(OpCodes.Conv_I4);
		}
		int num = (int)(bucket[bucket.Count - 1].Key - bucket[0].Key + 1m);
		Label[] array = new Label[num];
		int num2 = 0;
		foreach (SwitchLabel item in bucket)
		{
			while (key++ != item.Key)
			{
				array[num2++] = info.Default;
			}
			array[num2++] = item.Label;
		}
		_ilg.Emit(OpCodes.Switch, array);
		if (info.Is64BitSwitch)
		{
			_ilg.MarkLabel(label.Value);
		}
	}

	private bool TryEmitHashtableSwitch(SwitchExpression node, CompilationFlags flags)
	{
		if (node.Comparison != CachedReflectionInfo.String_op_Equality_String_String && node.Comparison != CachedReflectionInfo.String_Equals_String_String)
		{
			return false;
		}
		int num = 0;
		foreach (SwitchCase @case in node.Cases)
		{
			foreach (Expression testValue in @case.TestValues)
			{
				if (!(testValue is ConstantExpression))
				{
					return false;
				}
				num++;
			}
		}
		if (num < 7)
		{
			return false;
		}
		List<ElementInit> list = new List<ElementInit>(num);
		System.Collections.Generic.ArrayBuilder<SwitchCase> builder = new System.Collections.Generic.ArrayBuilder<SwitchCase>(node.Cases.Count);
		int value = -1;
		MethodInfo dictionaryOfStringInt32_Add_String_Int = CachedReflectionInfo.DictionaryOfStringInt32_Add_String_Int32;
		int i = 0;
		for (int count = node.Cases.Count; i < count; i++)
		{
			foreach (ConstantExpression testValue2 in node.Cases[i].TestValues)
			{
				if (testValue2.Value != null)
				{
					list.Add(Expression.ElementInit(dictionaryOfStringInt32_Add_String_Int, new TrueReadOnlyCollection<Expression>(testValue2, Utils.Constant(i))));
				}
				else
				{
					value = i;
				}
			}
			builder.UncheckedAdd(Expression.SwitchCase(node.Cases[i].Body, new TrueReadOnlyCollection<Expression>(Utils.Constant(i))));
		}
		MemberExpression memberExpression = CreateLazyInitializedField<Dictionary<string, int>>("dictionarySwitch");
		Expression dictInit = Expression.Condition(Expression.Equal(memberExpression, Expression.Constant(null, memberExpression.Type)), Expression.Assign(memberExpression, Expression.ListInit(Expression.New(CachedReflectionInfo.DictionaryOfStringInt32_Ctor_Int32, new TrueReadOnlyCollection<Expression>(Utils.Constant(list.Count))), list)), memberExpression);
		ParameterExpression parameterExpression = Expression.Variable(typeof(string), "switchValue");
		ParameterExpression parameterExpression2 = Expression.Variable(typeof(int), "switchIndex");
		BlockExpression node2 = Expression.Block(new TrueReadOnlyCollection<ParameterExpression>(parameterExpression2, parameterExpression), new TrueReadOnlyCollection<Expression>(Expression.Assign(parameterExpression, node.SwitchValue), Expression.IfThenElse(Expression.Equal(parameterExpression, Expression.Constant(null, typeof(string))), Expression.Assign(parameterExpression2, Utils.Constant(value)), Expression.IfThenElse(CallTryGetValue(dictInit, parameterExpression, parameterExpression2), Utils.Empty, Expression.Assign(parameterExpression2, Utils.Constant(-1)))), Expression.Switch(node.Type, parameterExpression2, node.DefaultBody, null, builder.ToReadOnly())));
		EmitExpression(node2, flags);
		return true;
	}

	[DynamicDependency("TryGetValue", typeof(Dictionary<, >))]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The method will be preserved by the DynamicDependency.")]
	private static MethodCallExpression CallTryGetValue(Expression dictInit, ParameterExpression switchValue, ParameterExpression switchIndex)
	{
		return Expression.Call(dictInit, "TryGetValue", null, switchValue, switchIndex);
	}

	private void CheckRethrow()
	{
		for (LabelScopeInfo labelScopeInfo = _labelBlock; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
		{
			if (labelScopeInfo.Kind == LabelScopeKind.Catch)
			{
				return;
			}
			if (labelScopeInfo.Kind == LabelScopeKind.Finally)
			{
				break;
			}
		}
		throw Error.RethrowRequiresCatch();
	}

	private void CheckTry()
	{
		for (LabelScopeInfo labelScopeInfo = _labelBlock; labelScopeInfo != null; labelScopeInfo = labelScopeInfo.Parent)
		{
			if (labelScopeInfo.Kind == LabelScopeKind.Filter)
			{
				throw Error.TryNotAllowedInFilter();
			}
		}
	}

	private void EmitSaveExceptionOrPop(CatchBlock cb)
	{
		if (cb.Variable != null)
		{
			_scope.EmitSet(cb.Variable);
		}
		else
		{
			_ilg.Emit(OpCodes.Pop);
		}
	}

	private void EmitTryExpression(Expression expr)
	{
		TryExpression tryExpression = (TryExpression)expr;
		CheckTry();
		PushLabelBlock(LabelScopeKind.Try);
		_ilg.BeginExceptionBlock();
		EmitExpression(tryExpression.Body);
		Type type = tryExpression.Type;
		LocalBuilder local = null;
		if (type != typeof(void))
		{
			local = GetLocal(type);
			_ilg.Emit(OpCodes.Stloc, local);
		}
		foreach (CatchBlock handler in tryExpression.Handlers)
		{
			PushLabelBlock(LabelScopeKind.Catch);
			if (handler.Filter == null)
			{
				_ilg.BeginCatchBlock(handler.Test);
			}
			else
			{
				_ilg.BeginExceptFilterBlock();
			}
			EnterScope(handler);
			EmitCatchStart(handler);
			EmitExpression(handler.Body);
			if (type != typeof(void))
			{
				_ilg.Emit(OpCodes.Stloc, local);
			}
			ExitScope(handler);
			PopLabelBlock(LabelScopeKind.Catch);
		}
		if (tryExpression.Finally != null || tryExpression.Fault != null)
		{
			PushLabelBlock(LabelScopeKind.Finally);
			if (tryExpression.Finally != null)
			{
				_ilg.BeginFinallyBlock();
			}
			else
			{
				_ilg.BeginFaultBlock();
			}
			EmitExpressionAsVoid(tryExpression.Finally ?? tryExpression.Fault);
			_ilg.EndExceptionBlock();
			PopLabelBlock(LabelScopeKind.Finally);
		}
		else
		{
			_ilg.EndExceptionBlock();
		}
		if (type != typeof(void))
		{
			_ilg.Emit(OpCodes.Ldloc, local);
			FreeLocal(local);
		}
		PopLabelBlock(LabelScopeKind.Try);
	}

	private void EmitCatchStart(CatchBlock cb)
	{
		if (cb.Filter == null)
		{
			EmitSaveExceptionOrPop(cb);
			return;
		}
		Label label = _ilg.DefineLabel();
		Label label2 = _ilg.DefineLabel();
		_ilg.Emit(OpCodes.Isinst, cb.Test);
		_ilg.Emit(OpCodes.Dup);
		_ilg.Emit(OpCodes.Brtrue, label2);
		_ilg.Emit(OpCodes.Pop);
		_ilg.Emit(OpCodes.Ldc_I4_0);
		_ilg.Emit(OpCodes.Br, label);
		_ilg.MarkLabel(label2);
		EmitSaveExceptionOrPop(cb);
		PushLabelBlock(LabelScopeKind.Filter);
		EmitExpression(cb.Filter);
		PopLabelBlock(LabelScopeKind.Filter);
		_ilg.MarkLabel(label);
		_ilg.BeginCatchBlock(null);
		_ilg.Emit(OpCodes.Pop);
	}

	private void EmitQuoteUnaryExpression(Expression expr)
	{
		EmitQuote((UnaryExpression)expr);
	}

	private void EmitQuote(UnaryExpression quote)
	{
		EmitConstant(quote.Operand, quote.Type);
		if (_scope.NearestHoistedLocals != null)
		{
			EmitConstant(_scope.NearestHoistedLocals, typeof(object));
			_scope.EmitGet(_scope.NearestHoistedLocals.SelfVariable);
			_ilg.Emit(OpCodes.Call, CachedReflectionInfo.RuntimeOps_Quote);
			_ilg.Emit(OpCodes.Castclass, quote.Type);
		}
	}

	private void EmitThrowUnaryExpression(Expression expr)
	{
		EmitThrow((UnaryExpression)expr, CompilationFlags.EmitAsDefaultType);
	}

	private void EmitThrow(UnaryExpression expr, CompilationFlags flags)
	{
		if (expr.Operand == null)
		{
			CheckRethrow();
			_ilg.Emit(OpCodes.Rethrow);
		}
		else
		{
			EmitExpression(expr.Operand);
			_ilg.Emit(OpCodes.Throw);
		}
		EmitUnreachable(expr, flags);
	}

	private void EmitUnaryExpression(Expression expr, CompilationFlags flags)
	{
		EmitUnary((UnaryExpression)expr, flags);
	}

	private void EmitUnary(UnaryExpression node, CompilationFlags flags)
	{
		if (node.Method != null)
		{
			EmitUnaryMethod(node, flags);
		}
		else if (node.NodeType == ExpressionType.NegateChecked && node.Operand.Type.IsInteger())
		{
			Type type = node.Type;
			if (type.IsNullableType())
			{
				Label label = _ilg.DefineLabel();
				Label label2 = _ilg.DefineLabel();
				EmitExpression(node.Operand);
				LocalBuilder local = GetLocal(type);
				_ilg.Emit(OpCodes.Stloc, local);
				_ilg.Emit(OpCodes.Ldloca, local);
				_ilg.EmitGetValueOrDefault(type);
				_ilg.Emit(OpCodes.Brfalse_S, label);
				Type nonNullableType = type.GetNonNullableType();
				_ilg.EmitDefault(nonNullableType, null);
				_ilg.Emit(OpCodes.Ldloca, local);
				_ilg.EmitGetValueOrDefault(type);
				EmitBinaryOperator(ExpressionType.SubtractChecked, nonNullableType, nonNullableType, nonNullableType, liftedToNull: false);
				_ilg.Emit(OpCodes.Newobj, TypeUtils.GetNullableConstructor(type));
				_ilg.Emit(OpCodes.Br_S, label2);
				_ilg.MarkLabel(label);
				_ilg.Emit(OpCodes.Ldloc, local);
				FreeLocal(local);
				_ilg.MarkLabel(label2);
			}
			else
			{
				_ilg.EmitDefault(type, null);
				EmitExpression(node.Operand);
				EmitBinaryOperator(ExpressionType.SubtractChecked, type, type, type, liftedToNull: false);
			}
		}
		else
		{
			EmitExpression(node.Operand);
			EmitUnaryOperator(node.NodeType, node.Operand.Type, node.Type);
		}
	}

	private void EmitUnaryOperator(ExpressionType op, Type operandType, Type resultType)
	{
		bool flag = operandType.IsNullableType();
		if (op == ExpressionType.ArrayLength)
		{
			_ilg.Emit(OpCodes.Ldlen);
			return;
		}
		if (flag)
		{
			switch (op)
			{
			case ExpressionType.UnaryPlus:
				return;
			case ExpressionType.TypeAs:
				if (operandType != resultType)
				{
					_ilg.Emit(OpCodes.Box, operandType);
					_ilg.Emit(OpCodes.Isinst, resultType);
					if (resultType.IsNullableType())
					{
						_ilg.Emit(OpCodes.Unbox_Any, resultType);
					}
				}
				return;
			}
			Label label = _ilg.DefineLabel();
			Label label2 = _ilg.DefineLabel();
			LocalBuilder local = GetLocal(operandType);
			_ilg.Emit(OpCodes.Stloc, local);
			_ilg.Emit(OpCodes.Ldloca, local);
			_ilg.EmitHasValue(operandType);
			_ilg.Emit(OpCodes.Brfalse_S, label);
			_ilg.Emit(OpCodes.Ldloca, local);
			_ilg.EmitGetValueOrDefault(operandType);
			Type nonNullableType = resultType.GetNonNullableType();
			EmitUnaryOperator(op, nonNullableType, nonNullableType);
			ConstructorInfo nullableConstructor = TypeUtils.GetNullableConstructor(resultType);
			_ilg.Emit(OpCodes.Newobj, nullableConstructor);
			_ilg.Emit(OpCodes.Br_S, label2);
			_ilg.MarkLabel(label);
			_ilg.Emit(OpCodes.Ldloc, local);
			FreeLocal(local);
			_ilg.MarkLabel(label2);
			return;
		}
		switch (op)
		{
		case ExpressionType.Not:
			if (operandType == typeof(bool))
			{
				_ilg.Emit(OpCodes.Ldc_I4_0);
				_ilg.Emit(OpCodes.Ceq);
				return;
			}
			goto case ExpressionType.OnesComplement;
		case ExpressionType.OnesComplement:
			_ilg.Emit(OpCodes.Not);
			if (!operandType.IsUnsigned())
			{
				return;
			}
			break;
		case ExpressionType.IsFalse:
			_ilg.Emit(OpCodes.Ldc_I4_0);
			_ilg.Emit(OpCodes.Ceq);
			return;
		case ExpressionType.IsTrue:
			_ilg.Emit(OpCodes.Ldc_I4_1);
			_ilg.Emit(OpCodes.Ceq);
			return;
		case ExpressionType.UnaryPlus:
			return;
		case ExpressionType.Negate:
		case ExpressionType.NegateChecked:
			_ilg.Emit(OpCodes.Neg);
			return;
		case ExpressionType.TypeAs:
			if (operandType != resultType)
			{
				if (operandType.IsValueType)
				{
					_ilg.Emit(OpCodes.Box, operandType);
				}
				_ilg.Emit(OpCodes.Isinst, resultType);
				if (resultType.IsNullableType())
				{
					_ilg.Emit(OpCodes.Unbox_Any, resultType);
				}
			}
			return;
		case ExpressionType.Increment:
			EmitConstantOne(resultType);
			_ilg.Emit(OpCodes.Add);
			break;
		case ExpressionType.Decrement:
			EmitConstantOne(resultType);
			_ilg.Emit(OpCodes.Sub);
			break;
		}
		EmitConvertArithmeticResult(op, resultType);
	}

	private void EmitConstantOne(Type type)
	{
		switch (type.GetTypeCode())
		{
		case TypeCode.Int64:
		case TypeCode.UInt64:
			_ilg.Emit(OpCodes.Ldc_I4_1);
			_ilg.Emit(OpCodes.Conv_I8);
			break;
		case TypeCode.Single:
			_ilg.Emit(OpCodes.Ldc_R4, 1f);
			break;
		case TypeCode.Double:
			_ilg.Emit(OpCodes.Ldc_R8, 1.0);
			break;
		default:
			_ilg.Emit(OpCodes.Ldc_I4_1);
			break;
		}
	}

	private void EmitUnboxUnaryExpression(Expression expr)
	{
		UnaryExpression unaryExpression = (UnaryExpression)expr;
		EmitExpression(unaryExpression.Operand);
		_ilg.Emit(OpCodes.Unbox_Any, unaryExpression.Type);
	}

	private void EmitConvertUnaryExpression(Expression expr, CompilationFlags flags)
	{
		EmitConvert((UnaryExpression)expr, flags);
	}

	private void EmitConvert(UnaryExpression node, CompilationFlags flags)
	{
		if (node.Method != null)
		{
			if (!node.IsLifted || (node.Type.IsValueType && node.Operand.Type.IsValueType))
			{
				EmitUnaryMethod(node, flags);
				return;
			}
			ParameterInfo[] parametersCached = node.Method.GetParametersCached();
			Type type = parametersCached[0].ParameterType;
			if (type.IsByRef)
			{
				type = type.GetElementType();
			}
			UnaryExpression arg = Expression.Convert(node.Operand, type);
			node = Expression.Convert(Expression.Call(node.Method, arg), node.Type);
		}
		if (node.Type == typeof(void))
		{
			EmitExpressionAsVoid(node.Operand, flags);
			return;
		}
		if (TypeUtils.AreEquivalent(node.Operand.Type, node.Type))
		{
			EmitExpression(node.Operand, flags);
			return;
		}
		EmitExpression(node.Operand);
		_ilg.EmitConvertToType(node.Operand.Type, node.Type, node.NodeType == ExpressionType.ConvertChecked, this);
	}

	private void EmitUnaryMethod(UnaryExpression node, CompilationFlags flags)
	{
		if (node.IsLifted)
		{
			ParameterExpression parameterExpression = Expression.Variable(node.Operand.Type.GetNonNullableType(), null);
			MethodCallExpression methodCallExpression = Expression.Call(node.Method, parameterExpression);
			Type nullableType = methodCallExpression.Type.GetNullableType();
			EmitLift(node.NodeType, nullableType, methodCallExpression, new ParameterExpression[1] { parameterExpression }, new Expression[1] { node.Operand });
			_ilg.EmitConvertToType(nullableType, node.Type, isChecked: false, this);
		}
		else
		{
			EmitMethodCallExpression(Expression.Call(node.Method, node.Operand), flags);
		}
	}
}
