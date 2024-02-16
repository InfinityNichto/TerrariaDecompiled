namespace System.Xml;

internal enum ElementProperties : uint
{
	DEFAULT = 0u,
	URI_PARENT = 1u,
	BOOL_PARENT = 2u,
	NAME_PARENT = 4u,
	EMPTY = 8u,
	NO_ENTITIES = 0x10u,
	HEAD = 0x20u,
	BLOCK_WS = 0x40u,
	HAS_NS = 0x80u
}
