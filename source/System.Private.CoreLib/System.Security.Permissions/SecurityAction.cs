namespace System.Security.Permissions;

[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public enum SecurityAction
{
	Assert = 3,
	Demand = 2,
	Deny = 4,
	InheritanceDemand = 7,
	LinkDemand = 6,
	PermitOnly = 5,
	RequestMinimum = 8,
	RequestOptional = 9,
	RequestRefuse = 10
}
