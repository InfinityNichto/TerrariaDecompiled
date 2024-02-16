using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen;

internal static class XmlILMethods
{
	public static readonly MethodInfo AncCreate = typeof(AncestorIterator).GetMethod("Create");

	public static readonly MethodInfo AncNext = typeof(AncestorIterator).GetMethod("MoveNext");

	public static readonly MethodInfo AncCurrent = typeof(AncestorIterator).GetMethod("get_Current");

	public static readonly MethodInfo AncDOCreate = typeof(AncestorDocOrderIterator).GetMethod("Create");

	public static readonly MethodInfo AncDONext = typeof(AncestorDocOrderIterator).GetMethod("MoveNext");

	public static readonly MethodInfo AncDOCurrent = typeof(AncestorDocOrderIterator).GetMethod("get_Current");

	public static readonly MethodInfo AttrContentCreate = typeof(AttributeContentIterator).GetMethod("Create");

	public static readonly MethodInfo AttrContentNext = typeof(AttributeContentIterator).GetMethod("MoveNext");

	public static readonly MethodInfo AttrContentCurrent = typeof(AttributeContentIterator).GetMethod("get_Current");

	public static readonly MethodInfo AttrCreate = typeof(AttributeIterator).GetMethod("Create");

	public static readonly MethodInfo AttrNext = typeof(AttributeIterator).GetMethod("MoveNext");

	public static readonly MethodInfo AttrCurrent = typeof(AttributeIterator).GetMethod("get_Current");

	public static readonly MethodInfo ContentCreate = typeof(ContentIterator).GetMethod("Create");

	public static readonly MethodInfo ContentNext = typeof(ContentIterator).GetMethod("MoveNext");

	public static readonly MethodInfo ContentCurrent = typeof(ContentIterator).GetMethod("get_Current");

	public static readonly MethodInfo ContentMergeCreate = typeof(ContentMergeIterator).GetMethod("Create");

	public static readonly MethodInfo ContentMergeNext = typeof(ContentMergeIterator).GetMethod("MoveNext");

	public static readonly MethodInfo ContentMergeCurrent = typeof(ContentMergeIterator).GetMethod("get_Current");

	public static readonly MethodInfo DescCreate = typeof(DescendantIterator).GetMethod("Create");

	public static readonly MethodInfo DescNext = typeof(DescendantIterator).GetMethod("MoveNext");

	public static readonly MethodInfo DescCurrent = typeof(DescendantIterator).GetMethod("get_Current");

	public static readonly MethodInfo DescMergeCreate = typeof(DescendantMergeIterator).GetMethod("Create");

	public static readonly MethodInfo DescMergeNext = typeof(DescendantMergeIterator).GetMethod("MoveNext");

	public static readonly MethodInfo DescMergeCurrent = typeof(DescendantMergeIterator).GetMethod("get_Current");

	public static readonly MethodInfo DiffCreate = typeof(DifferenceIterator).GetMethod("Create");

	public static readonly MethodInfo DiffNext = typeof(DifferenceIterator).GetMethod("MoveNext");

	public static readonly MethodInfo DiffCurrent = typeof(DifferenceIterator).GetMethod("get_Current");

	public static readonly MethodInfo DodMergeCreate = typeof(DodSequenceMerge).GetMethod("Create");

	public static readonly MethodInfo DodMergeAdd = typeof(DodSequenceMerge).GetMethod("AddSequence");

	public static readonly MethodInfo DodMergeSeq = typeof(DodSequenceMerge).GetMethod("MergeSequences");

	public static readonly MethodInfo ElemContentCreate = typeof(ElementContentIterator).GetMethod("Create");

	public static readonly MethodInfo ElemContentNext = typeof(ElementContentIterator).GetMethod("MoveNext");

	public static readonly MethodInfo ElemContentCurrent = typeof(ElementContentIterator).GetMethod("get_Current");

