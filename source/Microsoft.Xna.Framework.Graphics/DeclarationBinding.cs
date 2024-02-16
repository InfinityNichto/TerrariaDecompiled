using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

internal class DeclarationBinding
{
	internal class BindingNode
	{
		private List<KeyValuePair<DeclarationBinding, BindingNode>> children;

		internal unsafe IDirect3DVertexDeclaration9* pDecl;

		internal unsafe VertexShaderInputSemantics* pSemantics;

		internal BindingNode GetChild(DeclarationBinding key)
		{
			List<KeyValuePair<DeclarationBinding, BindingNode>> list = children;
			if (list != null)
			{
				int num = 0;
				if (0 < list.Count)
				{
					do
					{
						KeyValuePair<DeclarationBinding, BindingNode> keyValuePair = children[num];
						if (keyValuePair.Key != key)
						{
							num++;
							continue;
						}
						return keyValuePair.Value;
					}
					while (num < children.Count);
				}
			}
			return null;
		}

		internal void AddChild(DeclarationBinding key, BindingNode value)
		{
			if (children == null)
			{
				children = new List<KeyValuePair<DeclarationBinding, BindingNode>>();
			}
			KeyValuePair<DeclarationBinding, BindingNode> item = new KeyValuePair<DeclarationBinding, BindingNode>(key, value);
			children.Add(item);
		}

		internal unsafe void RemoveChild(DeclarationBinding key)
		{
			List<KeyValuePair<DeclarationBinding, BindingNode>> list = children;
			if (list == null)
			{
				return;
			}
			int num = 0;
			if (0 >= list.Count)
			{
				return;
			}
			KeyValuePair<DeclarationBinding, BindingNode> keyValuePair;
			while (true)
			{
				keyValuePair = children[num];
				if (keyValuePair.Key == key)
				{
					break;
				}
				num++;
				if (num >= children.Count)
				{
					return;
				}
			}
			children.RemoveAt(num);
			BindingNode value = keyValuePair.Value;
			IDirect3DVertexDeclaration9* ptr = value.pDecl;
			if (ptr != null)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 8)))((nint)ptr);
				value.pDecl = null;
			}
			VertexShaderInputSemantics* ptr2 = value.pSemantics;
			if (ptr2 != null)
			{
				_003CModule_003E.delete(ptr2);
				value.pSemantics = null;
			}
			List<KeyValuePair<DeclarationBinding, BindingNode>> list2 = value.children;
			if (list2 == null)
			{
				return;
			}
			List<KeyValuePair<DeclarationBinding, BindingNode>>.Enumerator enumerator = list2.GetEnumerator();
			if (enumerator.MoveNext())
			{
				do
				{
					KeyValuePair<DeclarationBinding, BindingNode> current = enumerator.Current;
					current.Key.indirectOffspring.Remove(value);
					current.Value.RecursiveRelease();
				}
				while (enumerator.MoveNext());
			}
			value.children = null;
		}

		internal unsafe void RecursiveRelease()
		{
			IDirect3DVertexDeclaration9* ptr = pDecl;
			if (ptr != null)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 8)))((nint)ptr);
				pDecl = null;
			}
			VertexShaderInputSemantics* ptr2 = pSemantics;
			if (ptr2 != null)
			{
				_003CModule_003E.delete(ptr2);
				pSemantics = null;
			}
			List<KeyValuePair<DeclarationBinding, BindingNode>> list = children;
			if (list == null)
			{
				return;
			}
			List<KeyValuePair<DeclarationBinding, BindingNode>>.Enumerator enumerator = list.GetEnumerator();
			if (enumerator.MoveNext())
			{
				do
				{
					KeyValuePair<DeclarationBinding, BindingNode> current = enumerator.Current;
					current.Key.indirectOffspring.Remove(this);
					current.Value.RecursiveRelease();
				}
				while (enumerator.MoveNext());
			}
			children = null;
		}
	}

	internal VertexElement[] elements;

	internal int referenceCount;

	internal BindingNode root;

	internal Dictionary<BindingNode, bool> indirectOffspring;

	internal DeclarationBinding(VertexElement[] elements)
	{
		this.elements = elements;
		referenceCount = 1;
		base._002Ector();
		root = new BindingNode();
		indirectOffspring = new Dictionary<BindingNode, bool>();
	}
}
