using System.Collections.Generic;

namespace System.Runtime.Serialization;

internal sealed class CodeTypeReference : CodeObject
{
	private string _baseType;

	private readonly bool _isInterface;

	private CodeTypeReferenceCollection _typeArguments;

	private bool _needsFixup;

	public CodeTypeReference ArrayElementType { get; set; }

	public int ArrayRank { get; set; }

	internal int NestedArrayDepth
	{
		get
		{
			if (ArrayElementType != null)
			{
				return 1 + ArrayElementType.NestedArrayDepth;
			}
			return 0;
		}
	}

	public string BaseType
	{
		get
		{
			if (ArrayRank > 0 && ArrayElementType != null)
			{
				return ArrayElementType.BaseType;
			}
			if (string.IsNullOrEmpty(_baseType))
			{
				return string.Empty;
			}
			string baseType = _baseType;
			if (!_needsFixup || TypeArguments.Count <= 0)
			{
				return baseType;
			}
			return $"{baseType}`{TypeArguments.Count}";
		}
		set
		{
			_baseType = value;
			Initialize(_baseType);
		}
	}

	public CodeTypeReferenceOptions Options { get; set; }

	public CodeTypeReferenceCollection TypeArguments
	{
		get
		{
			if (ArrayRank > 0 && ArrayElementType != null)
			{
				return ArrayElementType.TypeArguments;
			}
			if (_typeArguments == null)
			{
				_typeArguments = new CodeTypeReferenceCollection();
			}
			return _typeArguments;
		}
	}

	internal bool IsInterface => _isInterface;

	public CodeTypeReference()
	{
		_baseType = string.Empty;
		ArrayRank = 0;
		ArrayElementType = null;
	}

	public CodeTypeReference(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (type.IsArray)
		{
			ArrayRank = type.GetArrayRank();
			ArrayElementType = new CodeTypeReference(type.GetElementType());
			_baseType = null;
		}
		else
		{
			InitializeFromType(type);
			ArrayRank = 0;
			ArrayElementType = null;
		}
		_isInterface = type.IsInterface;
	}

	public CodeTypeReference(Type type, CodeTypeReferenceOptions codeTypeReferenceOption)
		: this(type)
	{
		Options = codeTypeReferenceOption;
	}

	public CodeTypeReference(string typeName, CodeTypeReferenceOptions codeTypeReferenceOption)
	{
		Initialize(typeName, codeTypeReferenceOption);
	}

	public CodeTypeReference(string typeName)
	{
		Initialize(typeName);
	}