	public static readonly MethodInfo FollSibCreate = typeof(FollowingSiblingIterator).GetMethod("Create");

	public static readonly MethodInfo FollSibNext = typeof(FollowingSiblingIterator).GetMethod("MoveNext");

	public static readonly MethodInfo FollSibCurrent = typeof(FollowingSiblingIterator).GetMethod("get_Current");

	public static readonly MethodInfo FollSibMergeCreate = typeof(FollowingSiblingMergeIterator).GetMethod("Create");

	public static readonly MethodInfo FollSibMergeNext = typeof(FollowingSiblingMergeIterator).GetMethod("MoveNext");

	public static readonly MethodInfo FollSibMergeCurrent = typeof(FollowingSiblingMergeIterator).GetMethod("get_Current");

	public static readonly MethodInfo IdCreate = typeof(IdIterator).GetMethod("Create");

	public static readonly MethodInfo IdNext = typeof(IdIterator).GetMethod("MoveNext");

	public static readonly MethodInfo IdCurrent = typeof(IdIterator).GetMethod("get_Current");

	public static readonly MethodInfo InterCreate = typeof(IntersectIterator).GetMethod("Create");

	public static readonly MethodInfo InterNext = typeof(IntersectIterator).GetMethod("MoveNext");

	public static readonly MethodInfo InterCurrent = typeof(IntersectIterator).GetMethod("get_Current");

	public static readonly MethodInfo KindContentCreate = typeof(NodeKindContentIterator).GetMethod("Create");

	public static readonly MethodInfo KindContentNext = typeof(NodeKindContentIterator).GetMethod("MoveNext");

	public static readonly MethodInfo KindContentCurrent = typeof(NodeKindContentIterator).GetMethod("get_Current");

	public static readonly MethodInfo NmspCreate = typeof(NamespaceIterator).GetMethod("Create");

	public static readonly MethodInfo NmspNext = typeof(NamespaceIterator).GetMethod("MoveNext");

	public static readonly MethodInfo NmspCurrent = typeof(NamespaceIterator).GetMethod("get_Current");

	public static readonly MethodInfo NodeRangeCreate = typeof(NodeRangeIterator).GetMethod("Create");

	public static readonly MethodInfo NodeRangeNext = typeof(NodeRangeIterator).GetMethod("MoveNext");

	public static readonly MethodInfo NodeRangeCurrent = typeof(NodeRangeIterator).GetMethod("get_Current");

	public static readonly MethodInfo ParentCreate = typeof(ParentIterator).GetMethod("Create");

	public static readonly MethodInfo ParentNext = typeof(ParentIterator).GetMethod("MoveNext");

	public static readonly MethodInfo ParentCurrent = typeof(ParentIterator).GetMethod("get_Current");

	public static readonly MethodInfo PrecCreate = typeof(PrecedingIterator).GetMethod("Create");

	public static readonly MethodInfo PrecNext = typeof(PrecedingIterator).GetMethod("MoveNext");

	public static readonly MethodInfo PrecCurrent = typeof(PrecedingIterator).GetMethod("get_Current");

	public static readonly MethodInfo PreSibCreate = typeof(PrecedingSiblingIterator).GetMethod("Create");

	public static readonly MethodInfo PreSibNext = typeof(PrecedingSiblingIterator).GetMethod("MoveNext");

	public static readonly MethodInfo PreSibCurrent = typeof(PrecedingSiblingIterator).GetMethod("get_Current");

	public static readonly MethodInfo PreSibDOCreate = typeof(PrecedingSiblingDocOrderIterator).GetMethod("Create");

	public static readonly MethodInfo PreSibDONext = typeof(PrecedingSiblingDocOrderIterator).GetMethod("MoveNext");

	public static readonly MethodInfo PreSibDOCurrent = typeof(PrecedingSiblingDocOrderIterator).GetMethod("get_Current");

