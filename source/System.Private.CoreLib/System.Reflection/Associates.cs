using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

internal static class Associates
{
	[Flags]
	internal enum Attributes
	{
		ComposedOfAllVirtualMethods = 1,
		ComposedOfAllPrivateMethods = 2,
		ComposedOfNoPublicMembers = 4,
		ComposedOfNoStaticMembers = 8
	}

	internal static bool IncludeAccessor(MethodInfo associate, bool nonPublic)
	{
		if ((object)associate == null)
		{
			return false;
		}
		if (nonPublic)
		{
			return true;
		}
		if (associate.IsPublic)
		{
			return true;
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Module.ResolveMethod is marked as RequiresUnreferencedCode because it relies on tokenswhich are not guaranteed to be stable across trimming. So if somebody hardcodes a token it could break.The usage here is not like that as all these tokens come from existing metadata loaded from some ILand so trimming has no effect (the tokens are read AFTER trimming occured).")]
	private static RuntimeMethodInfo AssignAssociates(int tkMethod, RuntimeType declaredType, RuntimeType reflectedType)
	{
		if (MetadataToken.IsNullToken(tkMethod))
		{
			return null;
		}
		bool flag = declaredType != reflectedType;
		IntPtr[] array = null;
		int typeInstCount = 0;
		RuntimeType[] instantiationInternal = declaredType.GetTypeHandleInternal().GetInstantiationInternal();
		if (instantiationInternal != null)
		{
			typeInstCount = instantiationInternal.Length;
			array = new IntPtr[instantiationInternal.Length];
			for (int i = 0; i < instantiationInternal.Length; i++)
			{
				array[i] = instantiationInternal[i].GetTypeHandleInternal().Value;
			}
		}
		RuntimeMethodHandleInternal runtimeMethodHandleInternal = ModuleHandle.ResolveMethodHandleInternal(RuntimeTypeHandle.GetModule(declaredType), tkMethod, array, typeInstCount, null, 0);
		if (flag)
		{
			MethodAttributes attributes = RuntimeMethodHandle.GetAttributes(runtimeMethodHandleInternal);
			if ((attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
			{
				return null;
			}
			if ((attributes & MethodAttributes.Virtual) != 0 && (RuntimeTypeHandle.GetAttributes(declaredType) & TypeAttributes.ClassSemanticsMask) == 0)
			{
				int slot = RuntimeMethodHandle.GetSlot(runtimeMethodHandleInternal);
				runtimeMethodHandleInternal = RuntimeTypeHandle.GetMethodAt(reflectedType, slot);
			}
		}
		RuntimeMethodInfo runtimeMethodInfo = RuntimeType.GetMethodBase(reflectedType, runtimeMethodHandleInternal) as RuntimeMethodInfo;
		return runtimeMethodInfo ?? (reflectedType.Module.ResolveMethod(tkMethod, null, null) as RuntimeMethodInfo);
	}

	internal static void AssignAssociates(MetadataImport scope, int mdPropEvent, RuntimeType declaringType, RuntimeType reflectedType, out RuntimeMethodInfo addOn, out RuntimeMethodInfo removeOn, out RuntimeMethodInfo fireOn, out RuntimeMethodInfo getter, out RuntimeMethodInfo setter, out MethodInfo[] other, out bool composedOfAllPrivateMethods, out BindingFlags bindingFlags)
	{
		addOn = (removeOn = (fireOn = (getter = (setter = null))));
		Attributes attributes = Attributes.ComposedOfAllVirtualMethods | Attributes.ComposedOfAllPrivateMethods | Attributes.ComposedOfNoPublicMembers | Attributes.ComposedOfNoStaticMembers;
		while (RuntimeTypeHandle.IsGenericVariable(reflectedType))
		{
			reflectedType = (RuntimeType)reflectedType.BaseType;
		}
		bool isInherited = declaringType != reflectedType;
		List<MethodInfo> list = null;
		scope.Enum(MetadataTokenType.MethodDef, mdPropEvent, out var result);
		int num = result.Length / 2;
		for (int i = 0; i < num; i++)
		{
			int tkMethod = result[i * 2];
			MethodSemanticsAttributes methodSemanticsAttributes = (MethodSemanticsAttributes)result[i * 2 + 1];
			RuntimeMethodInfo runtimeMethodInfo = AssignAssociates(tkMethod, declaringType, reflectedType);
			if (runtimeMethodInfo == null)
			{
				continue;
			}
			MethodAttributes attributes2 = runtimeMethodInfo.Attributes;
			bool flag = (attributes2 & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
			bool flag2 = (attributes2 & MethodAttributes.Virtual) != 0;
			MethodAttributes methodAttributes = attributes2 & MethodAttributes.MemberAccessMask;
			bool flag3 = methodAttributes == MethodAttributes.Public;
			bool flag4 = (attributes2 & MethodAttributes.Static) != 0;
			if (flag3)
			{
				attributes &= ~Attributes.ComposedOfNoPublicMembers;
				attributes &= ~Attributes.ComposedOfAllPrivateMethods;
			}
			else if (!flag)
			{
				attributes &= ~Attributes.ComposedOfAllPrivateMethods;
			}
			if (flag4)
			{
				attributes &= ~Attributes.ComposedOfNoStaticMembers;
			}
			if (!flag2)
			{
				attributes &= ~Attributes.ComposedOfAllVirtualMethods;
			}
			switch (methodSemanticsAttributes)
			{
			case MethodSemanticsAttributes.Setter:
				setter = runtimeMethodInfo;
				continue;
			case MethodSemanticsAttributes.Getter:
				getter = runtimeMethodInfo;
				continue;
			case MethodSemanticsAttributes.Fire:
				fireOn = runtimeMethodInfo;
				continue;
			case MethodSemanticsAttributes.AddOn:
				addOn = runtimeMethodInfo;
				continue;
			case MethodSemanticsAttributes.RemoveOn:
				removeOn = runtimeMethodInfo;
				continue;
			}
			if (list == null)
			{
				list = new List<MethodInfo>(num);
			}
			list.Add(runtimeMethodInfo);
		}
		bool isPublic = (attributes & Attributes.ComposedOfNoPublicMembers) == 0;
		bool isStatic = (attributes & Attributes.ComposedOfNoStaticMembers) == 0;
		bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
		composedOfAllPrivateMethods = (attributes & Attributes.ComposedOfAllPrivateMethods) != 0;
		other = list?.ToArray();
	}
}
