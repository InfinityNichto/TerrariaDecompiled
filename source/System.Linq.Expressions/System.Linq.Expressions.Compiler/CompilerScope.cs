using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class CompilerScope
{
	private abstract class Storage
	{
		internal readonly LambdaCompiler Compiler;

		internal readonly ParameterExpression Variable;

		internal Storage(LambdaCompiler compiler, ParameterExpression variable)
		{
			Compiler = compiler;
			Variable = variable;
		}

		internal abstract void EmitLoad();

		internal abstract void EmitAddress();

		internal abstract void EmitStore();

		internal virtual void EmitStore(Storage value)
		{
			value.EmitLoad();
			EmitStore();
		}

		internal virtual void FreeLocal()
		{
		}
	}

	private sealed class LocalStorage : Storage
	{
		private readonly LocalBuilder _local;

		internal LocalStorage(LambdaCompiler compiler, ParameterExpression variable)
			: base(compiler, variable)
		{
			_local = compiler.GetLocal(variable.IsByRef ? variable.Type.MakeByRefType() : variable.Type);
		}

		internal override void EmitLoad()
		{
			Compiler.IL.Emit(OpCodes.Ldloc, _local);
		}

		internal override void EmitStore()
		{
			Compiler.IL.Emit(OpCodes.Stloc, _local);
		}

		internal override void EmitAddress()
		{
			Compiler.IL.Emit(OpCodes.Ldloca, _local);
		}

		internal override void FreeLocal()
		{
			Compiler.FreeLocal(_local);
		}
	}

	private sealed class ArgumentStorage : Storage
	{
		private readonly int _argument;

		internal ArgumentStorage(LambdaCompiler compiler, ParameterExpression p)
			: base(compiler, p)
		{
			_argument = compiler.GetLambdaArgument(compiler.Parameters.IndexOf(p));
		}

		internal override void EmitLoad()
		{
			Compiler.IL.EmitLoadArg(_argument);
		}

		internal override void EmitStore()
		{
			Compiler.IL.EmitStoreArg(_argument);
		}

		internal override void EmitAddress()
		{
			Compiler.IL.EmitLoadArgAddress(_argument);
		}
	}

	private sealed class ElementBoxStorage : Storage
	{
		private readonly int _index;

		private readonly Storage _array;

		private readonly Type _boxType;

		private readonly FieldInfo _boxValueField;

		internal ElementBoxStorage(Storage array, int index, ParameterExpression variable)
			: base(array.Compiler, variable)
		{
			_array = array;
			_index = index;
			Type type = typeof(StrongBox<>).MakeGenericType(variable.Type);
			_boxValueField = type.GetField("Value");
			_boxType = type;
		}

		internal override void EmitLoad()
		{
			EmitLoadBox();
			Compiler.IL.Emit(OpCodes.Ldfld, _boxValueField);
		}

		internal override void EmitStore()
		{
			LocalBuilder local = Compiler.GetLocal(Variable.Type);
			Compiler.IL.Emit(OpCodes.Stloc, local);
			EmitLoadBox();
			Compiler.IL.Emit(OpCodes.Ldloc, local);
			Compiler.FreeLocal(local);
			Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
		}

		internal override void EmitStore(Storage value)
		{
			EmitLoadBox();
			value.EmitLoad();
			Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
		}

		internal override void EmitAddress()
		{
			EmitLoadBox();
			Compiler.IL.Emit(OpCodes.Ldflda, _boxValueField);
		}

		internal void EmitLoadBox()
		{
			_array.EmitLoad();
			Compiler.IL.EmitPrimitive(_index);
			Compiler.IL.Emit(OpCodes.Ldelem_Ref);
			Compiler.IL.Emit(OpCodes.Castclass, _boxType);
		}
	}

	private sealed class LocalBoxStorage : Storage
	{
		private readonly LocalBuilder _boxLocal;

		private readonly FieldInfo _boxValueField;

		internal LocalBoxStorage(LambdaCompiler compiler, ParameterExpression variable)
			: base(compiler, variable)
		{
			Type type = typeof(StrongBox<>).MakeGenericType(variable.Type);
			_boxValueField = type.GetField("Value");
			_boxLocal = compiler.GetLocal(type);
		}

		internal override void EmitLoad()
		{
			Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
			Compiler.IL.Emit(OpCodes.Ldfld, _boxValueField);
		}

		internal override void EmitAddress()
		{
			Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
			Compiler.IL.Emit(OpCodes.Ldflda, _boxValueField);
		}

		internal override void EmitStore()
		{
			LocalBuilder local = Compiler.GetLocal(Variable.Type);
			Compiler.IL.Emit(OpCodes.Stloc, local);
			Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
			Compiler.IL.Emit(OpCodes.Ldloc, local);
			Compiler.FreeLocal(local);
			Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
		}

		internal override void EmitStore(Storage value)
		{
			Compiler.IL.Emit(OpCodes.Ldloc, _boxLocal);
			value.EmitLoad();
			Compiler.IL.Emit(OpCodes.Stfld, _boxValueField);
		}

		internal void EmitStoreBox()
		{
			Compiler.IL.Emit(OpCodes.Stloc, _boxLocal);
		}

		internal override void FreeLocal()
		{
			Compiler.FreeLocal(_boxLocal);
		}
	}

	private CompilerScope _parent;

	internal readonly object Node;

	internal readonly bool IsMethod;

	internal bool NeedsClosure;

	internal readonly Dictionary<ParameterExpression, VariableStorageKind> Definitions = new Dictionary<ParameterExpression, VariableStorageKind>();

	internal Dictionary<ParameterExpression, int> ReferenceCount;

	internal HashSet<BlockExpression> MergedScopes;

	private HoistedLocals _hoistedLocals;

	private HoistedLocals _closureHoistedLocals;

	private readonly Dictionary<ParameterExpression, Storage> _locals = new Dictionary<ParameterExpression, Storage>();

	internal HoistedLocals NearestHoistedLocals => _hoistedLocals ?? _closureHoistedLocals;

	private string CurrentLambdaName
	{
		get
		{
			for (CompilerScope compilerScope = this; compilerScope != null; compilerScope = compilerScope._parent)
			{
				if (compilerScope.Node is LambdaExpression lambdaExpression)
				{
					return lambdaExpression.Name;
				}
			}
			throw ContractUtils.Unreachable;
		}
	}

	internal CompilerScope(object node, bool isMethod)
	{
		Node = node;
		IsMethod = isMethod;
		IReadOnlyList<ParameterExpression> variables = GetVariables(node);
		Definitions = new Dictionary<ParameterExpression, VariableStorageKind>(variables.Count);
		foreach (ParameterExpression item in variables)
		{
			Definitions.Add(item, VariableStorageKind.Local);
		}
	}

	internal CompilerScope Enter(LambdaCompiler lc, CompilerScope parent)
	{
		SetParent(lc, parent);
		AllocateLocals(lc);
		if (IsMethod && _closureHoistedLocals != null)
		{
			EmitClosureAccess(lc, _closureHoistedLocals);
		}
		EmitNewHoistedLocals(lc);
		if (IsMethod)
		{
			EmitCachedVariables();
		}
		return this;
	}

	internal CompilerScope Exit()
	{
		if (!IsMethod)
		{
			foreach (Storage value in _locals.Values)
			{
				value.FreeLocal();
			}
		}
		CompilerScope parent = _parent;
		_parent = null;
		_hoistedLocals = null;
		_closureHoistedLocals = null;
		_locals.Clear();
		return parent;
	}

	internal void EmitVariableAccess(LambdaCompiler lc, ReadOnlyCollection<ParameterExpression> vars)
	{
		if (NearestHoistedLocals != null && vars.Count > 0)
		{
			System.Collections.Generic.ArrayBuilder<long> arrayBuilder = new System.Collections.Generic.ArrayBuilder<long>(vars.Count);
			foreach (ParameterExpression var in vars)
			{
				ulong num = 0uL;
				HoistedLocals hoistedLocals = NearestHoistedLocals;
				while (!hoistedLocals.Indexes.ContainsKey(var))
				{
					num++;
					hoistedLocals = hoistedLocals.Parent;
				}
				ulong item = (num << 32) | (uint)hoistedLocals.Indexes[var];
				arrayBuilder.UncheckedAdd((long)item);
			}
			EmitGet(NearestHoistedLocals.SelfVariable);
			lc.EmitConstantArray(arrayBuilder.ToArray());
			lc.IL.Emit(OpCodes.Call, CachedReflectionInfo.RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array);
		}
		else
		{
			lc.IL.Emit(OpCodes.Call, CachedReflectionInfo.RuntimeOps_CreateRuntimeVariables);
		}
	}

	internal void AddLocal(LambdaCompiler gen, ParameterExpression variable)
	{
		_locals.Add(variable, new LocalStorage(gen, variable));
	}

	internal void EmitGet(ParameterExpression variable)
	{
		ResolveVariable(variable).EmitLoad();
	}

	internal void EmitSet(ParameterExpression variable)
	{
		ResolveVariable(variable).EmitStore();
	}

	internal void EmitAddressOf(ParameterExpression variable)
	{
		ResolveVariable(variable).EmitAddress();
	}

	private Storage ResolveVariable(ParameterExpression variable)
	{
		return ResolveVariable(variable, NearestHoistedLocals);
	}

	private Storage ResolveVariable(ParameterExpression variable, HoistedLocals hoistedLocals)
	{
		for (CompilerScope compilerScope = this; compilerScope != null; compilerScope = compilerScope._parent)
		{
			if (compilerScope._locals.TryGetValue(variable, out var value))
			{
				return value;
			}
			if (compilerScope.IsMethod)
			{
				break;
			}
		}
		for (HoistedLocals hoistedLocals2 = hoistedLocals; hoistedLocals2 != null; hoistedLocals2 = hoistedLocals2.Parent)
		{
			if (hoistedLocals2.Indexes.TryGetValue(variable, out var value2))
			{
				return new ElementBoxStorage(ResolveVariable(hoistedLocals2.SelfVariable, hoistedLocals), value2, variable);
			}
		}
		throw Error.UndefinedVariable(variable.Name, variable.Type, CurrentLambdaName);
	}

	private void SetParent(LambdaCompiler lc, CompilerScope parent)
	{
		_parent = parent;
		if (NeedsClosure && _parent != null)
		{
			_closureHoistedLocals = _parent.NearestHoistedLocals;
		}
		ReadOnlyCollection<ParameterExpression> readOnlyCollection = (from p in GetVariables()
			where Definitions[p] == VariableStorageKind.Hoisted
			select p).ToReadOnly();
		if (readOnlyCollection.Count > 0)
		{
			_hoistedLocals = new HoistedLocals(_closureHoistedLocals, readOnlyCollection);
			AddLocal(lc, _hoistedLocals.SelfVariable);
		}
	}

	private void EmitNewHoistedLocals(LambdaCompiler lc)
	{
		if (_hoistedLocals == null)
		{
			return;
		}
		lc.IL.EmitPrimitive(_hoistedLocals.Variables.Count);
		lc.IL.Emit(OpCodes.Newarr, typeof(object));
		int num = 0;
		foreach (ParameterExpression variable in _hoistedLocals.Variables)
		{
			lc.IL.Emit(OpCodes.Dup);
			lc.IL.EmitPrimitive(num++);
			Type type = typeof(StrongBox<>).MakeGenericType(variable.Type);
			int index;
			if (IsMethod && (index = lc.Parameters.IndexOf(variable)) >= 0)
			{
				lc.EmitLambdaArgument(index);
				lc.IL.Emit(OpCodes.Newobj, type.GetConstructor(new Type[1] { variable.Type }));
			}
			else if (variable == _hoistedLocals.ParentVariable)
			{
				ResolveVariable(variable, _closureHoistedLocals).EmitLoad();
				lc.IL.Emit(OpCodes.Newobj, type.GetConstructor(new Type[1] { variable.Type }));
			}
			else
			{
				lc.IL.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
			}
			if (ShouldCache(variable))
			{
				lc.IL.Emit(OpCodes.Dup);
				CacheBoxToLocal(lc, variable);
			}
			lc.IL.Emit(OpCodes.Stelem_Ref);
		}
		EmitSet(_hoistedLocals.SelfVariable);
	}

	private void EmitCachedVariables()
	{
		if (ReferenceCount == null)
		{
			return;
		}
		foreach (KeyValuePair<ParameterExpression, int> item in ReferenceCount)
		{
			if (ShouldCache(item.Key, item.Value) && ResolveVariable(item.Key) is ElementBoxStorage elementBoxStorage)
			{
				elementBoxStorage.EmitLoadBox();
				CacheBoxToLocal(elementBoxStorage.Compiler, item.Key);
			}
		}
	}

	private bool ShouldCache(ParameterExpression v, int refCount)
	{
		if (refCount > 2)
		{
			return !_locals.ContainsKey(v);
		}
		return false;
	}

	private bool ShouldCache(ParameterExpression v)
	{
		if (ReferenceCount == null)
		{
			return false;
		}
		if (ReferenceCount.TryGetValue(v, out var value))
		{
			return ShouldCache(v, value);
		}
		return false;
	}

	private void CacheBoxToLocal(LambdaCompiler lc, ParameterExpression v)
	{
		LocalBoxStorage localBoxStorage = new LocalBoxStorage(lc, v);
		localBoxStorage.EmitStoreBox();
		_locals.Add(v, localBoxStorage);
	}

	private void EmitClosureAccess(LambdaCompiler lc, HoistedLocals locals)
	{
		if (locals != null)
		{
			EmitClosureToVariable(lc, locals);
			while ((locals = locals.Parent) != null)
			{
				ParameterExpression selfVariable = locals.SelfVariable;
				LocalStorage localStorage = new LocalStorage(lc, selfVariable);
				localStorage.EmitStore(ResolveVariable(selfVariable));
				_locals.Add(selfVariable, localStorage);
			}
		}
	}

	private void EmitClosureToVariable(LambdaCompiler lc, HoistedLocals locals)
	{
		lc.EmitClosureArgument();
		lc.IL.Emit(OpCodes.Ldfld, CachedReflectionInfo.Closure_Locals);
		AddLocal(lc, locals.SelfVariable);
		EmitSet(locals.SelfVariable);
	}

	private void AllocateLocals(LambdaCompiler lc)
	{
		foreach (ParameterExpression variable in GetVariables())
		{
			if (Definitions[variable] == VariableStorageKind.Local)
			{
				Storage value = ((!IsMethod || !lc.Parameters.Contains(variable)) ? ((Storage)new LocalStorage(lc, variable)) : ((Storage)new ArgumentStorage(lc, variable)));
				_locals.Add(variable, value);
			}
		}
	}

	private IEnumerable<ParameterExpression> GetVariables()
	{
		if (MergedScopes != null)
		{
			return GetVariablesIncludingMerged();
		}
		return GetVariables(Node);
	}

	private IEnumerable<ParameterExpression> GetVariablesIncludingMerged()
	{
		foreach (ParameterExpression variable in GetVariables(Node))
		{
			yield return variable;
		}
		foreach (BlockExpression mergedScope in MergedScopes)
		{
			foreach (ParameterExpression variable2 in mergedScope.Variables)
			{
				yield return variable2;
			}
		}
	}

	private static IReadOnlyList<ParameterExpression> GetVariables(object scope)
	{
		if (scope is LambdaExpression provider)
		{
			return new ParameterList(provider);
		}
		if (!(scope is BlockExpression blockExpression))
		{
			return new ParameterExpression[1] { ((CatchBlock)scope).Variable };
		}
		return blockExpression.Variables;
	}
}
