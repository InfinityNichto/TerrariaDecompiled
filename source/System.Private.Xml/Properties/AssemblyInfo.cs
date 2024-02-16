using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Target = "M:System.Xml.Serialization.ReflectionXmlSerializationReader.#cctor", Scope = "member", Justification = "The reason why this warns is because the two static properties call GetTypeDesc() which internally will call ImportTypeDesc() when the passed in type is not considered a primitive type. That said, for both properties here we are passing in string and XmlQualifiedName which are considered primitive, so they are trim safe.")]
[assembly: CLSCompliant(true)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.AssemblyDirectory)]
[assembly: AssemblyDefaultAlias("System.Private.Xml")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyDescription("Internal implementation package not meant for direct consumption. Please do not reference directly.")]
[assembly: AssemblyFileVersion("6.0.21.52210")]
[assembly: AssemblyInformationalVersion("6.0.0-rtm.21522.10+4822e3c3aa77eb82b2fb33c9321f923cf11ddde6")]
[assembly: AssemblyProduct("Microsoft® .NET")]
[assembly: AssemblyTitle("System.Private.Xml")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/dotnet/runtime")]
[assembly: TargetPlatform("Windows7.0")]
[assembly: SupportedOSPlatform("Windows")]
[assembly: AssemblyVersion("6.0.0.0")]
[module: System.Runtime.CompilerServices.NullablePublicOnly(false)]
[module: SkipLocalsInit]
