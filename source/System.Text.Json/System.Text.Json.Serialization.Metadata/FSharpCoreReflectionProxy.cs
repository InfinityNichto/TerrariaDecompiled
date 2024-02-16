using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata;

internal sealed class FSharpCoreReflectionProxy
{
	public enum FSharpKind
	{
		Unrecognized,
		Option,
		ValueOption,
		List,
		Set,
		Map,
		Record,
		Union
	}

	public delegate TResult StructGetter<TStruct, TResult>(ref TStruct @this) where TStruct : struct;

	private enum SourceConstructFlags
	{
		None = 0,
		SumType = 1,
		RecordType = 2,
		ObjectType = 3,
		Field = 4,
		Exception = 5,
		Closure = 6,
		Module = 7,
		UnionCase = 8,
		Value = 9,
		KindMask = 31,
		NonPublicRepresentation = 32
	}

	private static FSharpCoreReflectionProxy s_singletonInstance;

	private readonly Type _compilationMappingAttributeType;

	private readonly MethodInfo _sourceConstructFlagsGetter;

	private readonly Type _fsharpOptionType;

	private readonly Type _fsharpValueOptionType;

	private readonly Type _fsharpListType;

	private readonly Type _fsharpSetType;

	private readonly Type _fsharpMapType;

	private readonly MethodInfo _fsharpListCtor;

	private readonly MethodInfo _fsharpSetCtor;

	private readonly MethodInfo _fsharpMapCtor;

