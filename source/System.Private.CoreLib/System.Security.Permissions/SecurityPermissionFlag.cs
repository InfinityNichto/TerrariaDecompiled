namespace System.Security.Permissions;

[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[Flags]
public enum SecurityPermissionFlag
{
	AllFlags = 0x3FFF,
	Assertion = 1,
	BindingRedirects = 0x2000,
	ControlAppDomain = 0x400,
	ControlDomainPolicy = 0x100,
	ControlEvidence = 0x20,
	ControlPolicy = 0x40,
	ControlPrincipal = 0x200,
	ControlThread = 0x10,
	Execution = 8,
	Infrastructure = 0x1000,
	NoFlags = 0,
	RemotingConfiguration = 0x800,
	SerializationFormatter = 0x80,
	SkipVerification = 4,
	UnmanagedCode = 2
}
