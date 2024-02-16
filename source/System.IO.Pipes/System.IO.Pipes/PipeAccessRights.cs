namespace System.IO.Pipes;

[Flags]
public enum PipeAccessRights
{
	ReadData = 1,
	WriteData = 2,
	ReadAttributes = 0x80,
	WriteAttributes = 0x100,
	ReadExtendedAttributes = 8,
	WriteExtendedAttributes = 0x10,
	CreateNewInstance = 4,
	Delete = 0x10000,
	ReadPermissions = 0x20000,
	ChangePermissions = 0x40000,
	TakeOwnership = 0x80000,
	Synchronize = 0x100000,
	FullControl = 0x1F019F,
	Read = 0x20089,
	Write = 0x112,
	ReadWrite = 0x2019B,
	AccessSystemSecurity = 0x1000000
}
