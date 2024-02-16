using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace System;

internal sealed class TypeNameParser : IDisposable
{
	private readonly SafeTypeNameParserHandle m_NativeParser;

	private static readonly char[] SPECIAL_CHARS = new char[7] { ',', '[', ']', '&', '*', '+', '\\' };

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _CreateTypeNameParser(string typeName, ObjectHandleOnStack retHandle, bool throwOnError);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _GetNames(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _GetTypeArguments(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _GetModifiers(SafeTypeNameParserHandle pTypeNameParser, ObjectHandleOnStack retArray);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _GetAssemblyName(SafeTypeNameParserHandle pTypeNameParser, StringHandleOnStack retString);

	[RequiresUnreferencedCode("The type might be removed")]
	internal static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
	{
		if (typeName == null)
		{
			throw new ArgumentNullException("typeName");
		}
		if (typeName.Length > 0 && typeName[0] == '\0')
		{
			throw new ArgumentException(SR.Format_StringZeroLength);
		}
		Type result = null;
		SafeTypeNameParserHandle safeTypeNameParserHandle = CreateTypeNameParser(typeName, throwOnError);
		if (safeTypeNameParserHandle != null)
		{
			using TypeNameParser typeNameParser = new TypeNameParser(safeTypeNameParserHandle);
			result = typeNameParser.ConstructType(assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
		}
		return result;
	}

	private TypeNameParser(SafeTypeNameParserHandle handle)
	{
		m_NativeParser = handle;
	}

	public void Dispose()
	{
		m_NativeParser.Dispose();
	}

	[RequiresUnreferencedCode("The type might be removed")]
	private unsafe Type ConstructType(Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
	{
		Assembly assembly = null;
		string assemblyName = GetAssemblyName();
		if (assemblyName.Length > 0)
		{
			assembly = ResolveAssembly(assemblyName, assemblyResolver, throwOnError, ref stackMark);
			if (assembly == null)
			{
				return null;
			}
		}
		string[] names = GetNames();
		if (names == null)
		{
			if (throwOnError)
			{
				throw new TypeLoadException(SR.Arg_TypeLoadNullStr);
			}
			return null;
		}
		Type type = ResolveType(assembly, names, typeResolver, throwOnError, ignoreCase, ref stackMark);
		if (type == null)
		{
			return null;
		}
		SafeTypeNameParserHandle[] typeArguments = GetTypeArguments();
		Type[] array = null;
		if (typeArguments != null)
		{
			array = new Type[typeArguments.Length];
			for (int i = 0; i < typeArguments.Length; i++)
			{
				using (TypeNameParser typeNameParser = new TypeNameParser(typeArguments[i]))
				{
					array[i] = typeNameParser.ConstructType(assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackMark);
				}
				if (array[i] == null)
				{
					return null;
				}
			}
		}
		int[] modifiers = GetModifiers();
		fixed (int* value = modifiers)
		{
			IntPtr pModifiers = new IntPtr(value);
			return RuntimeTypeHandle.GetTypeHelper(type, array, pModifiers, (modifiers != null) ? modifiers.Length : 0);
		}
	}

	private static Assembly ResolveAssembly(string asmName, Func<AssemblyName, Assembly> assemblyResolver, bool throwOnError, ref StackCrawlMark stackMark)
	{
		Assembly assembly;
		if (assemblyResolver == null)
		{
			if (throwOnError)
			{
				assembly = RuntimeAssembly.InternalLoad(asmName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
			}
			else
			{
				try
				{
					assembly = RuntimeAssembly.InternalLoad(asmName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
				}
				catch (FileNotFoundException)
				{
					return null;
				}
			}
		}
		else
		{
			assembly = assemblyResolver(new AssemblyName(asmName));
			if (assembly == null && throwOnError)
			{
				throw new FileNotFoundException(SR.Format(SR.FileNotFound_ResolveAssembly, asmName));
			}
		}
		return assembly;
	}

	[RequiresUnreferencedCode("The type might be removed")]
	private static Type ResolveType(Assembly assembly, string[] names, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase, ref StackCrawlMark stackMark)
	{
		string text = EscapeTypeName(names[0]);
		Type type;
		if (typeResolver == null)
		{
			type = ((!(assembly == null)) ? assembly.GetType(text, throwOnError, ignoreCase) : RuntimeType.GetType(text, throwOnError, ignoreCase, ref stackMark));
		}
		else
		{
			type = typeResolver(assembly, text, ignoreCase);
			if (type == null && throwOnError)
			{
				string message = ((assembly == null) ? SR.Format(SR.TypeLoad_ResolveType, text) : SR.Format(SR.TypeLoad_ResolveTypeFromAssembly, text, assembly.FullName));
				throw new TypeLoadException(message);
			}
		}
		if (type != null)
		{
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
			if (ignoreCase)
			{
				bindingFlags |= BindingFlags.IgnoreCase;
			}
			for (int i = 1; i < names.Length; i++)
			{
				type = type.GetNestedType(names[i], bindingFlags);
				if (type == null)
				{
					if (!throwOnError)
					{
						break;
					}
					throw new TypeLoadException(SR.Format(SR.TypeLoad_ResolveNestedType, names[i], names[i - 1]));
				}
			}
		}
		return type;
	}

	private static string EscapeTypeName(string name)
	{
		if (name.IndexOfAny(SPECIAL_CHARS) < 0)
		{
			return name;
		}
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		foreach (char c in name)
		{
			if (Array.IndexOf(SPECIAL_CHARS, c) >= 0)
			{
				valueStringBuilder.Append('\\');
			}
			valueStringBuilder.Append(c);
		}
		return valueStringBuilder.ToString();
	}

	private static SafeTypeNameParserHandle CreateTypeNameParser(string typeName, bool throwOnError)
	{
		SafeTypeNameParserHandle o = null;
		_CreateTypeNameParser(typeName, ObjectHandleOnStack.Create(ref o), throwOnError);
		return o;
	}

	private string[] GetNames()
	{
		string[] o = null;
		_GetNames(m_NativeParser, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	private SafeTypeNameParserHandle[] GetTypeArguments()
	{
		SafeTypeNameParserHandle[] o = null;
		_GetTypeArguments(m_NativeParser, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	private int[] GetModifiers()
	{
		int[] o = null;
		_GetModifiers(m_NativeParser, ObjectHandleOnStack.Create(ref o));
		return o;
	}

	private string GetAssemblyName()
	{
		string s = null;
		_GetAssemblyName(m_NativeParser, new StringHandleOnStack(ref s));
		return s;
	}
}
