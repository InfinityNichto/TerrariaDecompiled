namespace System.Security.Permissions;

[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class SecurityPermissionAttribute : CodeAccessSecurityAttribute
{
	public bool Assertion { get; set; }

	public bool BindingRedirects { get; set; }

	public bool ControlAppDomain { get; set; }

	public bool ControlDomainPolicy { get; set; }

	public bool ControlEvidence { get; set; }

	public bool ControlPolicy { get; set; }

	public bool ControlPrincipal { get; set; }

	public bool ControlThread { get; set; }

	public bool Execution { get; set; }

	public SecurityPermissionFlag Flags { get; set; }

	public bool Infrastructure { get; set; }

	public bool RemotingConfiguration { get; set; }

	public bool SerializationFormatter { get; set; }

	public bool SkipVerification { get; set; }

	public bool UnmanagedCode { get; set; }

	public SecurityPermissionAttribute(SecurityAction action)
		: base((SecurityAction)0)
	{
	}

	public override IPermission? CreatePermission()
	{
		return null;
	}
}
