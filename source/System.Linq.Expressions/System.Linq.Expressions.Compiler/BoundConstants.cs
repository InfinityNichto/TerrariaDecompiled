using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class BoundConstants
{
	private readonly struct TypedConstant : IEquatable<TypedConstant>
	{
		internal readonly object Value;

		internal readonly Type Type;

		internal TypedConstant(object value, Type type)
		{
			Value = value;
			Type = type;
		}

		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(Value) ^ Type.GetHashCode();
		}

		public bool Equals(TypedConstant other)
		{
			if (Value == other.Value)
			{
				return Type.Equals(other.Type);
			}
			return false;
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is TypedConstant other)
			{
				return Equals(other);
			}
			return false;
		}
	}

	private readonly List<object> _values = new List<object>();

	private readonly Dictionary<object, int> _indexes = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);

	private readonly Dictionary<TypedConstant, int> _references = new Dictionary<TypedConstant, int>();

	private readonly Dictionary<TypedConstant, LocalBuilder> _cache = new Dictionary<TypedConstant, LocalBuilder>();

	internal int Count => _values.Count;

	internal object[] ToArray()
	{
		return _values.ToArray();
	}

	internal void AddReference(object value, Type type)
	{
		if (_indexes.TryAdd(value, _values.Count))
		{
			_values.Add(value);
		}
		Helpers.IncrementCount(new TypedConstant(value, type), _references);
	}

	internal void EmitConstant(LambdaCompiler lc, object value, Type type)
	{
		if (_cache.TryGetValue(new TypedConstant(value, type), out var value2))
		{
			lc.IL.Emit(OpCodes.Ldloc, value2);
			return;
		}
		EmitConstantsArray(lc);
		EmitConstantFromArray(lc, value, type);
	}

	internal void EmitCacheConstants(LambdaCompiler lc)
	{
		int num = 0;
		foreach (KeyValuePair<TypedConstant, int> reference in _references)
		{
			if (ShouldCache(reference.Value))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return;
		}
		EmitConstantsArray(lc);
		_cache.Clear();
		foreach (KeyValuePair<TypedConstant, int> reference2 in _references)
		{
			if (ShouldCache(reference2.Value))
			{
				if (--num > 0)
				{
					lc.IL.Emit(OpCodes.Dup);
				}
				LocalBuilder localBuilder = lc.IL.DeclareLocal(reference2.Key.Type);
				EmitConstantFromArray(lc, reference2.Key.Value, localBuilder.LocalType);
				lc.IL.Emit(OpCodes.Stloc, localBuilder);
				_cache.Add(reference2.Key, localBuilder);
			}
		}
	}

	private static bool ShouldCache(int refCount)
	{
		return refCount > 2;
	}

	private static void EmitConstantsArray(LambdaCompiler lc)
	{
		lc.EmitClosureArgument();
		lc.IL.Emit(OpCodes.Ldfld, CachedReflectionInfo.Closure_Constants);
	}

	private void EmitConstantFromArray(LambdaCompiler lc, object value, Type type)
	{
		if (!_indexes.TryGetValue(value, out var value2))
		{
			_indexes.Add(value, value2 = _values.Count);
			_values.Add(value);
		}
		lc.IL.EmitPrimitive(value2);
		lc.IL.Emit(OpCodes.Ldelem_Ref);
		if (type.IsValueType)
		{
			lc.IL.Emit(OpCodes.Unbox_Any, type);
		}
		else if (type != typeof(object))
		{
			lc.IL.Emit(OpCodes.Castclass, type);
		}
	}
}
