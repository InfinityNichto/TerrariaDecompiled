using System.Collections.Generic;
using System.Text;

namespace System.Reflection.Emit;

internal sealed class TypeNameBuilder
{
	internal enum Format
	{
		ToString,
		FullName,
		AssemblyQualifiedName
	}

	private StringBuilder _str = new StringBuilder();

	private int _instNesting;

	private bool _firstInstArg;

	private bool _nestedName;

	private bool _hasAssemblySpec;

	private bool _useAngleBracketsForGenerics;

	private List<int> _stack = new List<int>();

	private int _stackIdx;

	private TypeNameBuilder()
	{
	}

	private void OpenGenericArguments()
	{
		_instNesting++;
		_firstInstArg = true;
		if (_useAngleBracketsForGenerics)
		{
			Append('<');
		}
		else
		{
			Append('[');
		}
	}

	private void CloseGenericArguments()
	{
		_instNesting--;
		if (_firstInstArg)
		{
			_str.Remove(_str.Length - 1, 1);
		}
		else if (_useAngleBracketsForGenerics)
		{
			Append('>');
		}
		else
		{
			Append(']');
		}
	}

	private void OpenGenericArgument()
	{
		_nestedName = false;
		if (!_firstInstArg)
		{
			Append(',');
		}
		_firstInstArg = false;
		if (_useAngleBracketsForGenerics)
		{
			Append('<');
		}
		else
		{
			Append('[');
		}
		PushOpenGenericArgument();
	}

	private void CloseGenericArgument()
	{
		if (_hasAssemblySpec)
		{
			if (_useAngleBracketsForGenerics)
			{
				Append('>');
			}
			else
			{
				Append(']');
			}
		}
		PopOpenGenericArgument();
	}

	private void AddName(string name)
	{
		if (_nestedName)
		{
			Append('+');
		}
		_nestedName = true;
		EscapeName(name);
	}

	private void AddArray(int rank)
	{
		if (rank == 1)
		{
			Append("[*]");
			return;
		}
		if (rank > 64)
		{
			_str.Append('[').Append(rank).Append(']');
			return;
		}
		Append('[');
		for (int i = 1; i < rank; i++)
		{
			Append(',');
		}
		Append(']');
	}

	private void AddAssemblySpec(string assemblySpec)
	{
		if (assemblySpec != null && !assemblySpec.Equals(""))
		{
			Append(", ");
			if (_instNesting > 0)
			{
				EscapeEmbeddedAssemblyName(assemblySpec);
			}
			else
			{
				EscapeAssemblyName(assemblySpec);
			}
			_hasAssemblySpec = true;
		}
	}

	public override string ToString()
	{
		return _str.ToString();
	}

	private static bool ContainsReservedChar(string name)
	{
		foreach (char c in name)
		{
			if (c == '\0')
			{
				break;
			}
			if (IsTypeNameReservedChar(c))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsTypeNameReservedChar(char ch)
	{
		switch (ch)
		{
		case '&':
		case '*':
		case '+':
		case ',':
		case '[':
		case '\\':
		case ']':
			return true;
		default:
			return false;
		}
	}

	private void EscapeName(string name)
	{
		if (ContainsReservedChar(name))
		{
			foreach (char c in name)
			{
				if (c != 0)
				{
					if (IsTypeNameReservedChar(c))
					{
						_str.Append('\\');
					}
					_str.Append(c);
					continue;
				}
				break;
			}
		}
		else
		{
			Append(name);
		}
	}

	private void EscapeAssemblyName(string name)
	{
		Append(name);
	}

	private void EscapeEmbeddedAssemblyName(string name)
	{
		if (name.Contains(']'))
		{
			foreach (char c in name)
			{
				if (c == ']')
				{
					Append('\\');
				}
				Append(c);
			}
		}
		else
		{
			Append(name);
		}
	}

	private void PushOpenGenericArgument()
	{
		_stack.Add(_str.Length);
		_stackIdx++;
	}

	private void PopOpenGenericArgument()
	{
		int num = _stack[--_stackIdx];
		_stack.RemoveAt(_stackIdx);
		if (!_hasAssemblySpec)
		{
			_str.Remove(num - 1, 1);
		}
		_hasAssemblySpec = false;
	}

	private void Append(string pStr)
	{
		foreach (char c in pStr)
		{
			if (c != 0)
			{
				_str.Append(c);
				continue;
			}
			break;
		}
	}

	private void Append(char c)
	{
		_str.Append(c);
	}

	internal static string ToString(Type type, Format format)
	{
		if ((format == Format.FullName || format == Format.AssemblyQualifiedName) && !type.IsGenericTypeDefinition && type.ContainsGenericParameters)
		{
			return null;
		}
		TypeNameBuilder typeNameBuilder = new TypeNameBuilder();
		typeNameBuilder.AddAssemblyQualifiedName(type, format);
		return typeNameBuilder.ToString();
	}

	private void AddElementType(Type type)
	{
		if (type.HasElementType)
		{
			AddElementType(type.GetElementType());
			if (type.IsPointer)
			{
				Append('*');
			}
			else if (type.IsByRef)
			{
				Append('&');
			}
			else if (type.IsSZArray)
			{
				Append("[]");
			}
			else if (type.IsArray)
			{
				AddArray(type.GetArrayRank());
			}
		}
	}

	private void AddAssemblyQualifiedName(Type type, Format format)
	{
		Type type2 = type;
		while (type2.HasElementType)
		{
			type2 = type2.GetElementType();
		}
		List<Type> list = new List<Type>();
		Type type3 = type2;
		while (type3 != null)
		{
			list.Add(type3);
			type3 = (type3.IsGenericParameter ? null : type3.DeclaringType);
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Type type4 = list[num];
			string text = type4.Name;
			if (num == list.Count - 1 && type4.Namespace != null && type4.Namespace.Length != 0)
			{
				text = type4.Namespace + "." + text;
			}
			AddName(text);
		}
		if (type2.IsGenericType && (!type2.IsGenericTypeDefinition || format == Format.ToString))
		{
			Type[] genericArguments = type2.GetGenericArguments();
			OpenGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				Format format2 = ((format == Format.FullName) ? Format.AssemblyQualifiedName : format);
				OpenGenericArgument();
				AddAssemblyQualifiedName(genericArguments[i], format2);
				CloseGenericArgument();
			}
			CloseGenericArguments();
		}
		AddElementType(type);
		if (format == Format.AssemblyQualifiedName)
		{
			AddAssemblySpec(type.Module.Assembly.FullName);
		}
	}
}
