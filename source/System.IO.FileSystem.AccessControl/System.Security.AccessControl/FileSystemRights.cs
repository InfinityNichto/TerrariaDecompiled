namespace System.Security.AccessControl;

[Flags]
public enum FileSystemRights
{
	ReadData = 1,
	ListDirectory = 1,
	WriteData = 2,
	CreateFiles = 2,
	AppendData = 4,
	CreateDirectories = 4,
	ReadExtendedAttributes = 8,
	WriteExtendedAttributes = 0x10,
	ExecuteFile = 0x20,
	Traverse = 0x20,
	DeleteSubdirectoriesAndFiles = 0x40,
	ReadAttributes = 0x80,
	WriteAttributes = 0x100,
	Delete = 0x10000,
	ReadPermissions = 0x20000,
	ChangePermissions = 0x40000,
	TakeOwnership = 0x80000,
	Synchronize = 0x100000,
	FullControl = 0x1F01FF,
	Read = 0x20089,
	ReadAndExecute = 0x200A9,
	Write = 0x116,
	Modify = 0x301BF
}