	private void InitializeFromType(Type type)
	{
		_baseType = type.Name;
		if (!type.IsGenericParameter)
		{
			Type type2 = type;
			while (type2.IsNested)
			{
				type2 = type2.DeclaringType;
				_baseType = type2.Name + "+" + _baseType;
			}
			if (!string.IsNullOrEmpty(type.Namespace))
			{
				_baseType = type.Namespace + "." + _baseType;
			}
		}
		if (type.IsGenericType && !type.ContainsGenericParameters)
		{
			Type[] genericArguments = type.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				TypeArguments.Add(new CodeTypeReference(genericArguments[i]));
			}
		}
		else if (!type.IsGenericTypeDefinition)
		{
			_needsFixup = true;
		}
	}

	private void Initialize(string typeName)
	{
		Initialize(typeName, Options);
	}

	private void Initialize(string typeName, CodeTypeReferenceOptions options)
	{
		Options = options;
		if (string.IsNullOrEmpty(typeName))
		{
			typeName = typeof(void).FullName;
			_baseType = typeName;
			ArrayRank = 0;
			ArrayElementType = null;
			return;
		}
		typeName = RipOffAssemblyInformationFromTypeName(typeName);
		int num = typeName.Length - 1;
		int num2 = num;
		_needsFixup = true;
		Queue<int> queue = new Queue<int>();
		while (num2 >= 0)
		{
			int num3 = 1;
			if (typeName[num2--] != ']')
			{
				break;
			}
			while (num2 >= 0 && typeName[num2] == ',')
			{
				num3++;
				num2--;
			}
			if (num2 < 0 || typeName[num2] != '[')
			{
				break;
			}
			queue.Enqueue(num3);
			num2--;
			num = num2;
		}
		num2 = num;
		List<CodeTypeReference> list = new List<CodeTypeReference>();
		Stack<string> stack = new Stack<string>();
		if (num2 > 0 && typeName[num2--] == ']')
		{
			_needsFixup = false;
			int num4 = 1;
			int num5 = num;
			while (num2 >= 0)
			{
				if (typeName[num2] == '[')
				{
					if (--num4 == 0)
					{
						break;
					}
				}
				else if (typeName[num2] == ']')
				{
					num4++;
				}
				else if (typeName[num2] == ',' && num4 == 1)
				{
					if (num2 + 1 < num5)
					{
						stack.Push(typeName.Substring(num2 + 1, num5 - num2 - 1));
					}
					num5 = num2;
				}
				num2--;
			}
			if (num2 > 0 && num - num2 - 1 > 0)
			{
				if (num2 + 1 < num5)
				{
					stack.Push(typeName.Substring(num2 + 1, num5 - num2 - 1));
				}
				while (stack.Count > 0)
				{
					string typeName2 = RipOffAssemblyInformationFromTypeName(stack.Pop());
					list.Add(new CodeTypeReference(typeName2));
				}
				num = num2 - 1;
			}
		}
		if (num < 0)
		{
			_baseType = typeName;
			return;
		}
		if (queue.Count > 0)
		{
			CodeTypeReference codeTypeReference = new CodeTypeReference(typeName.Substring(0, num + 1), Options);
			for (int i = 0; i < list.Count; i++)
			{
				codeTypeReference.TypeArguments.Add(list[i]);
			}
			while (queue.Count > 1)
			{
				codeTypeReference = new CodeTypeReference(codeTypeReference, queue.Dequeue());
			}
			_baseType = null;
			ArrayRank = queue.Dequeue();
			ArrayElementType = codeTypeReference;
		}
		else if (list.Count > 0)
		{
			for (int j = 0; j < list.Count; j++)
			{
				TypeArguments.Add(list[j]);
			}
			_baseType = typeName.Substring(0, num + 1);
		}
		else
		{
			_baseType = typeName;
		}
		if (_baseType != null && _baseType.IndexOf('`') != -1)
		{
			_needsFixup = false;
		}
	}

	public CodeTypeReference(string typeName, params CodeTypeReference[] typeArguments)
		: this(typeName)
	{
		if (typeArguments != null && typeArguments.Length != 0)
		{
			TypeArguments.AddRange(typeArguments);
		}
	}

	public CodeTypeReference(string baseType, int rank)
	{
		_baseType = null;
		ArrayRank = rank;
		ArrayElementType = new CodeTypeReference(baseType);
	}

	public CodeTypeReference(CodeTypeReference arrayType, int rank)
	{
		_baseType = null;
		ArrayRank = rank;
		ArrayElementType = arrayType;
	}

	private string RipOffAssemblyInformationFromTypeName(string typeName)
	{
		int i = 0;
		int num = typeName.Length - 1;
		string result = typeName;
		for (; i < typeName.Length && char.IsWhiteSpace(typeName[i]); i++)
		{
		}
		while (num >= 0 && char.IsWhiteSpace(typeName[num]))
		{
			num--;
		}
		if (i < num)
		{
			if (typeName[i] == '[' && typeName[num] == ']')
			{
				i++;
				num--;
			}
			if (typeName[num] != ']')
			{
				int num2 = 0;
				for (int num3 = num; num3 >= i; num3--)
				{
					if (typeName[num3] == ',')
					{
						num2++;
						if (num2 == 4)
						{
							result = typeName.Substring(i, num3 - i);
							break;
						}
					}
				}
			}
		}
		return result;
	}
}
