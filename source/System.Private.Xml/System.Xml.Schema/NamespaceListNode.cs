using System.Collections;

namespace System.Xml.Schema;

internal sealed class NamespaceListNode : SyntaxTreeNode
{
	private NamespaceList namespaceList;

	private object particle;

	public override bool IsNullable
	{
		get
		{
			throw new InvalidOperationException();
		}
	}

	public NamespaceListNode(NamespaceList namespaceList, object particle)
	{
		this.namespaceList = namespaceList;
		this.particle = particle;
	}

	public ICollection GetResolvedSymbols(SymbolsDictionary symbols)
	{
		return symbols.GetNamespaceListSymbols(namespaceList);
	}

	public override void ExpandTree(InteriorNode parent, SymbolsDictionary symbols, Positions positions)
	{
		SyntaxTreeNode syntaxTreeNode = null;
		foreach (int resolvedSymbol in GetResolvedSymbols(symbols))
		{
			if (symbols.GetParticle(resolvedSymbol) != particle)
			{
				symbols.IsUpaEnforced = false;
			}
			LeafNode leafNode = new LeafNode(positions.Add(resolvedSymbol, particle));
			if (syntaxTreeNode == null)
			{
				syntaxTreeNode = leafNode;
				continue;
			}
			InteriorNode interiorNode = new ChoiceNode();
			interiorNode.LeftChild = syntaxTreeNode;
			interiorNode.RightChild = leafNode;
			syntaxTreeNode = interiorNode;
		}
		if (parent.LeftChild == this)
		{
			parent.LeftChild = syntaxTreeNode;
		}
		else
		{
			parent.RightChild = syntaxTreeNode;
		}
	}

	public override void ConstructPos(BitSet firstpos, BitSet lastpos, BitSet[] followpos)
	{
		throw new InvalidOperationException();
	}
}