	public static readonly MethodInfo SortKeyCreate = typeof(XmlSortKeyAccumulator).GetMethod("Create");

	public static readonly MethodInfo SortKeyDateTime = typeof(XmlSortKeyAccumulator).GetMethod("AddDateTimeSortKey");

	public static readonly MethodInfo SortKeyDecimal = typeof(XmlSortKeyAccumulator).GetMethod("AddDecimalSortKey");

	public static readonly MethodInfo SortKeyDouble = typeof(XmlSortKeyAccumulator).GetMethod("AddDoubleSortKey");

	public static readonly MethodInfo SortKeyEmpty = typeof(XmlSortKeyAccumulator).GetMethod("AddEmptySortKey");

	public static readonly MethodInfo SortKeyFinish = typeof(XmlSortKeyAccumulator).GetMethod("FinishSortKeys");

	public static readonly MethodInfo SortKeyInt = typeof(XmlSortKeyAccumulator).GetMethod("AddIntSortKey");

	public static readonly MethodInfo SortKeyInteger = typeof(XmlSortKeyAccumulator).GetMethod("AddIntegerSortKey");

	public static readonly MethodInfo SortKeyKeys = typeof(XmlSortKeyAccumulator).GetMethod("get_Keys");

	public static readonly MethodInfo SortKeyString = typeof(XmlSortKeyAccumulator).GetMethod("AddStringSortKey");

	public static readonly MethodInfo UnionCreate = typeof(UnionIterator).GetMethod("Create");

	public static readonly MethodInfo UnionNext = typeof(UnionIterator).GetMethod("MoveNext");

	public static readonly MethodInfo UnionCurrent = typeof(UnionIterator).GetMethod("get_Current");

	public static readonly MethodInfo XPFollCreate = typeof(XPathFollowingIterator).GetMethod("Create");

	public static readonly MethodInfo XPFollNext = typeof(XPathFollowingIterator).GetMethod("MoveNext");

	public static readonly MethodInfo XPFollCurrent = typeof(XPathFollowingIterator).GetMethod("get_Current");

	public static readonly MethodInfo XPFollMergeCreate = typeof(XPathFollowingMergeIterator).GetMethod("Create");

	public static readonly MethodInfo XPFollMergeNext = typeof(XPathFollowingMergeIterator).GetMethod("MoveNext");

	public static readonly MethodInfo XPFollMergeCurrent = typeof(XPathFollowingMergeIterator).GetMethod("get_Current");

	public static readonly MethodInfo XPPrecCreate = typeof(XPathPrecedingIterator).GetMethod("Create");

	public static readonly MethodInfo XPPrecNext = typeof(XPathPrecedingIterator).GetMethod("MoveNext");

	public static readonly MethodInfo XPPrecCurrent = typeof(XPathPrecedingIterator).GetMethod("get_Current");

	public static readonly MethodInfo XPPrecDOCreate = typeof(XPathPrecedingDocOrderIterator).GetMethod("Create");

	public static readonly MethodInfo XPPrecDONext = typeof(XPathPrecedingDocOrderIterator).GetMethod("MoveNext");

	public static readonly MethodInfo XPPrecDOCurrent = typeof(XPathPrecedingDocOrderIterator).GetMethod("get_Current");

	public static readonly MethodInfo XPPrecMergeCreate = typeof(XPathPrecedingMergeIterator).GetMethod("Create");

	public static readonly MethodInfo XPPrecMergeNext = typeof(XPathPrecedingMergeIterator).GetMethod("MoveNext");

	public static readonly MethodInfo XPPrecMergeCurrent = typeof(XPathPrecedingMergeIterator).GetMethod("get_Current");

	public static readonly MethodInfo AddNewIndex = typeof(XmlQueryRuntime).GetMethod("AddNewIndex");

	public static readonly MethodInfo ChangeTypeXsltArg = typeof(XmlQueryRuntime).GetMethod("ChangeTypeXsltArgument", new Type[3]
	{
		typeof(int),
		typeof(object),
		typeof(Type)
	});