	public static FSharpCoreReflectionProxy Instance => s_singletonInstance;

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public static bool IsFSharpType(Type type)
	{
		if (s_singletonInstance == null)
		{
			Assembly fSharpCoreAssembly = GetFSharpCoreAssembly(type);
			if ((object)fSharpCoreAssembly != null)
			{
				if (s_singletonInstance == null)
				{
					s_singletonInstance = new FSharpCoreReflectionProxy(fSharpCoreAssembly);
				}
				return true;
			}
			return false;
		}
		return s_singletonInstance.GetFSharpCompilationMappingAttribute(type) != null;
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	private FSharpCoreReflectionProxy(Assembly fsharpCoreAssembly)
	{
		Type type = fsharpCoreAssembly.GetType("Microsoft.FSharp.Core.CompilationMappingAttribute");
		_sourceConstructFlagsGetter = type.GetMethod("get_SourceConstructFlags", BindingFlags.Instance | BindingFlags.Public);
		_compilationMappingAttributeType = type;
		_fsharpOptionType = fsharpCoreAssembly.GetType("Microsoft.FSharp.Core.FSharpOption`1");
		_fsharpValueOptionType = fsharpCoreAssembly.GetType("Microsoft.FSharp.Core.FSharpValueOption`1");
		_fsharpListType = fsharpCoreAssembly.GetType("Microsoft.FSharp.Collections.FSharpList`1");
		_fsharpSetType = fsharpCoreAssembly.GetType("Microsoft.FSharp.Collections.FSharpSet`1");
		_fsharpMapType = fsharpCoreAssembly.GetType("Microsoft.FSharp.Collections.FSharpMap`2");
		_fsharpListCtor = fsharpCoreAssembly.GetType("Microsoft.FSharp.Collections.ListModule")?.GetMethod("OfSeq", BindingFlags.Static | BindingFlags.Public);
		_fsharpSetCtor = fsharpCoreAssembly.GetType("Microsoft.FSharp.Collections.SetModule")?.GetMethod("OfSeq", BindingFlags.Static | BindingFlags.Public);
		_fsharpMapCtor = fsharpCoreAssembly.GetType("Microsoft.FSharp.Collections.MapModule")?.GetMethod("OfSeq", BindingFlags.Static | BindingFlags.Public);
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public FSharpKind DetectFSharpKind(Type type)
	{
		Attribute fSharpCompilationMappingAttribute = GetFSharpCompilationMappingAttribute(type);
		if (fSharpCompilationMappingAttribute == null)
		{
			return FSharpKind.Unrecognized;
		}
		if (type.IsGenericType)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			if (genericTypeDefinition == _fsharpOptionType)
			{
				return FSharpKind.Option;
			}
			if (genericTypeDefinition == _fsharpValueOptionType)
			{
				return FSharpKind.ValueOption;
			}
			if (genericTypeDefinition == _fsharpListType)
			{
				return FSharpKind.List;
			}
			if (genericTypeDefinition == _fsharpSetType)
			{
				return FSharpKind.Set;
			}
			if (genericTypeDefinition == _fsharpMapType)
			{
				return FSharpKind.Map;
			}
		}
		return (GetSourceConstructFlags(fSharpCompilationMappingAttribute) & SourceConstructFlags.KindMask) switch
		{
			SourceConstructFlags.RecordType => FSharpKind.Record, 
			SourceConstructFlags.SumType => FSharpKind.Union, 
			_ => FSharpKind.Unrecognized, 
		};
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public Func<TFSharpOption, T> CreateFSharpOptionValueGetter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TFSharpOption, T>()
	{
		MethodInfo methodInfo = EnsureMemberExists(typeof(TFSharpOption).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public), "Microsoft.FSharp.Core.FSharpOption<T>.get_Value()");
		return CreateDelegate<Func<TFSharpOption, T>>(methodInfo);
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public Func<TElement, TFSharpOption> CreateFSharpOptionSomeConstructor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TFSharpOption, TElement>()
	{
		MethodInfo methodInfo = EnsureMemberExists(typeof(TFSharpOption).GetMethod("Some", BindingFlags.Static | BindingFlags.Public), "Microsoft.FSharp.Core.FSharpOption<T>.Some(T value)");
		return CreateDelegate<Func<TElement, TFSharpOption>>(methodInfo);
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public StructGetter<TFSharpValueOption, TElement> CreateFSharpValueOptionValueGetter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TFSharpValueOption, TElement>() where TFSharpValueOption : struct
	{
		MethodInfo methodInfo = EnsureMemberExists(typeof(TFSharpValueOption).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public), "Microsoft.FSharp.Core.FSharpValueOption<T>.get_Value()");
		return CreateDelegate<StructGetter<TFSharpValueOption, TElement>>(methodInfo);
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public Func<TElement, TFSharpOption> CreateFSharpValueOptionSomeConstructor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TFSharpOption, TElement>()
	{
		MethodInfo methodInfo = EnsureMemberExists(typeof(TFSharpOption).GetMethod("Some", BindingFlags.Static | BindingFlags.Public), "Microsoft.FSharp.Core.FSharpValueOption<T>.ValueSome(T value)");
		return CreateDelegate<Func<TElement, TFSharpOption>>(methodInfo);
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public Func<IEnumerable<TElement>, TFSharpList> CreateFSharpListConstructor<TFSharpList, TElement>()
	{
		return CreateDelegate<Func<IEnumerable<TElement>, TFSharpList>>(EnsureMemberExists(_fsharpListCtor, "Microsoft.FSharp.Collections.ListModule.OfSeq<T>(IEnumerable<T> source)").MakeGenericMethod(typeof(TElement)));
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public Func<IEnumerable<TElement>, TFSharpSet> CreateFSharpSetConstructor<TFSharpSet, TElement>()
	{
		return CreateDelegate<Func<IEnumerable<TElement>, TFSharpSet>>(EnsureMemberExists(_fsharpSetCtor, "Microsoft.FSharp.Collections.SetModule.OfSeq<T>(IEnumerable<T> source)").MakeGenericMethod(typeof(TElement)));
	}

	[RequiresUnreferencedCode("Uses Reflection to access FSharp.Core components at runtime.")]
	public Func<IEnumerable<Tuple<TKey, TValue>>, TFSharpMap> CreateFSharpMapConstructor<TFSharpMap, TKey, TValue>()
	{
		return CreateDelegate<Func<IEnumerable<Tuple<TKey, TValue>>, TFSharpMap>>(EnsureMemberExists(_fsharpMapCtor, "Microsoft.FSharp.Collections.MapModule.OfSeq<TKey, TValue>(IEnumerable<Tuple<TKey, TValue>> source)").MakeGenericMethod(typeof(TKey), typeof(TValue)));
	}

	private Attribute GetFSharpCompilationMappingAttribute(Type type)
	{
		return type.GetCustomAttribute(_compilationMappingAttributeType, inherit: true);
	}

	private SourceConstructFlags GetSourceConstructFlags(Attribute compilationMappingAttribute)
	{
		if ((object)_sourceConstructFlagsGetter != null)
		{
			return (SourceConstructFlags)_sourceConstructFlagsGetter.Invoke(compilationMappingAttribute, null);
		}
		return SourceConstructFlags.None;
	}

	private static Assembly GetFSharpCoreAssembly(Type type)
	{
		object[] customAttributes = type.GetCustomAttributes(inherit: true);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			Attribute attribute = (Attribute)customAttributes[i];
			Type type2 = attribute.GetType();
			if (type2.FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute")
			{
				return type2.Assembly;
			}
		}
		return null;
	}

	private static TDelegate CreateDelegate<TDelegate>(MethodInfo methodInfo) where TDelegate : Delegate
	{
		return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), methodInfo, throwOnBindFailure: true);
	}

	private TMemberInfo EnsureMemberExists<TMemberInfo>(TMemberInfo memberInfo, string memberName) where TMemberInfo : MemberInfo
	{
		if ((object)memberInfo == null)
		{
			ThrowHelper.ThrowMissingMemberException_MissingFSharpCoreMember(memberName);
		}
		return memberInfo;
	}
}
