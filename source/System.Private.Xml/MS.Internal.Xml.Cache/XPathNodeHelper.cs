using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal abstract class XPathNodeHelper
{
	public static int GetLocalNamespaces(XPathNode[] pageElem, int idxElem, out XPathNode[] pageNmsp)
	{
		if (pageElem[idxElem].HasNamespaceDecls)
		{
			return pageElem[idxElem].Document.LookupNamespaces(pageElem, idxElem, out pageNmsp);
		}
		pageNmsp = null;
		return 0;
	}

	public static int GetInScopeNamespaces(XPathNode[] pageElem, int idxElem, out XPathNode[] pageNmsp)
	{
		if (pageElem[idxElem].NodeType == XPathNodeType.Element)
		{
			XPathDocument document = pageElem[idxElem].Document;
			while (!pageElem[idxElem].HasNamespaceDecls)
			{
				idxElem = pageElem[idxElem].GetParent(out pageElem);
				if (idxElem == 0)
				{
					return document.GetXmlNamespaceNode(out pageNmsp);
				}
			}
			return document.LookupNamespaces(pageElem, idxElem, out pageNmsp);
		}
		pageNmsp = null;
		return 0;
	}

	public static bool GetFirstAttribute(ref XPathNode[] pageNode, ref int idxNode)
	{
		if (pageNode[idxNode].HasAttribute)
		{
			GetChild(ref pageNode, ref idxNode);
			return true;
		}
		return false;
	}

	public static bool GetNextAttribute(ref XPathNode[] pageNode, ref int idxNode)
	{
		XPathNode[] pageNode2;
		int sibling = pageNode[idxNode].GetSibling(out pageNode2);
		if (sibling != 0 && pageNode2[sibling].NodeType == XPathNodeType.Attribute)
		{
			pageNode = pageNode2;
			idxNode = sibling;
			return true;
		}
		return false;
	}

	public static bool GetContentChild(ref XPathNode[] pageNode, ref int idxNode)
	{
		XPathNode[] pageNode2 = pageNode;
		int idxNode2 = idxNode;
		if (pageNode2[idxNode2].HasContentChild)
		{
			GetChild(ref pageNode2, ref idxNode2);
			while (pageNode2[idxNode2].NodeType == XPathNodeType.Attribute)
			{
				idxNode2 = pageNode2[idxNode2].GetSibling(out pageNode2);
			}
			pageNode = pageNode2;
			idxNode = idxNode2;
			return true;
		}
		return false;
	}

	public static bool GetContentSibling(ref XPathNode[] pageNode, ref int idxNode)
	{
		XPathNode[] pageNode2 = pageNode;
		int num = idxNode;
		if (!pageNode2[num].IsAttrNmsp)
		{
			num = pageNode2[num].GetSibling(out pageNode2);
			if (num != 0)
			{
				pageNode = pageNode2;
				idxNode = num;
				return true;
			}
		}
		return false;
	}

	public static bool GetParent(ref XPathNode[] pageNode, ref int idxNode)
	{
		XPathNode[] pageNode2 = pageNode;
		int num = idxNode;
		num = pageNode2[num].GetParent(out pageNode2);
		if (num != 0)
		{
			pageNode = pageNode2;
			idxNode = num;
			return true;
		}
		return false;
	}

	public static int GetLocation(XPathNode[] pageNode, int idxNode)
	{
		return (pageNode[0].PageInfo.PageNumber << 16) | idxNode;
	}

	public static bool GetElementChild(ref XPathNode[] pageNode, ref int idxNode, string localName, string namespaceName)
	{
		XPathNode[] pageNode2 = pageNode;
		int idxNode2 = idxNode;
		if (pageNode2[idxNode2].HasElementChild)
		{
			GetChild(ref pageNode2, ref idxNode2);
			do
			{
				if (pageNode2[idxNode2].ElementMatch(localName, namespaceName))
				{
					pageNode = pageNode2;
					idxNode = idxNode2;
					return true;
				}
				idxNode2 = pageNode2[idxNode2].GetSibling(out pageNode2);
			}
			while (idxNode2 != 0);
		}
		return false;
	}

	public static bool GetElementSibling(ref XPathNode[] pageNode, ref int idxNode, string localName, string namespaceName)
	{
		XPathNode[] pageNode2 = pageNode;
		int num = idxNode;
		if (pageNode2[num].NodeType != XPathNodeType.Attribute)
		{
			while (true)
			{
				num = pageNode2[num].GetSibling(out pageNode2);
				if (num == 0)
				{
					break;
				}
				if (pageNode2[num].ElementMatch(localName, namespaceName))
				{
					pageNode = pageNode2;
					idxNode = num;
					return true;
				}
			}
		}
		return false;
	}

	public static bool GetContentChild(ref XPathNode[] pageNode, ref int idxNode, XPathNodeType typ)
	{
		XPathNode[] pageNode2 = pageNode;
		int idxNode2 = idxNode;
		if (pageNode2[idxNode2].HasContentChild)
		{
			int contentKindMask = XPathNavigator.GetContentKindMask(typ);
			GetChild(ref pageNode2, ref idxNode2);
			do
			{
				if (((1 << (int)pageNode2[idxNode2].NodeType) & contentKindMask) != 0)
				{
					if (typ == XPathNodeType.Attribute)
					{
						return false;
					}
					pageNode = pageNode2;
					idxNode = idxNode2;
					return true;
				}
				idxNode2 = pageNode2[idxNode2].GetSibling(out pageNode2);
			}
			while (idxNode2 != 0);
		}
		return false;
	}

	public static bool GetContentSibling(ref XPathNode[] pageNode, ref int idxNode, XPathNodeType typ)
	{
		XPathNode[] pageNode2 = pageNode;
		int num = idxNode;
		int contentKindMask = XPathNavigator.GetContentKindMask(typ);
		if (pageNode2[num].NodeType != XPathNodeType.Attribute)
		{
			while (true)
			{
				num = pageNode2[num].GetSibling(out pageNode2);
				if (num == 0)
				{
					break;
				}
				if (((1 << (int)pageNode2[num].NodeType) & contentKindMask) != 0)
				{
					pageNode = pageNode2;
					idxNode = num;
					return true;
				}
			}
		}
		return false;
	}

	public static bool GetPreviousContentSibling(ref XPathNode[] pageNode, ref int idxNode)
	{
		int num = idxNode;
		num = pageNode[num].GetParent(out var pageNode2);
		if (num != 0)
		{
			int num2 = idxNode - 1;
			XPathNode[] array;
			if (num2 == 0)
			{
				array = pageNode[0].PageInfo.PreviousPage;
				num2 = array.Length - 1;
			}
			else
			{
				array = pageNode;
			}
			if (num == num2 && pageNode2 == array)
			{
				return false;
			}
			XPathNode[] pageNode3 = array;
			int num3 = num2;
			do
			{
				array = pageNode3;
				num2 = num3;
				num3 = pageNode3[num3].GetParent(out pageNode3);
			}
			while (num3 != num || pageNode3 != pageNode2);
			if (array[num2].NodeType != XPathNodeType.Attribute)
			{
				pageNode = array;
				idxNode = num2;
				return true;
			}
		}
		return false;
	}

	public static bool GetAttribute(ref XPathNode[] pageNode, ref int idxNode, string localName, string namespaceName)
	{
		XPathNode[] pageNode2 = pageNode;
		int idxNode2 = idxNode;
		if (pageNode2[idxNode2].HasAttribute)
		{
			GetChild(ref pageNode2, ref idxNode2);
			do
			{
				if (pageNode2[idxNode2].NameMatch(localName, namespaceName))
				{
					pageNode = pageNode2;
					idxNode = idxNode2;
					return true;
				}
				idxNode2 = pageNode2[idxNode2].GetSibling(out pageNode2);
			}
			while (idxNode2 != 0 && pageNode2[idxNode2].NodeType == XPathNodeType.Attribute);
		}
		return false;
	}

	public static bool GetElementFollowing(ref XPathNode[] pageCurrent, ref int idxCurrent, XPathNode[] pageEnd, int idxEnd, string localName, string namespaceName)
	{
		XPathNode[] pageNode = pageCurrent;
		int i = idxCurrent;
		if (pageNode[i].NodeType == XPathNodeType.Element && (object)pageNode[i].LocalName == localName)
		{
			int num = 0;
			if (pageEnd != null)
			{
				num = pageEnd[0].PageInfo.PageNumber;
				int pageNumber = pageNode[0].PageInfo.PageNumber;
				if (pageNumber > num || (pageNumber == num && i >= idxEnd))
				{
					pageEnd = null;
				}
			}
			while (true)
			{
				i = pageNode[i].GetSimilarElement(out pageNode);
				if (i != 0)
				{
					if (pageEnd != null)
					{
						int pageNumber = pageNode[0].PageInfo.PageNumber;
						if (pageNumber > num || (pageNumber == num && i >= idxEnd))
						{
							goto IL_00bd;
						}
					}
					if ((object)pageNode[i].LocalName == localName && pageNode[i].NamespaceUri == namespaceName)
					{
						break;
					}
					continue;
				}
				goto IL_00bd;
				IL_00bd:
				return false;
			}
		}
		else
		{
			i++;
			while (true)
			{
				if (pageNode == pageEnd && i <= idxEnd)
				{
					for (; i != idxEnd; i++)
					{
						if (pageNode[i].ElementMatch(localName, namespaceName))
						{
							goto end_IL_00c3;
						}
					}
				}
				else
				{
					for (; i < pageNode[0].PageInfo.NodeCount; i++)
					{
						if (pageNode[i].ElementMatch(localName, namespaceName))
						{
							goto end_IL_00c3;
						}
					}
					pageNode = pageNode[0].PageInfo.NextPage;
					i = 1;
					if (pageNode != null)
					{
						continue;
					}
				}
				return false;
				continue;
				end_IL_00c3:
				break;
			}
		}
		pageCurrent = pageNode;
		idxCurrent = i;
		return true;
	}

	public static bool GetContentFollowing(ref XPathNode[] pageCurrent, ref int idxCurrent, XPathNode[] pageEnd, int idxEnd, XPathNodeType typ)
	{
		XPathNode[] array = pageCurrent;
		int num = idxCurrent;
		int contentKindMask = XPathNavigator.GetContentKindMask(typ);
		num++;
		while (true)
		{
			if (array == pageEnd && num <= idxEnd)
			{
				for (; num != idxEnd; num++)
				{
					if (((1 << (int)array[num].NodeType) & contentKindMask) != 0)
					{
						goto end_IL_0012;
					}
				}
			}
			else
			{
				for (; num < array[0].PageInfo.NodeCount; num++)
				{
					if (((1 << (int)array[num].NodeType) & contentKindMask) != 0)
					{
						goto end_IL_0012;
					}
				}
				array = array[0].PageInfo.NextPage;
				num = 1;
				if (array != null)
				{
					continue;
				}
			}
			return false;
			continue;
			end_IL_0012:
			break;
		}
		pageCurrent = array;
		idxCurrent = num;
		return true;
	}

	public static bool GetTextFollowing(ref XPathNode[] pageCurrent, ref int idxCurrent, XPathNode[] pageEnd, int idxEnd)
	{
		XPathNode[] array = pageCurrent;
		int num = idxCurrent;
		num++;
		while (true)
		{
			if (array == pageEnd && num <= idxEnd)
			{
				for (; num != idxEnd; num++)
				{
					if (array[num].IsText || (array[num].NodeType == XPathNodeType.Element && array[num].HasCollapsedText))
					{
						goto end_IL_000a;
					}
				}
			}
			else
			{
				for (; num < array[0].PageInfo.NodeCount; num++)
				{
					if (array[num].IsText || (array[num].NodeType == XPathNodeType.Element && array[num].HasCollapsedText))
					{
						goto end_IL_000a;
					}
				}
				array = array[0].PageInfo.NextPage;
				num = 1;
				if (array != null)
				{
					continue;
				}
			}
			return false;
			continue;
			end_IL_000a:
			break;
		}
		pageCurrent = array;
		idxCurrent = num;
		return true;
	}

	public static bool GetNonDescendant(ref XPathNode[] pageNode, ref int idxNode)
	{
		XPathNode[] pageNode2 = pageNode;
		int num = idxNode;
		do
		{
			if (pageNode2[num].HasSibling)
			{
				pageNode = pageNode2;
				idxNode = pageNode2[num].GetSibling(out pageNode);
				return true;
			}
			num = pageNode2[num].GetParent(out pageNode2);
		}
		while (num != 0);
		return false;
	}

	private static void GetChild(ref XPathNode[] pageNode, ref int idxNode)
	{
		if (++idxNode >= pageNode.Length)
		{
			pageNode = pageNode[0].PageInfo.NextPage;
			idxNode = 1;
		}
	}
}