	public static readonly MethodInfo ChangeTypeXsltResult = typeof(XmlQueryRuntime).GetMethod("ChangeTypeXsltResult");

	public static readonly MethodInfo CompPos = typeof(XmlQueryRuntime).GetMethod("ComparePosition");

	public static readonly MethodInfo Context = typeof(XmlQueryRuntime).GetMethod("get_ExternalContext");

	public static readonly MethodInfo CreateCollation = typeof(XmlQueryRuntime).GetMethod("CreateCollation");

	public static readonly MethodInfo DocOrder = typeof(XmlQueryRuntime).GetMethod("DocOrderDistinct");

	public static readonly MethodInfo EndRtfConstr = typeof(XmlQueryRuntime).GetMethod("EndRtfConstruction");

	public static readonly MethodInfo EndSeqConstr = typeof(XmlQueryRuntime).GetMethod("EndSequenceConstruction");

	public static readonly MethodInfo FindIndex = typeof(XmlQueryRuntime).GetMethod("FindIndex");

	public static readonly MethodInfo GenId = typeof(XmlQueryRuntime).GetMethod("GenerateId");

	public static readonly MethodInfo GetAtomizedName = typeof(XmlQueryRuntime).GetMethod("GetAtomizedName");

	public static readonly MethodInfo GetCollation = typeof(XmlQueryRuntime).GetMethod("GetCollation");

	public static readonly MethodInfo GetEarly = typeof(XmlQueryRuntime).GetMethod("GetEarlyBoundObject");

	public static readonly MethodInfo GetNameFilter = typeof(XmlQueryRuntime).GetMethod("GetNameFilter");

	public static readonly MethodInfo GetOutput = typeof(XmlQueryRuntime).GetMethod("get_Output");

	public static readonly MethodInfo GetGlobalValue = typeof(XmlQueryRuntime).GetMethod("GetGlobalValue");

	public static readonly MethodInfo GetTypeFilter = typeof(XmlQueryRuntime).GetMethod("GetTypeFilter");

	public static readonly MethodInfo GlobalComputed = typeof(XmlQueryRuntime).GetMethod("IsGlobalComputed");

