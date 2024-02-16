namespace System.Data;

internal enum RBTreeError
{
	InvalidPageSize = 1,
	PagePositionInSlotInUse = 3,
	NoFreeSlots = 4,
	InvalidStateinInsert = 5,
	InvalidNextSizeInDelete = 7,
	InvalidStateinDelete = 8,
	InvalidNodeSizeinDelete = 9,
	InvalidStateinEndDelete = 10,
	CannotRotateInvalidsuccessorNodeinDelete = 11,
	IndexOutOFRangeinGetNodeByIndex = 13,
	RBDeleteFixup = 14,
	UnsupportedAccessMethod1 = 15,
	UnsupportedAccessMethod2 = 16,
	UnsupportedAccessMethodInNonNillRootSubtree = 17,
	AttachedNodeWithZerorbTreeNodeId = 18,
	CompareNodeInDataRowTree = 19,
	CompareSateliteTreeNodeInDataRowTree = 20,
	NestedSatelliteTreeEnumerator = 21
}