	public static readonly MethodInfo ItemMatchesCode = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new Type[2]
	{
		typeof(XPathItem),
		typeof(XmlTypeCode)
	});

	public static readonly MethodInfo ItemMatchesType = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new Type[2]
	{
		typeof(XPathItem),
		typeof(int)
	});

	public static readonly MethodInfo QNameEqualLit = typeof(XmlQueryRuntime).GetMethod("IsQNameEqual", new Type[3]
	{
		typeof(XPathNavigator),
		typeof(int),
		typeof(int)
	});

	public static readonly MethodInfo QNameEqualNav = typeof(XmlQueryRuntime).GetMethod("IsQNameEqual", new Type[2]
	{
		typeof(XPathNavigator),
		typeof(XPathNavigator)
	});

	public static readonly MethodInfo RtfConstr = typeof(XmlQueryRuntime).GetMethod("TextRtfConstruction");

	public static readonly MethodInfo SendMessage = typeof(XmlQueryRuntime).GetMethod("SendMessage");

	public static readonly MethodInfo SeqMatchesCode = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new Type[2]
	{
		typeof(IList<XPathItem>),
		typeof(XmlTypeCode)
	});

	public static readonly MethodInfo SeqMatchesType = typeof(XmlQueryRuntime).GetMethod("MatchesXmlType", new Type[2]
	{
		typeof(IList<XPathItem>),
		typeof(int)
	});

	public static readonly MethodInfo SetGlobalValue = typeof(XmlQueryRuntime).GetMethod("SetGlobalValue");

	public static readonly MethodInfo StartRtfConstr = typeof(XmlQueryRuntime).GetMethod("StartRtfConstruction");

	public static readonly MethodInfo StartSeqConstr = typeof(XmlQueryRuntime).GetMethod("StartSequenceConstruction");

	public static readonly MethodInfo TagAndMappings = typeof(XmlQueryRuntime).GetMethod("ParseTagName", new Type[2]
	{
		typeof(string),
		typeof(int)
	});

	public static readonly MethodInfo TagAndNamespace = typeof(XmlQueryRuntime).GetMethod("ParseTagName", new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo ThrowException = typeof(XmlQueryRuntime).GetMethod("ThrowException");

	public static readonly MethodInfo XsltLib = typeof(XmlQueryRuntime).GetMethod("get_XsltFunctions");

	public static readonly MethodInfo GetDataSource = typeof(XmlQueryContext).GetMethod("GetDataSource");

	public static readonly MethodInfo GetDefaultDataSource = typeof(XmlQueryContext).GetMethod("get_DefaultDataSource");

	public static readonly MethodInfo GetParam = typeof(XmlQueryContext).GetMethod("GetParameter");

	public static readonly MethodInfo InvokeXsltLate = GetInvokeXsltLateBoundFunction();

	public static readonly MethodInfo IndexAdd = typeof(XmlILIndex).GetMethod("Add");

	public static readonly MethodInfo IndexLookup = typeof(XmlILIndex).GetMethod("Lookup");

	public static readonly MethodInfo ItemIsNode = typeof(XPathItem).GetMethod("get_IsNode");

	public static readonly MethodInfo Value = typeof(XPathItem).GetMethod("get_Value");

	public static readonly MethodInfo ValueAsAny = typeof(XPathItem).GetMethod("ValueAs", new Type[2]
	{
		typeof(Type),
		typeof(IXmlNamespaceResolver)
	});

	public static readonly MethodInfo NavClone = typeof(XPathNavigator).GetMethod("Clone");

	public static readonly MethodInfo NavLocalName = typeof(XPathNavigator).GetMethod("get_LocalName");

	public static readonly MethodInfo NavMoveAttr = typeof(XPathNavigator).GetMethod("MoveToAttribute", new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo NavMoveId = typeof(XPathNavigator).GetMethod("MoveToId");

	public static readonly MethodInfo NavMoveParent = typeof(XPathNavigator).GetMethod("MoveToParent");

	public static readonly MethodInfo NavMoveRoot = typeof(XPathNavigator).GetMethod("MoveToRoot");

	public static readonly MethodInfo NavMoveTo = typeof(XPathNavigator).GetMethod("MoveTo");

	public static readonly MethodInfo NavNmsp = typeof(XPathNavigator).GetMethod("get_NamespaceURI");

	public static readonly MethodInfo NavPrefix = typeof(XPathNavigator).GetMethod("get_Prefix");

	public static readonly MethodInfo NavSamePos = typeof(XPathNavigator).GetMethod("IsSamePosition");

	public static readonly MethodInfo NavType = typeof(XPathNavigator).GetMethod("get_NodeType");

	public static readonly MethodInfo StartElemLitName = typeof(XmlQueryOutput).GetMethod("WriteStartElement", new Type[3]
	{
		typeof(string),
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StartElemLocName = typeof(XmlQueryOutput).GetMethod("WriteStartElementLocalName", new Type[1] { typeof(string) });

	public static readonly MethodInfo EndElemStackName = typeof(XmlQueryOutput).GetMethod("WriteEndElement");

	public static readonly MethodInfo StartAttrLitName = typeof(XmlQueryOutput).GetMethod("WriteStartAttribute", new Type[3]
	{
		typeof(string),
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StartAttrLocName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeLocalName", new Type[1] { typeof(string) });

	public static readonly MethodInfo EndAttr = typeof(XmlQueryOutput).GetMethod("WriteEndAttribute");

	public static readonly MethodInfo Text = typeof(XmlQueryOutput).GetMethod("WriteString");

	public static readonly MethodInfo NoEntText = typeof(XmlQueryOutput).GetMethod("WriteRaw", new Type[1] { typeof(string) });

	public static readonly MethodInfo StartTree = typeof(XmlQueryOutput).GetMethod("StartTree");

	public static readonly MethodInfo EndTree = typeof(XmlQueryOutput).GetMethod("EndTree");

	public static readonly MethodInfo StartElemLitNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartElementUnchecked", new Type[3]
	{
		typeof(string),
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StartElemLocNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartElementUnchecked", new Type[1] { typeof(string) });

	public static readonly MethodInfo StartContentUn = typeof(XmlQueryOutput).GetMethod("StartElementContentUnchecked");

	public static readonly MethodInfo EndElemLitNameUn = typeof(XmlQueryOutput).GetMethod("WriteEndElementUnchecked", new Type[3]
	{
		typeof(string),
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo EndElemLocNameUn = typeof(XmlQueryOutput).GetMethod("WriteEndElementUnchecked", new Type[1] { typeof(string) });

	public static readonly MethodInfo StartAttrLitNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeUnchecked", new Type[3]
	{
		typeof(string),
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StartAttrLocNameUn = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeUnchecked", new Type[1] { typeof(string) });

	public static readonly MethodInfo EndAttrUn = typeof(XmlQueryOutput).GetMethod("WriteEndAttributeUnchecked");

	public static readonly MethodInfo NamespaceDeclUn = typeof(XmlQueryOutput).GetMethod("WriteNamespaceDeclarationUnchecked");

	public static readonly MethodInfo TextUn = typeof(XmlQueryOutput).GetMethod("WriteStringUnchecked");

	public static readonly MethodInfo NoEntTextUn = typeof(XmlQueryOutput).GetMethod("WriteRawUnchecked");

	public static readonly MethodInfo StartRoot = typeof(XmlQueryOutput).GetMethod("WriteStartRoot");

	public static readonly MethodInfo EndRoot = typeof(XmlQueryOutput).GetMethod("WriteEndRoot");

	public static readonly MethodInfo StartElemCopyName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new Type[1] { typeof(XPathNavigator) });

	public static readonly MethodInfo StartElemMapName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new Type[2]
	{
		typeof(string),
		typeof(int)
	});

	public static readonly MethodInfo StartElemNmspName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StartElemQName = typeof(XmlQueryOutput).GetMethod("WriteStartElementComputed", new Type[1] { typeof(XmlQualifiedName) });

	public static readonly MethodInfo StartAttrCopyName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new Type[1] { typeof(XPathNavigator) });

	public static readonly MethodInfo StartAttrMapName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new Type[2]
	{
		typeof(string),
		typeof(int)
	});

	public static readonly MethodInfo StartAttrNmspName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StartAttrQName = typeof(XmlQueryOutput).GetMethod("WriteStartAttributeComputed", new Type[1] { typeof(XmlQualifiedName) });

	public static readonly MethodInfo NamespaceDecl = typeof(XmlQueryOutput).GetMethod("WriteNamespaceDeclaration");

	public static readonly MethodInfo StartComment = typeof(XmlQueryOutput).GetMethod("WriteStartComment");

	public static readonly MethodInfo CommentText = typeof(XmlQueryOutput).GetMethod("WriteCommentString");

	public static readonly MethodInfo EndComment = typeof(XmlQueryOutput).GetMethod("WriteEndComment");

	public static readonly MethodInfo StartPI = typeof(XmlQueryOutput).GetMethod("WriteStartProcessingInstruction");

	public static readonly MethodInfo PIText = typeof(XmlQueryOutput).GetMethod("WriteProcessingInstructionString");

	public static readonly MethodInfo EndPI = typeof(XmlQueryOutput).GetMethod("WriteEndProcessingInstruction");

	public static readonly MethodInfo WriteItem = typeof(XmlQueryOutput).GetMethod("WriteItem");

	public static readonly MethodInfo CopyOf = typeof(XmlQueryOutput).GetMethod("XsltCopyOf");

	public static readonly MethodInfo StartCopy = typeof(XmlQueryOutput).GetMethod("StartCopy");

	public static readonly MethodInfo EndCopy = typeof(XmlQueryOutput).GetMethod("EndCopy");

	public static readonly MethodInfo DecAdd = typeof(decimal).GetMethod("Add");

	public static readonly MethodInfo DecCmp = typeof(decimal).GetMethod("Compare", new Type[2]
	{
		typeof(decimal),
		typeof(decimal)
	});

	public static readonly MethodInfo DecEq = typeof(decimal).GetMethod("Equals", new Type[2]
	{
		typeof(decimal),
		typeof(decimal)
	});

	public static readonly MethodInfo DecSub = typeof(decimal).GetMethod("Subtract");

	public static readonly MethodInfo DecMul = typeof(decimal).GetMethod("Multiply");

	public static readonly MethodInfo DecDiv = typeof(decimal).GetMethod("Divide");

	public static readonly MethodInfo DecRem = typeof(decimal).GetMethod("Remainder");

	public static readonly MethodInfo DecNeg = typeof(decimal).GetMethod("Negate");

	public static readonly MethodInfo QNameEq = typeof(XmlQualifiedName).GetMethod("Equals");

	public static readonly MethodInfo StrEq = typeof(string).GetMethod("Equals", new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StrCat2 = typeof(string).GetMethod("Concat", new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StrCat3 = typeof(string).GetMethod("Concat", new Type[3]
	{
		typeof(string),
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StrCat4 = typeof(string).GetMethod("Concat", new Type[4]
	{
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StrCmp = typeof(string).GetMethod("CompareOrdinal", new Type[2]
	{
		typeof(string),
		typeof(string)
	});

	public static readonly MethodInfo StrLen = typeof(string).GetMethod("get_Length");

	public static readonly MethodInfo DblToDec = typeof(XsltConvert).GetMethod("ToDecimal", new Type[1] { typeof(double) });

	public static readonly MethodInfo DblToInt = typeof(XsltConvert).GetMethod("ToInt", new Type[1] { typeof(double) });

	public static readonly MethodInfo DblToLng = typeof(XsltConvert).GetMethod("ToLong", new Type[1] { typeof(double) });

	public static readonly MethodInfo DblToStr = typeof(XsltConvert).GetMethod("ToString", new Type[1] { typeof(double) });

	public static readonly MethodInfo DecToDbl = typeof(XsltConvert).GetMethod("ToDouble", new Type[1] { typeof(decimal) });

	public static readonly MethodInfo DTToStr = typeof(XsltConvert).GetMethod("ToString", new Type[1] { typeof(DateTime) });

	public static readonly MethodInfo IntToDbl = typeof(XsltConvert).GetMethod("ToDouble", new Type[1] { typeof(int) });

	public static readonly MethodInfo LngToDbl = typeof(XsltConvert).GetMethod("ToDouble", new Type[1] { typeof(long) });

	public static readonly MethodInfo StrToDbl = typeof(XsltConvert).GetMethod("ToDouble", new Type[1] { typeof(string) });

	public static readonly MethodInfo StrToDT = typeof(XsltConvert).GetMethod("ToDateTime", new Type[1] { typeof(string) });

	public static readonly MethodInfo ItemToBool = typeof(XsltConvert).GetMethod("ToBoolean", new Type[1] { typeof(XPathItem) });

	public static readonly MethodInfo ItemToDbl = typeof(XsltConvert).GetMethod("ToDouble", new Type[1] { typeof(XPathItem) });

	public static readonly MethodInfo ItemToStr = typeof(XsltConvert).GetMethod("ToString", new Type[1] { typeof(XPathItem) });

	public static readonly MethodInfo ItemToNode = typeof(XsltConvert).GetMethod("ToNode", new Type[1] { typeof(XPathItem) });

	public static readonly MethodInfo ItemToNodes = typeof(XsltConvert).GetMethod("ToNodeSet", new Type[1] { typeof(XPathItem) });

	public static readonly MethodInfo ItemsToBool = typeof(XsltConvert).GetMethod("ToBoolean", new Type[1] { typeof(IList<XPathItem>) });

	public static readonly MethodInfo ItemsToDbl = typeof(XsltConvert).GetMethod("ToDouble", new Type[1] { typeof(IList<XPathItem>) });

	public static readonly MethodInfo ItemsToNode = typeof(XsltConvert).GetMethod("ToNode", new Type[1] { typeof(IList<XPathItem>) });

	public static readonly MethodInfo ItemsToNodes = typeof(XsltConvert).GetMethod("ToNodeSet", new Type[1] { typeof(IList<XPathItem>) });

	public static readonly MethodInfo ItemsToStr = typeof(XsltConvert).GetMethod("ToString", new Type[1] { typeof(IList<XPathItem>) });

	public static readonly MethodInfo StrCatCat = typeof(StringConcat).GetMethod("Concat");

	public static readonly MethodInfo StrCatClear = typeof(StringConcat).GetMethod("Clear");

	public static readonly MethodInfo StrCatResult = typeof(StringConcat).GetMethod("GetResult");

	public static readonly MethodInfo StrCatDelim = typeof(StringConcat).GetMethod("set_Delimiter");

	public static readonly MethodInfo NavsToItems = typeof(XmlILStorageConverter).GetMethod("NavigatorsToItems");

	public static readonly MethodInfo ItemsToNavs = typeof(XmlILStorageConverter).GetMethod("ItemsToNavigators");

	public static readonly MethodInfo SetDod = typeof(XmlQueryNodeSequence).GetMethod("set_IsDocOrderDistinct");

	public static readonly MethodInfo GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

	public static readonly MethodInfo InitializeArray = typeof(RuntimeHelpers).GetMethod("InitializeArray");

	public static readonly Dictionary<Type, XmlILStorageMethods> StorageMethods = new Dictionary<Type, XmlILStorageMethods>(13)
	{
		{
			typeof(string),
			new XmlILStorageMethods(typeof(string))
		},
		{
			typeof(bool),
			new XmlILStorageMethods(typeof(bool))
		},
		{
			typeof(int),
			new XmlILStorageMethods(typeof(int))
		},
		{
			typeof(long),
			new XmlILStorageMethods(typeof(long))
		},
		{
			typeof(decimal),
			new XmlILStorageMethods(typeof(decimal))
		},
		{
			typeof(double),
			new XmlILStorageMethods(typeof(double))
		},
		{
			typeof(float),
			new XmlILStorageMethods(typeof(float))
		},
		{
			typeof(DateTime),
			new XmlILStorageMethods(typeof(DateTime))
		},
		{
			typeof(byte[]),
			new XmlILStorageMethods(typeof(byte[]))
		},
		{
			typeof(XmlQualifiedName),
			new XmlILStorageMethods(typeof(XmlQualifiedName))
		},
		{
			typeof(TimeSpan),
			new XmlILStorageMethods(typeof(TimeSpan))
		},
		{
			typeof(XPathItem),
			new XmlILStorageMethods(typeof(XPathItem))
		},
		{
			typeof(XPathNavigator),
			new XmlILStorageMethods(typeof(XPathNavigator))
		}
	};

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Supressing warning about not having the RequiresUnreferencedCode attribute since this code path will only be emitting IL that will later be called by Transform() method which is already annotated as RequiresUnreferencedCode")]
	private static MethodInfo GetInvokeXsltLateBoundFunction()
	{
		return typeof(XmlQueryContext).GetMethod("InvokeXsltLateBoundFunction");
	}
}
